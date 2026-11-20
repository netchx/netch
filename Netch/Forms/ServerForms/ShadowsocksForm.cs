using Netch.Enums;
using Netch.Forms;
using Netch.Utils;

namespace Netch.Servers;

[Fody.ConfigureAwait(true)]
public class ShadowsocksForm : ServerForm
{
    public ShadowsocksForm(ShadowsocksServer? server = default)
    {
        server ??= new ShadowsocksServer();
        Server = server;
        var extra = server.ProtoExtra;
        var password = CreateTextBox("Password", "Password", _ => true, s => server.Password = s, server.Password).Field;
        var cipher = CreateComboBox("EncryptMethod", "Encrypt Method", Constants.SsCiphers,
            s => extra.SsMethod = s, extra.SsMethod);
        CheckField(password, value => IsPasswordValid(cipher.Text, value));
        CreateCheckBox("Uot", "UDP over TCP", s => extra.Uot = s, extra.Uot == true);

        var originalPlugin = extra.SsPlugin ?? "";
        var originalOptions = extra.SsPluginOptions;
        void ResetTransport()
        {
            server.Network = "tcp";
            server.HeaderType = "";
            server.RequestHost = "";
            server.Path = "";
            server.StreamSecurity = "none";
        }
        var plugin = CreateComboBox("Plugin", "Plugin", ["", "obfs-local", "v2ray-plugin"], s =>
        {
            if (s == originalPlugin) return;
            extra.SsPlugin = s;
            extra.SsPluginOptions = "";
            if (s != "v2ray-plugin") server.AllowInsecure = null;
            ResetTransport();
        }, originalPlugin);
        var options = CreateTextBox("PluginOptions", "Plugin Options", _ => true, s =>
        {
            if (plugin.Text == originalPlugin && s.Trim() == (originalOptions ?? "").Trim()) return;
            extra.SsPluginOptions = s.Trim();
            ResetTransport();
        },
            originalOptions).Field;
        var insecure = CreateComboBox("AllowInsecure", "allowInsecure", Constants.AllowInsecure,
            value => server.AllowInsecure = bool.TryParse(value, out var result) ? result : null,
            server.AllowInsecure?.ToString().ToLowerInvariant());
        CheckField(options, value =>
        {
            if (plugin.Text == originalPlugin && value == originalOptions) return true;
            var parts = value.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            return plugin.Text switch
            {
                "obfs-local" => parts.All(part => !part.StartsWith("obfs=") || part is "obfs=http" or "obfs=tls"),
                "v2ray-plugin" => parts.All(part => !part.StartsWith("mode=") || part is "mode=websocket" or "mode=quic"),
                _ => true
            };
        });
        AddNotice("Plugin options use semicolons. Leave the plugin blank for plain Shadowsocks.");
        void RefreshPlugin()
        {
            ShowField(options, !string.IsNullOrEmpty(plugin.Text));
            ShowField(insecure, plugin.Text == "v2ray-plugin");
            options.PlaceholderText = plugin.Text switch
            {
                "obfs-local" => "obfs=http;obfs-host=example.com",
                "v2ray-plugin" => "mode=websocket;host=example.com;path=/;tls",
                _ => ""
            };
        }
        plugin.SelectedIndexChanged += (_, _) => UpdateFields(() =>
        {
            options.Text = plugin.Text == originalPlugin ? originalOptions ?? "" : "";
            RefreshPlugin();
        });
        RefreshPlugin();
    }

    protected override EConfigType TypeName { get; } = EConfigType.Shadowsocks;

    private static bool IsPasswordValid(string cipher, string value)
    {
        if (cipher is "none" or "plain") return true;
        if (!cipher.StartsWith("2022-", StringComparison.OrdinalIgnoreCase)) return !string.IsNullOrWhiteSpace(value);
        var length = cipher == "2022-blake3-aes-128-gcm" ? 16 : 32;
        return value.Split(':').All(key => IsKey(key, length));
    }

    private static bool IsKey(string key, int length) =>
        Convert.TryFromBase64String(key, stackalloc byte[32], out var count) && count == length;
}
