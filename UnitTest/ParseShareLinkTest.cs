using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Netch.Models;
using Netch.Servers.ShadowsocksR;
using Netch.Servers.VLESS;
using Netch.Servers.VMess;

namespace UnitTest
{
    [TestClass]
    public class TestParseShareLink : TestBase
    {
        [TestMethod]
        public void ParseSSR()
        {
            var server = ParseSingle<ShadowsocksR, SSRUtil>(
                @"ssr://MTI3LjAuMC4xOjEyMzQ6YXV0aF9hZXMxMjhfbWQ1OmFlcy0xMjgtY2ZiOnRsczEuMl90aWNrZXRfYXV0aDpZV0ZoWW1KaS8_b2Jmc3BhcmFtPVluSmxZV3QzWVRFeExtMXZaUSZyZW1hcmtzPTVyV0w2Sy1WNUxpdDVwYUg");

            Assert.IsNotNull(server);
            Console.WriteLine(JsonSerializerFormatted(server));
        }

        [TestMethod]
        public void ParseV2RayNFormatUri()
        {
            /*
{
"v": "2",
"ps": "备注别名",
"add": "111.111.111.111",
"port": "32000",
"id": "1386f85e-657b-4d6e-9d56-78badb75e1fd",
"aid": "100",
"net": "tcp",
"type": "none",
"host": "www.bbb.com",
"path": "/",
"tls": "tls"
} 
             */
            var server = ParseSingle<VMess, VMessUtil>(
                @"vmess://eyAidiI6ICIyIiwgInBzIjogIuWkh+azqOWIq+WQjSIsICJhZGQiOiAiMTExLjExMS4xMTEuMTExIiwgInBvcnQiOiAiMzIwMDAiLCAiaWQiOiAiMTM4NmY4NWUtNjU3Yi00ZDZlLTlkNTYtNzhiYWRiNzVlMWZkIiwgImFpZCI6ICIxMDAiLCAibmV0IjogInRjcCIsICJ0eXBlIjogIm5vbmUiLCAiaG9zdCI6ICJ3d3cuYmJiLmNvbSIsICJwYXRoIjogIi8iLCAidGxzIjogInRscyIgfQ==");

            Assert.IsNotNull(server);
            Console.WriteLine(JsonSerializerFormatted(server));
        }

        [TestMethod]
        public void ParseVLESSUri()
        {
            var server = ParseSingle<VLESS, VLESSUtil>(
                @"vless://399ce595-894d-4d40-add1-7d87f1a3bd10@qv2ray.net:41971?type=kcp&headerType=wireguard&seed=69f04be3-d64e-45a3-8550-af3172c63055#VLESSmKCPSeedWG");

            Assert.IsNotNull(server);
            Console.WriteLine(JsonSerializerFormatted(server));
        }

        public T ParseSingle<T, TUtil>(string data) where T : Server
                                                    where TUtil : IServerUtil, new()
        {
            return (T) new TUtil().ParseUri(data).First();
        }
    }
}