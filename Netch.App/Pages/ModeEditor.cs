using System.Net;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Windows.Storage.Pickers;
using Netch.App.Helpers;
using Netch.App.Services;
using Netch.Controllers;
using Netch.Models;
using Netch.Models.Modes;
using Netch.Models.Modes.ProcessMode;
using Netch.Models.Modes.ShareMode;
using Netch.Models.Modes.TunMode;
using Netch.Models.Modes.WebProxyMode;
using Netch.Services;
using Netch.Utils;
using static Netch.App.Helpers.Ui;

namespace Netch.App.Pages;

internal sealed class ModeEditor : UserControl
{
    private readonly Mode? _original;
    private readonly Mode? _template;
    private readonly Action<bool>? _onClose;
    private readonly TextBox _name;
    private readonly TextBox _group;
    private readonly ComboBox _type;
    private readonly ContentControl _editor = new()
    {
        HorizontalContentAlignment = HorizontalAlignment.Stretch, VerticalContentAlignment = VerticalAlignment.Stretch,
        Margin = new Thickness(0, 4, 0, 0)
    };
    private readonly InfoBar _message = new() { IsClosable = false };
    private readonly Dictionary<ModeType, Editor> _drafts = [];
    private sealed record Editor(Mode Mode, UIElement View, Action Read);

    public ModeEditor(ModeType type, Mode? original, Action<bool>? onClose = null, Mode? template = null)
    {
        _original = original;
        _template = template;
        _onClose = onClose;
        var initialName = original != null ? original.i18NRemark
            : template != null ? TF("{0} (copy)", "{0} 副本", template.i18NRemark)
            : null;
        var seed = original ?? template;
        var initialGroup = seed == null ? "Custom" : ModesPage.GroupOf(seed);
        if (template != null && initialGroup.Length == 0)
            initialGroup = "Custom";
        _name = Input(T("Name", "名称"), initialName, search: true);
        _group = Input(T("Group", "分组"), initialGroup, search: true);
        _group.Width = 142;
        _type = Choice(T("Type", "类型"), Enum.GetValues<ModeType>().Select(Helpers.Format.ModeTypeLongName), (int)type);
        _type.Width = 156;

        var breadcrumb = new BreadcrumbBar { ItemsSource = new[] { T("Modes", "模式"), initialName ?? T("New mode", "新建模式") } };
        breadcrumb.ItemClicked += (_, e) => { if (e.Index == 0) Close(saved: false); };
        var cancel = Button(T("Cancel", "取消"), () => Close(saved: false));
        var verify = Button(T("Validate rules", "验证规则"), () =>
        {
            var valid = ReadAndValidate() != null;
            if (valid)
                Report(T("Rules are valid.", "规则有效。"), InfoBarSeverity.Success);
        });
        Content = UiPage();
        _type.SelectionChanged += (_, _) => ShowType();
        ShowType();

        FrameworkElement UiPage()
        {
            var detail = Body(_message, _editor);
            detail.RowSpacing = 0;
            var layout = Body(Split(breadcrumb, Row(cancel, verify, AsyncButton(T("Save", "保存"), SaveAsync, true))),
                Split(Field(T("Name", "名称"), _name), Row(Field(T("Type", "类型"), _type), Field(T("Group", "分组"), _group))),
                detail);
            layout.RowSpacing = 9;
            return Helpers.Ui.Page(layout);
        }
    }

    private void Close(bool saved)
    {
        if (_onClose != null)
            _onClose(saved);
        else
            App.MainWindow?.Navigate("Modes", force: true);
    }

    private void ShowType()
    {
        var type = (ModeType)_type.SelectedIndex;
        if (!_drafts.TryGetValue(type, out var draft))
            _drafts[type] = draft = CreateEditor(type);
        _editor.Content = draft.View;
        _message.IsOpen = false;
        _message.Margin = new Thickness(0);
    }

    private Editor CreateEditor(ModeType type)
    {
        var source = _original ?? _template;
        switch (type)
        {
            case ModeType.ProcessMode:
            {
                var original = source as Redirector;
                var mode = new Redirector
                {
                    FilterTCP = original?.FilterTCP, FilterUDP = original?.FilterUDP, FilterICMP = original?.FilterICMP,
                    FilterDNS = original?.FilterDNS, ICMPDelay = original?.ICMPDelay, FilterParent = original?.FilterParent,
                    DNSProxy = original?.DNSProxy, DNSHost = original?.DNSHost, HandleOnlyDNS = original?.HandleOnlyDNS,
                    FilterLoopback = original?.FilterLoopback ?? false, FilterIntranet = original?.FilterIntranet ?? true
                };
                ModeHelper.CompleteSceneDefaults(mode);

                var include = Rules(T("Proxy these programs", "代理这些程序"), original?.Handle ?? [], true, true);
                var exclude = Rules(T("Exclude these programs", "排除这些程序"), original?.Bypass ?? [], true, false);
                var includeIps = Rules(T("Proxy these IPs / CIDRs", "代理这些 IP / CIDR"), original?.HandleIPs ?? [], false, false, "IP / CIDR");
                var excludeIps = Rules(T("Bypass these IPs / CIDRs", "绕过这些 IP / CIDR"), original?.BypassIPs ?? [], false, false, "IP / CIDR");
                var tcp = Switch(T("Handle TCP", "处理 TCP"), mode.FilterTCP == true);
                var udp = Switch(T("Handle UDP", "处理 UDP"), mode.FilterUDP == true);
                var icmp = Switch(T("Handle ICMP", "处理 ICMP"), mode.FilterICMP == true);
                var delay = Input(T("ICMP delay (ms)", "ICMP 延迟（毫秒）"), mode.ICMPDelay.ToString());
                var dns = Switch(T("Hijack DNS", "DNS 劫持"), mode.FilterDNS == true);
                var dnsHost = Input(T("DNS endpoint", "DNS 地址"), mode.DNSHost);
                var proxy = Switch(T("Proxy DNS", "DNS 走代理"), mode.DNSProxy == true);
                var dnsOnly = Switch(T("Handle only DNS on port 53", "端口 53 仅处理 DNS"), mode.HandleOnlyDNS == true);
                var parent = Switch(T("Handle child processes", "处理子进程"), mode.FilterParent == true);
                var loop = Switch(T("Handle loopback", "处理本地回环"), mode.FilterLoopback);
                var lan = Switch(T("Handle LAN", "处理局域网"), mode.FilterIntranet);

                void SyncIcmpState() => delay.IsEnabled = icmp.IsOn;
                void SyncDnsState()
                {
                    var enabled = dns.IsOn;
                    dnsHost.IsEnabled = enabled;
                    proxy.IsEnabled = enabled;
                    dnsOnly.IsEnabled = enabled;
                }
                icmp.Toggled += (_, _) => SyncIcmpState();
                dns.Toggled += (_, _) => SyncDnsState();
                SyncIcmpState();
                SyncDnsState();

                var settings = Card(Stack(
                    SwitchRow(tcp),
                    SwitchRow(udp),
                    SwitchRow(icmp),
                    delay,
                    SwitchRow(dns),
                    dnsHost,
                    SwitchRow(proxy),
                    SwitchRow(dnsOnly),
                    SwitchRow(parent),
                    SwitchRow(loop),
                    SwitchRow(lan)));
                var lists = Stack(include, exclude, includeIps, excludeIps);
                lists.Spacing = 8;
                return new Editor(mode, Layout(lists, settings), () =>
                {
                    mode.Handle = ReadRules(include);
                    mode.Bypass = ReadRules(exclude);
                    var rawIncludeIps = ReadRules(includeIps);
                    var rawExcludeIps = ReadRules(excludeIps);
                    mode.FilterTCP = tcp.IsOn;
                    mode.FilterUDP = udp.IsOn;
                    mode.FilterICMP = icmp.IsOn;
                    mode.FilterDNS = dns.IsOn;
                    mode.DNSProxy = proxy.IsOn;
                    mode.HandleOnlyDNS = dnsOnly.IsOn;
                    mode.FilterParent = parent.IsOn;
                    mode.FilterLoopback = loop.IsOn;
                    mode.FilterIntranet = lan.IsOn;
                    if (icmp.IsOn)
                    {
                        if (!int.TryParse(delay.Text, out var ms) || ms is < 0 or > 60000)
                            throw new InvalidDataException(T("Invalid ICMP delay.", "ICMP 延迟无效。"));
                        mode.ICMPDelay = ms;
                    }
                    else if (int.TryParse(delay.Text, out var ms) && ms is >= 0 and <= 60000)
                    {
                        mode.ICMPDelay = ms;
                    }
                    mode.DNSHost = dnsHost.Text.Trim();
                    if (dns.IsOn && mode.DNSHost.Length > 0 && !IPEndPoint.TryParse(mode.DNSHost, out _))
                        throw new InvalidDataException(T("Invalid DNS endpoint.", "DNS 地址无效。"));
                    if (!NFController.CheckIpRules(rawIncludeIps.Concat(rawExcludeIps), out var invalidIps))
                        throw new InvalidDataException(NFController.GenerateInvalidIpRulesMessage(invalidIps));
                    mode.HandleIPs = rawIncludeIps.Select(r => RouteUtils.TryNormalizeIpOrCidr(r, out var norm) ? norm : r).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                    mode.BypassIPs = rawExcludeIps.Select(r => RouteUtils.TryNormalizeIpOrCidr(r, out var norm) ? norm : r).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                    if (!Session.Current.IsIdle)
                        throw new InvalidOperationException(T("Disconnect before validating process rules.", "请先断开连接，再验证进程规则。"));
                    if (!NFController.CheckRules(mode.Handle.Concat(mode.Bypass), out var invalid))
                        throw new InvalidDataException(NFController.GenerateInvalidRulesMessage(invalid));
                });
            }
            case ModeType.TunMode:
            {
                var original = source as TunMode;
                var global = original?.Network ?? Global.Settings.TUNTAP;
                var config = new TUNConfig();
                var mode = new TunMode { Network = config };
                var include = Rules(T("Proxy these routes", "代理这些路由"), original?.Handle ?? ["0.0.0.0/0", "::/0"], false, true);
                var exclude = Rules(T("Exclude these routes", "排除这些路由"), original?.Bypass ?? [], false, false);
                var address = Input(T("Address", "地址"), global.Address);
                var mask = Input(T("Subnet mask", "子网掩码"), global.Netmask);
                var gateway = Input(T("Gateway", "网关"), global.Gateway);
                var dns = Input("DNS", global.DNS);
                var custom = Switch(T("Use custom DNS", "使用自定义 DNS"), global.UseCustomDNS);
                var proxy = Switch(T("Proxy DNS", "代理 DNS"), global.ProxyDNS);
                var ipv6 = Switch("IPv6", global.EnableIPv6);
                var ipv6Address = Input(T("IPv6 address", "IPv6 地址"), global.IPv6Address);
                var ipv6Gateway = Input(T("IPv6 gateway", "IPv6 网关"), global.IPv6Gateway);
                var prefix = Input(T("IPv6 prefix length", "IPv6 前缀长度"), global.IPv6PrefixLength.ToString());
                var bypass = Rules(T("Global bypass IPs", "全局绕过 IP"), global.BypassIPs, false, false);

                void SyncCustomDnsState()
                {
                    var enabled = custom.IsOn;
                    dns.IsEnabled = enabled;
                    proxy.IsEnabled = enabled;
                }
                void SyncIpv6State()
                {
                    var enabled = ipv6.IsOn;
                    ipv6Address.IsEnabled = enabled;
                    ipv6Gateway.IsEnabled = enabled;
                    prefix.IsEnabled = enabled;
                }
                custom.Toggled += (_, _) => SyncCustomDnsState();
                ipv6.Toggled += (_, _) => SyncIpv6State();
                SyncCustomDnsState();
                SyncIpv6State();

                return new Editor(mode, Layout(Stack(include, exclude), Card(Stack(address, mask, gateway, SwitchRow(custom),
                    dns, SwitchRow(proxy), SwitchRow(ipv6), ipv6Address, ipv6Gateway, prefix, bypass))), () =>
                {
                    mode.Handle = ReadRules(include);
                    mode.Bypass = ReadRules(exclude);
                    config.Address = address.Text.Trim();
                    config.Netmask = mask.Text.Trim();
                    config.Gateway = gateway.Text.Trim();
                    config.UseCustomDNS = custom.IsOn;
                    config.DNS = dns.Text.Trim();
                    config.ProxyDNS = proxy.IsOn;
                    config.EnableIPv6 = ipv6.IsOn;
                    config.IPv6Address = ipv6Address.Text.Trim();
                    config.IPv6Gateway = ipv6Gateway.Text.Trim();
                    if (ipv6.IsOn)
                    {
                        if (!byte.TryParse(prefix.Text, out var bits))
                            throw new InvalidDataException(T("Invalid IPv6 prefix length.", "IPv6 前缀长度无效。"));
                        config.IPv6PrefixLength = bits;
                    }
                    else
                    {
                        config.IPv6PrefixLength = byte.TryParse(prefix.Text, out var bits) ? bits : global.IPv6PrefixLength;
                    }
                    config.BypassIPs = ReadRules(bypass);
                    TUNController.ValidateConfiguration(mode, config);
                });
            }
            case ModeType.WebProxyMode:
            {
                var original = source as WebProxyMode;
                var mode = new WebProxyMode
                {
                    Strategy = original?.Strategy ?? WebProxyStrategy.Pac,
                    PacPreset = original?.PacPreset ?? PacRulePreset.ProxyList,
                    DefaultProxy = original?.DefaultProxy ?? false,
                    BypassLan = original?.BypassLan ?? true,
                    Handle = original?.Handle.ToList() ?? [],
                    Bypass = original?.Bypass.ToList() ?? [],
                    PacScriptPath = original?.PacScriptPath,
                    PacRuleUrl = original?.PacRuleUrl
                };

                var strategy = Choice(T("Proxy strategy", "代理策略"),
                    [T("PAC auto-proxy", "PAC 自动分流"), T("Global system proxy", "全局系统代理"), T("Local proxy only (Manual)", "仅本地端口（手动）")],
                    (int)mode.Strategy);
                strategy.Header = T("Proxy strategy", "代理策略");
                strategy.HorizontalAlignment = HorizontalAlignment.Stretch;

                var pacPreset = Choice(T("PAC rule set", "PAC 规则集"),
                    [
                        T("Proxy list (Loyalsoldier · overseas)", "海外代理列表（Loyalsoldier · proxy-list）"),
                        T("GFWList (Loyalsoldier · lightweight)", "精简 GFWList（Loyalsoldier · gfw）"),
                        T("Bypass China (Loyalsoldier · direct-list)", "绕过中国大陆（Loyalsoldier · direct-list）"),
                        T("Custom rules only", "仅使用自定义规则"),
                        T("Custom PAC script", "自定义 PAC 脚本")
                    ],
                    (int)mode.PacPreset);
                pacPreset.Header = T("PAC rule set", "PAC 规则集");
                pacPreset.HorizontalAlignment = HorizontalAlignment.Stretch;

                var ruleStatusText = Caption("");
                ruleStatusText.TextWrapping = TextWrapping.Wrap;
                var pacRuleUrl = Input(T("Rule download URL (optional)", "规则下载地址（可选）"), mode.PacRuleUrl);

                void RefreshRuleStatus()
                {
                    var preset = (PacRulePreset)pacPreset.SelectedIndex;
                    pacRuleUrl.PlaceholderText = PacRuleService.GetDefaultUrl(preset) ?? "";
                    var status = PacRuleService.GetRuleStatus(preset);
                    ruleStatusText.Text = status.Exists
                        ? TF("{0:N0} rules · {1}", "{0:N0} 条规则 · {1}",
                            status.RuleCount,
                            status.IsUserUpdated && status.LastWriteTime is { } dt
                                ? Format.LastUsed(dt)
                                : T("Bundled", "随包内置"))
                        : T("Not downloaded yet", "尚未下载");
                }

                var updateRuleBtn = AsyncButton(T("Downloads", "下载"), async () =>
                {
                    var preset = (PacRulePreset)pacPreset.SelectedIndex;
                    var customUrl = pacRuleUrl.Text.Trim();
                    if (customUrl.Length == 0)
                    {
                        App.MainWindow?.Navigate("Downloads", "pac");
                        return;
                    }

                    if (!Uri.TryCreate(customUrl, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
                    {
                        Report(T("Enter an HTTP or HTTPS PAC rule URL.", "请输入有效的 HTTP 或 HTTPS PAC 规则地址。"), InfoBarSeverity.Error);
                        return;
                    }

                    ruleStatusText.Text = T("Downloading PAC rule lists…", "正在下载 PAC 规则表…");
                    try
                    {
                        if (!await Downloads.UpdatePacRuleAsync(preset, customUrl))
                        {
                            Report(T("Another download is already in progress.", "当前已有下载任务在进行中。"), InfoBarSeverity.Warning);
                            return;
                        }

                        var updatedStatus = PacRuleService.GetRuleStatus(preset);
                        Report(TF("Updated {0:N0} PAC rules.", "已更新 {0:N0} 条 PAC 规则。", updatedStatus.RuleCount), InfoBarSeverity.Success);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "Update PAC rules failed");
                        Report(ex.Message, InfoBarSeverity.Error);
                    }
                    finally
                    {
                        RefreshRuleStatus();
                    }
                });

                void SyncRuleActionButton()
                {
                    var hasCustomUrl = pacRuleUrl.Text.Trim().Length > 0;
                    SetButtonContent(updateRuleBtn,
                        hasCustomUrl ? T("Update rules", "更新规则") : T("Downloads", "下载"),
                        hasCustomUrl ? Icons.Refresh : Icons.Download);
                }

                pacRuleUrl.TextChanged += (_, _) => SyncRuleActionButton();
                var ruleStatusRow = Split(ruleStatusText, updateRuleBtn);

                var defaultProxy = Switch(T("Proxy unmatched domains", "未命中域名走代理"), mode.DefaultProxy);
                var defaultProxyRow = SwitchRow(defaultProxy);

                var pacScriptPath = Input(T("PAC script path", "PAC 脚本路径"), mode.PacScriptPath);
                var browseScript = AsyncButton(T("Browse…", "浏览…"), async () =>
                {
                    var picker = new FileOpenPicker(App.MainWindow!.AppWindow.Id);
                    picker.FileTypeFilter.Add(".pac");
                    picker.FileTypeFilter.Add(".js");
                    if (await picker.PickSingleFileAsync() is { } file)
                        pacScriptPath.Text = file.Path;
                }, glyph: Icons.OpenFile);
                browseScript.VerticalAlignment = VerticalAlignment.Bottom;
                var pacScriptPanel = Split(pacScriptPath, browseScript, 8);

                var bypassLan = Switch(T("Bypass LAN and loopback", "绕过局域网与回环"), mode.BypassLan);
                var bypassLanRow = SwitchRow(bypassLan);

                var pacInclude = Rules(T("Proxy these domains", "代理这些域名"), mode.Handle, false, false, T("Domain", "域名"));
                var excludeList = Rules(T("Bypass these domains", "直连这些域名"), mode.Bypass, false, false, T("Bypass", "绕过"));
                var excludeTitle = (TextBlock)((Grid)excludeList.Children[0]).Children[0];
                var globalBypassHint = Caption(T("Leave empty to use the default LAN bypass list from Settings.", "留空时将使用“设置”中的默认局域网绕过列表。"));
                globalBypassHint.TextWrapping = TextWrapping.Wrap;

                var scriptHintTitle = Text(T("Custom PAC script", "自定义 PAC 脚本"), 14, true);
                var scriptHintBody = Caption(T("Select a local .pac or .js file defining FindProxyForURL(url, host). The __PROXY__ placeholder will automatically bind to the local mixed port.",
                    "选择包含 FindProxyForURL(url, host) 的本地 .pac 或 .js 脚本。脚本中的 __PROXY__ 占位符会自动替换为本地混合代理端口。"));
                scriptHintBody.TextWrapping = TextWrapping.Wrap;
                var scriptHintCard = Card(Stack(scriptHintTitle, scriptHintBody));

                var manualHintTitle = Text(T("Local proxy only (Manual)", "仅本地端口（手动）"), 14, true);
                var manualHintBody = Caption(TF("Starts only the local mixed proxy at 127.0.0.1:{0} (HTTP & SOCKS5) without modifying Windows system proxy.",
                    "仅在 127.0.0.1:{0} 启动本地混合代理（同时支持 HTTP 与 SOCKS5），不修改 Windows 系统代理。", Global.Settings.Socks5LocalPort));
                manualHintBody.TextWrapping = TextWrapping.Wrap;
                var manualHintCard = Card(Stack(manualHintTitle, manualHintBody));

                var leftHost = new StackPanel { Spacing = 8, Children = { pacInclude, excludeList, globalBypassHint, scriptHintCard, manualHintCard } };

                void SyncVisibility()
                {
                    var currentStrategy = (WebProxyStrategy)strategy.SelectedIndex;
                    var currentPreset = (PacRulePreset)pacPreset.SelectedIndex;
                    var isPac = currentStrategy == WebProxyStrategy.Pac;
                    var isCustomScript = isPac && currentPreset == PacRulePreset.CustomScript;
                    var hasRemoteRule = isPac && currentPreset is PacRulePreset.ProxyList or PacRulePreset.GfwList or PacRulePreset.BypassChina;

                    pacPreset.Visibility = isPac ? Visibility.Visible : Visibility.Collapsed;
                    ruleStatusRow.Visibility = hasRemoteRule ? Visibility.Visible : Visibility.Collapsed;
                    pacRuleUrl.Visibility = hasRemoteRule ? Visibility.Visible : Visibility.Collapsed;
                    defaultProxyRow.Visibility = isPac && currentPreset == PacRulePreset.CustomRulesOnly ? Visibility.Visible : Visibility.Collapsed;
                    pacScriptPanel.Visibility = isCustomScript ? Visibility.Visible : Visibility.Collapsed;
                    bypassLanRow.Visibility = currentStrategy != WebProxyStrategy.Manual && !isCustomScript
                        ? Visibility.Visible : Visibility.Collapsed;

                    pacInclude.Visibility = isPac && !isCustomScript ? Visibility.Visible : Visibility.Collapsed;
                    excludeList.Visibility = currentStrategy == WebProxyStrategy.Global || (isPac && !isCustomScript)
                        ? Visibility.Visible : Visibility.Collapsed;
                    excludeTitle.Text = currentStrategy == WebProxyStrategy.Global
                        ? T("Bypass these addresses or domains", "绕过这些地址或域名")
                        : T("Bypass these domains", "直连这些域名");
                    globalBypassHint.Visibility = currentStrategy == WebProxyStrategy.Global ? Visibility.Visible : Visibility.Collapsed;
                    scriptHintCard.Visibility = isCustomScript ? Visibility.Visible : Visibility.Collapsed;
                    manualHintCard.Visibility = currentStrategy == WebProxyStrategy.Manual ? Visibility.Visible : Visibility.Collapsed;

                    if (hasRemoteRule)
                    {
                        RefreshRuleStatus();
                        SyncRuleActionButton();
                    }
                }

                strategy.SelectionChanged += (_, _) => SyncVisibility();
                pacPreset.SelectionChanged += (_, _) => SyncVisibility();
                SyncVisibility();

                var settingsCard = Card(Stack(
                    strategy,
                    pacPreset,
                    ruleStatusRow,
                    pacRuleUrl,
                    defaultProxyRow,
                    pacScriptPanel,
                    bypassLanRow));

                return new Editor(mode, Layout(leftHost, settingsCard), () =>
                {
                    mode.Strategy = (WebProxyStrategy)strategy.SelectedIndex;
                    mode.PacPreset = (PacRulePreset)pacPreset.SelectedIndex;
                    mode.DefaultProxy = defaultProxy.IsOn;
                    mode.BypassLan = bypassLan.IsOn;

                    var isPac = mode.Strategy == WebProxyStrategy.Pac;
                    var isCustomScript = isPac && mode.PacPreset == PacRulePreset.CustomScript;
                    var hasRemoteRule = isPac && mode.PacPreset is PacRulePreset.ProxyList or PacRulePreset.GfwList or PacRulePreset.BypassChina;
                    mode.PacScriptPath = isCustomScript && !string.IsNullOrWhiteSpace(pacScriptPath.Text)
                        ? pacScriptPath.Text.Trim()
                        : null;
                    mode.PacRuleUrl = hasRemoteRule && !string.IsNullOrWhiteSpace(pacRuleUrl.Text)
                        ? pacRuleUrl.Text.Trim()
                        : null;
                    mode.Handle = isPac && !isCustomScript ? ReadRules(pacInclude) : [];
                    mode.Bypass = mode.Strategy == WebProxyStrategy.Global || (isPac && !isCustomScript)
                        ? ReadRules(excludeList)
                        : [];
                    PacScriptBuilder.ValidateMode(mode);
                });
            }
            default:
            {
                var mode = new ShareMode();
                var args = Input(T("pcap2socks arguments", "pcap2socks 参数"), (source as ShareMode)?.Argument ?? mode.Argument);
                var scroll = Scroll(Card(Stack(args)));
                scroll.Margin = new Thickness(0, 0, -Ui.ScrollBarGutter, 0);
                return new Editor(mode, scroll, () => mode.Argument = args.Text);
            }
        }
    }

    private static ToggleSwitch Switch(string label, bool value) => new()
    {
        OnContent = label, OffContent = label, IsOn = value
    };

    private static StackPanel Field(string label, FrameworkElement input)
    {
        input.Height = 36;
        return new StackPanel { Spacing = 5, Children = { Caption(label), input } };
    }

    private static Grid SwitchRow(ToggleSwitch toggle)
    {
        var label = (string)toggle.OnContent;
        CompactToggle(toggle);
        toggle.Width = 42;
        toggle.VerticalAlignment = VerticalAlignment.Center;
        AutomationProperties.SetName(toggle, label);
        var row = Split(Text(label), toggle);
        row.MinHeight = 40;
        return row;
    }

    private static FrameworkElement Layout(UIElement rules, Border settings)
    {
        if (settings.Child is FrameworkElement inner)
        {
            settings.Child = null;
            settings.Child = Scroll(inner);
        }
        settings.Padding = new Thickness(12, 10, 2, 10);
        return TwoColumns(Scroll(rules), settings, new GridLength(1.216, GridUnitType.Star));
    }

    private static StackPanel Rules(string title, IEnumerable<string> initial, bool executables, bool scan, string? ruleBadge = null)
    {
        var list = new ListView
        {
            MaxHeight = 238,
            ItemContainerStyle = Resource<Style>("NetchCompactRow"),
            Padding = new Thickness(0, 0, Ui.ScrollBarGutter, 0)
        };
        var console = new FontFamily("Consolas");
        void Add(string rule)
        {
            if (list.Items.OfType<ListViewItem>().Any(i => Equals(i.Tag, rule)))
                return;
            var label = Caption(rule);
            label.FontFamily = console;
            var item = new ListViewItem
            {
                Tag = rule, HorizontalContentAlignment = HorizontalAlignment.Stretch,
                Content = Split(label, Caption(ruleBadge ?? (executables ? T("Regex", "正则") : "CIDR")))
            };
            async Task EditSingleAsync()
            {
                var current = (string)item.Tag;
                var box = Input(T("Edit rule", "编辑规则"), current);
                if (!await ShowAsync(list.XamlRoot, T("Edit rule", "编辑规则"), box, T("Apply", "应用"), () =>
                    {
                        var updated = box.Text.Trim();
                        return updated.Length == 0 ? T("Enter a rule.", "请输入规则。") : null;
                    }))
                    return;

                var value = box.Text.Trim();
                if (value == current)
                    return;
                if (list.Items.OfType<ListViewItem>().Any(i => i != item && Equals(i.Tag, value)))
                {
                    list.Items.Remove(item);
                    return;
                }
                item.Tag = value;
                label.Text = value;
            }
            item.ContextFlyout = IconMenu(
                MenuItem(T("Edit rule", "编辑规则"), "\uE70F", () => EditSingleAsync().Forget()),
                MenuItem(T("Copy", "复制"), "\uE8C8", () => Copy((string)item.Tag)),
                new MenuFlyoutSeparator(),
                MenuItem(T("Delete", "删除"), "\uE74D", () => list.Items.Remove(item), destructive: true));
            list.Items.Add(item);
        }

        var header = Text(title, 14, true);

        async Task EditAsync()
        {
            var box = Input(header.Text, string.Join(Environment.NewLine, list.Items.OfType<ListViewItem>().Select(i => (string)i.Tag)),
                multiline: true);
            box.MinHeight = 105;
            if (!await ShowAsync(list.XamlRoot, header.Text, box, T("Apply", "应用")))
                return;
            list.Items.Clear();
            foreach (var rule in box.Text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                Add(rule);
        }

        list.DoubleTapped += (_, _) => EditAsync().Forget();
        foreach (var rule in initial)
            Add(rule);

        var editLabel = T("Edit rules", "编辑规则");
        var editButton = AsyncButton(editLabel, EditAsync);
        editButton.Content = GlyphIcon(Icons.Add, 13);
        AutomationProperties.SetName(editButton, editLabel);
        ToolTipService.SetToolTip(editButton, editLabel);
        var actions = Row(editButton);
        if (executables)
        {
            actions.Children.Add(AsyncButton(T("Choose…", "选择…"), async () =>
            {
                var picker = new FileOpenPicker(App.MainWindow!.AppWindow.Id);
                picker.FileTypeFilter.Add(".exe");
                foreach (var file in await picker.PickMultipleFilesAsync())
                    Add(Path.GetFileName(file.Path).ToRegexString());
            }));
        }

        if (scan && executables)
        {
            actions.Children.Add(AsyncButton(T("Scan folder…", "扫描目录…"), async () =>
            {
                var picker = new FolderPicker(App.MainWindow!.AppWindow.Id);
                if (await picker.PickSingleFolderAsync() is not { } folder)
                    return;
                var apps = await Task.Run(() => ModeHelper.ScanExecutables(folder.Path, 30).ToArray());
                foreach (var app in apps)
                    Add(Path.GetFileName(app).ToRegexString());
            }));
        }

        return new StackPanel { Spacing = 4, Children = { Split(header, actions), Card(list, 2) } };
    }

    private static List<string> ReadRules(StackPanel control) =>
        ((ListView)((Border)control.Children[1]).Child).Items.OfType<ListViewItem>().Select(i => (string)i.Tag).ToList();

    private void Report(string message, InfoBarSeverity severity)
    {
        _message.Message = message;
        _message.Severity = severity;
        _message.Margin = new Thickness(0, 0, 0, 9);
        _message.IsOpen = true;
    }

    private Mode? ReadAndValidate()
    {
        var draft = _drafts[(ModeType)_type.SelectedIndex];
        try
        {
            draft.Read();
            return draft.Mode;
        }
        catch (Exception e)
        {
            Report(e.Message, InfoBarSeverity.Error);
            return null;
        }
    }

    private async Task SaveAsync()
    {
        var name = _name.Text.Trim();
        var group = _group.Text.Trim();
        if (name.Length == 0)
        {
            Report(T("Enter a mode name.", "请输入模式名称。"), InfoBarSeverity.Error);
            return;
        }

        if (ReadAndValidate() is not { } mode)
            return;

        var oldId = _original == null ? null : SceneService.GetModeId(_original);
        if (_original != null && Path.GetExtension(_original.FullName).Equals(".json", StringComparison.OrdinalIgnoreCase) &&
            group.Equals(ModesPage.GroupOf(_original), StringComparison.OrdinalIgnoreCase))
            mode.FullName = _original.FullName;
        else
            mode.FullName = NewPath(group, name);

        mode.Remark = _original == null ? new() : new(_original.Remark);
        mode.i18NRemark = name;
        if (!mode.Remark.ContainsKey("en"))
            mode.Remark["en"] = name;
        Directory.CreateDirectory(Path.GetDirectoryName(mode.FullName)!);
        mode.WriteFile();

        var newId = SceneService.GetModeId(mode);
        if (oldId != null && !oldId.Equals(newId, StringComparison.OrdinalIgnoreCase))
        {
            var scenes = SceneService.Scenes.Where(scene => scene.UsesMode(oldId)).ToArray();
            if (scenes.Length > 0)
            {
                var snapshots = scenes
                    .Select(scene => (Scene: scene, ModeIds: scene.GetEffectiveModeIds().ToArray(), HotRuleModeId: scene.HotRuleModeId))
                    .ToArray();
                foreach (var scene in scenes)
                    scene.ReplaceModeId(oldId, newId);
                try
                {
                    await Configuration.SaveAsync(throwOnError: true);
                }
                catch
                {
                    foreach (var snapshot in snapshots)
                    {
                        snapshot.Scene.SetModeIds(snapshot.ModeIds);
                        snapshot.Scene.HotRuleModeId = snapshot.HotRuleModeId;
                    }
                    File.Delete(mode.FullName);
                    Report(T("Could not save scene references. Check the configuration file and try again.",
                        "无法保存场景引用，请检查配置文件后重试。"), InfoBarSeverity.Error);
                    return;
                }
            }
        }

        ModeService.Instance.Load(completeSceneDefaults: true);
        if (Session.Current.IsRunning && Session.Current.ActiveScene is { } activeScene &&
            (activeScene.UsesMode(newId) || (oldId != null && activeScene.UsesMode(oldId))))
        {
            Session.Current.StartAsync(activeScene).Forget();
        }
        Close(saved: true);
    }

    internal static string NewPath(string group, string name)
    {
        var parts = group.Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Any(part => part is "." or ".." || part.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0))
            throw new InvalidDataException(T("Invalid group directory.", "分组目录无效。"));
        var directory = Path.Combine([ModeService.Instance.ModeDirectoryFullName, .. parts]);
        var safe = string.Concat(name.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c)).TrimEnd('.', ' ');
        if (safe.Length == 0)
            safe = "Mode";
        var path = Path.Combine(directory, safe + ".json");
        for (var suffix = 2; File.Exists(path); suffix++)
            path = Path.Combine(directory, $"{safe} ({suffix}).json");
        return path;
    }
}
