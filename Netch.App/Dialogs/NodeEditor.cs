using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Netch.App.Services;
using Netch.App.Helpers;
using Netch.Enums;
using Netch.Models;
using Netch.Servers;
using Netch.Utils;
using QRCoder;
using static Netch.App.Helpers.Ui;

namespace Netch.App.Dialogs;

internal static class NodeEditor
{
    private static bool _natDialogActive;

    public static async Task<int> ImportClipboardAsync()
    {
        var result = await ImportTextAsync(await ClipboardTextAsync());
        return result.Count;
    }

    internal static async Task<(int Count, int Skipped)> ImportTextAsync(string text)
    {
        var imported = ShareLink.ParseText(text, out var skipped);
        if (imported.Count == 0)
            return (0, skipped);

        foreach (var server in imported)
        {
            server.Id = Guid.NewGuid().ToString("N");
            server.Group = Constants.DefaultGroup;
            server.IsSub = false;
        }
        var nodes = Global.Settings.Server;
        nodes.AddRange(imported);
        try
        {
            await Configuration.SaveAsync(throwOnError: true);
        }
        catch
        {
            foreach (var server in imported)
                nodes.Remove(server);
            throw;
        }
        return (imported.Count, skipped);
    }

    public static async Task<Subscription?> EditSubscriptionAsync(XamlRoot root, Subscription? existing)
    {
        var link = Input(T("Subscription URL", "订阅链接"), existing?.Link);
        var name = Input(T("Name", "名称"), existing?.Remark);
        var agent = Input("User-Agent", existing?.UserAgent);
        var hours = Input(T("Auto-update interval (hours, 0 = manual)", "自动更新间隔（小时，0 为手动）"),
            (existing?.UpdateIntervalHours ?? 6).ToString());
        var enabled = new CheckBox { Content = T("Enabled", "启用"), IsChecked = existing?.Enable ?? true };
        var subscription = existing ?? new Subscription();

        if (!await ShowAsync(root, T("Subscription", "订阅"), Stack(name, link, hours, agent, enabled), T("Save", "保存"), () =>
        {
            var remark = name.Text.Trim();
            if (remark.Length == 0 || remark == Constants.DefaultGroup ||
                Global.Settings.Subscription.Any(s => s != existing && s.Remark.Equals(remark, StringComparison.OrdinalIgnoreCase)))
                return T("Enter a unique subscription name.", "请输入不重复的订阅名称。");
            if (!Uri.TryCreate(link.Text.Trim(), UriKind.Absolute, out var url) || url.Scheme is not ("http" or "https"))
                return T("Enter an HTTP or HTTPS subscription URL.", "请输入 HTTP 或 HTTPS 订阅链接。");
            if (!int.TryParse(hours.Text, out var interval) || interval is < 0 or > 8760)
                return T("Enter an interval between 0 and 8760 hours.", "请输入 0 至 8760 小时。");
            if (existing != null)
                foreach (var server in Global.Settings.Server.Where(s => s.Group == existing.Remark))
                    server.Group = remark;
            subscription.Remark = remark;
            subscription.Link = url.AbsoluteUri;
            subscription.UserAgent = agent.Text.Trim();
            subscription.UpdateIntervalHours = interval;
            subscription.Enable = enabled.IsChecked == true;
            return null;
        }))
            return null;

        if (existing == null)
        {
            Global.Settings.Subscription.Add(subscription);
            await SubscriptionUtil.UpdateServerAsync(subscription);
        }
        await Configuration.SaveAsync();
        return subscription;
    }

    public static async Task<bool> EditAsync(XamlRoot root, Server? existing, string? type)
    {
        var util = ServerHelper.GetUtilByTypeName(type ?? existing!.ConfigType.ToString());
        var server = (Server?)existing?.Clone() ?? (Server)Activator.CreateInstance(util.ServerType)!;
        server.ProtoExtra = server.ProtoExtra with { };
        var extra = server.ProtoExtra;
        var fields = Stack();
        var apply = new List<Func<string?>>();
        var name = Input(T("Name", "名称"), server.Remarks);
        var address = Input(T("Address", "地址"), server.Address);
        var port = Input(T("Port", "端口"), server.Port == 0 ? "" : server.Port.ToString());
        fields.Children.Add(name);
        fields.Children.Add(TwoColumns(address, port, new GridLength(2, GridUnitType.Star)));
        var protocol = server.ConfigType;

        void Field(string label, string? value, Action<string> set)
        {
            var box = Input(label, value);
            fields.Children.Add(box);
            apply.Add(() => { set(box.Text.Trim()); return null; });
        }

        void Secret(string label, string? value, Action<string> set)
        {
            var box = new PasswordBox { Header = label, Password = value ?? "" };
            fields.Children.Add(box);
            apply.Add(() => { set(box.Password); return null; });
        }

        void Number(string label, int? value, Action<int?> set, int min = 0, int max = int.MaxValue)
        {
            var box = Input(label, value?.ToString());
            fields.Children.Add(box);
            apply.Add(() =>
            {
                if (box.Text.Trim().Length == 0)
                    set(null);
                else if (int.TryParse(box.Text, out var number) && number >= min && number <= max)
                    set(number);
                else
                    return T($"Invalid value for {label}.", $"{label}的值无效。");
                return null;
            });
        }

        void Options(string label, string? value, Action<string> set, params string[] choices)
        {
            var items = choices.Prepend(value ?? "").Distinct().ToArray();
            var box = Choice(label, items);
            box.Header = label;
            box.HorizontalAlignment = HorizontalAlignment.Stretch;
            fields.Children.Add(box);
            apply.Add(() => { set((string)box.SelectedItem); return null; });
        }

        void Toggle(string label, bool? value, Action<bool?> set)
        {
            var box = new CheckBox { Content = label, IsChecked = value };
            fields.Children.Add(box);
            apply.Add(() => { set(box.IsChecked); return null; });
        }

        if (protocol is EConfigType.SOCKS or EConfigType.HTTP or EConfigType.Naive or EConfigType.MASQUE or EConfigType.TUIC)
            Field(protocol == EConfigType.TUIC ? "UUID" : T("Username", "用户名"), server.Username, s => server.Username = s);
        Secret(protocol switch
        {
            EConfigType.WireGuard => T("Private key", "私钥"),
            EConfigType.VLESS or EConfigType.VMess => "UUID",
            EConfigType.Hysteria2 => T("Authentication", "认证"),
            _ => T("Password", "密码")
        }, server.Password, s => server.Password = s);

        switch (server)
        {
            case ShadowsocksRServer ssr:
                Options(T("Encryption", "加密"), ssr.EncryptMethod, s => ssr.EncryptMethod = s, "none", "rc4-md5", "aes-128-ctr",
                    "aes-192-ctr", "aes-256-ctr", "aes-128-cfb", "aes-192-cfb", "aes-256-cfb", "chacha20", "chacha20-ietf", "xchacha20");
                Field(T("Protocol", "协议"), ssr.Protocol, s => ssr.Protocol = s);
                Field(T("Protocol parameter", "协议参数"), ssr.ProtocolParam, s => ssr.ProtocolParam = s);
                Field(T("Obfuscation", "混淆"), ssr.OBFS, s => ssr.OBFS = s);
                Field(T("Obfuscation parameter", "混淆参数"), ssr.OBFSParam, s => ssr.OBFSParam = s);
                break;
            case ShadowsocksServer:
                Options(T("Encryption", "加密"), extra.SsMethod, s => extra.SsMethod = s, Constants.SsSecuritiesInXray.ToArray());
                Options(T("Plugin", "插件"), extra.SsPlugin, s => extra.SsPlugin = s, "", "obfs-local", "v2ray-plugin");
                Field(T("Plugin options", "插件参数"), extra.SsPluginOptions, s => extra.SsPluginOptions = s);
                Toggle("UDP over TCP", extra.Uot, b => extra.Uot = b);
                break;
            case WireGuardServer:
                Field(T("Peer public key", "对端公钥"), extra.WgPublicKey, s => extra.WgPublicKey = s);
                Secret(T("Preshared key", "预共享密钥"), extra.WgPresharedKey, s => extra.WgPresharedKey = s);
                Field(T("Local IP / CIDR", "本地 IP / CIDR"), extra.WgInterfaceAddress, s => extra.WgInterfaceAddress = s);
                Field(T("Reserved bytes", "保留字节"), extra.WgReserved, s => extra.WgReserved = s);
                Number("MTU", extra.WgMtu, n => extra.WgMtu = n, 1);
                break;
        }

        switch (protocol)
        {
            case EConfigType.VMess:
                Number("Alter ID", extra.AlterId, n => extra.AlterId = n, max: ushort.MaxValue);
                Options(T("Encryption", "加密"), extra.VmessSecurity, s => extra.VmessSecurity = s, Constants.VmessSecurities.ToArray());
                break;
            case EConfigType.VLESS:
                Options(T("Flow", "流控"), extra.Flow, s => extra.Flow = s, "", "xtls-rprx-vision");
                Field(T("Encryption", "加密"), extra.VlessEncryption, s => extra.VlessEncryption = s);
                break;
            case EConfigType.SOCKS:
                Toggle("UDP over TCP", extra.Uot, b => extra.Uot = b);
                break;
            case EConfigType.HTTP:
                Field(T("HTTP headers", "HTTP 请求头"), extra.HttpHeaders, s => extra.HttpHeaders = s);
                break;
            case EConfigType.TUIC:
                Options(T("Congestion control", "拥塞控制"), extra.CongestionControl, s => extra.CongestionControl = s, "bbr", "cubic", "new_reno");
                break;
            case EConfigType.Hysteria2:
                Options(T("Obfuscation", "混淆"), extra.Obfs, s => extra.Obfs = s, "none", "salamander", "gecko");
                Secret(T("Obfuscation password", "混淆密码"), extra.SalamanderPass, s => extra.SalamanderPass = s);
                Field(T("Port hopping range", "端口跳跃范围"), extra.Ports, s => extra.Ports = s);
                Field(T("Hop interval (s)", "跳跃间隔（秒）"), extra.HopInterval, s => extra.HopInterval = s);
                Number(T("Upload (Mbps)", "上传（Mbps）"), extra.UpMbps, n => extra.UpMbps = n);
                Number(T("Download (Mbps)", "下载（Mbps）"), extra.DownMbps, n => extra.DownMbps = n);
                break;
            case EConfigType.Naive:
                Toggle("QUIC", extra.NaiveQuic, b => extra.NaiveQuic = b);
                Number(T("Tunnel connections", "隧道连接数"), extra.InsecureConcurrency, n => extra.InsecureConcurrency = n);
                break;
            case EConfigType.MASQUE:
                Field(T("URI template", "URI 模板"), extra.MasquePath, s => extra.MasquePath = s);
                Field(T("HTTP headers", "HTTP 请求头"), extra.MasqueHeaders, s => extra.MasqueHeaders = s);
                break;
        }

        if (protocol is EConfigType.VLESS or EConfigType.VMess or EConfigType.Trojan)
        {
            Options(T("Transport", "传输"), server.Network, s => server.Network = s, "tcp", "ws", "grpc", "httpupgrade", "xhttp", "kcp");
            Field(T("Request host", "请求主机"), server.RequestHost, s => server.RequestHost = s);
            Field(T("Path / service name", "路径 / 服务名"), server.Path, s => server.Path = s);
            Field(T("Header type", "伪装类型"), server.HeaderType, s => server.HeaderType = s);
            Toggle("Mux", server.MuxEnabled, b => server.MuxEnabled = b);
            Field(T("Reality public key", "Reality 公钥"), server.PublicKey, s => server.PublicKey = s);
            Field(T("Short ID", "短 ID"), server.ShortId, s => server.ShortId = s);
            Field("SpiderX", server.SpiderX, s => server.SpiderX = s);
            Field("ML-DSA-65", server.Mldsa65Verify, s => server.Mldsa65Verify = s);
            Field(T("Transport extensions", "传输扩展"), server.Extra, s => server.Extra = s);
        }

        if (protocol is not (EConfigType.WireGuard or EConfigType.ShadowsocksR))
        {
            Options(T("TLS security", "TLS 安全"), server.StreamSecurity, s => server.StreamSecurity = s, "", "tls", "reality");
            Field("SNI", server.Sni, s => server.Sni = s);
            Field("ALPN", server.Alpn, s => server.Alpn = s);
            Field(T("Fingerprint", "指纹"), server.Fingerprint, s => server.Fingerprint = s);
            Toggle(T("Allow insecure", "允许不安全连接"), server.AllowInsecure, b => server.AllowInsecure = b);
            Field(T("Certificate", "证书"), server.Cert, s => server.Cert = s);
            Field(T("Certificate SHA", "证书 SHA"), server.CertSha, s => server.CertSha = s);
            Field("ECH", server.EchConfigList, s => server.EchConfigList = s);
            Field("ECH Force Query", server.EchForceQuery, s => server.EchForceQuery = s);
        }

        var title = existing == null ? T("Add node", "添加节点") : T("Edit node", "编辑节点");
        if (!await ShowAsync(root, $"{title} · {ServerHelper.GetDisplayName(util)}", fields, T("Save", "保存"), () =>
        {
            if (address.Text.Trim().Length == 0 || !ushort.TryParse(port.Text, out var number) || number == 0)
                return T("Enter an address and a valid port.", "请输入地址及有效端口。");
            server.Remarks = name.Text.Trim();
            server.Address = address.Text.Trim();
            server.Port = number;
            foreach (var read in apply)
                if (read() is { } error)
                    return error;

            return util.CheckServer(server) ? null : T("Check the required protocol fields.", "请检查该协议的必填字段。");
        }))
            return false;

        if (existing != null && Global.Settings.Server.IndexOf(existing) is var index && index >= 0)
            Global.Settings.Server[index] = server;
        else if (existing == null)
        {
            server.IsSub = false;
            Global.Settings.Server.Add(server);
        }
        await Configuration.SaveAsync();
        return true;
    }

    public static async Task ShareAsync(XamlRoot root, Server server, bool qr)
    {
        try
        {
            var link = ShareLink.GetShareLink(server);
            if (!qr)
            {
                Copy(link);
                return;
            }

            var bytes = PngByteQRCodeHelper.GetQRCode(link, QRCodeGenerator.ECCLevel.L, 6);
            using var png = new MemoryStream(bytes);
            var bitmap = new BitmapImage();
            await bitmap.SetSourceAsync(png.AsRandomAccessStream());
            var image = new Image { Source = bitmap, Width = 270, Height = 270 };
            Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(image, T("Node QR code", "节点二维码"));
            await ShowAsync(root, Format.NodeName(server), Stack(image,
                Button(T("Copy share link", "复制分享链接"), () => Copy(link))));
        }
        catch (Exception e)
        {
            await ShowAsync(root, T("Share link", "分享链接"), Text(e.Message));
        }
    }

    public static async Task TestNatAsync(XamlRoot root, Server server)
    {
        if (_natDialogActive)
            return;

        _natDialogActive = true;
        var title = $"NAT · {Format.NodeName(server)}";
        try
        {
            var result = await Session.Current.TestNodeNatAsync(server);
            var label = Text(Format.Nat(result));
            var content = Format.NatFailed(result) ? (UIElement)Stack(label, Caption(result)) : label;
            await ShowAsync(root, title, content);
        }
        catch (Exception e)
        {
            await ShowAsync(root, title, Stack(Text(T("Test failed", "测试失败")), Caption(e.Message)));
        }
        finally
        {
            _natDialogActive = false;
        }
    }
}
