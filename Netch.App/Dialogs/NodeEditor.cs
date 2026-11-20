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
        var server = (Server?)existing?.Clone() ?? ServerEditorHelper.CreateDefault(util);
        server.ProtoExtra = server.ProtoExtra with { };
        var extra = server.ProtoExtra;
        var protocol = server.ConfigType;

        var fields = Stack();
        var apply = new List<Func<string?>>();
        var name = Input(T("Name", "名称"), server.Remarks);
        var address = Input(T("Address", "地址"), server.Address);
        var port = Input(T("Port", "端口"), server.Port == 0 ? "" : server.Port.ToString());
        fields.Children.Add(name);
        fields.Children.Add(TwoColumns(address, port, new GridLength(2, GridUnitType.Star)));

        static void Show(UIElement element, bool visible) =>
            element.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;

        TextBox Field(string label, string? value, Action<string> set, bool multiline = false)
        {
            var box = Input(label, value, multiline: multiline);
            if (multiline)
            {
                box.MinHeight = 76;
                box.MaxHeight = 160;
                ScrollViewer.SetVerticalScrollBarVisibility(box, ScrollBarVisibility.Auto);
            }
            fields.Children.Add(box);
            apply.Add(() =>
            {
                if (box.Visibility == Visibility.Visible)
                    set(box.Text.Trim());
                return null;
            });
            return box;
        }

        PasswordBox Secret(string label, string? value, Action<string> set)
        {
            var box = new PasswordBox { Header = label, Password = value ?? "" };
            fields.Children.Add(box);
            apply.Add(() =>
            {
                if (box.Visibility == Visibility.Visible)
                    set(box.Password);
                return null;
            });
            return box;
        }

        TextBox Number(string label, int? value, Action<int?> set, int min = 0, int max = int.MaxValue)
        {
            var box = Input(label, value?.ToString());
            fields.Children.Add(box);
            apply.Add(() =>
            {
                if (box.Visibility != Visibility.Visible)
                    return null;
                if (box.Text.Trim().Length == 0)
                    set(null);
                else if (int.TryParse(box.Text, out var number) && number >= min && number <= max)
                    set(number);
                else
                    return TF("Invalid value for {0}.", "{0}的值无效。", label);
                return null;
            });
            return box;
        }

        ComboBox Options(string label, string? value, Action<string> set, params string[] choices)
        {
            var list = choices.ToList();
            if (!string.IsNullOrEmpty(value) && !list.Contains(value))
                list.Insert(0, value);
            var index = value != null ? list.IndexOf(value) : 0;
            if (index < 0)
                index = 0;
            var box = Choice(label, list, index);
            box.Header = label;
            box.HorizontalAlignment = HorizontalAlignment.Stretch;
            fields.Children.Add(box);
            apply.Add(() =>
            {
                if (box.Visibility == Visibility.Visible)
                    set(box.SelectedItem as string ?? "");
                return null;
            });
            return box;
        }

        ComboBox EditableOptions(string label, string? value, Action<string> set, params string[] choices)
        {
            var list = choices.ToList();
            if (!string.IsNullOrEmpty(value) && !list.Contains(value))
                list.Insert(0, value);
            var box = new ComboBox
            {
                Header = label,
                IsEditable = true,
                ItemsSource = list,
                Text = value ?? (list.FirstOrDefault() ?? string.Empty),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(box, label);
            fields.Children.Add(box);
            apply.Add(() =>
            {
                if (box.Visibility == Visibility.Visible)
                    set(box.Text.Trim());
                return null;
            });
            return box;
        }

        CheckBox Toggle(string label, bool? value, Action<bool?> set, bool threeState = false)
        {
            var box = new CheckBox { Content = label, IsThreeState = threeState, IsChecked = value };
            fields.Children.Add(box);
            apply.Add(() =>
            {
                if (box.Visibility == Visibility.Visible)
                    set(box.IsChecked);
                return null;
            });
            return box;
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

        ComboBox? vlessFlowBox = null;
        switch (server)
        {
            case ShadowsocksRServer ssr:
                Options(T("Encryption", "加密"), ssr.EncryptMethod, s => ssr.EncryptMethod = s, Constants.SsrEncryptMethods.ToArray());
                EditableOptions(T("Protocol", "协议"), ssr.Protocol, s => ssr.Protocol = s, Constants.SsrProtocols.ToArray());
                Field(T("Protocol parameter", "协议参数"), ssr.ProtocolParam, s => ssr.ProtocolParam = s);
                EditableOptions(T("Obfuscation", "混淆"), ssr.OBFS, s => ssr.OBFS = s, Constants.SsrObfs.ToArray());
                Field(T("Obfuscation parameter", "混淆参数"), ssr.OBFSParam, s => ssr.OBFSParam = s);
                break;
            case ShadowsocksServer:
                Options(T("Encryption", "加密"), extra.SsMethod, s => extra.SsMethod = s, Constants.SsCiphers.ToArray());
                var originalPlugin = extra.SsPlugin ?? "";
                var originalPluginOptions = extra.SsPluginOptions ?? "";
                var ssPlugin = Options(T("Plugin", "插件"), originalPlugin, s =>
                {
                    if (s == originalPlugin) return;
                    extra.SsPlugin = s;
                    extra.SsPluginOptions = "";
                    if (s != "v2ray-plugin") server.AllowInsecure = null;
                    ServerEditorHelper.ResetShadowsocksTransport(server);
                }, "", "obfs-local", "v2ray-plugin");
                var ssPluginOptions = Field(T("Plugin options", "插件参数"), originalPluginOptions, s =>
                {
                    var selectedPlugin = ssPlugin.SelectedItem as string ?? "";
                    if (selectedPlugin == originalPlugin && s == originalPluginOptions.Trim()) return;
                    extra.SsPluginOptions = s;
                    ServerEditorHelper.ResetShadowsocksTransport(server);
                });
                var ssAllowInsecure = Toggle(T("Allow insecure", "允许不安全连接"), server.AllowInsecure, b => server.AllowInsecure = b, threeState: true);
                Toggle("UDP over TCP", extra.Uot, b => extra.Uot = b);
                void RefreshSsPlugin()
                {
                    var selected = ssPlugin.SelectedItem as string ?? "";
                    Show(ssPluginOptions, selected.Length > 0);
                    Show(ssAllowInsecure, selected == "v2ray-plugin");
                    ssPluginOptions.PlaceholderText = selected switch
                    {
                        "obfs-local" => "obfs=http;obfs-host=example.com",
                        "v2ray-plugin" => "mode=websocket;host=example.com;path=/;tls",
                        _ => ""
                    };
                }
                ssPlugin.SelectionChanged += (_, _) =>
                {
                    var selected = ssPlugin.SelectedItem as string ?? "";
                    ssPluginOptions.Text = selected == originalPlugin ? originalPluginOptions : "";
                    RefreshSsPlugin();
                };
                RefreshSsPlugin();
                break;
            case WireGuardServer:
                Field(T("Peer public key", "对端公钥"),
                    string.IsNullOrWhiteSpace(extra.WgPublicKey) ? server.PublicKey : extra.WgPublicKey,
                    s => extra.WgPublicKey = s);
                Secret(T("Preshared key", "预共享密钥"), extra.WgPresharedKey, s => extra.WgPresharedKey = s);
                Field(T("Local IP / CIDR", "本地 IP / CIDR"), extra.WgInterfaceAddress, s => extra.WgInterfaceAddress = s);
                Field(T("Reserved bytes", "保留字节"), extra.WgReserved, s => extra.WgReserved = s);
                Number("MTU", extra.WgMtu, n => extra.WgMtu = n, 1);
                break;
        }

        switch (protocol)
        {
            case EConfigType.VMess:
                Number("Alter ID", extra.AlterId ?? 0, n => extra.AlterId = n ?? 0, max: ushort.MaxValue);
                Options(T("Encryption", "加密"), extra.VmessSecurity, s => extra.VmessSecurity = s, Constants.VmessSecurities.ToArray());
                break;
            case EConfigType.VLESS:
                vlessFlowBox = Options(T("Flow", "流控"), extra.Flow, s => extra.Flow = s, Constants.Flow.ToArray());
                Field(T("Encryption", "加密"), string.IsNullOrEmpty(extra.VlessEncryption) ? Constants.None : extra.VlessEncryption,
                    s => extra.VlessEncryption = string.IsNullOrWhiteSpace(s) ? Constants.None : s);
                break;
            case EConfigType.Trojan:
                if (extra.Flow is { Length: > 0 } legacyFlow && legacyFlow != Constants.None)
                    Options(T("Legacy flow", "旧版流控"), legacyFlow, s => extra.Flow = s, "", legacyFlow);
                break;
            case EConfigType.SOCKS:
                Toggle("UDP over TCP", extra.Uot, b => extra.Uot = b);
                break;
            case EConfigType.HTTP:
                Field(T("HTTP headers", "HTTP 请求头"), extra.HttpHeaders, s => extra.HttpHeaders = s, multiline: true);
                break;
            case EConfigType.TUIC:
                Options(T("Congestion control", "拥塞控制"), extra.CongestionControl, s => extra.CongestionControl = s, "bbr", "cubic", "new_reno");
                Options(T("UDP relay mode", "UDP 转发模式"), extra.TuicUdpRelayMode, s => extra.TuicUdpRelayMode = s, Constants.TuicUdpRelayModes.ToArray());
                Toggle(T("Reduce RTT", "0-RTT 握手"), extra.TuicReduceRtt, b => extra.TuicReduceRtt = b);
                Toggle(T("Disable SNI", "禁用 SNI"), extra.TuicDisableSni, b => extra.TuicDisableSni = b);
                break;
            case EConfigType.AnyTLS:
                Number(T("Idle check interval (s)", "空闲检查间隔（秒）"), extra.AnyTlsIdleSessionCheckIntervalSeconds,
                    n => extra.AnyTlsIdleSessionCheckIntervalSeconds = n);
                Number(T("Idle session timeout (s)", "空闲会话超时（秒）"), extra.AnyTlsIdleSessionTimeoutSeconds,
                    n => extra.AnyTlsIdleSessionTimeoutSeconds = n);
                Number(T("Min idle sessions", "最小空闲会话数"), extra.AnyTlsMinIdleSession,
                    n => extra.AnyTlsMinIdleSession = n);
                break;
            case EConfigType.Hysteria2:
                var initialHy2Obfs = string.IsNullOrEmpty(extra.SalamanderPass) ? Constants.None : extra.Obfs;
                var hy2Obfs = Options(T("Obfuscation", "混淆"), initialHy2Obfs, s =>
                {
                    extra.Obfs = s;
                    if (s == Constants.None) extra.SalamanderPass = "";
                    if (s != "gecko")
                    {
                        extra.GeckoMinPacketSize = "";
                        extra.GeckoMaxPacketSize = "";
                    }
                }, Constants.None, "salamander", "gecko");
                var hy2ObfsPassword = Secret(T("Obfuscation password", "混淆密码"), extra.SalamanderPass, s => extra.SalamanderPass = s);
                var hy2GeckoMin = Field(T("Gecko min packet size", "Gecko 最小包长"), extra.GeckoMinPacketSize, s => extra.GeckoMinPacketSize = s);
                var hy2GeckoMax = Field(T("Gecko max packet size", "Gecko 最大包长"), extra.GeckoMaxPacketSize, s => extra.GeckoMaxPacketSize = s);
                var hy2Ports = Field(T("Port hopping range", "端口跳跃范围"), extra.Ports, s => extra.Ports = s);
                var hy2HopInterval = Field(T("Hop interval (s)", "跳跃间隔（秒）"), extra.HopInterval, s => extra.HopInterval = s);
                Number(T("Upload (Mbps)", "上传（Mbps）"), extra.UpMbps, n => extra.UpMbps = n);
                Number(T("Download (Mbps)", "下载（Mbps）"), extra.DownMbps, n => extra.DownMbps = n);
                void RefreshHy2()
                {
                    var obfsSelected = hy2Obfs.SelectedItem as string ?? Constants.None;
                    Show(hy2ObfsPassword, obfsSelected != Constants.None);
                    Show(hy2GeckoMin, obfsSelected == "gecko");
                    Show(hy2GeckoMax, obfsSelected == "gecko");
                    Show(hy2HopInterval, !string.IsNullOrWhiteSpace(hy2Ports.Text));
                }
                hy2Obfs.SelectionChanged += (_, _) => RefreshHy2();
                hy2Ports.TextChanged += (_, _) => RefreshHy2();
                RefreshHy2();
                break;
            case EConfigType.Naive:
                var naiveQuic = Toggle("QUIC", extra.NaiveQuic, b =>
                {
                    extra.NaiveQuic = b;
                    if (b != true) extra.CongestionControl = "";
                });
                var naiveCongestion = Options(T("QUIC congestion control", "QUIC 拥塞控制"), extra.CongestionControl,
                    s => extra.CongestionControl = s, Constants.NaiveQuicCongestionControls.ToArray());
                Number(T("Tunnel connections", "隧道连接数"), extra.InsecureConcurrency, n => extra.InsecureConcurrency = n);
                Toggle("UDP over TCP", extra.Uot, b => extra.Uot = b);
                void RefreshNaive() => Show(naiveCongestion, naiveQuic.IsChecked == true);
                naiveQuic.Checked += (_, _) => RefreshNaive();
                naiveQuic.Unchecked += (_, _) => RefreshNaive();
                RefreshNaive();
                break;
            case EConfigType.MASQUE:
                Field(T("URI template", "URI 模板"), extra.MasquePath, s => extra.MasquePath = s);
                Field(T("HTTP headers", "HTTP 请求头"), extra.MasqueHeaders, s => extra.MasqueHeaders = s, multiline: true);
                break;
        }

        ComboBox? transportBox = null;
        if (protocol is EConfigType.VLESS or EConfigType.VMess or EConfigType.Trojan)
        {
            var initialNetwork = string.IsNullOrWhiteSpace(server.Network) ? Constants.DefaultNetwork : server.Network.Trim().ToLowerInvariant();
            if (initialNetwork == "raw") initialNetwork = "tcp";
            if (initialNetwork == "http") initialNetwork = "h2";
            var networks = protocol == EConfigType.VMess
                ? new[] { "tcp", "ws", "grpc", "httpupgrade", "h2", "xhttp", "quic", "kcp" }
                : new[] { "tcp", "ws", "grpc", "httpupgrade", "h2", "xhttp", "quic" };
            transportBox = Options(T("Transport", "传输"), initialNetwork, s => server.Network = s, networks);
            var hostBox = Field(T("Request host", "请求主机"), server.RequestHost, s => server.RequestHost = s);
            var pathBox = Field(T("Path / service name", "路径 / 服务名"), server.Path, s => server.Path = s);
            var headerBox = Field(T("Header type", "伪装类型"), server.HeaderType, s => server.HeaderType = s);
            if (protocol is EConfigType.VLESS or EConfigType.VMess)
                Toggle("Mux", server.MuxEnabled, b => server.MuxEnabled = b);
            var extraBox = Field(T("Transport extensions", "传输扩展"), server.Extra, s => server.Extra = s, multiline: true);
            void RefreshTransport()
            {
                var net = transportBox.SelectedItem as string ?? Constants.DefaultNetwork;
                Show(hostBox, net is "tcp" or "ws" or "httpupgrade" or "xhttp" or "h2" or "quic" or "kcp" || !string.IsNullOrEmpty(server.RequestHost));
                Show(pathBox, net is "tcp" or "ws" or "httpupgrade" or "xhttp" or "h2" or "grpc" or "kcp" or "quic" || !string.IsNullOrEmpty(server.Path));
                Show(headerBox, net is "tcp" or "kcp" or "xhttp" or "grpc" or "quic" || !string.IsNullOrEmpty(server.HeaderType));
                Show(extraBox, net == "xhttp" || !string.IsNullOrEmpty(server.Extra));
            }
            transportBox.SelectionChanged += (_, _) => RefreshTransport();
            RefreshTransport();
        }

        if (protocol is not (EConfigType.WireGuard or EConfigType.ShadowsocksR or EConfigType.SOCKS or EConfigType.Shadowsocks))
        {
            var initialSecurity = server.StreamSecurity == Constants.StreamSecurityReality
                ? Constants.StreamSecurityReality
                : server.StreamSecurity == Constants.StreamSecurity ? Constants.StreamSecurity : Constants.None;
            var fixedTls = protocol is EConfigType.Hysteria2 or EConfigType.TUIC or EConfigType.AnyTLS or EConfigType.Naive or EConfigType.MASQUE;
            var supportsReality = protocol is EConfigType.VLESS or EConfigType.VMess or EConfigType.Trojan;
            var naive = protocol == EConfigType.Naive;

            ComboBox? securityBox = null;
            if (fixedTls)
                server.StreamSecurity = Constants.StreamSecurity;
            else
            {
                var secChoices = supportsReality && ServerValidator.SupportsReality(protocol, transportBox?.SelectedItem as string)
                    ? new[] { Constants.None, Constants.StreamSecurity, Constants.StreamSecurityReality }
                    : new[] { Constants.None, Constants.StreamSecurity };
                securityBox = Options(T("TLS security", "TLS 安全"), initialSecurity, s => server.StreamSecurity = s, secChoices);
            }

            var sniBox = Field("SNI", server.Sni, s => server.Sni = s);
            ComboBox? alpnBox = naive ? null : Options("ALPN", server.Alpn, s => server.Alpn = s, Constants.Alpn.ToArray());
            ComboBox? fpBox = naive ? null : Options(T("Fingerprint", "指纹"), server.Fingerprint, s => server.Fingerprint = s, Constants.Fingerprints.ToArray());
            CheckBox? insecureBox = naive ? null : Toggle(T("Allow insecure", "允许不安全连接"), server.AllowInsecure, b => server.AllowInsecure = b, threeState: true);
            var certBox = Field(T("Certificate", "证书"), server.Cert, s =>
            {
                server.Cert = s;
                if (protocol == EConfigType.Hysteria2 && s.Length > 0) server.CertSha = "";
            }, multiline: true);
            TextBox? certShaBox = naive || string.IsNullOrWhiteSpace(server.CertSha)
                ? null
                : Field(T("Certificate SHA", "证书 SHA"), server.CertSha, s => server.CertSha = s);

            TextBox? pubKeyBox = null;
            TextBox? shortIdBox = null;
            TextBox? spiderXBox = null;
            TextBox? mldsaBox = null;
            TextBox? echBox = null;
            ComboBox? echQueryBox = null;
            if (supportsReality)
            {
                pubKeyBox = Field(T("Reality public key", "Reality 公钥"), server.PublicKey, s => server.PublicKey = s);
                shortIdBox = Field(T("Short ID", "短 ID"), server.ShortId, s => server.ShortId = s);
                spiderXBox = Field("SpiderX", server.SpiderX, s => server.SpiderX = s);
                mldsaBox = Field("ML-DSA-65", server.Mldsa65Verify, s => server.Mldsa65Verify = s);
                echBox = Field("ECH", server.EchConfigList, s => server.EchConfigList = s);
                echQueryBox = Options("ECH Force Query", server.EchForceQuery, s => server.EchForceQuery = s, Constants.EchForceQuerys.ToArray());
            }

            void RefreshSecurity()
            {
                var sec = fixedTls ? Constants.StreamSecurity : (securityBox?.SelectedItem as string ?? Constants.None);
                var net = transportBox?.SelectedItem as string ?? Constants.DefaultNetwork;
                var secure = sec is Constants.StreamSecurity or Constants.StreamSecurityReality;
                var tlsOnly = sec == Constants.StreamSecurity;
                var reality = sec == Constants.StreamSecurityReality;
                var xhttp = net == "xhttp";

                Show(sniBox, secure);
                if (alpnBox != null) Show(alpnBox, secure);
                if (fpBox != null) Show(fpBox, secure);
                if (insecureBox != null) Show(insecureBox, tlsOnly);
                Show(certBox, tlsOnly);
                if (certShaBox != null) Show(certShaBox, tlsOnly);
                if (pubKeyBox != null) Show(pubKeyBox, reality);
                if (shortIdBox != null) Show(shortIdBox, reality);
                if (spiderXBox != null) Show(spiderXBox, reality && (xhttp || !string.IsNullOrEmpty(server.SpiderX)));
                if (mldsaBox != null) Show(mldsaBox, reality && (xhttp || !string.IsNullOrEmpty(server.Mldsa65Verify)));
                if (echBox != null) Show(echBox, tlsOnly && (xhttp || !string.IsNullOrEmpty(server.EchConfigList)));
                if (echQueryBox != null) Show(echQueryBox, tlsOnly && (xhttp || !string.IsNullOrEmpty(server.EchForceQuery)));
                if (vlessFlowBox != null) Show(vlessFlowBox, secure && net is "tcp" or "raw");
            }

            if (securityBox != null)
            {
                securityBox.SelectionChanged += (_, _) => RefreshSecurity();
            }
            if (transportBox != null && securityBox != null)
            {
                transportBox.SelectionChanged += (_, _) =>
                {
                    var currentSec = securityBox.SelectedItem as string ?? Constants.None;
                    var secChoices = ServerValidator.SupportsReality(protocol, transportBox.SelectedItem as string)
                        ? new[] { Constants.None, Constants.StreamSecurity, Constants.StreamSecurityReality }
                        : new[] { Constants.None, Constants.StreamSecurity };
                    securityBox.ItemsSource = secChoices;
                    securityBox.SelectedIndex = Array.IndexOf(secChoices, currentSec) is var i and >= 0 ? i : 0;
                    RefreshSecurity();
                };
            }
            RefreshSecurity();
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

            ServerEditorHelper.PrepareForSave(server);
            return ServerEditorHelper.ValidateForSave(server, util) is { } validationError
                ? i18N.Translate(validationError)
                : null;
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
