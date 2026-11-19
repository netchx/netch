using System.Net;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Windows.Storage.Pickers;
using Netch.App.Services;
using Netch.Controllers;
using Netch.Models;
using Netch.Models.Modes;
using Netch.Models.Modes.ProcessMode;
using Netch.Models.Modes.ShareMode;
using Netch.Models.Modes.TunMode;
using Netch.Services;
using Netch.Utils;
using static Netch.App.Helpers.Ui;

namespace Netch.App.Pages;

internal sealed class ModeEditor : UserControl
{
    private readonly Mode? _original;
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

    public ModeEditor(ModeType type, Mode? original)
    {
        _original = original;
        _name = Input(T("Name", "名称"), original?.i18NRemark, search: true);
        _group = Input(T("Group", "分组"), original == null ? "Custom" : ModesPage.GroupOf(original), search: true);
        _group.Width = 142;
        _type = Choice(T("Type", "类型"), Enum.GetValues<ModeType>().Select(Helpers.Format.ModeTypeLongName), (int)type);
        _type.Width = 156;

        var breadcrumb = new BreadcrumbBar { ItemsSource = new[] { T("Modes", "模式"), original?.i18NRemark ?? T("New mode", "新建模式") } };
        breadcrumb.ItemClicked += (_, e) => { if (e.Index == 0) App.MainWindow?.Navigate("Modes", force: true); };
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
            var layout = Body(Split(breadcrumb, Row(verify, AsyncButton(T("Save", "保存"), SaveAsync, true))),
                Split(Field(T("Name", "名称"), _name), Row(Field(T("Type", "类型"), _type), Field(T("Group", "分组"), _group))),
                detail);
            layout.RowSpacing = 9;
            return Helpers.Ui.Page(layout);
        }
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
        switch (type)
        {
            case ModeType.ProcessMode:
            {
                var original = _original as Redirector;
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
                var tcp = Switch(T("Handle TCP", "处理 TCP"), mode.FilterTCP == true);
                var udp = Switch(T("Handle UDP", "处理 UDP"), mode.FilterUDP == true);
                var icmp = Switch(T("Handle ICMP", "处理 ICMP"), mode.FilterICMP == true);
                var delay = InlineOption(T("ICMP delay (ms)", "ICMP 延迟（毫秒）"), mode.ICMPDelay.ToString());
                var dns = Switch(T("Hijack DNS", "DNS 劫持"), mode.FilterDNS == true);
                var dnsHost = InlineOption(T("DNS endpoint", "DNS 地址"), mode.DNSHost);
                var proxy = Switch(T("Proxy DNS", "DNS 走代理"), mode.DNSProxy == true);
                var dnsOnly = Switch(T("Handle only DNS on port 53", "端口 53 仅处理 DNS"), mode.HandleOnlyDNS == true);
                var parent = Switch(T("Handle child processes", "处理子进程"), mode.FilterParent == true);
                var loop = Switch(T("Handle loopback", "处理本地回环"), mode.FilterLoopback);
                var lan = Switch(T("Handle LAN", "处理局域网"), mode.FilterIntranet);
                var settings = Card(new StackPanel
                {
                    Children = { SwitchRow(tcp), SwitchRow(udp), SwitchRow(icmp, delay, 55), SwitchRow(dns, dnsHost, 53),
                        SwitchRow(proxy), SwitchRow(parent), SwitchRow(loop), SwitchRow(lan), SwitchRow(dnsOnly) }
                });
                var lists = Stack(include, exclude);
                lists.Spacing = 8;
                return new Editor(mode, Layout(lists, settings), () =>
                {
                    mode.Handle = ReadRules(include);
                    mode.Bypass = ReadRules(exclude);
                    mode.FilterTCP = tcp.IsOn;
                    mode.FilterUDP = udp.IsOn;
                    mode.FilterICMP = icmp.IsOn;
                    mode.FilterDNS = dns.IsOn;
                    mode.DNSProxy = proxy.IsOn;
                    mode.HandleOnlyDNS = dnsOnly.IsOn;
                    mode.FilterParent = parent.IsOn;
                    mode.FilterLoopback = loop.IsOn;
                    mode.FilterIntranet = lan.IsOn;
                    if (!int.TryParse(delay.Text, out var ms) || ms < 0)
                        throw new InvalidDataException(T("Invalid ICMP delay.", "ICMP 延迟无效。"));
                    mode.ICMPDelay = ms;
                    mode.DNSHost = dnsHost.Text.Trim();
                    if (dns.IsOn && mode.DNSHost.Length > 0 && !IPEndPoint.TryParse(mode.DNSHost, out _))
                        throw new InvalidDataException(T("Invalid DNS endpoint.", "DNS 地址无效。"));
                    if (!Session.Current.IsIdle)
                        throw new InvalidOperationException(T("Disconnect before validating process rules.", "请先断开连接，再验证进程规则。"));
                    if (!NFController.CheckRules(mode.Handle.Concat(mode.Bypass), out var invalid))
                        throw new InvalidDataException(NFController.GenerateInvalidRulesMessage(invalid));
                });
            }
            case ModeType.TunMode:
            {
                var original = _original as TunMode;
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
                    if (!byte.TryParse(prefix.Text, out var bits))
                        throw new InvalidDataException(T("Invalid IPv6 prefix length.", "IPv6 前缀长度无效。"));
                    config.IPv6PrefixLength = bits;
                    config.BypassIPs = ReadRules(bypass);
                    TUNController.ValidateConfiguration(mode, config);
                });
            }
            default:
            {
                var mode = new ShareMode();
                var args = Input(T("pcap2socks arguments", "pcap2socks 参数"), (_original as ShareMode)?.Argument ?? mode.Argument);
                return new Editor(mode, Scroll(Card(Stack(args))), () => mode.Argument = args.Text);
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

    private static TextBox InlineOption(string label, string? value)
    {
        var input = Input(label, value, search: true);
        input.MinHeight = 0;
        input.Height = 25;
        input.FontSize = 13;
        input.Padding = new Thickness(3, 0, 3, 0);
        input.Width = 160;
        input.HorizontalAlignment = HorizontalAlignment.Left;
        return input;
    }

    private static Grid SwitchRow(ToggleSwitch toggle, FrameworkElement? option = null, double height = 40)
    {
        var label = (string)toggle.OnContent;
        CompactToggle(toggle);
        toggle.Width = 42;
        toggle.VerticalAlignment = VerticalAlignment.Center;
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(toggle, label);
        var title = Text(label);
        var content = option == null ? (FrameworkElement)title : new StackPanel
        {
            Spacing = 0, VerticalAlignment = VerticalAlignment.Center, Children = { title, option }
        };
        var row = Split(content, toggle);
        row.MinHeight = height;
        return row;
    }

    private static FrameworkElement Layout(UIElement rules, Border settings)
    {
        var inner = settings.Child;
        settings.Child = null;
        settings.Child = Scroll(inner);
        settings.Padding = new Thickness(12, 4, 12, 4);
        return TwoColumns(Scroll(rules), settings, new GridLength(1.216, GridUnitType.Star));
    }

    private static StackPanel Rules(string title, IEnumerable<string> initial, bool executables, bool scan)
    {
        var list = new ListView { MaxHeight = 238, ItemContainerStyle = Resource<Style>("NetchCompactRow") };
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
                Content = Split(label, Caption(executables ? T("Regex", "正则") : "CIDR"))
            };
            item.ContextFlyout = Menu((T("Delete", "删除"), () => list.Items.Remove(item)));
            list.Items.Add(item);
        }

        async Task Edit()
        {
            var box = Input(title, string.Join(Environment.NewLine, list.Items.OfType<ListViewItem>().Select(i => (string)i.Tag)),
                multiline: true);
            box.MinHeight = 105;
            if (!await ShowAsync(list.XamlRoot, title, box, T("Apply", "应用")))
                return;
            list.Items.Clear();
            foreach (var rule in box.Text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                Add(rule);
        }

        list.DoubleTapped += async (_, _) => await Edit();
        foreach (var rule in initial)
            Add(rule);

        var actions = Row(AsyncButton("＋", Edit));
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

        return new StackPanel { Spacing = 4, Children = { Split(Text(title, 14, true), actions), Card(list, 2) } };
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
            var scenes = SceneService.Scenes.Where(scene => scene.ModeId.Equals(oldId, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (scenes.Length > 0)
            {
                foreach (var scene in scenes)
                    scene.ModeId = newId;
                try
                {
                    await Configuration.SaveAsync(throwOnError: true);
                }
                catch
                {
                    foreach (var scene in scenes)
                        scene.ModeId = oldId;
                    File.Delete(mode.FullName);
                    Report(T("Could not save scene references. Check the configuration file and try again.",
                        "无法保存场景引用，请检查配置文件后重试。"), InfoBarSeverity.Error);
                    return;
                }
            }
        }

        ModeService.Instance.Load(completeSceneDefaults: true);
        App.MainWindow?.Navigate("Modes", force: true);
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
