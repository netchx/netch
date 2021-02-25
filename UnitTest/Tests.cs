using System.Windows.Forms;
using Netch.Servers.VMess.Form;
using Netch.Utils;

namespace UnitTest
{
    public class Tests : TestBase
    {
        public static void TestServerForm()
        {
            i18N.Load("zh-CN");

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new VMessForm());
        }
    }
}