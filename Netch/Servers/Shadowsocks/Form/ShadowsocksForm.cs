using Netch.Forms;

namespace Netch.Servers.Shadowsocks.Form
{
    public class ShadowsocksForm : ServerForm
    {
        public ShadowsocksForm(Shadowsocks? server = default)
        {
            server ??= new Shadowsocks();
            Server = server;
            CreateTextBox("Password", "Password", s => true, s => server.Password = s, server.Password);
            CreateComboBox("EncryptMethod", "Encrypt Method", SSGlobal.EncryptMethods, s => server.EncryptMethod = s, server.EncryptMethod);
            CreateTextBox("Plugin", "Plugin", s => true, s => server.Plugin = s, server.Plugin);
            CreateTextBox("PluginsOption", "Plugin Options", s => true, s => server.PluginOption = s, server.PluginOption);
        }

        protected override string TypeName { get; } = "Shadowsocks";
    }
}