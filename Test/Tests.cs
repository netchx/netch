using System.Linq;
using System.Windows.Forms;
using Netch.Servers.ShadowsocksR.Form;
using Netch.Servers.ShadowsocksR;
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

            var server = (ShadowsocksR) new SSRUtil().ParseUri(@"ssr://MTI3LjAuMC4xOjEyMzQ6YXV0aF9hZXMxMjhfbWQ1OmFlcy0xMjgtY2ZiOnRsczEuMl90aWNrZXRfYXV0aDpZV0ZoWW1KaS8_b2Jmc3BhcmFtPVluSmxZV3QzWVRFeExtMXZaUSZyZW1hcmtzPTVyV0w2Sy1WNUxpdDVwYUg").First();
            server.ProtocolParam = "ProtocolParam";

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new ShadowsocksRForm(server));
        }
    }
}