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
            CreateTextBox("Password", "Password",
                s => true,
                s => server.Password = s,
                server.Password);
            CreateTextBox("Host", "Host",
                s => true,
                s => server.Host = s,
                server.Host);
        }
    }
}