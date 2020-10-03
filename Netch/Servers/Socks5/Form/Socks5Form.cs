using Netch.Forms;

namespace Netch.Servers.Socks5.Form
{
    public class Socks5Form : ServerForm
    {
        protected override string TypeName { get; } = "Socks5";

        public Socks5Form(Socks5 server = default)
        {
            server ??= new Socks5();
            Server = server;
        }
    }
}