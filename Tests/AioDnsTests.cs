#nullable enable
using System;
using System.Buffers.Binary;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Netch.Servers;
using Netch.Services;
using Netch.Utils;

namespace Tests;

[TestClass]
public class AioDnsTests
{
    private sealed class StubDohHandler(Func<byte[], HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert.AreEqual(HttpMethod.Post, request.Method);
            Assert.AreEqual("application/dns-message", request.Content?.Headers.ContentType?.MediaType);
            var body = await request.Content!.ReadAsByteArrayAsync(cancellationToken);
            return responder(body);
        }
    }

    private static byte[] BuildDnsQuery(ushort txId, string domain, ushort qtype = 1)
    {
        using var ms = new MemoryStream();
        Span<byte> header = stackalloc byte[12];
        BinaryPrimitives.WriteUInt16BigEndian(header[..2], txId);
        header[2] = 0x01; // RD = 1
        header[3] = 0x00;
        BinaryPrimitives.WriteUInt16BigEndian(header[4..6], 1); // QDCOUNT = 1
        ms.Write(header);

        foreach (var label in domain.TrimEnd('.').Split('.', StringSplitOptions.RemoveEmptyEntries))
        {
            var bytes = Encoding.ASCII.GetBytes(label);
            ms.WriteByte((byte)bytes.Length);
            ms.Write(bytes);
        }
        ms.WriteByte(0);

        Span<byte> tail = stackalloc byte[4];
        BinaryPrimitives.WriteUInt16BigEndian(tail[..2], qtype);
        BinaryPrimitives.WriteUInt16BigEndian(tail[2..4], 1); // IN
        ms.Write(tail);
        return ms.ToArray();
    }

    private static byte[] BuildSimpleAResponse(byte[] query, byte[] ipv4, bool truncated = false)
    {
        if (truncated)
        {
            var tc = (byte[])query.Clone();
            tc[2] = (byte)((query[2] & 0x79) | 0x80 | 0x02);
            tc[3] = 0x80;
            return tc;
        }

        var resp = new byte[query.Length + 16];
        query.CopyTo(resp, 0);
        resp[2] = (byte)((query[2] & 0x79) | 0x80);
        resp[3] = 0x80;
        BinaryPrimitives.WriteUInt16BigEndian(resp.AsSpan(6, 2), 1); // ANCOUNT = 1
        var p = query.Length;
        resp[p++] = 0xC0;
        resp[p++] = 0x0C;
        BinaryPrimitives.WriteUInt16BigEndian(resp.AsSpan(p, 2), 1); // A
        p += 2;
        BinaryPrimitives.WriteUInt16BigEndian(resp.AsSpan(p, 2), 1); // IN
        p += 2;
        BinaryPrimitives.WriteUInt32BigEndian(resp.AsSpan(p, 4), 60);
        p += 4;
        BinaryPrimitives.WriteUInt16BigEndian(resp.AsSpan(p, 2), 4);
        p += 2;
        ipv4.CopyTo(resp, p);
        return resp;
    }

    [TestMethod]
    public void DomainRuleMatcherMatchesExactSubdomainAndInAddrArpaCorrectly()
    {
        var matcher = new DomainRuleMatcher(["# comment", "baidu.com.", "qq.com"]);

        Assert.AreEqual(2, matcher.Count);
        Assert.IsTrue(matcher.IsChinaDomain("baidu.com"));
        Assert.IsTrue(matcher.IsChinaDomain("www.baidu.com."));
        Assert.IsTrue(matcher.IsChinaDomain("api.map.baidu.com"));
        Assert.IsFalse(matcher.IsChinaDomain("notbaidu.com"));
        Assert.IsFalse(matcher.IsChinaDomain("google.com"));

        Assert.IsTrue(DomainRuleMatcher.IsInAddrArpa("1.0.0.127.in-addr.arpa."));
        Assert.IsTrue(DomainRuleMatcher.IsInAddrArpa("IN-ADDR.ARPA"));
        Assert.IsFalse(DomainRuleMatcher.IsInAddrArpa("example.in-addr.arpa.com"));
    }

    [TestMethod]
    public void DnsWireMessageParsesQuestionsAndBuildsPtrAndServFail()
    {
        var query = BuildDnsQuery(0x1234, "sub.example.com", 28);
        Assert.IsTrue(DnsWireMessage.TryParseQuestion(query, out var qname, out var qtype, out var qEnd));
        Assert.AreEqual("sub.example.com", qname);
        Assert.AreEqual((ushort)28, qtype);
        Assert.AreEqual(query.Length, qEnd);

        var ptrQuery = BuildDnsQuery(0xABCD, "1.0.0.127.in-addr.arpa", 12);
        var ptrReply = DnsWireMessage.BuildNetchPtrReply(ptrQuery);
        Assert.AreEqual(0xABCD, BinaryPrimitives.ReadUInt16BigEndian(ptrReply.AsSpan(0, 2)));
        Assert.AreEqual(1, BinaryPrimitives.ReadUInt16BigEndian(ptrReply.AsSpan(6, 2)));
        StringAssert.Contains(Encoding.ASCII.GetString(ptrReply), "Netch");

        var fail = DnsWireMessage.BuildServFail(query);
        Assert.AreEqual(0x1234, BinaryPrimitives.ReadUInt16BigEndian(fail.AsSpan(0, 2)));
        Assert.AreEqual(2, fail[3] & 0x0F);
    }

    [TestMethod]
    public async Task AioDnsServerRoutesChinaUdpWithTcpFallbackAndOtherDohEndToEnd()
    {
        var tempDir = Directory.CreateTempSubdirectory("netch-aiodns-test-");
        try
        {
            var rulesPath = Path.Combine(tempDir.FullName, "aiodns.conf");
            var logPath = Path.Combine(tempDir.FullName, "AioDNS.log");
            await File.WriteAllTextAsync(rulesPath, "cn-test.example.\n");

            // Mock China upstream listening on same loopback port for both UDP (returns TC=1) and TCP (returns full answer).
            using var chinaUdp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            chinaUdp.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            var chinaPort = (ushort)((IPEndPoint)chinaUdp.LocalEndPoint!).Port;

            using var chinaTcp = new TcpListener(IPAddress.Loopback, chinaPort);
            chinaTcp.Start();

            using var serverCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var udpCalls = 0;
            var tcpFallbackCalls = 0;
            var dohSawZeroTxId = false;

            var chinaUdpTask = Task.Run(async () =>
            {
                var buf = new byte[512];
                var res = await chinaUdp.ReceiveFromAsync(buf, SocketFlags.None, new IPEndPoint(IPAddress.Any, 0), serverCts.Token);
                Interlocked.Increment(ref udpCalls);
                var req = buf.AsSpan(0, res.ReceivedBytes).ToArray();
                var tcResp = BuildSimpleAResponse(req, [11, 22, 33, 44], truncated: true);
                await chinaUdp.SendToAsync(tcResp, SocketFlags.None, res.RemoteEndPoint, serverCts.Token);
            }, serverCts.Token);

            var chinaTcpTask = Task.Run(async () =>
            {
                using var client = await chinaTcp.AcceptTcpClientAsync(serverCts.Token);
                await using var stream = client.GetStream();
                var lenBuf = new byte[2];
                await stream.ReadExactlyAsync(lenBuf, serverCts.Token);
                var len = BinaryPrimitives.ReadUInt16BigEndian(lenBuf);
                var req = new byte[len];
                await stream.ReadExactlyAsync(req, serverCts.Token);
                Interlocked.Increment(ref tcpFallbackCalls);
                var fullResp = BuildSimpleAResponse(req, [11, 22, 33, 44], truncated: false);
                var frame = new byte[2 + fullResp.Length];
                BinaryPrimitives.WriteUInt16BigEndian(frame.AsSpan(0, 2), (ushort)fullResp.Length);
                fullResp.CopyTo(frame, 2);
                await stream.WriteAsync(frame, serverCts.Token);
            }, serverCts.Token);

            Assert.IsTrue(DnsEndpoint.TryParse($"udp://127.0.0.1:{chinaPort}", out var chinaEndpoint));
            Assert.IsTrue(DnsEndpoint.TryParse("https://dns.example.org/dns-query", out var otherEndpoint));

            await using var aioDns = new AioDnsServer(new AioDnsOptions
            {
                ListenAddress = IPAddress.Loopback,
                ListenPort = 0,
                RuleFilePath = rulesPath,
                LogFilePath = logPath,
                ChinaEndpoint = chinaEndpoint,
                OtherEndpoint = otherEndpoint,
                DohHandlerFactory = (_, _, _) => new StubDohHandler(wireReq =>
                {
                    dohSawZeroTxId = BinaryPrimitives.ReadUInt16BigEndian(wireReq.AsSpan(0, 2)) == 0;
                    var wireResp = BuildSimpleAResponse(wireReq, [99, 88, 77, 66]);
                    var msg = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new ByteArrayContent(wireResp)
                    };
                    msg.Content.Headers.ContentType = new MediaTypeHeaderValue("application/dns-message");
                    return msg;
                })
            });

            await aioDns.StartAsync(serverCts.Token);
            Assert.AreNotEqual((ushort)0, aioDns.BoundUdpPort);
            Assert.AreNotEqual((ushort)0, aioDns.BoundTcpPort);

            // 1. China domain over local UDP triggers upstream UDP + TC->TCP fallback.
            using var clientUdp = new UdpClient();
            var cnQuery = BuildDnsQuery(0x2244, "sub.cn-test.example");
            await clientUdp.SendAsync(cnQuery, cnQuery.Length, "127.0.0.1", aioDns.BoundUdpPort);
            var cnRecv = await clientUdp.ReceiveAsync(serverCts.Token);
            Assert.AreEqual(0x2244, BinaryPrimitives.ReadUInt16BigEndian(cnRecv.Buffer.AsSpan(0, 2)));
            Assert.AreEqual(1, DnsWireMessage.ReadAnswerCount(cnRecv.Buffer));
            CollectionAssert.AreEqual(new byte[] { 11, 22, 33, 44 }, cnRecv.Buffer[^4..]);

            // 2. Foreign domain over local UDP routes via DoH with TxID=0 on wire and restores client TxID.
            var intlQuery = BuildDnsQuery(0x5566, "foreign.example.com");
            await clientUdp.SendAsync(intlQuery, intlQuery.Length, "127.0.0.1", aioDns.BoundUdpPort);
            var intlRecv = await clientUdp.ReceiveAsync(serverCts.Token);
            Assert.IsTrue(dohSawZeroTxId);
            Assert.AreEqual(0x5566, BinaryPrimitives.ReadUInt16BigEndian(intlRecv.Buffer.AsSpan(0, 2)));
            CollectionAssert.AreEqual(new byte[] { 99, 88, 77, 66 }, intlRecv.Buffer[^4..]);

            // 3. PTR in-addr.arpa over local TCP listener returns Netch PTR locally.
            using var clientTcp = new TcpClient();
            await clientTcp.ConnectAsync(IPAddress.Loopback, aioDns.BoundTcpPort, serverCts.Token);
            await using (var tcpStream = clientTcp.GetStream())
            {
                var ptrQuery = BuildDnsQuery(0x7788, "1.0.0.127.in-addr.arpa", 12);
                var frame = new byte[2 + ptrQuery.Length];
                BinaryPrimitives.WriteUInt16BigEndian(frame.AsSpan(0, 2), (ushort)ptrQuery.Length);
                ptrQuery.CopyTo(frame, 2);
                await tcpStream.WriteAsync(frame, serverCts.Token);

                var respLenBuf = new byte[2];
                await tcpStream.ReadExactlyAsync(respLenBuf, serverCts.Token);
                var respBuf = new byte[BinaryPrimitives.ReadUInt16BigEndian(respLenBuf)];
                await tcpStream.ReadExactlyAsync(respBuf, serverCts.Token);
                Assert.AreEqual(0x7788, BinaryPrimitives.ReadUInt16BigEndian(respBuf.AsSpan(0, 2)));
                StringAssert.Contains(Encoding.ASCII.GetString(respBuf), "Netch");
            }

            await Task.WhenAll(chinaUdpTask, chinaTcpTask);
            Assert.AreEqual(1, udpCalls);
            Assert.AreEqual(1, tcpFallbackCalls);

            await aioDns.StopAsync();
            Assert.IsTrue(File.Exists(logPath));
            var logText = await File.ReadAllTextAsync(logPath);
            StringAssert.Contains(logText, "ChinaDNS");
            StringAssert.Contains(logText, "OtherDNS");
        }
        finally
        {
            tempDir.Delete(recursive: true);
        }
    }

    [TestMethod]
    public async Task ConnectViaSocks5HandlesBracketedIpv6TargetAndProxyAddresses()
    {
        var targetIpv6 = IPAddress.Parse("2606:4700:4700::1111");
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        async Task AssertConnectRequestAsync(IPAddress bindAddress, string proxyAddressText)
        {
            using var listener = new TcpListener(bindAddress, 0);
            listener.Start();
            var port = (ushort)((IPEndPoint)listener.LocalEndpoint).Port;

            byte[]? capturedConnect = null;
            var serverTask = Task.Run(async () =>
            {
                using var client = await listener.AcceptTcpClientAsync(cts.Token);
                await using var stream = client.GetStream();

                var greeting = new byte[3];
                await stream.ReadExactlyAsync(greeting, cts.Token);
                CollectionAssert.AreEqual(new byte[] { 0x05, 0x01, 0x00 }, greeting);
                await stream.WriteAsync(new byte[] { 0x05, 0x00 }, cts.Token);

                // VER(1) + CMD(1) + RSV(1) + ATYP(1) + IPv6(16) + PORT(2) = 22 bytes.
                var req = new byte[22];
                await stream.ReadExactlyAsync(req, cts.Token);
                capturedConnect = req;

                await stream.WriteAsync(new byte[] { 0x05, 0x00, 0x00, 0x01, 127, 0, 0, 1, 0, 0 }, cts.Token);
            }, cts.Token);

            var proxy = new SocksServer(proxyAddressText, port);
            await using var tunnel = await AioDnsServer.ConnectViaSocks5Async(proxy, "[2606:4700:4700::1111]", 443, cts.Token);
            await serverTask;

            Assert.IsNotNull(capturedConnect);
            Assert.AreEqual((byte)0x05, capturedConnect[0]);
            Assert.AreEqual((byte)0x01, capturedConnect[1]);
            Assert.AreEqual((byte)0x04, capturedConnect[3]);
            CollectionAssert.AreEqual(targetIpv6.GetAddressBytes(), capturedConnect[4..20]);
            Assert.AreEqual((ushort)443, BinaryPrimitives.ReadUInt16BigEndian(capturedConnect.AsSpan(20, 2)));
        }

        await AssertConnectRequestAsync(IPAddress.Loopback, "[127.0.0.1]");
        await AssertConnectRequestAsync(IPAddress.IPv6Loopback, "[::1]");
    }
}
