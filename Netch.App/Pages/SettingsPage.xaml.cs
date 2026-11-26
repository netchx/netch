using System.Net;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.Threading;
using Netch.App.Helpers;
using Netch.App.Services;
using Netch.Controllers;
using Netch.Enums;
using Netch.Models;
using Netch.Services;
using Netch.Utils;
using Windows.System;
using static Netch.App.Helpers.Ui;

namespace Netch.App.Pages;

public sealed partial class SettingsPage : Page
{
    private static readonly object OptionalDividerTag = new();

    private enum SettingsCategory
    {
        General,
        Network,
        Cores,
        Updates,
        Maintenance
    }

    private readonly ListView _categories = Categories();
    private readonly ContentControl _pane = new()
    {
        HorizontalContentAlignment = HorizontalAlignment.Stretch,
        VerticalContentAlignment = VerticalAlignment.Stretch
    };
    private readonly InfoBar _settingsError = new()
    {
        Severity = InfoBarSeverity.Error,
        Title = T("Setting could not be changed", "无法更改设置"),
        IsClosable = true
    };

    public SettingsPage()
    {
        InitializeComponent();
        _settingsError.Closed += (_, _) => _settingsError.Margin = new Thickness(0);

        _categories.Items.Add(CategoryItem(Icons.Settings, T("General", "常规"), SettingsCategory.General));
        _categories.Items.Add(CategoryItem(Icons.NetworkPort, T("Network", "网络"), SettingsCategory.Network));
        _categories.Items.Add(CategoryItem(Icons.Flash, T("Cores", "内核"), SettingsCategory.Cores));
        _categories.Items.Add(CategoryItem(Icons.Download, T("Updates and downloads", "更新与下载"), SettingsCategory.Updates));
        _categories.Items.Add(CategoryItem(Icons.Repair, T("Maintenance", "维护"), SettingsCategory.Maintenance));
        _categories.SelectionChanged += (_, _) =>
        {
            if (_categories.SelectedItem is ListViewItem { Tag: SettingsCategory category })
            {
                SetErrorOpen(false);
                ShowCategory(category);
            }
        };
        _categories.SelectedIndex = 0;

        var heading = Stack(Text(T("Settings", "设置"), 24, true), _settingsError);
        heading.Spacing = 0;
        Content = Ui.Page(Body(heading, TwoColumns(_categories, _pane, new GridLength(165))));
    }

    private void ShowCategory(SettingsCategory category)
    {
        var body = category switch
        {
            SettingsCategory.Network => NetworkPane(),
            SettingsCategory.Cores => CoresPane(),
            SettingsCategory.Updates => UpdatesPane(),
            SettingsCategory.Maintenance => MaintenancePane(),
            _ => GeneralPane()
        };
        body.Spacing = 14;
        body.Margin = new Thickness(0, 2, 0, 12);
        var scroll = Scroll(body);
        scroll.Margin = new Thickness(0, 0, -Ui.ScrollBarGutter, 0);
        _pane.Content = scroll;
    }

    private StackPanel GeneralPane()
    {
        var settings = Global.Settings;
        var theme = Choice(T("Appearance", "外观"),
            [T("Follow system", "跟随系统"), T("Light", "浅色"), T("Dark", "深色")], (int)settings.Theme);
        BindSelection(theme, index => (AppTheme)index, () => settings.Theme, value => settings.Theme = value,
            () => App.ApplyTheme(settings.Theme));

        var languages = i18N.GetTranslateList().ToArray();
        var language = Choice(T("Language", "语言"),
            languages.Select(s => s == "System" ? T("Follow system", "跟随系统") : i18N.GetDisplayName(s)),
            Math.Max(0, Array.FindIndex(languages, s => string.Equals(s, settings.Language, StringComparison.OrdinalIgnoreCase))));
        BindSelection(language, index => languages[index], () => settings.Language, value => settings.Language = value, () =>
        {
            i18N.Load(settings.Language);
            ModeService.Instance.Sort();
            App.MainWindow?.ReloadLanguage();
        });

        var startup = SettingToggle(settings.RunAtStartup, b =>
        {
            settings.RunAtStartup = b;
            Utils.Utils.RegisterNetchStartupItem();
        });
        var autoStart = SettingToggle(settings.StartWhenOpened, v => settings.StartWhenOpened = v);
        var minimizeOnStart = SettingToggle(settings.MinimizeWhenStarted, v => settings.MinimizeWhenStarted = v);
        var close = Choice(T("When closing the window", "关闭窗口时"),
            [T("Minimize to tray", "最小化到托盘"), T("Exit", "退出")], settings.ExitWhenClosed ? 1 : 0);
        BindSelection(close, index => index == 1, () => settings.ExitWhenClosed, value => settings.ExitWhenClosed = value);

        return Stack(
            Group(T("Appearance and language", "外观与语言"),
                SettingRow(Icons.Theme, T("Appearance", "外观"), theme),
                SettingRow(Icons.Language, T("Language", "语言"), language)),
            Group(T("Startup and window", "启动与窗口"),
                SettingRow(Icons.Power, T("Start at sign-in", "开机启动"), startup, T("Run as administrator", "以管理员身份启动")),
                SettingRow(Icons.Play, T("Start automatically at launch", "启动后自动开启"), autoStart,
                    T("Connect the startup or last used scene", "自动连接启动场景或最近使用的场景")),
                SettingRow(Icons.Minimize, T("Minimize to tray when started", "启动后最小化到托盘"), minimizeOnStart),
                SettingRow(Icons.CloseWindow, T("When closing the window", "关闭窗口时"), close)));
    }

    private StackPanel NetworkPane()
    {
        var settings = Global.Settings;
        var port = NumericBox(T("Local mixed port", "本地混合端口"), settings.Socks5LocalPort,
            raw => ushort.TryParse(raw, out var value) && value > 0
                ? (true, value, null)
                : (false, 0, T("Enter a port between 1 and 65535.", "请输入 1 至 65535 之间的端口。")),
            () => settings.Socks5LocalPort,
            value => settings.Socks5LocalPort = (ushort)value);

        var lan = SettingToggle(settings.LocalAddress, b => b ? "0.0.0.0" : "127.0.0.1", address => settings.LocalAddress = address);

        var probeSummary = SummaryText($"{ProbeModeLabel(settings.DelayTestMode)} · {settings.DelayTestTimeout} ms");
        var webProxySummary = SummaryText(TF("Port {0}", "端口 {0}", settings.WebProxy.PacPort));
        var dnsSummary = SummaryText(settings.OutboundDNS, mono: true);

        return Stack(
            Group(T("Local proxy", "本地代理"),
                SettingRow(Icons.NetworkPort, T("Local mixed port", "本地混合端口"), port, "SOCKS5 / HTTP"),
                SettingRow(Icons.Share, T("Allow LAN connections", "允许局域网连接"), lan, T("Listen on 0.0.0.0", "监听 0.0.0.0"))),
            Group(T("Probing, web proxy and DNS", "测速 · 网页代理 · DNS"),
                ActionSettingRow(Icons.Probe, T("Latency test · STUN", "测速方式 · STUN"), probeSummary, async () =>
                {
                    await EditProbeAsync();
                    probeSummary.Text = $"{ProbeModeLabel(settings.DelayTestMode)} · {settings.DelayTestTimeout} ms";
                }),
                ActionSettingRow(Icons.Globe, T("Web proxy · PAC", "网页代理 · PAC"), webProxySummary, async () =>
                {
                    await EditWebProxyAsync();
                    webProxySummary.Text = TF("Port {0}", "端口 {0}", settings.WebProxy.PacPort);
                }, T("PAC port and default bypass list", "PAC 端口与默认绕过列表")),
                ActionSettingRow(Icons.Dns, "DNS", dnsSummary, async () =>
                {
                    await EditDnsAsync();
                    dnsSummary.Text = settings.OutboundDNS;
                }, T("AioDNS · Outbound DNS", "AioDNS · 出站 DNS"))));
    }

    private StackPanel CoresPane()
    {
        var core = Global.Settings.V2RayConfig;
        var basic = core.CoreBasicItem;
        var kcp = core.KcpItem;
        var hysteria = core.HysteriaItem;

        var fpValues = Constants.Fingerprints.ToList();
        var currentFp = basic.DefFingerprint ?? "";
        if (!string.IsNullOrEmpty(currentFp) && !fpValues.Contains(currentFp))
            fpValues.Insert(0, currentFp);
        var fpLabels = fpValues.Select(fp => string.IsNullOrEmpty(fp) ? T("None", "无") : fp);
        var fingerprint = Choice(T("TLS fingerprint", "TLS 指纹"), fpLabels, Math.Max(0, fpValues.IndexOf(currentFp)));
        BindSelection(fingerprint, index => fpValues[index], () => basic.DefFingerprint ?? "", value => basic.DefFingerprint = value);

        var fragment = SettingToggle(basic.EnableFragment, v => basic.EnableFragment = v);
        var concurrency = NumericBox(T("Mux concurrency", "Mux 并发数"), core.Mux4RayItem.Concurrency ?? 8,
            NonNegativeIntValidator,
            () => core.Mux4RayItem.Concurrency ?? 8,
            n => core.Mux4RayItem.Concurrency = n);
        var (concurrencyRow, setConcurrencyVisible) = OptionalRow(SettingRow(Icons.Settings, T("Mux concurrency", "Mux 并发数"), concurrency));
        setConcurrencyVisible(basic.MuxEnabled);

        var mux = SettingToggle(basic.MuxEnabled, v => basic.MuxEnabled = v, enabled => setConcurrencyVisible(enabled));
        var insecure = SettingToggle(basic.DefAllowInsecure, v => basic.DefAllowInsecure = v);
        var kcpSummary = SummaryText($"MTU {kcp.Mtu} · TTI {kcp.Tti}", mono: true);

        var upMbps = NumericBox(T("Upload (Mbps)", "上传（Mbps）"), hysteria.UpMbps,
            NonNegativeIntValidator, () => hysteria.UpMbps, n => hysteria.UpMbps = n);
        var downMbps = NumericBox(T("Download (Mbps)", "下载（Mbps）"), hysteria.DownMbps,
            NonNegativeIntValidator, () => hysteria.DownMbps, n => hysteria.DownMbps = n);
        var hopInterval = NumericBox(T("Hop interval (s)", "跳跃间隔（秒）"), hysteria.HopInterval,
            NonNegativeIntValidator, () => hysteria.HopInterval, n => hysteria.HopInterval = n);

        return Stack(
            Group("Xray / sing-box",
                SettingRow(Icons.Lock, T("TLS fingerprint", "TLS 指纹"), fingerprint),
                SettingRow(Icons.Filter, T("Fragment initial packets", "分片"), fragment),
                SettingRow(Icons.Switch, T("Use Mux", "Mux 多路复用"), mux),
                concurrencyRow,
                SettingRow(Icons.Shield, T("Allow insecure connections", "允许不安全连接"), insecure),
                ActionSettingRow(Icons.Settings, T("KCP parameters", "KCP 参数"), kcpSummary, async () =>
                {
                    if (await EditKcpAsync())
                        kcpSummary.Text = $"MTU {kcp.Mtu} · TTI {kcp.Tti}";
                })),
            Group("Hysteria",
                SettingRow(Icons.Upload, T("Upload (Mbps)", "上传（Mbps）"), upMbps),
                SettingRow(Icons.Download, T("Download (Mbps)", "下载（Mbps）"), downMbps),
                SettingRow(Icons.Refresh, T("Hop interval (s)", "跳跃间隔（秒）"), hopInterval)));
    }

    private StackPanel UpdatesPane()
    {
        var settings = Global.Settings;
        var channels = new[] { T("Direct", "直连"), T("Current proxy", "通过当前代理"), T("GitHub mirror", "GitHub 镜像") };
        var channel = Choice(T("Download channel", "下载通道"), channels, (int)settings.DownloadChannel);
        var mirrorBox = Input(T("HTTPS mirror prefix", "HTTPS 镜像前缀"), settings.GitHubMirror, search: true);
        mirrorBox.Width = 220;
        mirrorBox.PlaceholderText = "https://...";
        var saveMirror = new Button { Content = T("Save", "保存") };
        AutomationProperties.SetName(saveMirror, T("Save", "保存"));
        var (mirrorRow, setMirrorVisible) = OptionalRow(
            SettingRow(Icons.Link, T("HTTPS mirror prefix", "HTTPS 镜像前缀"), Row(mirrorBox, saveMirror)));
        setMirrorVisible(settings.DownloadChannel == DownloadChannel.GitHubMirror);

        async Task CommitMirrorAsync()
        {
            if (!saveMirror.IsEnabled)
                return;
            if (!TryNormalizeHttpsUrl(mirrorBox.Text, out var normalized))
            {
                ReportSettingError(T("Enter a valid HTTPS URL.", "请输入有效的 HTTPS 地址。"));
                return;
            }
            SetErrorOpen(false);
            var previous = (settings.DownloadChannel, settings.GitHubMirror);
            if (previous == (DownloadChannel.GitHubMirror, normalized))
            {
                mirrorBox.Text = normalized;
                return;
            }
            saveMirror.IsEnabled = false;
            mirrorBox.IsEnabled = false;
            channel.IsEnabled = false;
            try
            {
                if (await SaveSettingAsync(
                    () => (settings.DownloadChannel, settings.GitHubMirror) = (DownloadChannel.GitHubMirror, normalized),
                    () => (settings.DownloadChannel, settings.GitHubMirror) = previous))
                {
                    mirrorBox.Text = settings.GitHubMirror;
                    channel.SelectedIndex = (int)settings.DownloadChannel;
                }
            }
            finally
            {
                channel.IsEnabled = true;
                mirrorBox.IsEnabled = true;
                saveMirror.IsEnabled = true;
            }
        }

        saveMirror.Click += (_, _) => CommitMirrorAsync().Forget();
        mirrorBox.KeyDown += (_, e) =>
        {
            if (e.Key == VirtualKey.Enter)
            {
                e.Handled = true;
                CommitMirrorAsync().Forget();
            }
        };

        async Task SelectChannelAsync()
        {
            if (!channel.IsEnabled || channel.SelectedIndex < 0)
                return;
            var selected = (DownloadChannel)channel.SelectedIndex;
            SetErrorOpen(false);
            setMirrorVisible(selected == DownloadChannel.GitHubMirror);

            var nextMirror = settings.GitHubMirror;
            if (selected == DownloadChannel.GitHubMirror)
            {
                mirrorBox.Text = settings.GitHubMirror;
                if (!TryNormalizeHttpsUrl(settings.GitHubMirror, out nextMirror))
                {
                    mirrorBox.Focus(FocusState.Programmatic);
                    return;
                }
            }
            if (selected == settings.DownloadChannel && nextMirror == settings.GitHubMirror)
                return;

            var previous = (settings.DownloadChannel, settings.GitHubMirror);
            channel.IsEnabled = false;
            mirrorBox.IsEnabled = false;
            saveMirror.IsEnabled = false;
            try
            {
                await SaveSettingAsync(
                    () => (settings.DownloadChannel, settings.GitHubMirror) = (selected, nextMirror),
                    () => (settings.DownloadChannel, settings.GitHubMirror) = previous);
            }
            finally
            {
                channel.SelectedIndex = (int)settings.DownloadChannel;
                setMirrorVisible(settings.DownloadChannel == DownloadChannel.GitHubMirror);
                saveMirror.IsEnabled = true;
                mirrorBox.IsEnabled = true;
                channel.IsEnabled = true;
            }
        }

        channel.SelectionChanged += (_, _) => SelectChannelAsync().Forget();

        return Stack(
            Group(T("Download channel", "下载通道"),
                SettingRow(Icons.Download, T("Download channel", "下载通道"), channel),
                mirrorRow),
            Group(T("Automatic checks", "自动检查"),
                SettingRow(Icons.Refresh, T("Check for updates at launch", "启动时检查更新"),
                    SettingToggle(settings.CheckUpdateWhenOpened, v => settings.CheckUpdateWhenOpened = v),
                    T("App, cores and mode rules", "程序、核心和模式规则")),
                SettingRow(Icons.Cloud, T("Update subscriptions at launch", "打开时自动更新订阅"),
                    SettingToggle(settings.UpdateServersWhenOpened, v => settings.UpdateServersWhenOpened = v)),
                SettingRow(Icons.PreRelease, T("Include prereleases", "包含预发布版本"),
                    SettingToggle(settings.CheckBetaUpdate, v => settings.CheckBetaUpdate = v))));
    }

    private StackPanel MaintenancePane()
    {
        var uninstall = AsyncButton(T("Uninstall", "卸载"), UninstallAsync);
        uninstall.Style = Resource<Style>("NetchDangerButton");
        void SyncDangerBrushes()
        {
            uninstall.Resources["ButtonForegroundPointerOver"] = uninstall.Foreground;
            uninstall.Resources["ButtonForegroundPressed"] = uninstall.Foreground;
            uninstall.Resources["ButtonBorderBrushPointerOver"] = uninstall.BorderBrush;
            uninstall.Resources["ButtonBorderBrushPressed"] = uninstall.BorderBrush;
        }
        uninstall.Loaded += (_, _) => SyncDangerBrushes();
        uninstall.ActualThemeChanged += (_, _) => SyncDangerBrushes();

        return Stack(
            Group(T("System maintenance", "系统维护"),
                SettingRow(Icons.Repair, T("Flush DNS · Firewall rules", "清理 DNS · 防火墙规则"),
                    AsyncButton(T("Run", "执行"), ClearNetworkAsync),
                    T("Flush the DNS cache and remove Netch firewall rules", "清理系统 DNS 缓存并移除 Netch 防火墙规则")),
                SettingRow(Icons.Reset, T("Reset system proxy", "重置系统代理"),
                    AsyncButton(T("Reset", "重置"), ResetSystemProxyAsync),
                    T("Clear Windows system proxy and PAC settings", "清除 Windows 系统代理与 PAC 自动配置")),
                SettingRow(Icons.Warning, T("Uninstall NF driver service", "卸载 NF 驱动服务"),
                    uninstall,
                    T("Remove the netfilter2 service", "移除 netfilter2 驱动服务"))));
    }

    private static FontIcon SecondaryIcon(string glyph, double size, double? width = null)
    {
        var icon = GlyphIcon(glyph, size);
        icon.Style = Resource<Style>("NetchSecondaryIcon");
        if (width.HasValue)
            icon.Width = width.Value;
        icon.VerticalAlignment = VerticalAlignment.Center;
        return icon;
    }

    private static ListViewItem CategoryItem(string glyph, string label, SettingsCategory category)
    {
        var row = Row(SecondaryIcon(glyph, 14, 16), Text(label, strong: true));
        row.Spacing = 8;
        var item = new ListViewItem
        {
            Content = row,
            Height = 36,
            Margin = new Thickness(0, 0, 0, 2),
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            Tag = category
        };
        AutomationProperties.SetName(item, label);
        AddSelectionIndicator(item);
        return item;
    }

    private static Border Divider() => new()
    {
        Height = 1,
        Margin = new Thickness(14, 0, 14, 0),
        Style = Resource<Style>("NetchDivider")
    };

    private static StackPanel Group(string title, params UIElement[] rows)
    {
        var list = new StackPanel { Spacing = 0 };
        for (var i = 0; i < rows.Length; i++)
        {
            if (i > 0 && (rows[i] as FrameworkElement)?.Tag != OptionalDividerTag)
                list.Children.Add(Divider());
            list.Children.Add(rows[i]);
        }
        var heading = Text(title, 12.5, strong: true, secondary: true);
        heading.Margin = new Thickness(2, 0, 0, 6);
        return new StackPanel { Spacing = 0, Children = { heading, Card(list, 0) } };
    }

    private static (UIElement Row, Action<bool> SetVisible) OptionalRow(UIElement row)
    {
        var panel = new StackPanel
        {
            Spacing = 0,
            Tag = OptionalDividerTag,
            Children = { Divider(), row }
        };
        return (panel, visible => panel.Visibility = visible ? Visibility.Visible : Visibility.Collapsed);
    }

    private static Grid RowLabel(string iconGlyph, string title, string? description)
    {
        var glyph = SecondaryIcon(iconGlyph, 15, 18);
        UIElement label = description == null
            ? Text(title)
            : new StackPanel
            {
                Spacing = 1,
                VerticalAlignment = VerticalAlignment.Center,
                Children = { Text(title), Text(description, 12, secondary: true) }
            };
        return TwoColumns(glyph, label, GridLength.Auto, 12);
    }

    private static Border SettingRow(string iconGlyph, string title, UIElement control, string? description = null)
    {
        ((FrameworkElement)control).VerticalAlignment = VerticalAlignment.Center;
        return new Border
        {
            MinHeight = 46,
            Padding = new Thickness(14, 8, 14, 8),
            Child = Split(RowLabel(iconGlyph, title, description), control, 12)
        };
    }

    private static Button ActionSettingRow(string iconGlyph, string title, TextBlock summary, Func<Task> action, string? description = null)
    {
        var right = Row(summary, SecondaryIcon(Icons.ChevronRight, 11));
        right.Spacing = 8;
        right.VerticalAlignment = VerticalAlignment.Center;
        var button = new Button
        {
            Style = Resource<Style>("NetchSettingRowButton"),
            Margin = new Thickness(3, 2, 3, 2),
            Content = Split(RowLabel(iconGlyph, title, description), right, 12)
        };
        AutomationProperties.SetName(button, title);
        async Task ExecuteAsync()
        {
            button.IsEnabled = false;
            try { await action(); }
            catch (Exception e)
            {
                Log.Warning(e, "{Action} failed", title);
                if (button.XamlRoot is { } root)
                    await ShowAsync(root, T("Operation failed", "操作失败"), Text(e.Message));
            }
            finally { button.IsEnabled = true; }
        }

        button.Click += (_, _) => ExecuteAsync().Forget();
        return button;
    }

    private static TextBlock SummaryText(string text, bool mono = false)
    {
        var block = Text(text, 12.5, secondary: true);
        block.MaxWidth = 220;
        if (mono)
            block.FontFamily = new FontFamily("Consolas");
        return block;
    }

    private TextBox NumericBox(string accessibleName, int initial,
        Func<string, (bool Valid, int Value, string? Error)> validate,
        Func<int> read, Action<int> write)
    {
        var box = Input(accessibleName, initial.ToString(), search: true);
        box.PlaceholderText = "";
        box.Width = 84;
        box.MinWidth = 0;
        box.FontFamily = new FontFamily("Consolas");
        var saving = false;

        async Task CommitAsync()
        {
            if (saving || !box.IsEnabled)
                return;
            var (valid, value, error) = validate(box.Text.Trim());
            if (!valid)
            {
                ReportSettingError(error ?? T("Setting could not be changed", "无法更改设置"));
                return;
            }
            SetErrorOpen(false);
            var previous = read();
            if (value == previous)
            {
                box.Text = previous.ToString();
                return;
            }
            saving = true;
            box.IsEnabled = false;
            try
            {
                await SaveSettingAsync(() => write(value), () => write(previous));
            }
            finally
            {
                box.Text = read().ToString();
                box.IsEnabled = true;
                saving = false;
            }
        }

        box.KeyDown += (_, e) =>
        {
            if (e.Key == VirtualKey.Enter)
            {
                e.Handled = true;
                CommitAsync().Forget();
            }
        };
        box.LostFocus += (_, _) => CommitAsync().Forget();
        return box;
    }

    private static (bool Valid, int Value, string? Error) NonNegativeIntValidator(string raw) =>
        int.TryParse(raw, out var value) && value >= 0
            ? (true, value, null)
            : (false, 0, T("Enter nonnegative integers.", "请输入非负整数。"));

    private static bool TryNormalizeHttpsUrl(string raw, out string normalized)
    {
        if (Uri.TryCreate(raw.Trim(), UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps)
        {
            normalized = uri.AbsoluteUri.TrimEnd('/');
            return true;
        }
        normalized = "";
        return false;
    }

    private void BindSelection<T>(ComboBox choice, Func<int, T> select, Func<T> read, Action<T> write, Action? saved = null)
    {
        var committedIndex = choice.SelectedIndex;
        async Task CommitSelectionAsync()
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
        }

        choice.SelectionChanged += (_, _) => CommitSelectionAsync().Forget();
    }

    private ToggleSwitch SettingToggle(bool value, Action<bool> write, Action<bool>? saved = null) =>
        SettingToggle(value, enabled => enabled, write, saved);

    private ToggleSwitch SettingToggle<T>(T value, Func<bool, T> select, Action<T> write, Action<T>? saved = null)
    {
        bool IsOn() => EqualityComparer<T>.Default.Equals(value, select(true));
        var toggle = CompactToggle(new ToggleSwitch { IsOn = IsOn() });
        toggle.Margin = new Thickness(0, 0, 2, 0);
        async Task CommitToggleAsync()
        {
            if (!toggle.IsEnabled)
                return;
            var next = select(toggle.IsOn);
            var previous = value;
            toggle.IsEnabled = false;
            try
            {
                if (await SaveSettingAsync(() => write(next), () => write(previous)))
                {
                    value = next;
                    saved?.Invoke(next);
                }
                else
                    toggle.IsOn = IsOn();
            }
            finally { toggle.IsEnabled = true; }
        }

        toggle.Toggled += (_, _) => CommitToggleAsync().Forget();
        return toggle;
    }

    private async Task<bool> SaveSettingAsync(Action apply, Action restore)
    {
        SetErrorOpen(false);
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

    private void ReportSettingError(string message) => SetErrorOpen(true, message);

    private void SetErrorOpen(bool open, string? message = null)
    {
        if (message != null)
            _settingsError.Message = message;
        _settingsError.IsOpen = open;
        _settingsError.Margin = open ? new Thickness(0, 8, 0, 0) : new Thickness(0);
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
            Enum.IsDefined(s.DelayTestMode) ? (int)s.DelayTestMode : 0);
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
            }
        }
    }

    private async Task EditWebProxyAsync()
    {
        var settings = Global.Settings;
        var pacPort = Input(T("PAC port", "PAC 端口"), settings.WebProxy.PacPort.ToString());
        var defaultBypass = Input(T("Default bypass list (semicolon-separated)", "默认绕过列表（分号分隔）"), settings.WebProxy.DefaultBypass, multiline: true);
        defaultBypass.MinHeight = 76;

        if (await ShowAsync(XamlRoot, T("Web proxy · PAC", "网页代理 · PAC"),
            Stack(pacPort, defaultBypass), T("Save", "保存"), () =>
        {
            if (!ushort.TryParse(pacPort.Text.Trim(), out var portValue) || portValue == 0)
                return T("Enter a port between 1 and 65535.", "请输入 1 至 65535 之间的端口。");
            return null;
        }))
        {
            var nextPort = ushort.Parse(pacPort.Text.Trim());
            var nextBypass = string.IsNullOrWhiteSpace(defaultBypass.Text)
                ? WebProxyConfig.DefaultBypassValue
                : defaultBypass.Text.Trim();
            var previous = (settings.WebProxy.PacPort, settings.WebProxy.DefaultBypass);
            await SaveSettingAsync(
                () => (settings.WebProxy.PacPort, settings.WebProxy.DefaultBypass) = (nextPort, nextBypass),
                () => (settings.WebProxy.PacPort, settings.WebProxy.DefaultBypass) = previous);
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
            if (await SaveSettingAsync(
                () => (settings.AioDNS.ChinaDNS, settings.AioDNS.OtherDNS, settings.OutboundDNS) = next,
                () => (settings.AioDNS.ChinaDNS, settings.AioDNS.OtherDNS, settings.OutboundDNS) = previous) &&
                !string.Equals(previous.OutboundDNS, settings.OutboundDNS, StringComparison.OrdinalIgnoreCase))
            {
                DnsUtils.ClearCache();
            }
        }
    }

    private Task<bool> EditKcpAsync()
    {
        var kcp = Global.Settings.V2RayConfig.KcpItem;
        var congestion = new CheckBox { Content = T("KCP congestion control", "KCP 拥塞控制"), IsChecked = kcp.Congestion };
        var fields = new (string Label, int Value, Action<int> Set)[]
        {
            ("KCP MTU", kcp.Mtu, n => kcp.Mtu = n),
            ("KCP TTI", kcp.Tti, n => kcp.Tti = n),
            ("KCP uplink", kcp.UplinkCapacity, n => kcp.UplinkCapacity = n),
            ("KCP downlink", kcp.DownlinkCapacity, n => kcp.DownlinkCapacity = n),
            ("KCP read buffer", kcp.ReadBufferSize, n => kcp.ReadBufferSize = n),
            ("KCP write buffer", kcp.WriteBufferSize, n => kcp.WriteBufferSize = n)
        };
        var previous = kcp.Congestion;
        var next = false;
        return SaveNumbersAsync(T("KCP parameters", "KCP 参数"), Stack(congestion), fields,
            () =>
            {
                next = congestion.IsChecked == true;
                return () => kcp.Congestion = next;
            },
            () => kcp.Congestion = previous);
    }

    private async Task<bool> SaveNumbersAsync(string title, StackPanel editor, (string Label, int Value, Action<int> Set)[] fields,
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
            return false;

        var apply = capture?.Invoke();
        return await SaveSettingAsync(() =>
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
        await Task.Run(() =>
        {
            NativeMethods.RefreshDNSCache();
            DnsUtils.ClearCache();
            Firewall.RemoveNetchFwRules();
        });
        await ShowAsync(XamlRoot, title, Text(T("Maintenance completed.", "维护已完成。")));
    }

    private async Task ResetSystemProxyAsync()
    {
        var title = T("Reset system proxy", "重置系统代理");
        if (!Session.Current.IsIdle)
            throw new InvalidOperationException(T("Disconnect before maintenance.", "请先断开连接，再执行维护操作。"));
        if (!await ShowAsync(XamlRoot, title, Text(T("Clear Windows system proxy and PAC settings?", "清除 Windows 系统代理与 PAC 自动配置设置？")), T("Reset", "重置")))
            return;
        await Task.Run(() =>
        {
            WinInetSystemProxyService.Instance.SetDirect();
            SystemProxyStateStore.ClearState();
        });
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
