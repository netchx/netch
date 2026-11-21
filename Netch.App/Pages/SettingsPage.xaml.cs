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
    private readonly InfoBar _settingsError = new()
    {
        Severity = InfoBarSeverity.Error,
        Title = T("Setting could not be changed", "无法更改设置")
    };

    public SettingsPage()
    {
        InitializeComponent();
        var settings = Global.Settings;
        var theme = Choice(T("Appearance", "外观"), [T("Follow system", "跟随系统"), T("Light", "浅色"), T("Dark", "深色")], (int)settings.Theme);
        BindSelection(theme, index => (AppTheme)index, () => settings.Theme, value => settings.Theme = value,
            () => App.ApplyTheme(settings.Theme));

        var close = Choice(T("When closing the window", "关闭窗口时"), [T("Minimize to tray", "最小化到托盘"), T("Exit", "退出")], settings.ExitWhenClosed ? 1 : 0);
        BindSelection(close, index => index == 1, () => settings.ExitWhenClosed, value => settings.ExitWhenClosed = value);

        var languages = i18N.GetTranslateList().ToArray();
        var language = Choice(T("Language", "语言"), languages.Select(s => s == "System" ? T("Follow system", "跟随系统")
            : CultureInfo.GetCultureInfo(s).NativeName), Math.Max(0, Array.IndexOf(languages, settings.Language)));
        BindSelection(language, index => languages[index], () => settings.Language, value => settings.Language = value, () =>
        {
            i18N.Load(settings.Language);
            Netch.Services.ModeService.Instance.Sort();
            App.MainWindow?.ReloadLanguage();
        });

        var channels = new[] { T("Direct", "直连"), T("Current proxy", "通过当前代理"), T("GitHub mirror", "GitHub 镜像") };
        var channel = Choice(T("Download channel", "下载通道"), channels, (int)settings.DownloadChannel);
        channel.SelectionChanged += async (_, _) =>
        {
            if (!channel.IsEnabled || channel.SelectedIndex < 0)
                return;
            var selected = (DownloadChannel)channel.SelectedIndex;
            if (selected == settings.DownloadChannel)
                return;
            var previous = (settings.DownloadChannel, settings.GitHubMirror);
            var prefix = settings.GitHubMirror;
            channel.IsEnabled = false;
            try
            {
                if (selected == DownloadChannel.GitHubMirror)
                {
                    var url = Input(T("HTTPS mirror prefix", "HTTPS 镜像前缀"), prefix);
                    if (!await ShowAsync(XamlRoot, channels[(int)selected], url, T("Save", "保存"), () =>
                    {
                        if (!Uri.TryCreate(url.Text.Trim(), UriKind.Absolute, out var uri) || uri.Scheme != "https")
                            return T("Enter a valid HTTPS URL.", "请输入有效的 HTTPS 地址。");
                        prefix = uri.AbsoluteUri.TrimEnd('/');
                        return null;
                    }))
                        return;
                }
                await SaveSettingAsync(
                    () => (settings.DownloadChannel, settings.GitHubMirror) = (selected, prefix),
                    () => (settings.DownloadChannel, settings.GitHubMirror) = previous);
            }
            finally
            {
                channel.SelectedIndex = (int)settings.DownloadChannel;
                channel.IsEnabled = true;
            }
        };

        var port = Input(T("Mixed port", "混合端口"), settings.Socks5LocalPort.ToString(), search: true);
        port.Width = 66;
        port.MinWidth = 0;
        port.LostFocus += async (_, _) =>
        {
            if (!port.IsEnabled)
                return;
            if (!ushort.TryParse(port.Text, out var value) || value == 0)
            {
                ReportSettingError(T("Enter a port between 1 and 65535.", "请输入 1 至 65535 之间的端口。"));
                return;
            }
            _settingsError.IsOpen = false;
            var previous = settings.Socks5LocalPort;
            if (value != previous)
            {
                port.IsEnabled = false;
                try
                {
                    await SaveSettingAsync(() => settings.Socks5LocalPort = value, () => settings.Socks5LocalPort = previous);
                }
                finally { port.IsEnabled = true; }
            }
            port.Text = settings.Socks5LocalPort.ToString();
        };
        var lan = SettingToggle(settings.LocalAddress, b => b ? "0.0.0.0" : "127.0.0.1", address => settings.LocalAddress = address);
        var startup = SettingToggle(settings.RunAtStartup, b =>
        {
            settings.RunAtStartup = b;
            Utils.Utils.RegisterNetchStartupItem();
        });

        var common = Stack(
            Heading(T("General", "常规")),
            Setting("◐", T("Appearance", "外观"), theme),
            Setting("⏻", T("Start at sign-in", "开机启动"), startup, T("Run as administrator", "以管理员身份启动")),
            Setting("▶", T("Start automatically at launch", "启动后自动开启"),
                SettingToggle(settings.StartWhenOpened, v => settings.StartWhenOpened = v),
                T("Connect the startup or last used scene", "自动连接启动场景或最近使用的场景")),
            Setting("▾", T("Minimize to tray when started", "启动后最小化到托盘"),
                SettingToggle(settings.MinimizeWhenStarted, v => settings.MinimizeWhenStarted = v)),
            Setting("▣", T("When closing the window", "关闭窗口时"), close),
            Setting("文", T("Language", "语言"), language),
            Heading(T("Network", "网络")),
            Setting("⇄", T("Local mixed port", "本地混合端口"), Row(port, lan), T("Allow LAN connections", "允许局域网连接")),
            Setting("◎", T("Latency test · STUN", "测速方式 · STUN"), AsyncButton($"{ProbeModeLabel(settings.DelayTestMode)} ›", EditProbeAsync)),
            Setting("◇", "DNS", AsyncButton("›", EditDnsAsync), T("AioDNS · Outbound DNS", "AioDNS · 出站 DNS")));

        var advanced = Stack(
            Heading(T("Cores", "内核")),
            Setting("⚙", "Xray / sing-box", AsyncButton("›", EditCoreAsync), T("Fingerprint, fragmentation, Mux, KCP", "指纹、分片、Mux、KCP")),
            Setting("⚡", T("Hysteria bandwidth", "Hysteria 带宽"), AsyncButton("›", EditHysteriaAsync)),
            Heading(T("Updates and downloads", "更新与下载"), 4),
            Setting("⤓", T("Download channel", "下载通道"), channel),
            Setting("⟳", T("Check for updates at launch", "启动时检查更新"), SettingToggle(settings.CheckUpdateWhenOpened, v => settings.CheckUpdateWhenOpened = v),
                T("App, cores and mode rules", "程序、核心和模式规则")),
            Setting("↻", T("Update subscriptions at launch", "打开时自动更新订阅"),
                SettingToggle(settings.UpdateServersWhenOpened, v => settings.UpdateServersWhenOpened = v)),
            Setting("β", T("Include prereleases", "包含预发布版本"), SettingToggle(settings.CheckBetaUpdate, v => settings.CheckBetaUpdate = v)),
            Heading(T("Maintenance", "维护"), 3),
            Setting("🧹", T("Flush DNS · Firewall rules", "清理 DNS · 防火墙规则"), AsyncButton(T("Run", "执行"), ClearNetworkAsync)),
            Setting("⚠", T("Uninstall NF driver service", "卸载 NF 驱动服务"), AsyncButton(T("Uninstall", "卸载"), UninstallAsync)));

        common.Spacing = advanced.Spacing = 3.5;
        var heading = Stack(Text(T("Settings", "设置"), 24, true), _settingsError);
        heading.Spacing = 0;
        Content = Ui.Page(Body(heading,
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

    private void BindSelection<T>(ComboBox choice, Func<int, T> select, Func<T> read, Action<T> write, Action? saved = null)
    {
        var committedIndex = choice.SelectedIndex;
        choice.SelectionChanged += async (_, _) =>
        {
            var index = choice.SelectedIndex;
            if (!choice.IsEnabled || index < 0 || index == committedIndex)
                return;
            var previous = read();
            choice.IsEnabled = false;
            try
            {
                if (await SaveSettingAsync(() => write(select(index)), () => write(previous)))
                {
                    committedIndex = index;
                    saved?.Invoke();
                }
                else
                    choice.SelectedIndex = committedIndex;
            }
            finally { choice.IsEnabled = true; }
        };
    }

    private ToggleSwitch SettingToggle(bool value, Action<bool> write) => SettingToggle(value, enabled => enabled, write);

    private ToggleSwitch SettingToggle<T>(T value, Func<bool, T> select, Action<T> write)
    {
        bool IsOn() => EqualityComparer<T>.Default.Equals(value, select(true));
        var toggle = CompactToggle(new ToggleSwitch { IsOn = IsOn() });
        toggle.Margin = new Thickness(0, 0, 2, 0);
        toggle.Toggled += async (_, _) =>
        {
            if (!toggle.IsEnabled)
                return;
            var next = select(toggle.IsOn);
            var previous = value;
            toggle.IsEnabled = false;
            try
            {
                if (await SaveSettingAsync(() => write(next), () => write(previous)))
                    value = next;
                else
                    toggle.IsOn = IsOn();
            }
            finally { toggle.IsEnabled = true; }
        };
        return toggle;
    }

    private async Task<bool> SaveSettingAsync(Action apply, Action restore)
    {
        _settingsError.IsOpen = false;
        try
        {
            await Configuration.ApplyChangeAsync(apply, restore);
            return true;
        }
        catch (Exception e)
        {
            Log.Warning(e, "Could not save settings");
            ReportSettingError(e.Message);
            return false;
        }
    }

    private void ReportSettingError(string message)
    {
        _settingsError.Message = message;
        _settingsError.IsOpen = true;
    }

    private static string ProbeModeLabel(DelayTestMode mode) => mode switch
    {
        DelayTestMode.Hybrid => T("Smart hybrid", "智能混合"),
        DelayTestMode.UrlTest => "URL-Test",
        DelayTestMode.TCPing => "TCPing",
        DelayTestMode.ICMPing => "ICMPing",
        _ => T("Smart hybrid", "智能混合")
    };

    private async Task EditProbeAsync()
    {
        var s = Global.Settings;
        var method = Choice(
            T("Latency test", "测速方式"),
            [
                T("Smart hybrid (recommended)", "智能混合（推荐）"),
                T("Real connection (URL-Test)", "真连接测速（URL-Test）"),
                "TCPing",
                "ICMPing"
            ],
            (int)s.DelayTestMode);
        method.Header = T("Latency test", "测速方式");
        method.HorizontalAlignment = HorizontalAlignment.Stretch;
        var url = Input(T("Test URL", "测速 URL"), s.DelayTestUrl);
        var timeout = Input(T("Timeout (ms)", "超时时间（毫秒）"), s.DelayTestTimeout.ToString());
        var steady = new CheckBox
        {
            Content = T("Measure steady-state RTT (second request on same tunnel)", "测稳态延迟（同一隧道发第二次请求）"),
            IsChecked = s.DelayTestSteadySecondRequest
        };
        var interval = Input(T("Auto test interval (s, 0 = manual only)", "自动测速间隔（秒，0 为仅手动）"), s.DetectionTick.ToString());
        var stun = Input(T("STUN host", "STUN 主机"), s.STUN_Server);
        var port = Input(T("STUN port", "STUN 端口"), s.STUN_Server_Port.ToString());
        if (await ShowAsync(XamlRoot, T("Latency test · STUN", "测速方式 · STUN"),
            Stack(method, url, timeout, steady, interval, stun, port), T("Save", "保存"), () =>
        {
            var rawUrl = url.Text.Trim();
            if (!Uri.TryCreate(rawUrl, UriKind.Absolute, out var parsedUrl) ||
                (parsedUrl.Scheme != Uri.UriSchemeHttp && parsedUrl.Scheme != Uri.UriSchemeHttps))
                return T("Enter a valid HTTP or HTTPS test URL.", "请输入有效的 HTTP 或 HTTPS 测速 URL。");
            if (!int.TryParse(timeout.Text.Trim(), out var timeoutMs) ||
                timeoutMs < Constants.MinDelayTestTimeout || timeoutMs > Constants.MaxDelayTestTimeout)
                return TF("Enter a timeout between {0} and {1} ms.", "请输入 {0} 至 {1} 毫秒之间的超时时间。",
                    Constants.MinDelayTestTimeout, Constants.MaxDelayTestTimeout);
            if (!int.TryParse(interval.Text.Trim(), out var tickSeconds) ||
                (tickSeconds != 0 && (tickSeconds < 10 || tickSeconds > 86400)))
                return T("Enter 0 for manual testing, or an interval between 10 and 86400 seconds.", "请输入 0（仅手动）或 10 至 86400 秒之间的间隔。");
            if (Uri.CheckHostName(stun.Text.Trim()) == UriHostNameType.Unknown || !ushort.TryParse(port.Text.Trim(), out var value) || value == 0)
                return T("Enter a STUN host and a valid port.", "请输入 STUN 主机及有效端口。");
            return null;
        }))
        {
            var nextMode = (DelayTestMode)Math.Clamp(method.SelectedIndex, 0, 3);
            var nextUrl = url.Text.Trim();
            var nextTimeout = int.Parse(timeout.Text.Trim());
            var nextSteady = steady.IsChecked == true;
            var nextTick = int.Parse(interval.Text.Trim());
            var nextStun = stun.Text.Trim();
            var nextStunPort = (int)ushort.Parse(port.Text.Trim());

            var previous = (
                s.DelayTestMode,
                s.ServerTCPing,
                s.DelayTestUrl,
                s.DelayTestTimeout,
                s.DelayTestSteadySecondRequest,
                s.DetectionTick,
                s.DelayTestConfigVersion,
                s.STUN_Server,
                s.STUN_Server_Port);

            if (await SaveSettingAsync(
                () =>
                {
                    s.DelayTestMode = nextMode;
                    s.ServerTCPing = nextMode != DelayTestMode.ICMPing;
                    s.DelayTestUrl = nextUrl;
                    s.DelayTestTimeout = nextTimeout;
                    s.DelayTestSteadySecondRequest = nextSteady;
                    s.DetectionTick = nextTick;
                    s.DelayTestConfigVersion = 1;
                    s.STUN_Server = nextStun;
                    s.STUN_Server_Port = nextStunPort;
                },
                () => (
                    s.DelayTestMode,
                    s.ServerTCPing,
                    s.DelayTestUrl,
                    s.DelayTestTimeout,
                    s.DelayTestSteadySecondRequest,
                    s.DetectionTick,
                    s.DelayTestConfigVersion,
                    s.STUN_Server,
                    s.STUN_Server_Port) = previous))
            {
                DelayTestHelper.UpdateTick(false);
                App.MainWindow?.Navigate("Settings", force: true);
            }
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
            return null;
        }))
        {
            var previous = (settings.AioDNS.ChinaDNS, settings.AioDNS.OtherDNS, settings.OutboundDNS);
            var next = (DnsUtils.AppendPort(china.Text.Trim()), DnsUtils.AppendPort(other.Text.Trim()), outbound.Text.Trim());
            await SaveSettingAsync(
                () => (settings.AioDNS.ChinaDNS, settings.AioDNS.OtherDNS, settings.OutboundDNS) = next,
                () => (settings.AioDNS.ChinaDNS, settings.AioDNS.OtherDNS, settings.OutboundDNS) = previous);
        }
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
        var previous = (basic.DefFingerprint, basic.EnableFragment, basic.MuxEnabled, basic.DefAllowInsecure,
            kcp.Congestion, core.Mux4RayItem.Concurrency);
        await SaveNumbersAsync("Xray / sing-box", editor, fields, () =>
        {
            var next = (fingerprint.Text.Trim(), fragment.IsChecked == true, mux.IsChecked == true,
                insecure.IsChecked == true, congestion.IsChecked == true);
            return () => (basic.DefFingerprint, basic.EnableFragment, basic.MuxEnabled, basic.DefAllowInsecure, kcp.Congestion) = next;
        }, () => (basic.DefFingerprint, basic.EnableFragment, basic.MuxEnabled, basic.DefAllowInsecure,
            kcp.Congestion, core.Mux4RayItem.Concurrency) = previous);
    }

    private Task EditHysteriaAsync()
    {
        var h = Global.Settings.V2RayConfig.HysteriaItem;
        return SaveNumbersAsync("Hysteria", Stack(),
            [(T("Upload (Mbps)", "上传（Mbps）"), h.UpMbps, n => h.UpMbps = n), (T("Download (Mbps)", "下载（Mbps）"), h.DownMbps, n => h.DownMbps = n),
                (T("Hop interval (s)", "跳跃间隔（秒）"), h.HopInterval, n => h.HopInterval = n)]);
    }

    private async Task SaveNumbersAsync(string title, StackPanel editor, (string Label, int Value, Action<int> Set)[] fields,
        Func<Action>? capture = null, Action? restore = null)
    {
        var boxes = fields.Select(field => Input(field.Label, field.Value.ToString())).ToArray();
        foreach (var box in boxes)
            editor.Children.Add(box);
        var values = new int[boxes.Length];
        if (!await ShowAsync(XamlRoot, title, editor, T("Save", "保存"), () =>
        {
            for (var i = 0; i < boxes.Length; i++)
                if (!int.TryParse(boxes[i].Text, out values[i]) || values[i] < 0)
                    return T("Enter nonnegative integers.", "请输入非负整数。");
            return null;
        }))
            return;

        var apply = capture?.Invoke();
        await SaveSettingAsync(() =>
        {
            for (var i = 0; i < fields.Length; i++)
                fields[i].Set(values[i]);
            apply?.Invoke();
        }, () =>
        {
            foreach (var field in fields)
                field.Set(field.Value);
            restore?.Invoke();
        });
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
