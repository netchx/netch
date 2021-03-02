using System;
using System.Linq;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Netch;
using Netch.Servers.ShadowsocksR;
using Netch.Servers.VMess;
using Netch.Utils;

namespace UnitTest
{
    [TestClass]
    public class TestParseShareLink : TestBase
    {
        [TestMethod]
        public void TestServerFromSSR()
        {
            const string normalCase =
                "ssr://MTI3LjAuMC4xOjEyMzQ6YXV0aF9hZXMxMjhfbWQ1OmFlcy0xMjgtY2ZiOnRsczEuMl90aWNrZXRfYXV0aDpZV0ZoWW1KaS8_b2Jmc3BhcmFtPVluSmxZV3QzWVRFeExtMXZaUQ";

            var server = (ShadowsocksR) ShareLink.ParseText(normalCase).First();
            Assert.AreEqual(server.Hostname, "127.0.0.1");
            Assert.AreEqual(server.Port, (ushort) 1234);
            Assert.AreEqual(server.Protocol, "auth_aes128_md5");
            Assert.AreEqual(server.EncryptMethod, "aes-128-cfb");
            Assert.AreEqual(server.OBFS, "tls1.2_ticket_auth");
            Assert.AreEqual(server.OBFSParam, "breakwa11.moe");
            Assert.AreEqual(server.Password, "aaabbb");


            const string normalCaseWithRemark =
                "ssr://MTI3LjAuMC4xOjEyMzQ6YXV0aF9hZXMxMjhfbWQ1OmFlcy0xMjgtY2ZiOnRsczEuMl90aWNrZXRfYXV0aDpZV0ZoWW1KaS8_b2Jmc3BhcmFtPVluSmxZV3QzWVRFeExtMXZaUSZyZW1hcmtzPTVyV0w2Sy1WNUxpdDVwYUg";

            server = (ShadowsocksR) ShareLink.ParseText(normalCaseWithRemark).First();
            Assert.AreEqual(server.Hostname, "127.0.0.1");
            Assert.AreEqual(server.Port, (ushort) 1234);
            Assert.AreEqual(server.Protocol, "auth_aes128_md5");
            Assert.AreEqual(server.EncryptMethod, "aes-128-cfb");
            Assert.AreEqual(server.OBFS, "tls1.2_ticket_auth");
            Assert.AreEqual(server.OBFSParam, "breakwa11.moe");
            Assert.AreEqual(server.Password, "aaabbb");
            Assert.AreEqual(server.Remark, "测试中文");
        }

        [TestMethod]
        public void ParseV2RayNFormatUri()
        {
            var server = (VMess) ShareLink.ParseText(
                    @"vmess://eyAidiI6ICIyIiwgInBzIjogIuWkh+azqOWIq+WQjSIsICJhZGQiOiAiMTExLjExMS4xMTEuMTExIiwgInBvcnQiOiAiMzIwMDAiLCAiaWQiOiAiMTM4NmY4NWUtNjU3Yi00ZDZlLTlkNTYtNzhiYWRiNzVlMWZkIiwgImFpZCI6ICIxMDAiLCAibmV0IjogInRjcCIsICJ0eXBlIjogIm5vbmUiLCAiaG9zdCI6ICJ3d3cuYmJiLmNvbSIsICJwYXRoIjogIi8iLCAidGxzIjogInRscyIgfQ==")
                .First();

            Assert.AreEqual(server.Remark, "备注别名");
            Assert.AreEqual(server.Hostname, "111.111.111.111");
            Assert.AreEqual(server.Port, (ushort) 32000);
            Assert.AreEqual(server.UserID, "1386f85e-657b-4d6e-9d56-78badb75e1fd");
            Assert.AreEqual(server.AlterID, 100);
            Assert.AreEqual(server.TransferProtocol, "tcp");
            Assert.AreEqual(server.FakeType, "none");
            Assert.AreEqual(server.Host, "www.bbb.com");
            Assert.AreEqual(server.Path, "/");
            Assert.AreEqual(server.TLSSecureType, "tls");
        }

        [TestMethod]
        public void ParseVLESSUri()
        {
            var servers = ShareLink.ParseText(@"vmess://99c80931-f3f1-4f84-bffd-6eed6030f53d@qv2ray.net:31415?encryption=none#VMessTCPNaked
vmess://f08a563a-674d-4ffb-9f02-89d28aec96c9@qv2ray.net:9265#VMessTCPAuto
vmess://5dc94f3a-ecf0-42d8-ae27-722a68a6456c@qv2ray.net:35897?encryption=aes-128-gcm#VMessTCPAES
vmess://136ca332-f855-4b53-a7cc-d9b8bff1a8d7@qv2ray.net:9323?encryption=none&security=tls#VMessTCPTLSNaked
vmess://be5459d9-2dc8-4f47-bf4d-8b479fc4069d@qv2ray.net:8462?security=tls#VMessTCPTLS
vmess://c7199cd9-964b-4321-9d33-842b6fcec068@qv2ray.net:64338?encryption=none&security=tls&sni=fastgit.org#VMessTCPTLSSNI
vless://b0dd64e4-0fbd-4038-9139-d1f32a68a0dc@qv2ray.net:3279?security=xtls&flow=rprx-xtls-splice#VLESSTCPXTLSSplice
vless://399ce595-894d-4d40-add1-7d87f1a3bd10@qv2ray.net:50288?type=kcp&seed=69f04be3-d64e-45a3-8550-af3172c63055#VLESSmKCPSeed
vless://399ce595-894d-4d40-add1-7d87f1a3bd10@qv2ray.net:41971?type=kcp&headerType=wireguard&seed=69f04be3-d64e-45a3-8550-af3172c63055#VLESSmKCPSeedWG
vmess://44efe52b-e143-46b5-a9e7-aadbfd77eb9c@qv2ray.net:6939?type=ws&security=tls&host=qv2ray.net&path=%2Fsomewhere#VMessWebSocketTLS");

            foreach (var server in servers)
            {
                var jsonSerializerOptions = Global.NewDefaultJsonSerializerOptions;
                jsonSerializerOptions.WriteIndented = false;
                Console.WriteLine(JsonSerializer.Serialize<object>(server, jsonSerializerOptions) + "\n");
                Assert.IsNotNull(server);

                Assert.AreEqual(server.Hostname, "qv2ray.net");
            }
        }
    }
}