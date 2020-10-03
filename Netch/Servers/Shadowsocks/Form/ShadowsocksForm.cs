using Netch.Forms;

namespace Netch.Servers.Shadowsocks.Form
{
    public class ShadowsocksForm : ServerForm
    {
        protected override string TypeName { get; } = "Shadowsocks";

        public ShadowsocksForm(Shadowsocks server = default)
        {
            server ??= new Shadowsocks();
            Server = server;
            CreateTextBox("Password", "Password",
                s => true,
                s => server.Password = (string) s,
                server.Password);
            CreateComboBox("EncryptMethod", "Encrypt Method",
                SSGlobal.EncryptMethods,
                s => SSGlobal.EncryptMethods.Contains(s),
                s => server.EncryptMethod = (string) s,
                server.EncryptMethod);
            CreateTextBox("Plugin", "Plugin",
                s => true,
                s => server.Plugin = (string) s,
                server.Plugin);
            CreateTextBox("PluginsOption", "Plugin Options",
                s => true,
                s => server.PluginOption = (string) s,
                server.PluginOption);
        }
    }
}