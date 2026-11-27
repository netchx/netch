using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Netch.Utils;

namespace Tests;

[TestClass]
public class NatDiscovery
{
    [TestMethod]
    [DataRow(BindingTestResult.Success, MappingBehavior.EndpointIndependent, FilteringBehavior.EndpointIndependent, "1")]
    [DataRow(BindingTestResult.Success, MappingBehavior.EndpointIndependent, FilteringBehavior.AddressAndPortDependent, "2")]
    [DataRow(BindingTestResult.Success, MappingBehavior.AddressAndPortDependent, FilteringBehavior.AddressDependent, "3")]
    [DataRow(BindingTestResult.Success, MappingBehavior.EndpointIndependent, FilteringBehavior.UnsupportedServer, "UnsupportedServer")]
    [DataRow(BindingTestResult.Success, MappingBehavior.EndpointIndependent, FilteringBehavior.Unknown, "Incomplete")]
    [DataRow(BindingTestResult.Success, MappingBehavior.Fail, FilteringBehavior.EndpointIndependent, "Incomplete")]
    [DataRow(BindingTestResult.Fail, MappingBehavior.Unknown, FilteringBehavior.Unknown, "NoUDP")]
    public void OnlyCompletedProbesProduceANumericType(BindingTestResult binding, MappingBehavior mapping, FilteringBehavior filtering, string expected)
    {
        var method = typeof(Socks5ServerTestUtils).GetMethod("GetSimpleResult", BindingFlags.NonPublic | BindingFlags.Static);
        var state = new StunResult5389 { BindingTestResult = binding, MappingBehavior = mapping, FilteringBehavior = filtering };
        Assert.AreEqual(expected, method.Invoke(null, [state]));
    }

    [TestMethod]
    [DataRow("stun.fitauto.ru", "stun.fitauto.ru,stun.m-online.net")]
    [DataRow("stun.syncthing.net", "stun.syncthing.net,stun.fitauto.ru,stun.m-online.net")]
    [DataRow("stun.example.test:5349", "stun.example.test:5349")]
    public void PresetsCanFallBackButCustomServersAreTestedAlone(string address, string expected)
    {
        Assert.IsTrue(StunServer.TryParse(address, out var selected));
        Assert.IsTrue(StunServer.TryParse("stun.fitauto.ru", out var first));
        Assert.IsTrue(StunServer.TryParse("stun.m-online.net", out var second));
        var method = typeof(Socks5ServerTestUtils).GetMethod("GetCandidates", BindingFlags.NonPublic | BindingFlags.Static);
        var candidates = (IReadOnlyList<StunServer>)method.Invoke(null, [selected, new[] { first, second }]);
        Assert.AreEqual(expected, string.Join(",", candidates.Select(server => server.ToString())));
    }

    [TestMethod]
    public void StunProtocolBuildsAndParsesIpv4AndIpv6BindingPackets()
    {
        byte[] txId = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12];
        var req = StunProtocol.BuildBindingRequest(txId, changeIp: true, changePort: true);
        Assert.AreEqual(28, req.Length);
        Assert.AreEqual(0x0001, BinaryPrimitives.ReadUInt16BigEndian(req.AsSpan(0, 2)));
        Assert.AreEqual(8, BinaryPrimitives.ReadUInt16BigEndian(req.AsSpan(2, 2)));
        Assert.AreEqual(StunProtocol.MagicCookie, BinaryPrimitives.ReadUInt32BigEndian(req.AsSpan(4, 4)));
        Assert.AreEqual(0x0003, BinaryPrimitives.ReadUInt16BigEndian(req.AsSpan(20, 2)));
        Assert.AreEqual(0x00000006u, BinaryPrimitives.ReadUInt32BigEndian(req.AsSpan(24, 4)));

        // IPv4 XOR-MAPPED-ADDRESS (203.0.113.5:54321) + OTHER-ADDRESS (198.51.100.2:3479) + RESPONSE-ORIGIN (198.51.100.2:3479)
        var v4Ip = IPAddress.Parse("203.0.113.5").GetAddressBytes();
        var otherIp = IPAddress.Parse("198.51.100.2").GetAddressBytes();
        byte[] xorMask = [0x21, 0x12, 0xA4, 0x42, .. txId];
        var respV4 = new byte[20 + 12 + 12 + 12];
        BinaryPrimitives.WriteUInt16BigEndian(respV4.AsSpan(0, 2), 0x0101);
        BinaryPrimitives.WriteUInt16BigEndian(respV4.AsSpan(2, 2), 36);
        BinaryPrimitives.WriteUInt32BigEndian(respV4.AsSpan(4, 4), StunProtocol.MagicCookie);
        txId.CopyTo(respV4, 8);

        BinaryPrimitives.WriteUInt16BigEndian(respV4.AsSpan(20, 2), 0x0020);
        BinaryPrimitives.WriteUInt16BigEndian(respV4.AsSpan(22, 2), 8);
        respV4[25] = 0x01;
        BinaryPrimitives.WriteUInt16BigEndian(respV4.AsSpan(26, 2), (ushort)(54321 ^ 0x2112));
        for (var i = 0; i < 4; i++)
            respV4[28 + i] = (byte)(v4Ip[i] ^ xorMask[i]);

        BinaryPrimitives.WriteUInt16BigEndian(respV4.AsSpan(32, 2), 0x802C);
        BinaryPrimitives.WriteUInt16BigEndian(respV4.AsSpan(34, 2), 8);
        respV4[37] = 0x01;
        BinaryPrimitives.WriteUInt16BigEndian(respV4.AsSpan(38, 2), 3479);
        otherIp.CopyTo(respV4, 40);

        BinaryPrimitives.WriteUInt16BigEndian(respV4.AsSpan(44, 2), 0x802B);
        BinaryPrimitives.WriteUInt16BigEndian(respV4.AsSpan(46, 2), 8);
        respV4[49] = 0x01;
        BinaryPrimitives.WriteUInt16BigEndian(respV4.AsSpan(50, 2), 3479);
        otherIp.CopyTo(respV4, 52);

        Assert.IsTrue(StunProtocol.TryParseBindingResponse(respV4, txId, out var mappedV4, out var otherV4, out var originV4));
        Assert.AreEqual(new IPEndPoint(IPAddress.Parse("203.0.113.5"), 54321), mappedV4);
        Assert.AreEqual(new IPEndPoint(IPAddress.Parse("198.51.100.2"), 3479), otherV4);
        Assert.AreEqual(new IPEndPoint(IPAddress.Parse("198.51.100.2"), 3479), originV4);

        // Mismatched transaction ID must be rejected.
        byte[] wrongTxId = [9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9];
        Assert.IsFalse(StunProtocol.TryParseBindingResponse(respV4, wrongTxId, out _, out _, out _));

        // IPv6 XOR-MAPPED-ADDRESS ([2001:db8::1]:40000)
        var v6Bytes = IPAddress.Parse("2001:db8::1").GetAddressBytes();
        var respV6 = new byte[20 + 24];
        BinaryPrimitives.WriteUInt16BigEndian(respV6.AsSpan(0, 2), 0x0101);
        BinaryPrimitives.WriteUInt16BigEndian(respV6.AsSpan(2, 2), 24);
        BinaryPrimitives.WriteUInt32BigEndian(respV6.AsSpan(4, 4), StunProtocol.MagicCookie);
        txId.CopyTo(respV6, 8);
        BinaryPrimitives.WriteUInt16BigEndian(respV6.AsSpan(20, 2), 0x0020);
        BinaryPrimitives.WriteUInt16BigEndian(respV6.AsSpan(22, 2), 20);
        respV6[25] = 0x02;
        BinaryPrimitives.WriteUInt16BigEndian(respV6.AsSpan(26, 2), (ushort)(40000 ^ 0x2112));
        for (var i = 0; i < 16; i++)
            respV6[28 + i] = (byte)(v6Bytes[i] ^ xorMask[i]);

        Assert.IsTrue(StunProtocol.TryParseBindingResponse(respV6, txId, out var mappedV6, out var otherV6, out var originV6));
        Assert.AreEqual(new IPEndPoint(IPAddress.Parse("2001:db8::1"), 40000), mappedV6);
        Assert.IsNull(otherV6);
        Assert.IsNull(originV6);
    }

    [TestMethod]
    public void StunProtocolWrapsAndUnwrapsSocks5UdpDatagrams()
    {
        byte[] payload = [0x00, 0x01, 0xAA, 0xBB];

        var epV4 = new IPEndPoint(IPAddress.Parse("198.51.100.10"), 3478);
        var wrappedV4 = StunProtocol.WrapSocks5UdpPacket(epV4, payload);
        Assert.IsTrue(StunProtocol.TryUnwrapSocks5UdpPacket(wrappedV4, out var srcV4, out var outV4));
        Assert.AreEqual(epV4, srcV4);
        CollectionAssert.AreEqual(payload, outV4.ToArray());

        var epV6 = new IPEndPoint(IPAddress.Parse("2001:db8::42"), 3479);
        var wrappedV6 = StunProtocol.WrapSocks5UdpPacket(epV6, payload);
        Assert.IsTrue(StunProtocol.TryUnwrapSocks5UdpPacket(wrappedV6, out var srcV6, out var outV6));
        Assert.AreEqual(epV6, srcV6);
        CollectionAssert.AreEqual(payload, outV6.ToArray());

        // ATYP=0x03 domain header
        byte[] domainPacket = [0x00, 0x00, 0x00, 0x03, 0x04, (byte)'t', (byte)'e', (byte)'s', (byte)'t', 0x0D, 0x96, .. payload];
        Assert.IsTrue(StunProtocol.TryUnwrapSocks5UdpPacket(domainPacket, out var srcDomain, out var outDomain));
        Assert.IsNull(srcDomain);
        CollectionAssert.AreEqual(payload, outDomain.ToArray());

        // Non-zero FRAG must be rejected
        wrappedV4[2] = 0x01;
        Assert.IsFalse(StunProtocol.TryUnwrapSocks5UdpPacket(wrappedV4, out _, out _));
    }

    [TestMethod]
    public async Task StunClient5389UdpCompletesRfc5780ViaSocks5AuthAndAnyAddress()
    {
        var serverEndPoint = new IPEndPoint(IPAddress.Parse("203.0.113.10"), 3478);
        var otherEndPoint = new IPEndPoint(IPAddress.Parse("198.51.100.2"), 3479);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await using var mock = StartMockSocks5StunServer(otherEndPoint, echoSameOriginOnFiltering: false, cts.Token);

        using var client = new StunClient5389Udp(serverEndPoint, IPAddress.Any, mock.SocksPort, "user", "pass");
        await client.ConnectProxyAsync(cts.Token);
        await client.QueryAsync(cts.Token);

        Assert.AreEqual(BindingTestResult.Success, client.State.BindingTestResult);
        Assert.AreEqual(new IPEndPoint(IPAddress.Parse("203.0.113.99"), 55555), client.State.PublicEndPoint);
        Assert.AreEqual(otherEndPoint, client.State.OtherEndPoint);
        Assert.AreEqual(MappingBehavior.EndpointIndependent, client.State.MappingBehavior);
        Assert.AreEqual(FilteringBehavior.EndpointIndependent, client.State.FilteringBehavior);
    }

    [TestMethod]
    public async Task StunClient5389UdpRejectsUnchangedOriginOnFilteringProbe()
    {
        var serverEndPoint = new IPEndPoint(IPAddress.Parse("203.0.113.10"), 3478);
        var otherEndPoint = new IPEndPoint(IPAddress.Parse("198.51.100.2"), 3479);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await using var mock = StartMockSocks5StunServer(otherEndPoint, echoSameOriginOnFiltering: true, cts.Token);

        using var client = new StunClient5389Udp(serverEndPoint, IPAddress.Any, mock.SocksPort, "user", "pass");
        await client.ConnectProxyAsync(cts.Token);
        await client.QueryAsync(cts.Token);

        Assert.AreEqual(BindingTestResult.Success, client.State.BindingTestResult);
        Assert.AreEqual(MappingBehavior.EndpointIndependent, client.State.MappingBehavior);
        Assert.AreEqual(FilteringBehavior.AddressAndPortDependent, client.State.FilteringBehavior);
    }

    private static MockSocks5StunServer StartMockSocks5StunServer(
        IPEndPoint otherEndPoint,
        bool echoSameOriginOnFiltering,
        CancellationToken ct)
    {
        var tcpListener = new TcpListener(IPAddress.Loopback, 0);
        tcpListener.Start();
        var socksPort = (ushort)((IPEndPoint)tcpListener.LocalEndpoint).Port;

        var udpRelay = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
        var relayPort = (ushort)((IPEndPoint)udpRelay.Client.LocalEndPoint).Port;

        var serverCts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        var tcpTask = Task.Run(async () =>
        {
            try
            {
                using var client = await tcpListener.AcceptTcpClientAsync(serverCts.Token);
                var stream = client.GetStream();

                var hdr = new byte[2];
                await stream.ReadExactlyAsync(hdr, serverCts.Token);
                var methods = new byte[hdr[1]];
                await stream.ReadExactlyAsync(methods, serverCts.Token);
                Assert.AreEqual(0x05, hdr[0]);
                CollectionAssert.Contains(methods, (byte)0x02);
                await stream.WriteAsync(new byte[] { 0x05, 0x02 }, serverCts.Token);

                var authHdr = new byte[2];
                await stream.ReadExactlyAsync(authHdr, serverCts.Token);
                var userBuf = new byte[authHdr[1]];
                await stream.ReadExactlyAsync(userBuf, serverCts.Token);
                var passLenBuf = new byte[1];
                await stream.ReadExactlyAsync(passLenBuf, serverCts.Token);
                var passBuf = new byte[passLenBuf[0]];
                await stream.ReadExactlyAsync(passBuf, serverCts.Token);
                Assert.AreEqual("user", Encoding.UTF8.GetString(userBuf));
                Assert.AreEqual("pass", Encoding.UTF8.GetString(passBuf));
                await stream.WriteAsync(new byte[] { 0x01, 0x00 }, serverCts.Token);

                var assoc = new byte[10];
                await stream.ReadExactlyAsync(assoc, serverCts.Token);
                Assert.AreEqual(0x03, assoc[1]);

                byte[] reply = [0x05, 0x00, 0x00, 0x01, 0, 0, 0, 0, (byte)(relayPort >> 8), (byte)(relayPort & 0xFF)];
                await stream.WriteAsync(reply, serverCts.Token);

                var drain = new byte[16];
                while (await stream.ReadAsync(drain, serverCts.Token) > 0)
                {
                }
            }
            catch (OperationCanceledException)
            {
            }
        }, CancellationToken.None);

        var udpTask = Task.Run(async () =>
        {
            var mappedEndPoint = new IPEndPoint(IPAddress.Parse("203.0.113.99"), 55555);
            try
            {
                while (!serverCts.IsCancellationRequested)
                {
                    var datagram = await udpRelay.ReceiveAsync(serverCts.Token);
                    if (!StunProtocol.TryUnwrapSocks5UdpPacket(datagram.Buffer, out var target, out var payload) ||
                        target == null ||
                        payload.Length < 20)
                    {
                        continue;
                    }

                    var txId = payload.Slice(8, 12);
                    uint changeFlags = 0;
                    if (payload.Length >= 28 && BinaryPrimitives.ReadUInt16BigEndian(payload.Slice(20, 2)) == 0x0003)
                        changeFlags = BinaryPrimitives.ReadUInt32BigEndian(payload.Slice(24, 4));

                    var origin = changeFlags != 0 && !echoSameOriginOnFiltering ? otherEndPoint : target;
                    var stunResp = BuildMockStunResponse(txId, mappedEndPoint, otherEndPoint, origin);
                    var wrapped = StunProtocol.WrapSocks5UdpPacket(origin, stunResp);
                    await udpRelay.SendAsync(wrapped, datagram.RemoteEndPoint, serverCts.Token);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }, CancellationToken.None);

        return new MockSocks5StunServer(socksPort, tcpListener, udpRelay, serverCts, tcpTask, udpTask);
    }

    private static byte[] BuildMockStunResponse(
        ReadOnlySpan<byte> txId,
        IPEndPoint mapped,
        IPEndPoint other,
        IPEndPoint responseOrigin)
    {
        var resp = new byte[20 + 12 + 12 + 12];
        BinaryPrimitives.WriteUInt16BigEndian(resp.AsSpan(0, 2), 0x0101);
        BinaryPrimitives.WriteUInt16BigEndian(resp.AsSpan(2, 2), 36);
        BinaryPrimitives.WriteUInt32BigEndian(resp.AsSpan(4, 4), StunProtocol.MagicCookie);
        txId.CopyTo(resp.AsSpan(8, 12));

        // XOR-MAPPED-ADDRESS (0x0020)
        var mappedBytes = mapped.Address.GetAddressBytes();
        BinaryPrimitives.WriteUInt16BigEndian(resp.AsSpan(20, 2), 0x0020);
        BinaryPrimitives.WriteUInt16BigEndian(resp.AsSpan(22, 2), 8);
        resp[25] = 0x01;
        BinaryPrimitives.WriteUInt16BigEndian(resp.AsSpan(26, 2), (ushort)(mapped.Port ^ 0x2112));
        byte[] cookie = [0x21, 0x12, 0xA4, 0x42];
        for (var i = 0; i < 4; i++)
            resp[28 + i] = (byte)(mappedBytes[i] ^ cookie[i]);

        // OTHER-ADDRESS (0x802C)
        BinaryPrimitives.WriteUInt16BigEndian(resp.AsSpan(32, 2), 0x802C);
        BinaryPrimitives.WriteUInt16BigEndian(resp.AsSpan(34, 2), 8);
        resp[37] = 0x01;
        BinaryPrimitives.WriteUInt16BigEndian(resp.AsSpan(38, 2), (ushort)other.Port);
        other.Address.GetAddressBytes().CopyTo(resp, 40);

        // RESPONSE-ORIGIN (0x802B)
        BinaryPrimitives.WriteUInt16BigEndian(resp.AsSpan(44, 2), 0x802B);
        BinaryPrimitives.WriteUInt16BigEndian(resp.AsSpan(46, 2), 8);
        resp[49] = 0x01;
        BinaryPrimitives.WriteUInt16BigEndian(resp.AsSpan(50, 2), (ushort)responseOrigin.Port);
        responseOrigin.Address.GetAddressBytes().CopyTo(resp, 52);

        return resp;
    }

    private sealed class MockSocks5StunServer(
        ushort socksPort,
        TcpListener tcpListener,
        UdpClient udpRelay,
        CancellationTokenSource cts,
        Task tcpTask,
        Task udpTask) : IAsyncDisposable
    {
        public ushort SocksPort => socksPort;

        public async ValueTask DisposeAsync()
        {
            cts.Cancel();
            tcpListener.Stop();
            udpRelay.Dispose();
            await Task.WhenAll(tcpTask, udpTask);
            cts.Dispose();
        }
    }
}
