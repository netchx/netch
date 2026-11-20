using System.Globalization;
using System.Net;
using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Netch.App.Helpers;
using Netch.App.Services;
using Netch.Controllers;
using Netch.Enums;
using Netch.Utils;
using static Netch.App.Helpers.Ui;

namespace Netch.App.Pages;

public sealed partial class SettingsPage : Page
{
    public SettingsPage()
    {
        InitializeComponent();
        var settings = Global.Settings;
        var theme = Choice(T("Appearance", "外观"), [T("Follow system", "跟随系统"), T("Light", "浅色"), T("Dark", "深色")], (int)settings.Theme);
        theme.SelectionChanged += async (_, _) =>
        {
            settings.Theme = (AppTheme)theme.SelectedIndex;
            App.ApplyTheme(settings.Theme);
            await Configuration.SaveAsync();
        };

        var close = Choice(T("When closing the window", "关闭窗口时"), [T("Minimize to tray", "最小化到托盘"), T("Exit", "退出")], settings.ExitWhenClosed ? 1 : 0);
        close.SelectionChanged += async (_, _) => { settings.ExitWhenClosed = close.SelectedIndex == 1; await Configuration.SaveAsync(); };

        var languages = i18N.GetTranslateList().ToArray();
        var language = Choice(T("Language", "语言"), languages.Select(s => s == "System" ? T("Follow system", "跟随系统")
            : CultureInfo.GetCultureInfo(s).NativeName), Math.Max(0, Array.IndexOf(languages, settings.Language)));
        language.SelectionChanged += async (_, _) =>
        {
            settings.Language = languages[language.SelectedIndex];
            await Configuration.SaveAsync();
            i18N.Load(settings.Language);
            Netch.Services.ModeService.Instance.Sort();
            App.MainWindow?.ReloadLanguage();
        };

        var channels = new[] { T("Direct", "直连"), T("Current proxy", "通过当前代理"), T("GitHub mirror", "GitHub 镜像") };
        var channel = Choice(T("Download channel", "下载通道"), channels, (int)settings.DownloadChannel);
        channel.SelectionChanged += async (_, _) =>
        {
            var selected = (DownloadChannel)channel.SelectedIndex;
            if (selected == settings.DownloadChannel)
                return;
            if (selected == DownloadChannel.GitHubMirror)
            {
                var url = Input(T("HTTPS mirror prefix", "HTTPS 镜像前缀"), settings.GitHubMirror);
                if (!await ShowAsync(XamlRoot, channels[channel.SelectedIndex], url, T("Save", "保存"), () =>
                {
                    if (!Uri.TryCreate(url.Text.Trim(), UriKind.Absolute, out var uri) || uri.Scheme != "https")
                        return T("Enter a valid HTTPS URL.", "请输入有效的 HTTPS 地址。");
                    settings.GitHubMirror = uri.AbsoluteUri.TrimEnd('/');
                    return null;
                }))
                {
                    channel.SelectedIndex = (int)settings.DownloadChannel;
                    return;
                }
            }
            settings.DownloadChannel = selected;
            await Configuration.SaveAsync();
        };

        var port = Input(T("Mixed port", "混合端口"), settings.Socks5LocalPort.ToString(), search: true);
        port.Width = 66;
        port.MinWidth = 0;
        port.LostFocus += async (_, _) =>
        {
            if (ushort.TryParse(port.Text, out var value) && value > 0 && value != settings.HTTPLocalPort)
            {
                settings.Socks5LocalPort = value;
                await Configuration.SaveAsync();
            }
            port.Text = settings.Socks5LocalPort.ToString();
        };
        var lan = SettingToggle(settings.LocalAddress == "0.0.0.0", b => settings.LocalAddress = b ? "0.0.0.0" : "127.0.0.1");
        var startup = SettingToggle(settings.RunAtStartup, b =>
        {
            var old = settings.RunAtStartup;
            settings.RunAtStartup = b;
            try { Utils.Utils.RegisterNetchStartupItem(); }
            catch { settings.RunAtStartup = old; throw; }
        });

        var common = Stack(
            Heading(T("General", "常规")),
            Setting("◐", T("Appearance", "外观"), theme),
            Setting("⏻", T("Start at sign-in", "开机启动"), startup, T("Run as administrator", "以管理员身份启动")),
            Setting("▣", T("When closing the window", "关闭窗口时"), close),
            Setting("文", T("Language", "语言"), language),
            Heading(T("Network", "网络")),
            Setting("⇄", T("Local mixed port", "本地混合端口"), Row(port, lan), T("Allow LAN connections", "允许局域网连接")),
            Setting("◎", T("Latency test · STUN", "测速方式 · STUN"), AsyncButton(settings.ServerTCPing ? "TCPing ›" : "ICMPing ›", EditProbeAsync)),
            Setting("◇", "DNS", AsyncButton("›", EditDnsAsync), T("AioDNS · Outbound DNS", "AioDNS · 出站 DNS")));

        var advanced = Stack(
            Heading(T("Cores", "内核")),
            Setting("⚙", "Xray / sing-box", AsyncButton("›", EditCoreAsync), T("Fingerprint, fragmentation, Mux, KCP", "指纹、分片、Mux、KCP")),
            Setting("⚡", T("Hysteria bandwidth", "Hysteria 带宽"), AsyncButton("›", EditHysteriaAsync)),
            Heading(T("Updates and downloads", "更新与下载"), 4),
            Setting("⤓", T("Download channel", "下载通道"), channel),
            Setting("⟳", T("Check for updates at launch", "启动时检查更新"), SettingToggle(settings.CheckUpdateWhenOpened, v => settings.CheckUpdateWhenOpened = v),
                T("App, cores and mode rules", "程序、核心和模式规则")),
            Setting("β", T("Include prereleases", "包含预发布版本"), SettingToggle(settings.CheckBetaUpdate, v => settings.CheckBetaUpdate = v)),
            Heading(T("Maintenance", "维护"), 3),
            Setting("🧹", T("Flush DNS · Firewall rules", "清理 DNS · 防火墙规则"), AsyncButton(T("Run", "执行"), ClearNetworkAsync)),
            Setting("⚠", T("Uninstall NF driver service", "卸载 NF 驱动服务"), AsyncButton(T("Uninstall", "卸载"), UninstallAsync)));

        common.Spacing = advanced.Spacing = 3.5;
        Content = Ui.Page(Body(Text(T("Settings", "设置"), 24, true),
            Scroll(TwoColumns(common, advanced, new GridLength(1, GridUnitType.Star), 14))));
    }

    private static TextBlock Heading(string title, double topMargin = 5)
    {
        var text = Text(title, strong: true);
        text.Margin = new Thickness(0, topMargin, 0, 2);
        return text;
    }

    private static SettingsCard Setting(string icon, string title, UIElement content, string? description = null)
    {
        var label = description == null ? (UIElement)Text(title) : new StackPanel { Children = { Text(title), Caption(description) } };
        var glyph = Text(icon, 15, secondary: true);
        glyph.Width = 19;
        var text = TwoColumns(glyph, label, GridLength.Auto, 10);
        ((FrameworkElement)content).VerticalAlignment = VerticalAlignment.Center;
        var layout = Split(text, content);
        var card = new SettingsCard
        {
            Content = layout,
            MinHeight = description != null ? 58 : content switch
            {
                ToggleSwitch => 43,
                Button { Content: "›" } => 40,
                _ => 52
            },
            Padding = new Thickness(12, 4, 12, 4), HorizontalContentAlignment = HorizontalAlignment.Stretch
        };
        card.Resources["SettingsCardWrapThreshold"] = 0d;
        card.Resources["SettingsCardWrapNoIconThreshold"] = 0d;
        card.SizeChanged += (_, e) => layout.Width = Math.Max(0, e.NewSize.Width - 26);
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(card, title);
        return card;
    }

    private ToggleSwitch SettingToggle(bool isOn, Action<bool> apply)
    {
        var toggle = CompactToggle(new ToggleSwitch { IsOn = isOn });
        toggle.Margin = new Thickness(0, 0, 2, 0);
        var restoring = false;
        toggle.Toggled += async (_, _) =>
        {
            if (restoring)
                return;
            try
            {
                apply(toggle.IsOn);
                await Configuration.SaveAsync();
            }
            catch (Exception e)
            {
                restoring = true;
                toggle.IsOn = !toggle.IsOn;
                restoring = false;
                await ShowAsync(XamlRoot, T("Setting could not be changed", "无法更改设置"), Text(e.Message));
            }
        };
        return toggle;
    }

    private async Task EditProbeAsync()
    {
        var s = Global.Settings;
        var method = Choice(T("Latency test", "测速方式"), ["TCPing", "ICMPing"], s.ServerTCPing ? 0 : 1);
        var stun = Input(T("STUN host", "STUN 主机"), s.STUN_Server);
        var port = Input(T("STUN port", "STUN 端口"), s.STUN_Server_Port.ToString());
        if (await ShowAsync(XamlRoot, T("Latency test · STUN", "测速方式 · STUN"), Stack(method, stun, port), T("Save", "保存"), () =>
        {
            if (Uri.CheckHostName(stun.Text.Trim()) == UriHostNameType.Unknown || !ushort.TryParse(port.Text, out var value) || value == 0)
                return T("Enter a STUN host and a valid port.", "请输入 STUN 主机及有效端口。");
            s.ServerTCPing = method.SelectedIndex == 0;
            s.STUN_Server = stun.Text.Trim();
            s.STUN_Server_Port = value;
            return null;
        }))
        {
            await Configuration.SaveAsync();
            App.MainWindow?.Navigate("Settings", force: true);
        }
    }

    private async Task EditDnsAsync()
    {
        var settings = Global.Settings;
        var china = Input(T("AioDNS domestic upstream", "AioDNS 国内上游"), settings.AioDNS.ChinaDNS);
        var other = Input(T("AioDNS international upstream", "AioDNS 国外上游"), settings.AioDNS.OtherDNS);
        var outbound = Input(T("Outbound DNS", "出站 DNS"), settings.OutboundDNS);
        if (await ShowAsync(XamlRoot, "DNS", Stack(china, other, outbound), T("Save", "保存"), () =>
        {
            if (china.Text.Trim().Length == 0 || other.Text.Trim().Length == 0 || !IPAddress.TryParse(outbound.Text.Trim(), out _))
                return T("Enter upstream DNS addresses and an outbound IP.", "请输入上游 DNS 地址和出站 DNS IP。");
            settings.AioDNS.ChinaDNS = DnsUtils.AppendPort(china.Text.Trim());
            settings.AioDNS.OtherDNS = DnsUtils.AppendPort(other.Text.Trim());
            settings.OutboundDNS = outbound.Text.Trim();
            return null;
        }))
            await Configuration.SaveAsync();
    }

    private async Task EditCoreAsync()
    {
        var core = Global.Settings.V2RayConfig;
        var basic = core.CoreBasicItem;
        var kcp = core.KcpItem;
        var fingerprint = Input(T("TLS fingerprint", "TLS 指纹"), basic.DefFingerprint);
        var fragment = new CheckBox { Content = T("Fragment initial packets", "分片"), IsChecked = basic.EnableFragment };
        var mux = new CheckBox { Content = "Mux", IsChecked = basic.MuxEnabled };
        var insecure = new CheckBox { Content = T("Allow insecure connections", "允许不安全连接"), IsChecked = basic.DefAllowInsecure };
        var congestion = new CheckBox { Content = T("KCP congestion control", "KCP 拥塞控制"), IsChecked = kcp.Congestion };
        var fields = new (string Label, int Value, Action<int> Set)[]
        {
            ("Mux concurrency", core.Mux4RayItem.Concurrency ?? 8, n => core.Mux4RayItem.Concurrency = n),
            ("KCP MTU", kcp.Mtu, n => kcp.Mtu = n), ("KCP TTI", kcp.Tti, n => kcp.Tti = n),
            ("KCP uplink", kcp.UplinkCapacity, n => kcp.UplinkCapacity = n), ("KCP downlink", kcp.DownlinkCapacity, n => kcp.DownlinkCapacity = n),
            ("KCP read buffer", kcp.ReadBufferSize, n => kcp.ReadBufferSize = n), ("KCP write buffer", kcp.WriteBufferSize, n => kcp.WriteBufferSize = n)
        };
        var editor = Stack(fingerprint, fragment, mux, insecure, congestion);
        await SaveNumbersAsync("Xray / sing-box", editor, fields, () =>
        {
            basic.DefFingerprint = fingerprint.Text.Trim();
            basic.EnableFragment = fragment.IsChecked == true;
            basic.MuxEnabled = mux.IsChecked == true;
            basic.DefAllowInsecure = insecure.IsChecked == true;
            kcp.Congestion = congestion.IsChecked == true;
        });
    }

    private Task EditHysteriaAsync()
    {
        var h = Global.Settings.V2RayConfig.HysteriaItem;
        return SaveNumbersAsync("Hysteria", Stack(),
            [(T("Upload (Mbps)", "上传（Mbps）"), h.UpMbps, n => h.UpMbps = n), (T("Download (Mbps)", "下载（Mbps）"), h.DownMbps, n => h.DownMbps = n),
                (T("Hop interval (s)", "跳跃间隔（秒）"), h.HopInterval, n => h.HopInterval = n)]);
    }

    private async Task SaveNumbersAsync(string title, StackPanel editor, (string Label, int Value, Action<int> Set)[] fields, Action? apply = null)
    {
        var boxes = fields.Select(field => Input(field.Label, field.Value.ToString())).ToArray();
        foreach (var box in boxes)
            editor.Children.Add(box);
        if (await ShowAsync(XamlRoot, title, editor, T("Save", "保存"), () =>
        {
            var values = new int[boxes.Length];
            for (var i = 0; i < boxes.Length; i++)
                if (!int.TryParse(boxes[i].Text, out values[i]) || values[i] < 0)
                    return T("Enter nonnegative integers.", "请输入非负整数。");
            for (var i = 0; i < fields.Length; i++)
                fields[i].Set(values[i]);
            apply?.Invoke();
            return null;
        }))
            await Configuration.SaveAsync();
    }

    private async Task ClearNetworkAsync()
    {
        var title = T("Flush DNS · Firewall rules", "清理 DNS · 防火墙规则");
        if (!Session.Current.IsIdle)
            throw new InvalidOperationException(T("Disconnect before maintenance.", "请先断开连接，再执行维护操作。"));
        if (!await ShowAsync(XamlRoot, title, Text(T("Flush the DNS cache and remove Netch firewall rules?", "清理 DNS 缓存并移除 Netch 防火墙规则？")), T("Run", "执行")))
            return;
        await Task.Run(() => { NativeMethods.RefreshDNSCache(); Firewall.RemoveNetchFwRules(); });
        await ShowAsync(XamlRoot, title, Text(T("Maintenance completed.", "维护已完成。")));
    }

    private async Task UninstallAsync()
    {
        var title = T("Uninstall NF driver service", "卸载 NF 驱动服务");
        if (!Session.Current.IsIdle)
            throw new InvalidOperationException(T("Disconnect before maintenance.", "请先断开连接，再执行维护操作。"));
        if (!await ShowAsync(XamlRoot, title, Text(T("Remove the netfilter2 service?", "移除 netfilter2 服务？")), T("Uninstall", "卸载")))
            return;
        var success = await Task.Run(NFController.UninstallDriver);
        await ShowAsync(XamlRoot, title, Text(success ? T("Service removed.", "服务已卸载。") : T("Uninstall failed.", "卸载失败。")));
    }
}
