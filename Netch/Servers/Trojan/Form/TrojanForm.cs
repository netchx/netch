using Netch.Forms;

namespace Netch.Servers.Trojan.Form
{
    public class TrojanForm : ServerForm
    {
        protected override string TypeName { get; } = "Trojan";

        public TrojanForm(Trojan server = default)
        {
            server ??= new Trojan();
            Server = server;
        }
    }
}