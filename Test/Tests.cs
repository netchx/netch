using System.Linq;
using System.Windows.Forms;
using Netch.Servers.ShadowsocksR;
using Netch.Servers.VLESS;
using Netch.Servers.VMess;
using Netch.Servers.VMess.Form;
using Netch.Utils;
using NUnit.Framework;

namespace Test
{
    [TestFixture]
    public class Tests
    {
        [Test]
        public void TestServerForm()
        {
            i18N.Load("zh-CN");

            var server = ParseVMessUri();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new VMessForm(server));
        }

        private static ShadowsocksR ParseSSRUri()
        {
            return (ShadowsocksR) new SSRUtil().ParseUri(@"ssr://MTI3LjAuMC4xOjEyMzQ6YXV0aF9hZXMxMjhfbWQ1OmFlcy0xMjgtY2ZiOnRsczEuMl90aWNrZXRfYXV0aDpZV0ZoWW1KaS8_b2Jmc3BhcmFtPVluSmxZV3QzWVRFeExtMXZaUSZyZW1hcmtzPTVyV0w2Sy1WNUxpdDVwYUg").First();
        }

        private static VMess ParseVMessUri()
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
            return (VMess) new VMessUtil().ParseUri(@"vmess://eyAidiI6ICIyIiwgInBzIjogIuWkh+azqOWIq+WQjSIsICJhZGQiOiAiMTExLjExMS4xMTEuMTExIiwgInBvcnQiOiAiMzIwMDAiLCAiaWQiOiAiMTM4NmY4NWUtNjU3Yi00ZDZlLTlkNTYtNzhiYWRiNzVlMWZkIiwgImFpZCI6ICIxMDAiLCAibmV0IjogInRjcCIsICJ0eXBlIjogIm5vbmUiLCAiaG9zdCI6ICJ3d3cuYmJiLmNvbSIsICJwYXRoIjogIi8iLCAidGxzIjogInRscyIgfQ==").First();
        }

        [Test]
        public void ParseVLESSUri()
        {
            var server = new VLESSUtil().ParseUri(@"vless://399ce595-894d-4d40-add1-7d87f1a3bd10@qv2ray.net:41971?type=kcp&headerType=wireguard&seed=69f04be3-d64e-45a3-8550-af3172c63055#VLESSmKCPSeedWG").First();
        }
    }
}