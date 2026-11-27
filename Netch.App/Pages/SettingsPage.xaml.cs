using System.Net;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.Threading;
using Microsoft.Windows.Storage.Pickers;
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
    private static readonly object AddCustomSentinel = new();

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

        static string ProbeSummary(Setting setting) =>
            $"{ProbeModeLabel(setting.DelayTestMode)} · {setting.DelayTestTimeout} ms · {EndpointPresetHelper.FormatStunEndpoint(setting.STUN_Server, setting.STUN_Server_Port)}";
        var probeSummary = SummaryText(ProbeSummary(settings));
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
                    probeSummary.Text = ProbeSummary(settings);
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
            BuildCustomCoresGroup(),
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

    private StackPanel BuildCustomCoresGroup()
    {
        var cores = CustomCoreHelper.GetAll();
        var rows = new List<UIElement>(cores.Count + 1);
        foreach (var core in cores)
        {
            var modeLabel = core.Mode == CustomCoreMode.CliArguments && !string.IsNullOrWhiteSpace(core.ConfigTemplate)
                ? T("CLI arguments + config", "CLI 参数 + 配置模板")
                : CustomCoreModeLabel(core.Mode);
            var summary = $"{ServerHelper.GetDisplayName(core.Protocol)} · {modeLabel}" +
                (core.IsProtocolDefault ? $" · {T("Default", "默认")}" : string.Empty);
            var editRow = ActionSettingRow(Icons.Flash, core.Name, SummaryText(summary), () => EditCustomCoreAsync(core),
                CustomCoreHelper.NormalizeExecutableInput(core.ExecutablePath));
            var deleteBtn = IconButton(Icons.Delete, T("Delete", "删除"), () => DeleteCustomCoreAsync(core).Forget());
            deleteBtn.VerticalAlignment = VerticalAlignment.Center;
            deleteBtn.Margin = new Thickness(0, 0, 10, 0);
            rows.Add(Split(editRow, deleteBtn, 4));
        }

        var addSummary = cores.Count == 0
            ? T("Optional", "可选")
            : TF("{0} configured", "已配置 {0} 项", cores.Count);
        rows.Add(ActionSettingRow(Icons.Add, T("Add protocol custom core", "添加协议自定义核心"),
            SummaryText(addSummary), () => EditCustomCoreAsync(null),
            T("Bind custom CLI or modified core binary per protocol", "为指定协议绑定自定义 CLI 或魔改内核程序")));

        return Group(T("Protocol custom cores", "协议自定义核心"), rows.ToArray());
    }

    private static string CustomCoreModeLabel(CustomCoreMode mode) => mode switch
    {
        CustomCoreMode.MihomoConfig => T("Reuse Mihomo config", "复用 Mihomo 配置"),
        CustomCoreMode.SingboxConfig => T("Reuse sing-box config", "复用 sing-box 配置"),
        CustomCoreMode.XrayConfig => T("Reuse Xray config", "复用 Xray 配置"),
        _ => T("CLI arguments", "CLI 启动参数")
    };

    private async Task EditCustomCoreAsync(CustomCore? existing)
    {
        var protocols = ServerEditorHelper.GetOrderedProtocols()
            .Where(util => util.TypeName != nameof(EConfigType.ExternalCli))
            .ToArray();
        var initialProtocol = existing?.Protocol ?? EConfigType.Shadowsocks;
        var protoIndex = Math.Max(0, Array.FindIndex(protocols, u =>
            Enum.TryParse<EConfigType>(u.TypeName, out var parsed) && parsed == initialProtocol));

        var nameBox = Input(T("Name", "名称"), existing?.Name);
        var protocolBox = Choice(T("Protocol", "适用协议"), protocols.Select(ServerHelper.GetDisplayName), protoIndex);
        protocolBox.Header = T("Protocol", "适用协议");
        protocolBox.HorizontalAlignment = HorizontalAlignment.Stretch;

        EConfigType SelectedProtocol()
        {
            var pIdx = Math.Clamp(protocolBox.SelectedIndex, 0, protocols.Length - 1);
            return Enum.TryParse<EConfigType>(protocols[pIdx].TypeName, out var parsed)
                ? parsed
                : EConfigType.Shadowsocks;
        }

        var exeBox = Input(T("CLI executable", "CLI 可执行文件"), existing?.ExecutablePath);
        var browseBtn = AsyncButton(T("Browse .exe…", "浏览 .exe…"), async () =>
        {
            if (App.MainWindow is not { } window)
                return;
            var picker = new FileOpenPicker(window.AppWindow.Id);
            picker.FileTypeFilter.Add(".exe");
            if (await picker.PickSingleFileAsync() is { } file)
                exeBox.Text = file.Path;
        }, glyph: Icons.OpenFile);
        browseBtn.VerticalAlignment = VerticalAlignment.Bottom;
        var exeRow = Split(exeBox, browseBtn, 8);

        var currentModes = CustomCoreHelper.GetSupportedModes(initialProtocol).ToArray();
        var initialMode = existing?.Mode ?? CustomCoreMode.CliArguments;
        if (Array.IndexOf(currentModes, initialMode) < 0)
            initialMode = CustomCoreMode.CliArguments;
        var modeIndex = Math.Max(0, Array.IndexOf(currentModes, initialMode));
        var modeBox = Choice(T("Launch mode", "启动方式"), currentModes.Select(CustomCoreModeLabel), modeIndex);
        modeBox.Header = T("Launch mode", "启动方式");
        modeBox.HorizontalAlignment = HorizontalAlignment.Stretch;

        var argsBox = Input(T("Arguments template", "参数模板"),
            existing?.ArgumentsTemplate ?? CustomCoreHelper.GetDefaultCliTemplate(initialProtocol),
            multiline: true);
        argsBox.AcceptsReturn = false;
        argsBox.MinHeight = 64;
        argsBox.MaxHeight = 120;
        ScrollViewer.SetVerticalScrollBarVisibility(argsBox, ScrollBarVisibility.Auto);

        var useConfigBox = new CheckBox
        {
            Content = T("Use config file template", "使用配置文件模板"),
            IsChecked = !string.IsNullOrWhiteSpace(existing?.ConfigTemplate)
        };
        var initialExtIdx = Math.Max(0, Array.IndexOf(CustomCoreHelper.SupportedConfigExtensions,
            CustomCoreHelper.NormalizeConfigExtension(existing?.ConfigFileExtension)));
        var extBox = Choice(T("Config file format", "配置文件格式"), CustomCoreHelper.SupportedConfigExtensions, initialExtIdx);
        extBox.Header = T("Config file format", "配置文件格式");
        extBox.HorizontalAlignment = HorizontalAlignment.Stretch;

        var configBox = Input(T("Config file template", "配置文件模板"), existing?.ConfigTemplate, multiline: true);
        configBox.FontFamily = new FontFamily("Consolas");
        configBox.MinHeight = 110;
        configBox.MaxHeight = 190;
        ScrollViewer.SetVerticalScrollBarVisibility(configBox, ScrollBarVisibility.Auto);
        var configPanel = Stack(extBox, configBox);

        TextBox lastFocusedBox = argsBox;
        argsBox.GotFocus += (_, _) => lastFocusedBox = argsBox;
        configBox.GotFocus += (_, _) => lastFocusedBox = configBox;

        var chipsHint = Caption(T("Click a placeholder to insert", "点击占位符插入（支持条件段与空值回退）"));
        var chipsPanel = new StackPanel { Spacing = 4 };
        void RebuildChips(EConfigType proto)
        {
            chipsPanel.Children.Clear();
            var tokens = CustomCoreHelper.GetPlaceholdersForProtocol(proto);
            const int maxRowChars = 40;
            StackPanel? row = null;
            var rowChars = 0;
            foreach (var token in tokens)
            {
                var weight = token.Length + 4;
                if (row == null || (row.Children.Count > 0 && rowChars + weight > maxRowChars))
                {
                    row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
                    chipsPanel.Children.Add(row);
                    rowChars = 0;
                }

                var insertText = "{" + token + "}";
                var chip = new Button
                {
                    Content = insertText,
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 11.5,
                    Padding = new Thickness(7, 2, 7, 2),
                    MinHeight = 24,
                    MinWidth = 0
                };
                chip.Click += (_, _) =>
                {
                    var target = useConfigBox.IsChecked == true && ReferenceEquals(lastFocusedBox, configBox)
                        ? configBox
                        : argsBox;
                    var text = target.Text ?? string.Empty;
                    var start = Math.Clamp(target.SelectionStart, 0, text.Length);
                    var len = Math.Clamp(target.SelectionLength, 0, text.Length - start);
                    target.Text = text.Remove(start, len).Insert(start, insertText);
                    target.Focus(FocusState.Programmatic);
                    target.SelectionStart = start + insertText.Length;
                    target.SelectionLength = 0;
                };
                row.Children.Add(chip);
                rowChars += weight;
            }
        }
        RebuildChips(initialProtocol);

        var previewArgsText = new TextBlock
        {
            FontFamily = new FontFamily("Consolas"),
            FontSize = 11.5,
            TextWrapping = TextWrapping.Wrap,
            IsTextSelectionEnabled = true
        };
        var previewConfigText = new TextBlock
        {
            FontFamily = new FontFamily("Consolas"),
            FontSize = 11.5,
            TextWrapping = TextWrapping.Wrap,
            IsTextSelectionEnabled = true,
            Visibility = Visibility.Collapsed
        };
        var previewCard = Card(Stack(Caption(T("Live preview", "实时预览")), previewArgsText, previewConfigText), 10);

        void RefreshPreview()
        {
            var proto = SelectedProtocol();
            var sample = CustomCoreHelper.CreateSampleServer(proto);
            var ext = CustomCoreHelper.SupportedConfigExtensions[
                Math.Clamp(extBox.SelectedIndex, 0, CustomCoreHelper.SupportedConfigExtensions.Length - 1)];
            var usingCfg = useConfigBox.IsChecked == true;
            var fakeConfigPath = usingCfg ? $@"data\custom-core-preview{ext}" : null;
            var previewPort = Global.Settings.Socks5LocalPort is > 0 ? Global.Settings.Socks5LocalPort : (ushort)2801;

            var renderedArgs = CustomCoreHelper.RenderArguments(argsBox.Text, sample, "1.2.3.4", "127.0.0.1", previewPort, fakeConfigPath);
            previewArgsText.Text = string.IsNullOrEmpty(renderedArgs) ? "—" : renderedArgs;

            if (usingCfg && !string.IsNullOrWhiteSpace(configBox.Text))
            {
                var renderedCfg = CustomCoreHelper.RenderConfigTemplate(configBox.Text, ext, sample, "1.2.3.4", "127.0.0.1", previewPort);
                if (renderedCfg.Length > 600)
                    renderedCfg = renderedCfg[..600] + "\n…";
                previewConfigText.Text = renderedCfg;
                previewConfigText.Visibility = Visibility.Visible;
            }
            else
            {
                previewConfigText.Visibility = Visibility.Collapsed;
            }
        }

        var syncing = false;
        var initialConfigPreset = CustomCoreHelper.GetPresetsForProtocol(initialProtocol)
            .FirstOrDefault(p => !string.IsNullOrWhiteSpace(p.ConfigTemplate));
        var previousDefaultArgs = CustomCoreHelper.NormalizeArgumentsInput(CustomCoreHelper.GetDefaultCliTemplate(initialProtocol));
        var previousDefaultConfig = CustomCoreHelper.NormalizeConfigTemplate(initialConfigPreset?.ConfigTemplate);
        var previousDefaultConfigArgs = CustomCoreHelper.NormalizeArgumentsInput(initialConfigPreset?.ArgumentsTemplate);
        StackPanel? argsPanel = null;

        void UpdateArgsPanelVisibility()
        {
            var idx = Math.Clamp(modeBox.SelectedIndex, 0, currentModes.Length - 1);
            var isCliArgs = currentModes[idx] == CustomCoreMode.CliArguments;
            if (argsPanel != null)
                argsPanel.Visibility = isCliArgs ? Visibility.Visible : Visibility.Collapsed;
            configPanel.Visibility = isCliArgs && useConfigBox.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            if (isCliArgs && string.IsNullOrWhiteSpace(argsBox.Text))
                argsBox.Text = CustomCoreHelper.GetDefaultCliTemplate(SelectedProtocol());
            RefreshPreview();
        }

        void ApplyPreset(CustomCorePreset preset)
        {
            syncing = true;
            try
            {
                argsBox.Text = preset.ArgumentsTemplate;
                if (!string.IsNullOrWhiteSpace(preset.ConfigTemplate))
                {
                    useConfigBox.IsChecked = true;
                    extBox.SelectedIndex = Math.Max(0, Array.IndexOf(CustomCoreHelper.SupportedConfigExtensions,
                        CustomCoreHelper.NormalizeConfigExtension(preset.ConfigFileExtension)));
                    configBox.Text = preset.ConfigTemplate;
                }
                else
                {
                    useConfigBox.IsChecked = false;
                    configBox.Text = string.Empty;
                }
            }
            finally
            {
                syncing = false;
            }
            var selectedProto = SelectedProtocol();
            var cfgPreset = CustomCoreHelper.GetPresetsForProtocol(selectedProto)
                .FirstOrDefault(p => !string.IsNullOrWhiteSpace(p.ConfigTemplate));
            previousDefaultArgs = CustomCoreHelper.NormalizeArgumentsInput(CustomCoreHelper.GetDefaultCliTemplate(selectedProto));
            previousDefaultConfig = CustomCoreHelper.NormalizeConfigTemplate(cfgPreset?.ConfigTemplate);
            previousDefaultConfigArgs = CustomCoreHelper.NormalizeArgumentsInput(cfgPreset?.ArgumentsTemplate);
            UpdateArgsPanelVisibility();
        }

        Button? fillBtn = null;
        fillBtn = Button(T("Fill preset", "填入常用参数"), () =>
        {
            var presets = CustomCoreHelper.GetPresetsForProtocol(SelectedProtocol());
            if (presets.Count <= 1)
            {
                if (presets.Count == 1)
                    ApplyPreset(presets[0]);
                return;
            }

            var menu = new MenuFlyout();
            foreach (var preset in presets)
            {
                var captured = preset;
                var item = new MenuFlyoutItem { Text = captured.Name };
                item.Click += (_, _) => ApplyPreset(captured);
                menu.Items.Add(item);
            }
            if (fillBtn != null)
                menu.ShowAt(fillBtn);
        });
        fillBtn.VerticalAlignment = VerticalAlignment.Center;

        argsPanel = Stack(argsBox, chipsHint, chipsPanel, Split(useConfigBox, fillBtn, 8), configPanel, previewCard);

        protocolBox.SelectionChanged += (_, _) =>
        {
            if (syncing)
                return;

            var nextProto = SelectedProtocol();
            var prevMode = currentModes[Math.Clamp(modeBox.SelectedIndex, 0, currentModes.Length - 1)];
            currentModes = CustomCoreHelper.GetSupportedModes(nextProto).ToArray();
            var nextModeIdx = Array.IndexOf(currentModes, prevMode);
            if (nextModeIdx < 0)
                nextModeIdx = 0;

            syncing = true;
            try
            {
                modeBox.ItemsSource = currentModes.Select(CustomCoreModeLabel).ToArray();
                modeBox.SelectedIndex = nextModeIdx;
                RebuildChips(nextProto);

                var nextDefaultArgs = CustomCoreHelper.GetDefaultCliTemplate(nextProto);
                var nextConfigPreset = CustomCoreHelper.GetPresetsForProtocol(nextProto)
                    .FirstOrDefault(p => !string.IsNullOrWhiteSpace(p.ConfigTemplate));
                if (existing == null)
                {
                    if (useConfigBox.IsChecked == true && nextConfigPreset != null)
                    {
                        if (string.IsNullOrWhiteSpace(configBox.Text) ||
                            string.Equals(CustomCoreHelper.NormalizeConfigTemplate(configBox.Text), previousDefaultConfig, StringComparison.Ordinal))
                        {
                            extBox.SelectedIndex = Math.Max(0, Array.IndexOf(CustomCoreHelper.SupportedConfigExtensions,
                                CustomCoreHelper.NormalizeConfigExtension(nextConfigPreset.ConfigFileExtension)));
                            configBox.Text = nextConfigPreset.ConfigTemplate;
                        }
                        if (string.IsNullOrWhiteSpace(argsBox.Text) ||
                            string.Equals(CustomCoreHelper.NormalizeArgumentsInput(argsBox.Text), previousDefaultConfigArgs, StringComparison.Ordinal))
                        {
                            argsBox.Text = nextConfigPreset.ArgumentsTemplate;
                        }
                    }
                    else if (useConfigBox.IsChecked != true &&
                        (string.IsNullOrWhiteSpace(argsBox.Text) ||
                         string.Equals(CustomCoreHelper.NormalizeArgumentsInput(argsBox.Text), previousDefaultArgs, StringComparison.Ordinal)))
                    {
                        argsBox.Text = nextDefaultArgs;
                    }
                }
                previousDefaultArgs = CustomCoreHelper.NormalizeArgumentsInput(nextDefaultArgs);
                previousDefaultConfig = CustomCoreHelper.NormalizeConfigTemplate(nextConfigPreset?.ConfigTemplate);
                previousDefaultConfigArgs = CustomCoreHelper.NormalizeArgumentsInput(nextConfigPreset?.ArgumentsTemplate);
            }
            finally
            {
                syncing = false;
            }

            UpdateArgsPanelVisibility();
        };

        modeBox.SelectionChanged += (_, _) =>
        {
            if (!syncing)
                UpdateArgsPanelVisibility();
        };

        void OnUseConfigToggled()
        {
            if (syncing)
                return;

            if (useConfigBox.IsChecked == true)
            {
                var configPreset = CustomCoreHelper.GetPresetsForProtocol(SelectedProtocol())
                    .FirstOrDefault(p => !string.IsNullOrWhiteSpace(p.ConfigTemplate));
                syncing = true;
                try
                {
                    if (string.IsNullOrWhiteSpace(configBox.Text) && configPreset != null)
                    {
                        extBox.SelectedIndex = Math.Max(0, Array.IndexOf(CustomCoreHelper.SupportedConfigExtensions,
                            CustomCoreHelper.NormalizeConfigExtension(configPreset.ConfigFileExtension)));
                        configBox.Text = configPreset.ConfigTemplate;
                    }
                    if (!argsBox.Text.Contains("{config_file}", StringComparison.OrdinalIgnoreCase))
                        argsBox.Text = configPreset?.ArgumentsTemplate ?? "-c {config_file}";
                }
                finally
                {
                    syncing = false;
                }
            }
            else if (argsBox.Text.Contains("{config_file}", StringComparison.OrdinalIgnoreCase))
            {
                syncing = true;
                try
                {
                    argsBox.Text = CustomCoreHelper.GetDefaultCliTemplate(SelectedProtocol());
                }
                finally
                {
                    syncing = false;
                }
            }

            UpdateArgsPanelVisibility();
        }

        useConfigBox.Checked += (_, _) => OnUseConfigToggled();
        useConfigBox.Unchecked += (_, _) => OnUseConfigToggled();
        extBox.SelectionChanged += (_, _) => { if (!syncing) RefreshPreview(); };
        argsBox.TextChanged += (_, _) => { if (!syncing) RefreshPreview(); };
        configBox.TextChanged += (_, _) => { if (!syncing) RefreshPreview(); };

        var defaultBox = new CheckBox
        {
            Content = T("Set as default core for this protocol", "设为该协议默认核心"),
            IsChecked = existing?.IsProtocolDefault ?? false
        };

        UpdateArgsPanelVisibility();

        var editor = Stack(nameBox, protocolBox, exeRow, modeBox, argsPanel, defaultBox);
        editor.Width = XamlRoot?.Size.Width > 0 ? Math.Min(480, Math.Max(320, XamlRoot.Size.Width - 110)) : 480;

        CustomCore? candidate = null;
        var title = existing == null
            ? T("Add protocol custom core", "添加协议自定义核心")
            : T("Edit protocol custom core", "编辑协议自定义核心");
        if (!await ShowAsync(XamlRoot, title, editor, T("Save", "保存"), () =>
        {
            var selectedProto = SelectedProtocol();
            var mIdx = Math.Clamp(modeBox.SelectedIndex, 0, currentModes.Length - 1);
            var selectedMode = currentModes[mIdx];
            var selectedExt = CustomCoreHelper.SupportedConfigExtensions[
                Math.Clamp(extBox.SelectedIndex, 0, CustomCoreHelper.SupportedConfigExtensions.Length - 1)];

            candidate = new CustomCore
            {
                Id = existing?.Id ?? Guid.NewGuid().ToString("N"),
                Name = nameBox.Text.Trim(),
                Protocol = selectedProto,
                ExecutablePath = CustomCoreHelper.NormalizeExecutableInput(exeBox.Text),
                Mode = selectedMode,
                ArgumentsTemplate = selectedMode == CustomCoreMode.CliArguments ? CustomCoreHelper.NormalizeArgumentsInput(argsBox.Text) : string.Empty,
                ConfigTemplate = selectedMode == CustomCoreMode.CliArguments && useConfigBox.IsChecked == true ? CustomCoreHelper.NormalizeConfigTemplate(configBox.Text) : string.Empty,
                ConfigFileExtension = CustomCoreHelper.NormalizeConfigExtension(selectedExt),
                IsProtocolDefault = defaultBox.IsChecked == true
            };
            return CustomCoreHelper.Validate(candidate) is { } err ? i18N.Translate(err) : null;
        }, contentMaxWidth: 560) || candidate == null)
            return;

        var snapshot = Global.Settings.CustomCores
            .Select(c => new CustomCore
            {
                Id = c.Id,
                Name = c.Name,
                Protocol = c.Protocol,
                ExecutablePath = c.ExecutablePath,
                Mode = c.Mode,
                ArgumentsTemplate = c.ArgumentsTemplate,
                ConfigTemplate = c.ConfigTemplate,
                ConfigFileExtension = c.ConfigFileExtension,
                IsProtocolDefault = c.IsProtocolDefault
            })
            .ToList();
        var serverOverrides = existing == null
            ? new List<(Server Server, string? CoreId)>()
            : Global.Settings.Server
                .Where(s => string.Equals(s.ProtoExtra.CustomCoreId, candidate.Id, StringComparison.OrdinalIgnoreCase))
                .Select(s => (Server: s, CoreId: s.ProtoExtra.CustomCoreId))
                .ToList();

        if (await SaveSettingAsync(
            () => CustomCoreHelper.Save(candidate),
            () =>
            {
                Global.Settings.CustomCores = snapshot;
                foreach (var (s, oldId) in serverOverrides)
                    s.ProtoExtra.CustomCoreId = oldId;
            }))
        {
            ShowCategory(SettingsCategory.Cores);
        }
    }

    private async Task DeleteCustomCoreAsync(CustomCore core)
    {
        var title = TF("Delete custom core \"{0}\"?", "删除自定义核心「{0}」？", core.Name);
        if (!await ShowAsync(XamlRoot, title,
            Text(T("Nodes and subscriptions using this core will revert to the default behavior.",
                "使用此核心的节点与订阅将恢复默认运行方式。")),
            T("Delete", "删除")))
            return;

        var coresSnapshot = Global.Settings.CustomCores.ToList();
        var serverOverrides = Global.Settings.Server
            .Where(s => string.Equals(s.ProtoExtra.CustomCoreId, core.Id, StringComparison.OrdinalIgnoreCase))
            .Select(s => (Server: s, CoreId: s.ProtoExtra.CustomCoreId))
            .ToList();
        var subOverrides = Global.Settings.Subscription
            .Where(s => string.Equals(s.CustomCoreId, core.Id, StringComparison.OrdinalIgnoreCase))
            .Select(s => (Sub: s, CoreId: s.CustomCoreId))
            .ToList();

        if (await SaveSettingAsync(
            () => CustomCoreHelper.Delete(core.Id),
            () =>
            {
                Global.Settings.CustomCores = coresSnapshot;
                foreach (var (s, oldId) in serverOverrides)
                    s.ProtoExtra.CustomCoreId = oldId;
                foreach (var (s, oldId) in subOverrides)
                    s.CustomCoreId = oldId;
            }))
        {
            ShowCategory(SettingsCategory.Cores);
        }
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

        var intervals = Constants.AutoCheckUpdateIntervals;
        var intervalLabels = new[]
        {
            T("Off", "关闭"),
            T("Every 6 hours", "每 6 小时"),
            T("Every 12 hours", "每 12 小时"),
            T("Daily", "每天"),
            T("Weekly", "每周")
        };
        var currentIntervalIndex = Math.Max(0, intervals.ToList().IndexOf(settings.AutoCheckUpdateIntervalHours));
        var autoUpdate = Choice(T("Automatic update checks", "自动检查更新"), intervalLabels, currentIntervalIndex);
        BindSelection(
            autoUpdate,
            index => intervals[index],
            () => settings.AutoCheckUpdateIntervalHours,
            hours =>
            {
                settings.AutoCheckUpdateIntervalHours = hours;
                settings.CheckUpdateWhenOpened = hours > 0;
            });

        return Stack(
            Group(T("Download channel", "下载通道"),
                SettingRow(Icons.Download, T("Download channel", "下载通道"), channel),
                mirrorRow),
            Group(T("Automatic checks", "自动检查"),
                SettingRow(Icons.Refresh, T("Automatic update checks", "自动检查更新"),
                    autoUpdate,
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

    private (UIElement Control, Func<string> GetValue, Func<List<string>> GetCustomItems, Func<string?> CommitPendingInput) CustomizableChoice(
        string label,
        IReadOnlyList<string> presets,
        IEnumerable<string> customItems,
        string? initialValue,
        string placeholder,
        Func<string, (bool Valid, string Normalized, string? Error)> validate)
    {
        var workingCustom = customItems.ToList();
        var initialTrimmed = (initialValue ?? string.Empty).Trim();
        if (initialTrimmed.Length > 0 &&
            !presets.Any(p => string.Equals(p.Trim(), initialTrimmed, StringComparison.OrdinalIgnoreCase)))
        {
            var (valid, normalized, _) = validate(initialTrimmed);
            if (valid && normalized.Length > 0)
            {
                initialTrimmed = normalized;
                EndpointPresetHelper.RecordCustomValue(workingCustom, presets, normalized);
            }
        }

        var box = new ComboBox
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            FontFamily = new FontFamily("Consolas")
        };
        AutomationProperties.SetName(box, label);

        var deleteBtn = IconButton(Icons.Delete, T("Remove custom entry", "删除自定义项"));
        deleteBtn.VerticalAlignment = VerticalAlignment.Center;
        deleteBtn.Margin = new Thickness(6, 0, 0, 0);
        var selectRow = Split(box, deleteBtn, 0);

        var inlineInput = new TextBox
        {
            PlaceholderText = placeholder,
            FontFamily = new FontFamily("Consolas"),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Center
        };
        AutomationProperties.SetName(inlineInput, T("Add custom…", "添加自定义…"));

        var inlineError = new TextBlock
        {
            FontSize = 12,
            Style = Resource<Style>("NetchErrorText"),
            TextWrapping = TextWrapping.Wrap,
            Visibility = Visibility.Collapsed
        };

        var selectedValue = string.Empty;
        var syncing = false;

        ComboBoxItem? FindItem(string value) =>
            box.Items.OfType<ComboBoxItem>()
                .FirstOrDefault(item => item.Tag is string tag && string.Equals(tag, value, StringComparison.OrdinalIgnoreCase));

        void RefreshDeleteButton()
        {
            deleteBtn.Visibility = EndpointPresetHelper.IsCustomValue(workingCustom, presets, selectedValue)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        void RebuildItems(string? targetSelection)
        {
            var options = EndpointPresetHelper.BuildOptions(presets, workingCustom, targetSelection);
            var target = options.FirstOrDefault(o => string.Equals(o, targetSelection?.Trim(), StringComparison.OrdinalIgnoreCase))
                ?? options.FirstOrDefault()
                ?? string.Empty;

            syncing = true;
            try
            {
                // Detach selected visual content before clearing items so WinUI never hits stale parent errors.
                box.SelectedIndex = -1;
                box.Items.Clear();

                foreach (var option in options)
                {
                    if (EndpointPresetHelper.IsCustomValue(workingCustom, presets, option))
                    {
                        var textBlock = Text(option);
                        textBlock.FontFamily = new FontFamily("Consolas");
                        box.Items.Add(new ComboBoxItem
                        {
                            Tag = option,
                            Content = Split(textBlock, Pill(T("Custom", "自定义")), 8),
                            HorizontalContentAlignment = HorizontalAlignment.Stretch
                        });
                    }
                    else
                    {
                        box.Items.Add(new ComboBoxItem
                        {
                            Tag = option,
                            Content = option,
                            FontFamily = new FontFamily("Consolas")
                        });
                    }
                }

                box.Items.Add(new ComboBoxItem
                {
                    Tag = AddCustomSentinel,
                    Content = IconContent(Icons.Add, T("Add custom…", "添加自定义…"), iconSize: 12, spacing: 6),
                    Foreground = Resource<Brush>("AccentTextFillColorPrimaryBrush"),
                    FontFamily = FontFamily.XamlAutoFontFamily,
                    BorderThickness = new Thickness(0, 1, 0, 0),
                    BorderBrush = Resource<Brush>("CardStrokeColorDefaultBrush"),
                    Margin = new Thickness(0, 2, 0, 0)
                });

                var selectedItem = FindItem(target)
                    ?? box.Items.OfType<ComboBoxItem>().FirstOrDefault(i => i.Tag is string);
                box.SelectedItem = selectedItem;
                selectedValue = selectedItem?.Tag as string ?? string.Empty;
                RefreshDeleteButton();
            }
            finally
            {
                syncing = false;
            }
        }

        var inlineRow = new Grid
        {
            ColumnSpacing = 6,
            Visibility = Visibility.Collapsed
        };

        void EnterInlineMode()
        {
            inlineInput.Text = string.Empty;
            inlineError.Visibility = Visibility.Collapsed;
            selectRow.Visibility = Visibility.Collapsed;
            inlineRow.Visibility = Visibility.Visible;
            _ = DispatcherQueue.TryEnqueue(() => inlineInput.Focus(FocusState.Programmatic));
        }

        void CancelInline()
        {
            inlineInput.Text = string.Empty;
            inlineError.Visibility = Visibility.Collapsed;
            inlineRow.Visibility = Visibility.Collapsed;
            selectRow.Visibility = Visibility.Visible;
        }

        string? TryCommitInline()
        {
            var (valid, normalized, error) = validate(inlineInput.Text.Trim());
            if (!valid || string.IsNullOrWhiteSpace(normalized))
            {
                var message = error ?? T("Setting could not be changed", "无法更改设置");
                inlineError.Text = message;
                inlineError.Visibility = Visibility.Visible;
                return message;
            }

            EndpointPresetHelper.RecordCustomValue(workingCustom, presets, normalized);
            inlineInput.Text = string.Empty;
            inlineError.Visibility = Visibility.Collapsed;
            inlineRow.Visibility = Visibility.Collapsed;
            selectRow.Visibility = Visibility.Visible;
            RebuildItems(normalized);
            return null;
        }

        string? CommitPendingInput()
        {
            if (inlineRow.Visibility != Visibility.Visible)
                return null;

            if (string.IsNullOrWhiteSpace(inlineInput.Text))
            {
                CancelInline();
                return null;
            }

            return TryCommitInline();
        }

        var confirmBtn = Button(T("Add", "添加"), () => TryCommitInline(), accent: true);
        confirmBtn.VerticalAlignment = VerticalAlignment.Center;
        var cancelBtn = Button(T("Cancel", "取消"), CancelInline);
        cancelBtn.VerticalAlignment = VerticalAlignment.Center;

        inlineRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        inlineRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        inlineRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        inlineRow.Children.Add(inlineInput);
        Grid.SetColumn(confirmBtn, 1);
        Grid.SetColumn(cancelBtn, 2);
        inlineRow.Children.Add(confirmBtn);
        inlineRow.Children.Add(cancelBtn);

        inlineInput.TextChanged += (_, _) => inlineError.Visibility = Visibility.Collapsed;
        inlineInput.KeyDown += (_, e) =>
        {
            if (e.Key == VirtualKey.Enter)
            {
                e.Handled = true;
                TryCommitInline();
            }
            else if (e.Key == VirtualKey.Escape)
            {
                e.Handled = true;
                CancelInline();
            }
        };

        box.SelectionChanged += (_, _) =>
        {
            if (syncing)
                return;

            if (box.SelectedItem is ComboBoxItem { Tag: var tag })
            {
                if (ReferenceEquals(tag, AddCustomSentinel))
                {
                    syncing = true;
                    try { box.SelectedItem = FindItem(selectedValue); }
                    finally { syncing = false; }
                    EnterInlineMode();
                }
                else if (tag is string value)
                {
                    selectedValue = value;
                    RefreshDeleteButton();
                }
            }
        };

        deleteBtn.Click += (_, _) =>
        {
            if (!EndpointPresetHelper.RemoveCustomValue(workingCustom, presets, selectedValue))
                return;
            RebuildItems(presets.FirstOrDefault());
        };

        RebuildItems(initialTrimmed);

        var header = new TextBlock { Text = label };
        var container = new StackPanel
        {
            Spacing = 4,
            Children = { header, selectRow, inlineRow, inlineError }
        };

        return (container, () => selectedValue, () => workingCustom.ToList(), CommitPendingInput);
    }

    private async Task EditProbeAsync()
    {
        var s = Global.Settings;
        var stunPresets = Socks5ServerTestUtils.GetStunPresetEndpoints();
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

        static (bool Valid, string Normalized, string? Error) ValidateTestUrl(string raw)
        {
            if (Uri.TryCreate(raw, UriKind.Absolute, out var parsed) &&
                (parsed.Scheme == Uri.UriSchemeHttp || parsed.Scheme == Uri.UriSchemeHttps))
                return (true, raw, null);
            return (false, string.Empty, T("Enter a valid HTTP or HTTPS test URL.", "请输入有效的 HTTP 或 HTTPS 测速 URL。"));
        }

        static (bool Valid, string Normalized, string? Error) ValidateStun(string raw)
        {
            if (EndpointPresetHelper.TryParseStunEndpoint(raw, out var host, out var port))
                return (true, EndpointPresetHelper.FormatStunEndpoint(host, port), null);
            return (false, string.Empty, T("Enter a valid STUN server (host or host:port).", "请输入有效的 STUN 服务器（主机或 主机:端口）。"));
        }

        var urlChoice = CustomizableChoice(
            T("Test URL", "测速 URL"),
            Constants.DelayTestUrlPresets,
            s.CustomDelayTestUrls,
            s.DelayTestUrl,
            "https://example.com/generate_204",
            ValidateTestUrl);
        var timeout = Input(T("Timeout (ms)", "超时时间（毫秒）"), s.DelayTestTimeout.ToString());
        var steady = new CheckBox
        {
            Content = T("Measure steady-state RTT (second request on same tunnel)", "测稳态延迟（同一隧道发第二次请求）"),
            IsChecked = s.DelayTestSteadySecondRequest
        };
        var interval = Input(T("Auto test interval (s, 0 = manual only)", "自动测速间隔（秒，0 为仅手动）"), s.DetectionTick.ToString());
        var stunChoice = CustomizableChoice(
            T("STUN server", "STUN 服务器"),
            stunPresets,
            s.CustomStunServers,
            EndpointPresetHelper.FormatStunEndpoint(s.STUN_Server, s.STUN_Server_Port),
            "stun.example.com:3478",
            ValidateStun);

        void SyncMethodVisibility()
        {
            var needsUrl = method.SelectedIndex is 0 or 1;
            var vis = needsUrl ? Visibility.Visible : Visibility.Collapsed;
            urlChoice.Control.Visibility = vis;
            steady.Visibility = vis;
        }
        method.SelectionChanged += (_, _) => SyncMethodVisibility();
        SyncMethodVisibility();

        var editor = Stack(method, urlChoice.Control, timeout, steady, interval, stunChoice.Control);
        editor.Width = XamlRoot?.Size.Width > 0 ? Math.Min(440, Math.Max(320, XamlRoot.Size.Width - 110)) : 440;

        if (await ShowAsync(XamlRoot, T("Latency test · STUN", "测速方式 · STUN"),
            editor, T("Save", "保存"), () =>
        {
            var needsUrl = method.SelectedIndex is 0 or 1;
            if (needsUrl)
            {
                if (urlChoice.CommitPendingInput() is { } urlErr)
                    return urlErr;
                var rawUrl = urlChoice.GetValue();
                if (!Uri.TryCreate(rawUrl, UriKind.Absolute, out var parsedUrl) ||
                    (parsedUrl.Scheme != Uri.UriSchemeHttp && parsedUrl.Scheme != Uri.UriSchemeHttps))
                    return T("Enter a valid HTTP or HTTPS test URL.", "请输入有效的 HTTP 或 HTTPS 测速 URL。");
            }
            if (!int.TryParse(timeout.Text.Trim(), out var timeoutMs) ||
                timeoutMs < Constants.MinDelayTestTimeout || timeoutMs > Constants.MaxDelayTestTimeout)
                return TF("Enter a timeout between {0} and {1} ms.", "请输入 {0} 至 {1} 毫秒之间的超时时间。",
                    Constants.MinDelayTestTimeout, Constants.MaxDelayTestTimeout);
            if (!int.TryParse(interval.Text.Trim(), out var tickSeconds) ||
                (tickSeconds != 0 && (tickSeconds < 10 || tickSeconds > 86400)))
                return T("Enter 0 for manual testing, or an interval between 10 and 86400 seconds.", "请输入 0（仅手动）或 10 至 86400 秒之间的间隔。");
            if (stunChoice.CommitPendingInput() is { } stunErr)
                return stunErr;
            if (!EndpointPresetHelper.TryParseStunEndpoint(stunChoice.GetValue(), out _, out _))
                return T("Enter a valid STUN server (host or host:port).", "请输入有效的 STUN 服务器（主机或 主机:端口）。");
            return null;
        }, contentMaxWidth: 520))
        {
            var nextMode = (DelayTestMode)Math.Clamp(method.SelectedIndex, 0, 3);
            var needsUrl = nextMode is DelayTestMode.Hybrid or DelayTestMode.UrlTest;
            var nextUrl = needsUrl ? urlChoice.GetValue() : s.DelayTestUrl;
            var nextCustomUrls = urlChoice.GetCustomItems();
            if (needsUrl)
                EndpointPresetHelper.RecordCustomValue(nextCustomUrls, Constants.DelayTestUrlPresets, nextUrl);

            var nextTimeout = int.Parse(timeout.Text.Trim());
            var nextSteady = steady.IsChecked == true;
            var nextTick = int.Parse(interval.Text.Trim());
            EndpointPresetHelper.TryParseStunEndpoint(stunChoice.GetValue(), out var nextStun, out var nextStunPort);
            var formattedStun = EndpointPresetHelper.FormatStunEndpoint(nextStun, nextStunPort);
            var nextCustomStuns = stunChoice.GetCustomItems();
            EndpointPresetHelper.RecordCustomValue(nextCustomStuns, stunPresets, formattedStun);

            var previous = (
                s.DelayTestMode,
                s.ServerTCPing,
                s.DelayTestUrl,
                CustomDelayTestUrls: s.CustomDelayTestUrls.ToList(),
                s.DelayTestTimeout,
                s.DelayTestSteadySecondRequest,
                s.DetectionTick,
                s.DelayTestConfigVersion,
                s.STUN_Server,
                s.STUN_Server_Port,
                CustomStunServers: s.CustomStunServers.ToList());

            if (await SaveSettingAsync(
                () =>
                {
                    s.DelayTestMode = nextMode;
                    s.ServerTCPing = nextMode != DelayTestMode.ICMPing;
                    s.DelayTestUrl = nextUrl;
                    s.CustomDelayTestUrls = nextCustomUrls;
                    s.DelayTestTimeout = nextTimeout;
                    s.DelayTestSteadySecondRequest = nextSteady;
                    s.DetectionTick = nextTick;
                    s.DelayTestConfigVersion = 1;
                    s.STUN_Server = nextStun;
                    s.STUN_Server_Port = nextStunPort;
                    s.CustomStunServers = nextCustomStuns;
                },
                () =>
                {
                    s.DelayTestMode = previous.DelayTestMode;
                    s.ServerTCPing = previous.ServerTCPing;
                    s.DelayTestUrl = previous.DelayTestUrl;
                    s.CustomDelayTestUrls = previous.CustomDelayTestUrls;
                    s.DelayTestTimeout = previous.DelayTestTimeout;
                    s.DelayTestSteadySecondRequest = previous.DelayTestSteadySecondRequest;
                    s.DetectionTick = previous.DetectionTick;
                    s.DelayTestConfigVersion = previous.DelayTestConfigVersion;
                    s.STUN_Server = previous.STUN_Server;
                    s.STUN_Server_Port = previous.STUN_Server_Port;
                    s.CustomStunServers = previous.CustomStunServers;
                }))
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

        static (bool Valid, string Normalized, string? Error) ValidateDnsEndpoint(string raw)
        {
            if (DnsEndpoint.TryParse(raw, out var ep))
                return (true, ep.Normalized, null);
            return (false, string.Empty, T(
                "Enter a valid upstream DNS endpoint (udp://, tcp://, tls://, or https://).",
                "请输入有效的上游 DNS 地址（支持 udp://、tcp://、tls://、https://）。"));
        }

        static (bool Valid, string Normalized, string? Error) ValidateOutboundIp(string raw)
        {
            if (DnsEndpoint.TryParseStrictIpAddress(raw, out var ip))
                return (true, ip.ToString(), null);
            return (false, string.Empty, T("Enter a valid IPv4 or IPv6 address.", "请输入有效的 IPv4 或 IPv6 地址。"));
        }

        var chinaChoice = CustomizableChoice(
            T("AioDNS domestic upstream", "AioDNS 国内上游"),
            Constants.AioDnsChinaPresets,
            settings.AioDNS.CustomChinaDns,
            settings.AioDNS.ChinaDNS,
            "udp:// / tls:// / https://",
            ValidateDnsEndpoint);
        var otherChoice = CustomizableChoice(
            T("AioDNS international upstream", "AioDNS 国外上游"),
            Constants.AioDnsOtherPresets,
            settings.AioDNS.CustomOtherDns,
            settings.AioDNS.OtherDNS,
            "udp:// / tls:// / https://",
            ValidateDnsEndpoint);
        var outboundChoice = CustomizableChoice(
            T("Outbound DNS", "出站 DNS"),
            Constants.OutboundDnsPresets,
            settings.CustomOutboundDns,
            settings.OutboundDNS,
            "223.5.5.5",
            ValidateOutboundIp);

        var editor = Stack(chinaChoice.Control, otherChoice.Control, outboundChoice.Control);
        editor.Width = XamlRoot?.Size.Width > 0 ? Math.Min(440, Math.Max(320, XamlRoot.Size.Width - 110)) : 440;

        if (await ShowAsync(XamlRoot, "DNS", editor, T("Save", "保存"), () =>
        {
            if (chinaChoice.CommitPendingInput() is { } chinaErr)
                return chinaErr;
            if (otherChoice.CommitPendingInput() is { } otherErr)
                return otherErr;
            if (outboundChoice.CommitPendingInput() is { } outboundErr)
                return outboundErr;
            if (!DnsEndpoint.TryParse(chinaChoice.GetValue(), out _) ||
                !DnsEndpoint.TryParse(otherChoice.GetValue(), out _) ||
                !DnsEndpoint.TryParseStrictIpAddress(outboundChoice.GetValue(), out _))
            {
                return T("Enter valid upstream DNS endpoints (udp://, tcp://, tls://, or https://) and an outbound DNS IP.",
                    "请输入有效的上游 DNS 地址（支持 udp://、tcp://、tls://、https://）和出站 DNS IP。");
            }
            return null;
        }, contentMaxWidth: 520))
        {
            if (!DnsEndpoint.TryParse(chinaChoice.GetValue(), out var chinaEndpoint) ||
                !DnsEndpoint.TryParse(otherChoice.GetValue(), out var otherEndpoint) ||
                !DnsEndpoint.TryParseStrictIpAddress(outboundChoice.GetValue(), out var outboundIp))
                return;

            var nextChina = chinaEndpoint.Normalized;
            var nextOther = otherEndpoint.Normalized;
            var nextOutbound = outboundIp.ToString();

            var nextCustomChina = chinaChoice.GetCustomItems();
            var nextCustomOther = otherChoice.GetCustomItems();
            var nextCustomOutbound = outboundChoice.GetCustomItems();
            EndpointPresetHelper.RecordCustomValue(nextCustomChina, Constants.AioDnsChinaPresets, nextChina);
            EndpointPresetHelper.RecordCustomValue(nextCustomOther, Constants.AioDnsOtherPresets, nextOther);
            EndpointPresetHelper.RecordCustomValue(nextCustomOutbound, Constants.OutboundDnsPresets, nextOutbound);

            var previous = (
                China: settings.AioDNS.ChinaDNS,
                Other: settings.AioDNS.OtherDNS,
                Outbound: settings.OutboundDNS,
                CustomChina: settings.AioDNS.CustomChinaDns.ToList(),
                CustomOther: settings.AioDNS.CustomOtherDns.ToList(),
                CustomOutbound: settings.CustomOutboundDns.ToList());

            if (await SaveSettingAsync(
                () =>
                {
                    settings.AioDNS.ChinaDNS = nextChina;
                    settings.AioDNS.OtherDNS = nextOther;
                    settings.OutboundDNS = nextOutbound;
                    settings.AioDNS.CustomChinaDns = nextCustomChina;
                    settings.AioDNS.CustomOtherDns = nextCustomOther;
                    settings.CustomOutboundDns = nextCustomOutbound;
                },
                () =>
                {
                    settings.AioDNS.ChinaDNS = previous.China;
                    settings.AioDNS.OtherDNS = previous.Other;
                    settings.OutboundDNS = previous.Outbound;
                    settings.AioDNS.CustomChinaDns = previous.CustomChina;
                    settings.AioDNS.CustomOtherDns = previous.CustomOther;
                    settings.CustomOutboundDns = previous.CustomOutbound;
                }) &&
                !string.Equals(previous.Outbound, settings.OutboundDNS, StringComparison.OrdinalIgnoreCase))
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
