using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.Storage.Pickers;
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

    internal static async Task<(int Count, int Skipped, string Group)> ImportTextAsync(string text, string? filePath = null, XamlRoot? root = null)
    {
        var imported = ShareLink.ParseText(text, out var skipped);
        if (imported.Count == 0)
            return (0, skipped, Constants.DefaultGroup);

        var group = NodeGroupHelper.ResolveImportGroupName(text, filePath);
        foreach (var server in imported)
        {
            server.Id = Guid.NewGuid().ToString("N");
            server.Group = group;
            server.IsSub = false;
        }
        if (root != null)
            await PromptImportCustomCoresAsync(root, imported);

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
        return (imported.Count, skipped, group);
    }

    internal static async Task PromptImportCustomCoresAsync(XamlRoot? root, IReadOnlyList<Server> imported, Subscription? subscription = null)
    {
        if (root is null || imported.Count == 0)
            return;

        var matches = CustomCoreHelper.GetImportCustomCoreMatches(imported);
        if (matches.Count == 0)
            return;

        var panel = Stack();
        panel.Spacing = 10;
        panel.Width = root.Size.Width > 0 ? Math.Min(420, Math.Max(280, root.Size.Width - 110)) : 420;

        var intro = Caption(matches.Count == 1
            ? TF("Detected {0} imported {1} nodes. Choose whether to bind a custom core:",
                "已解析 {0} 个 {1} 节点，是否改用自定义核心运行？",
                matches[0].Servers.Count,
                ServerHelper.GetDisplayName(matches[0].Protocol))
            : T("Custom cores are configured for the imported protocols. Choose a core for each protocol:",
                "导入节点的协议已配置自定义核心，可按协议选择运行核心："));
        intro.TextWrapping = TextWrapping.Wrap;
        panel.Children.Add(intro);

        var selectors = new List<(EConfigType Protocol, ComboBox Box, List<(string Id, string Label)> Options)>(matches.Count);
        foreach (var (protocol, servers, cores) in matches)
        {
            var options = new List<(string Id, string Label)>(cores.Count + 2)
            {
                (string.Empty, T("Follow default", "跟随默认")),
                (CustomCoreHelper.BuiltInCoreId, T("Built-in core", "内置核心"))
            };
            foreach (var core in cores)
                options.Add((core.Id, core.Name));

            var preferred = cores.FirstOrDefault(c => c.IsProtocolDefault) ?? cores[0];
            var defaultIdx = options.FindIndex(o => string.Equals(o.Id, preferred.Id, StringComparison.OrdinalIgnoreCase));
            if (defaultIdx < 0)
                defaultIdx = 2;

            var header = TF("{0} ({1} nodes)", "{0}（{1} 个节点）", ServerHelper.GetDisplayName(protocol), servers.Count);
            var box = Choice(header, options.Select(o => o.Label), defaultIdx);
            box.Header = header;
            box.HorizontalAlignment = HorizontalAlignment.Stretch;
            panel.Children.Add(box);
            selectors.Add((protocol, box, options));
        }

        if (!await ShowAsync(
            root,
            T("Use custom core for imported nodes?", "为导入的节点指定自定义核心？"),
            panel,
            confirm: T("Apply", "应用"),
            contentMaxWidth: 520,
            cancel: T("Keep default", "保持默认")))
        {
            return;
        }

        var selections = new Dictionary<EConfigType, string?>(selectors.Count);
        foreach (var (protocol, box, options) in selectors)
        {
            var idx = box.SelectedIndex;
            selections[protocol] = idx > 0 && idx < options.Count ? options[idx].Id : null;
        }

        if (subscription != null)
        {
            var nonEmpty = selections.Where(kv => !string.IsNullOrWhiteSpace(kv.Value)).ToList();
            if (nonEmpty.Count == 1 && !CustomCoreHelper.IsBuiltInCoreId(nonEmpty[0].Value))
            {
                subscription.CustomCoreId = nonEmpty[0].Value;
            }
            else if (nonEmpty.Count == matches.Count &&
                     nonEmpty.All(kv => CustomCoreHelper.IsBuiltInCoreId(kv.Value)) &&
                     imported.All(s => matches.Any(m => m.Protocol == s.ConfigType)))
            {
                subscription.CustomCoreId = CustomCoreHelper.BuiltInCoreId;
            }
            else
            {
                CustomCoreHelper.ApplyCustomCoreSelections(imported, selections);
            }
        }
        else
        {
            CustomCoreHelper.ApplyCustomCoreSelections(imported, selections);
        }
    }

    public static async Task<string?> RenameGroupAsync(XamlRoot? root, string currentName, bool isSubscription)
    {
        var box = Input(T("Group name", "分组名称"), currentName);
        if (!await ShowAsync(root, T("Rename group", "重命名分组"), box, T("Save", "保存"), () =>
            NodeGroupHelper.ValidateGroupName(box.Text, currentName)))
            return null;

        var trimmed = box.Text.Trim();
        if (trimmed.Equals(currentName, StringComparison.Ordinal))
            return trimmed;

        if (!NodeGroupHelper.RenameGroup(currentName, trimmed, isSubscription, out _))
            return null;

        try
        {
            await Configuration.SaveAsync(throwOnError: true);
        }
        catch
        {
            NodeGroupHelper.RenameGroup(trimmed, currentName, isSubscription, out _);
            await ShowAsync(root, T("Operation failed", "操作失败"),
                Text(T("Could not save group changes. Check the configuration file and try again.",
                    "无法保存分组修改，请检查配置文件后重试。")));
            return null;
        }

        return trimmed;
    }

    public static async Task<Subscription?> EditSubscriptionAsync(XamlRoot? root, Subscription? existing)
    {
        var link = Input(T("Subscription URL", "订阅链接"), existing?.Link);
        var name = Input(T("Name", "名称"), existing?.Remark);
        var agent = Input("User-Agent", existing?.UserAgent);
        var hours = Input(T("Auto-update interval (hours, 0 = manual)", "自动更新间隔（小时，0 为手动）"),
            (existing?.UpdateIntervalHours ?? 6).ToString());
        var enabled = new CheckBox { Content = T("Enabled", "启用"), IsChecked = existing?.Enable ?? true };
        var subscription = existing ?? new Subscription();

        var customCores = CustomCoreHelper.GetAll();
        ComboBox? subCoreBox = null;
        var subCoreOptions = new List<(string Id, string Label)>();
        if (customCores.Count > 0 || !string.IsNullOrWhiteSpace(existing?.CustomCoreId))
        {
            subCoreOptions.Add((string.Empty, T("Follow protocol default", "跟随协议默认")));
            subCoreOptions.Add((CustomCoreHelper.BuiltInCoreId, T("Built-in core", "内置核心")));
            foreach (var core in customCores)
                subCoreOptions.Add((core.Id, $"{core.Name} ({ServerHelper.GetDisplayName(core.Protocol)})"));

            var currentCoreId = existing?.CustomCoreId?.Trim() ?? string.Empty;
            var selectedIdx = subCoreOptions.FindIndex(o => string.Equals(o.Id, currentCoreId, StringComparison.OrdinalIgnoreCase));
            if (selectedIdx < 0)
                selectedIdx = 0;

            subCoreBox = Choice(T("Running core", "运行核心"), subCoreOptions.Select(o => o.Label), selectedIdx);
            subCoreBox.Header = T("Running core", "运行核心");
            subCoreBox.HorizontalAlignment = HorizontalAlignment.Stretch;
        }

        var subFields = Stack(name, link, hours, agent);
        if (subCoreBox != null)
            subFields.Children.Add(subCoreBox);
        subFields.Children.Add(enabled);

        if (!await ShowAsync(root, T("Subscription", "订阅"), subFields, T("Save", "保存"), () =>
        {
            var remark = name.Text.Trim();
            if (NodeGroupHelper.ValidateGroupName(remark, existing?.Remark) != null)
                return T("Enter a unique subscription name.", "请输入不重复的订阅名称。");
            if (!Uri.TryCreate(link.Text.Trim(), UriKind.Absolute, out var url) || url.Scheme is not ("http" or "https"))
                return T("Enter an HTTP or HTTPS subscription URL.", "请输入 HTTP 或 HTTPS 订阅链接。");
            if (!int.TryParse(hours.Text, out var interval) || interval is < 0 or > 8760)
                return T("Enter an interval between 0 and 8760 hours.", "请输入 0 至 8760 小时。");
            if (existing != null)
                NodeGroupHelper.RenameGroup(existing.Remark, remark, isSubscription: true, out _);
            else
                subscription.Remark = remark;
            subscription.Link = url.AbsoluteUri;
            subscription.UserAgent = agent.Text.Trim();
            subscription.UpdateIntervalHours = interval;
            subscription.Enable = enabled.IsChecked == true;
            if (subCoreBox != null)
            {
                var idx = subCoreBox.SelectedIndex;
                subscription.CustomCoreId = idx > 0 && idx < subCoreOptions.Count ? subCoreOptions[idx].Id : null;
            }
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

    public static async Task<bool> EditAsync(XamlRoot? root, Server? existing, string? type, Microsoft.UI.WindowId? windowId = null)
    {
        if (root is null)
            return false;

        var util = ServerHelper.GetUtilByTypeName(type ?? existing!.ConfigType.ToString());
        var server = (Server?)existing?.Clone() ?? ServerEditorHelper.CreateDefault(util);
        server.ProtoExtra = server.ProtoExtra with { };
        var extra = server.ProtoExtra;
        var protocol = server.ConfigType;

        var fields = Stack();
        fields.Width = root.Size.Width > 0 ? Math.Min(460, Math.Max(300, root.Size.Width - 110)) : 460;
        var apply = new List<Func<string?>>();
        var name = Input(T("Name", "名称"), server.Remarks);
        var address = Input(T("Address", "地址"), server.Address);
        var port = Input(
            protocol == EConfigType.ExternalCli ? T("Local SOCKS5 port", "本地 SOCKS5 端口") : T("Port", "端口"),
            server.Port == 0 ? (protocol == EConfigType.ExternalCli ? "1080" : "") : server.Port.ToString());
        fields.Children.Add(name);
        if (protocol != EConfigType.ExternalCli)
            fields.Children.Add(TwoColumns(address, port, new GridLength(2, GridUnitType.Star), 10));

        static void Show(UIElement element, bool visible) =>
            element.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;

        TextBox Field(string label, string? value, Action<string> set, bool multiline = false, bool addToFields = true)
        {
            var box = Input(label, value, multiline: multiline);
            if (multiline)
            {
                box.MinHeight = 76;
                box.MaxHeight = 160;
                ScrollViewer.SetVerticalScrollBarVisibility(box, ScrollBarVisibility.Auto);
            }
            if (addToFields)
                fields.Children.Add(box);
            apply.Add(() =>
            {
                if (box.Visibility == Visibility.Visible)
                    set(box.Text.Trim());
                return null;
            });
            return box;
        }

        PasswordBox Secret(string label, string? value, Action<string> set, bool addToFields = true)
        {
            var box = new PasswordBox { Header = label, Password = value ?? "" };
            if (addToFields)
                fields.Children.Add(box);
            apply.Add(() =>
            {
                if (box.Visibility == Visibility.Visible)
                    set(box.Password);
                return null;
            });
            return box;
        }

        TextBox Number(string label, int? value, Action<int?> set, int min = 0, int max = int.MaxValue, bool addToFields = true)
        {
            var box = Input(label, value?.ToString());
            if (addToFields)
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
            return OptionsCore(label, value, set, addToFields: true, choices);
        }

        ComboBox OptionsCore(string label, string? value, Action<string> set, bool addToFields, params string[] choices)
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
            if (addToFields)
                fields.Children.Add(box);
            apply.Add(() =>
            {
                if (box.Visibility == Visibility.Visible)
                    set(box.SelectedItem as string ?? "");
                return null;
            });
            return box;
        }

        ComboBox EditableOptions(string label, string? value, Action<string> set, bool addToFields = true, params string[] choices)
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
            if (addToFields)
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

        var secretLabel = protocol switch
        {
            EConfigType.WireGuard or EConfigType.AmneziaWG => T("Private key", "私钥"),
            EConfigType.VLESS or EConfigType.VMess => "UUID",
            EConfigType.Hysteria2 => T("Authentication", "认证"),
            EConfigType.ShadowTLS => T("Shadowsocks password", "Shadowsocks 密码"),
            _ => T("Password", "密码")
        };
        if (protocol is EConfigType.SOCKS or EConfigType.HTTP or EConfigType.Naive or EConfigType.MASQUE
            or EConfigType.TUIC or EConfigType.SSH or EConfigType.TrustTunnel or EConfigType.Mieru or EConfigType.ShadowQUIC)
        {
            var userBox = Field(protocol == EConfigType.TUIC ? "UUID" : T("Username", "用户名"), server.Username, s => server.Username = s, addToFields: false);
            var passBox = Secret(secretLabel, server.Password, s => server.Password = s, addToFields: false);
            fields.Children.Add(TwoColumns(userBox, passBox, new GridLength(1, GridUnitType.Star), 10));
        }
        else if (protocol != EConfigType.ExternalCli)
        {
            Secret(secretLabel, server.Password, s => server.Password = s);
        }

        void AddWireGuardFields()
        {
            Field(T("Peer public key", "对端公钥"),
                string.IsNullOrWhiteSpace(extra.WgPublicKey) ? server.PublicKey : extra.WgPublicKey,
                s => extra.WgPublicKey = s);
            Secret(T("Preshared key", "预共享密钥"), extra.WgPresharedKey, s => extra.WgPresharedKey = s);
            Field(T("Local IP / CIDR", "本地 IP / CIDR"), extra.WgInterfaceAddress, s => extra.WgInterfaceAddress = s);
            Field(T("Reserved bytes", "保留字节"), extra.WgReserved, s => extra.WgReserved = s);
            Number("MTU", extra.WgMtu, n => extra.WgMtu = n, 1);
        }

        ComboBox? vlessFlowBox = null;
        switch (server)
        {
            case ShadowsocksRServer ssr:
                Options(T("Encryption", "加密"), ssr.EncryptMethod, s => ssr.EncryptMethod = s, Constants.SsrEncryptMethods.ToArray());
                var ssrProtocol = EditableOptions(T("Protocol", "协议"), ssr.Protocol, s => ssr.Protocol = s, addToFields: false, Constants.SsrProtocols.ToArray());
                var ssrProtocolParam = Field(T("Protocol parameter", "协议参数"), ssr.ProtocolParam, s => ssr.ProtocolParam = s, addToFields: false);
                fields.Children.Add(TwoColumns(ssrProtocol, ssrProtocolParam, new GridLength(1, GridUnitType.Star), 10));
                var ssrObfs = EditableOptions(T("Obfuscation", "混淆"), ssr.OBFS, s => ssr.OBFS = s, addToFields: false, Constants.SsrObfs.ToArray());
                var ssrObfsParam = Field(T("Obfuscation parameter", "混淆参数"), ssr.OBFSParam, s => ssr.OBFSParam = s, addToFields: false);
                fields.Children.Add(TwoColumns(ssrObfs, ssrObfsParam, new GridLength(1, GridUnitType.Star), 10));
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
            case AmneziaWGServer:
                AddWireGuardFields();
                var initialAwgVersion = extra.AwgVersion is { } awgVer and > 0
                    ? awgVer.ToString()
                    : (ServerEditorHelper.HasAwgV3Options(extra) ? "3" : string.Empty);
                var awgVersionBox = Options(T("AWG version", "AWG 版本"), initialAwgVersion,
                    s =>
                    {
                        extra.AwgVersion = int.TryParse(s, out var parsedAwgVer) ? parsedAwgVer : null;
                        if (extra.AwgVersion != 3)
                            ServerEditorHelper.ClearAwgV3Options(extra);
                    },
                    Constants.AwgVersions.ToArray());
                var jcBox = Number("Jc", extra.AwgJc, n => extra.AwgJc = n, 0, addToFields: false);
                var jminBox = Number("Jmin", extra.AwgJmin, n => extra.AwgJmin = n, 0, addToFields: false);
                var jmaxBox = Number("Jmax", extra.AwgJmax, n => extra.AwgJmax = n, 0, addToFields: false);
                var junkGrid = new Grid { ColumnSpacing = 10 };
                junkGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                junkGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                junkGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                junkGrid.Children.Add(jcBox);
                Grid.SetColumn(jminBox, 1); junkGrid.Children.Add(jminBox);
                Grid.SetColumn(jmaxBox, 2); junkGrid.Children.Add(jmaxBox);
                fields.Children.Add(junkGrid);

                var s1Box = Number("S1", extra.AwgS1, n => extra.AwgS1 = n, 0, addToFields: false);
                var s2Box = Number("S2", extra.AwgS2, n => extra.AwgS2 = n, 0, addToFields: false);
                fields.Children.Add(TwoColumns(s1Box, s2Box, new GridLength(1, GridUnitType.Star), 10));
                var s3Box = Number("S3", extra.AwgS3, n => extra.AwgS3 = n, 0, addToFields: false);
                var s4Box = Number("S4", extra.AwgS4, n => extra.AwgS4 = n, 0, addToFields: false);
                fields.Children.Add(TwoColumns(s3Box, s4Box, new GridLength(1, GridUnitType.Star), 10));

                var rangeHint = T("e.g. 1 or 100-200", "如 1 或 100-200");
                var h1Box = Field("H1", extra.AwgH1, s => extra.AwgH1 = s, addToFields: false);
                var h2Box = Field("H2", extra.AwgH2, s => extra.AwgH2 = s, addToFields: false);
                h1Box.PlaceholderText = rangeHint;
                h2Box.PlaceholderText = rangeHint;
                fields.Children.Add(TwoColumns(h1Box, h2Box, new GridLength(1, GridUnitType.Star), 10));
                var h3Box = Field("H3", extra.AwgH3, s => extra.AwgH3 = s, addToFields: false);
                var h4Box = Field("H4", extra.AwgH4, s => extra.AwgH4 = s, addToFields: false);
                h3Box.PlaceholderText = rangeHint;
                h4Box.PlaceholderText = rangeHint;
                fields.Children.Add(TwoColumns(h3Box, h4Box, new GridLength(1, GridUnitType.Star), 10));

                Field("I1", extra.AwgI1, s => extra.AwgI1 = s);
                Field("I2", extra.AwgI2, s => extra.AwgI2 = s);
                Field("I3", extra.AwgI3, s => extra.AwgI3 = s);
                Field("I4", extra.AwgI4, s => extra.AwgI4 = s);
                Field("I5", extra.AwgI5, s => extra.AwgI5 = s);

                var awgHpkBox = Secret(T("Header protection key", "头部保护密钥"), extra.AwgHeaderProtectionKey, s => extra.AwgHeaderProtectionKey = s);
                var awgPadBox = Field(T("Content padding addition", "内容填充增量"), extra.AwgContentPaddingAddition, s => extra.AwgContentPaddingAddition = s);
                var awgRandomTrailers = Toggle(T("Random trailers", "随机尾部填充"), extra.AwgRandomTrailers, b => extra.AwgRandomTrailers = b);
                var awgDisableCookies = Toggle(T("Disable cookies", "禁用 Cookie"), extra.AwgDisableCookies, b => extra.AwgDisableCookies = b);
                void RefreshAwg()
                {
                    var showV3 = (awgVersionBox.SelectedItem as string) == "3";
                    Show(awgHpkBox, showV3);
                    Show(awgPadBox, showV3);
                    Show(awgRandomTrailers, showV3);
                    Show(awgDisableCookies, showV3);
                }
                awgVersionBox.SelectionChanged += (_, _) => RefreshAwg();
                RefreshAwg();
                break;
            case WireGuardServer:
                AddWireGuardFields();
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
                var hy2GeckoMin = Field(T("Gecko min packet size", "Gecko 最小包长"), extra.GeckoMinPacketSize, s => extra.GeckoMinPacketSize = s, addToFields: false);
                var hy2GeckoMax = Field(T("Gecko max packet size", "Gecko 最大包长"), extra.GeckoMaxPacketSize, s => extra.GeckoMaxPacketSize = s, addToFields: false);
                var hy2GeckoRow = TwoColumns(hy2GeckoMin, hy2GeckoMax, new GridLength(1, GridUnitType.Star), 10);
                fields.Children.Add(hy2GeckoRow);
                var hy2Ports = Field(T("Port hopping range", "端口跳跃范围"), extra.Ports, s => extra.Ports = s);
                var hy2HopInterval = Field(T("Hop interval (s)", "跳跃间隔（秒）"), extra.HopInterval, s => extra.HopInterval = s, addToFields: false);
                var hy2HopIntervalMax = Field(T("Max hop interval (s)", "最大跳跃间隔（秒）"), extra.HopIntervalMax, s => extra.HopIntervalMax = s, addToFields: false);
                var hy2HopRow = TwoColumns(hy2HopInterval, hy2HopIntervalMax, new GridLength(1, GridUnitType.Star), 10);
                fields.Children.Add(hy2HopRow);
                Options(T("BBR profile", "BBR 配置"), extra.BbrProfile, s => extra.BbrProfile = s, Constants.Hysteria2BbrProfiles.ToArray());
                var hy2Up = Number(T("Upload (Mbps)", "上传（Mbps）"), extra.UpMbps, n => extra.UpMbps = n, addToFields: false);
                var hy2Down = Number(T("Download (Mbps)", "下载（Mbps）"), extra.DownMbps, n => extra.DownMbps = n, addToFields: false);
                fields.Children.Add(TwoColumns(hy2Up, hy2Down, new GridLength(1, GridUnitType.Star), 10));
                void RefreshHy2()
                {
                    var obfsSelected = hy2Obfs.SelectedItem as string ?? Constants.None;
                    var isGecko = obfsSelected == "gecko";
                    var hasPorts = !string.IsNullOrWhiteSpace(hy2Ports.Text);
                    Show(hy2ObfsPassword, obfsSelected != Constants.None);
                    Show(hy2GeckoRow, isGecko);
                    Show(hy2GeckoMin, isGecko);
                    Show(hy2GeckoMax, isGecko);
                    Show(hy2HopRow, hasPorts);
                    Show(hy2HopInterval, hasPorts);
                    Show(hy2HopIntervalMax, hasPorts);
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
            case EConfigType.ShadowTLS:
                Options(T("Encryption", "加密"), extra.SsMethod, s => extra.SsMethod = s, Constants.SsCiphers.ToArray());
                var stlsVersion = Options(T("ShadowTLS version", "ShadowTLS 版本"),
                    (extra.ShadowTlsVersion ?? 3).ToString(),
                    s => extra.ShadowTlsVersion = int.TryParse(s, out var parsedVer) ? parsedVer : 3,
                    Constants.ShadowTlsVersions.ToArray());
                var stlsPassword = Secret(T("Handshake password", "握手密码"), extra.ShadowTlsPassword, s => extra.ShadowTlsPassword = s);
                Toggle("UDP over TCP", extra.Uot, b => extra.Uot = b);
                void RefreshShadowTls()
                {
                    var ver = stlsVersion.SelectedItem as string ?? "3";
                    Show(stlsPassword, ver != "1");
                }
                stlsVersion.SelectionChanged += (_, _) => RefreshShadowTls();
                RefreshShadowTls();
                break;
            case EConfigType.SSH:
                Field(T("Private key (PEM)", "私钥（PEM）"), extra.SshPrivateKey, s => extra.SshPrivateKey = s, multiline: true);
                Secret(T("Key passphrase", "私钥密码"), extra.SshPrivateKeyPassphrase, s => extra.SshPrivateKeyPassphrase = s);
                Field(T("Host key", "主机公钥"), extra.SshHostKey, s => extra.SshHostKey = s, multiline: true);
                Field(T("Host key algorithms", "主机公钥算法"), extra.SshHostKeyAlgorithms, s => extra.SshHostKeyAlgorithms = s);
                Field(T("Client version", "客户端版本"), extra.SshClientVersion, s => extra.SshClientVersion = s);
                break;
            case EConfigType.TrustTunnel:
                var ttQuic = Toggle("QUIC (HTTP/3)", extra.TrustTunnelQuic, b =>
                {
                    extra.TrustTunnelQuic = b;
                    if (b != true)
                    {
                        extra.CongestionControl = "";
                        extra.BbrProfile = "";
                    }
                });
                var ttCongestion = Options(T("Congestion control", "拥塞控制"), extra.CongestionControl?.Trim().ToLowerInvariant() ?? string.Empty,
                    s =>
                    {
                        extra.CongestionControl = s;
                        if (s != "bbr") extra.BbrProfile = "";
                    },
                    Constants.TrustTunnelCongestionControls.ToArray());
                var ttBbrProfile = Options(T("BBR profile", "BBR 配置"), extra.BbrProfile?.Trim().ToLowerInvariant() ?? string.Empty,
                    s => extra.BbrProfile = s, Constants.Hysteria2BbrProfiles.ToArray());
                Toggle(T("Health check", "健康检查"), extra.TrustTunnelHealthCheck, b => extra.TrustTunnelHealthCheck = b);
                Field(T("Name cert verify", "证书名称校验"), extra.TrustTunnelNameCertVerify, s => extra.TrustTunnelNameCertVerify = s);
                var ttMaxConn = Number(T("Max connections", "最大连接数"), extra.TrustTunnelMaxConnections, n => extra.TrustTunnelMaxConnections = n, 0, addToFields: false);
                var ttMinStreams = Number(T("Min streams", "最小流数"), extra.TrustTunnelMinStreams, n => extra.TrustTunnelMinStreams = n, 0, addToFields: false);
                var ttMaxStreams = Number(T("Max streams", "最大流数"), extra.TrustTunnelMaxStreams, n => extra.TrustTunnelMaxStreams = n, 0, addToFields: false);
                var ttReuseGrid = new Grid { ColumnSpacing = 10 };
                ttReuseGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                ttReuseGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                ttReuseGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                ttReuseGrid.Children.Add(ttMaxConn);
                Grid.SetColumn(ttMinStreams, 1); ttReuseGrid.Children.Add(ttMinStreams);
                Grid.SetColumn(ttMaxStreams, 2); ttReuseGrid.Children.Add(ttMaxStreams);
                fields.Children.Add(ttReuseGrid);
                void RefreshTrustTunnel()
                {
                    var quicOn = ttQuic.IsChecked == true;
                    Show(ttCongestion, quicOn);
                    Show(ttBbrProfile, quicOn && (ttCongestion.SelectedItem as string) == "bbr");
                }
                ttQuic.Checked += (_, _) => RefreshTrustTunnel();
                ttQuic.Unchecked += (_, _) => RefreshTrustTunnel();
                ttCongestion.SelectionChanged += (_, _) => RefreshTrustTunnel();
                RefreshTrustTunnel();
                break;
            case EConfigType.Mieru:
                var mieruTransport = OptionsCore(T("Transport", "传输"),
                    string.IsNullOrWhiteSpace(extra.MieruTransport) ? "TCP" : extra.MieruTransport.Trim().ToUpperInvariant(),
                    s => extra.MieruTransport = s, addToFields: false, Constants.MieruTransports.ToArray());
                var mieruPortRange = Field(T("Port range", "端口范围"), extra.MieruPortRange, s => extra.MieruPortRange = s, addToFields: false);
                mieruPortRange.PlaceholderText = "2010-2020";
                fields.Children.Add(TwoColumns(mieruTransport, mieruPortRange, new GridLength(1, GridUnitType.Star), 10));
                var mieruMux = OptionsCore(T("Multiplexing", "多路复用"), extra.MieruMultiplexing?.Trim().ToUpperInvariant() ?? string.Empty, s => extra.MieruMultiplexing = s, addToFields: false, Constants.MieruMultiplexingModes.ToArray());
                var mieruHandshake = OptionsCore(T("Handshake mode", "握手模式"), extra.MieruHandshakeMode?.Trim().ToUpperInvariant() ?? string.Empty, s => extra.MieruHandshakeMode = s, addToFields: false, Constants.MieruHandshakeModes.ToArray());
                fields.Children.Add(TwoColumns(mieruMux, mieruHandshake, new GridLength(1, GridUnitType.Star), 10));
                Field(T("Traffic pattern", "流量模式"), extra.MieruTrafficPattern, s => extra.MieruTrafficPattern = s);
                break;
            case EConfigType.ShadowQUIC:
                var sqSni = Field("SNI", server.Sni, s => server.Sni = s, addToFields: false);
                var sqAlpn = Field("ALPN", server.Alpn, s => server.Alpn = s, addToFields: false);
                fields.Children.Add(TwoColumns(sqSni, sqAlpn, new GridLength(1, GridUnitType.Star), 10));
                EditableOptions(T("QUIC versions", "QUIC 版本"), string.IsNullOrEmpty(extra.ShadowQuicVersions) ? "v1" : extra.ShadowQuicVersions,
                    s => extra.ShadowQuicVersions = s, addToFields: true, "v1", "v2", "v1,v2");
                var sqCongestion = Options(T("Congestion control", "拥塞控制"), extra.CongestionControl?.Trim().ToLowerInvariant() ?? string.Empty,
                    s =>
                    {
                        extra.CongestionControl = s;
                        if (s != "bbr") extra.BbrProfile = "";
                    },
                    Constants.ShadowQuicCongestionControls.ToArray());
                var sqBbrProfile = Options(T("BBR profile", "BBR 配置"), extra.BbrProfile?.Trim().ToLowerInvariant() ?? string.Empty,
                    s => extra.BbrProfile = s, Constants.Hysteria2BbrProfiles.ToArray());
                var sqUp = Number(T("Upload (Mbps)", "上传（Mbps）"), extra.UpMbps, n => extra.UpMbps = n, addToFields: false);
                var sqDown = Number(T("Download (Mbps)", "下载（Mbps）"), extra.DownMbps, n => extra.DownMbps = n, addToFields: false);
                fields.Children.Add(TwoColumns(sqUp, sqDown, new GridLength(1, GridUnitType.Star), 10));
                Number(T("Keep-alive interval (ms)", "保活间隔（毫秒）"), extra.ShadowQuicKeepAliveInterval, n => extra.ShadowQuicKeepAliveInterval = n);
                Toggle("UDP over stream", extra.ShadowQuicUdpOverStream, b => extra.ShadowQuicUdpOverStream = b);
                Toggle("0-RTT", extra.ShadowQuicZeroRtt, b => extra.ShadowQuicZeroRtt = b);
                Toggle(T("Disable MTU discovery", "禁用 MTU 探测"), extra.ShadowQuicDisableMtuDiscovery, b => extra.ShadowQuicDisableMtuDiscovery = b);
                void RefreshShadowQuic() => Show(sqBbrProfile, (sqCongestion.SelectedItem as string) == "bbr");
                sqCongestion.SelectionChanged += (_, _) => RefreshShadowQuic();
                RefreshShadowQuic();
                break;
            case EConfigType.ExternalCli:
                var cliExeBox = Field(T("CLI executable", "CLI 可执行文件"), extra.CliExecutable,
                    s => extra.CliExecutable = CustomCoreHelper.NormalizeExecutableInput(s), addToFields: false);
                var browseExe = AsyncButton(T("Browse .exe…", "浏览 .exe…"), async () =>
                {
                    var ownerWindowId = windowId ?? App.MainWindow?.AppWindow.Id;
                    if (ownerWindowId is not { } targetWindowId)
                        return;
                    var picker = new FileOpenPicker(targetWindowId);
                    picker.FileTypeFilter.Add(".exe");
                    if (await picker.PickSingleFileAsync() is { } file)
                        cliExeBox.Text = file.Path;
                }, glyph: Icons.OpenFile);
                browseExe.VerticalAlignment = VerticalAlignment.Bottom;
                fields.Children.Add(Split(cliExeBox, browseExe, 8));
                var cliArgsBox = Field(T("CLI arguments", "CLI 启动参数"), extra.CliArguments,
                    s => extra.CliArguments = s, multiline: true);
                cliArgsBox.AcceptsReturn = false;
                cliArgsBox.PlaceholderText = "-s 1.2.3.4 -p 2211 -b 127.0.0.1 -l 1080";
                fields.Children.Add(port);
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

        if (protocol is not (EConfigType.WireGuard or EConfigType.AmneziaWG or EConfigType.ShadowsocksR or EConfigType.SOCKS or EConfigType.Shadowsocks or EConfigType.SSH or EConfigType.Mieru or EConfigType.ShadowQUIC or EConfigType.ExternalCli))
        {
            var initialSecurity = server.StreamSecurity == Constants.StreamSecurityReality
                ? Constants.StreamSecurityReality
                : server.StreamSecurity == Constants.StreamSecurity ? Constants.StreamSecurity : Constants.None;
            var fixedTls = protocol is EConfigType.Hysteria2 or EConfigType.TUIC or EConfigType.AnyTLS or EConfigType.Naive or EConfigType.MASQUE or EConfigType.ShadowTLS or EConfigType.TrustTunnel;
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
                if (protocol is EConfigType.Hysteria2 or EConfigType.TrustTunnel && s.Length > 0) server.CertSha = "";
            }, multiline: true);
            TextBox? certShaBox = naive || string.IsNullOrWhiteSpace(server.CertSha)
                ? null
                : Field(T("Certificate SHA", "证书 SHA"), server.CertSha, s => server.CertSha = s);

            TextBox? pubKeyBox = null;
            TextBox? shortIdBox = null;
            Grid? realityKeyRow = null;
            TextBox? spiderXBox = null;
            TextBox? mldsaBox = null;
            TextBox? echBox = null;
            ComboBox? echQueryBox = null;
            if (supportsReality)
            {
                pubKeyBox = Field(T("Reality public key", "Reality 公钥"), server.PublicKey, s => server.PublicKey = s, addToFields: false);
                shortIdBox = Field(T("Short ID", "短 ID"), server.ShortId, s => server.ShortId = s, addToFields: false);
                realityKeyRow = TwoColumns(pubKeyBox, shortIdBox, new GridLength(1, GridUnitType.Star), 10);
                fields.Children.Add(realityKeyRow);
                spiderXBox = Field("SpiderX", server.SpiderX, s => server.SpiderX = s);
                mldsaBox = Field("ML-DSA-65", server.Mldsa65Verify, s => server.Mldsa65Verify = s);
                echBox = Field("ECH", server.EchConfigList, s => server.EchConfigList = s);
                echQueryBox = Options("ECH Force Query", server.EchForceQuery, s => server.EchForceQuery = s, Constants.EchForceQuerys.ToArray());
            }
            else if (protocol == EConfigType.TrustTunnel)
            {
                echBox = Field("ECH", server.EchConfigList, s => server.EchConfigList = s);
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
                if (realityKeyRow != null) Show(realityKeyRow, reality);
                if (pubKeyBox != null) Show(pubKeyBox, reality);
                if (shortIdBox != null) Show(shortIdBox, reality);
                if (spiderXBox != null) Show(spiderXBox, reality && (xhttp || !string.IsNullOrEmpty(server.SpiderX)));
                if (mldsaBox != null) Show(mldsaBox, reality && (xhttp || !string.IsNullOrEmpty(server.Mldsa65Verify)));
                if (echBox != null) Show(echBox, tlsOnly && (xhttp || protocol == EConfigType.TrustTunnel || !string.IsNullOrEmpty(server.EchConfigList)));
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

        if (protocol != EConfigType.ExternalCli)
        {
            var protocolCores = CustomCoreHelper.GetForProtocol(protocol);
            if (protocolCores.Count > 0 || !string.IsNullOrWhiteSpace(extra.CustomCoreId))
            {
                var coreOptions = new List<(string Id, string Label)>
                {
                    (string.Empty, T("Follow default", "跟随默认")),
                    (CustomCoreHelper.BuiltInCoreId, T("Built-in core", "内置核心"))
                };
                foreach (var core in protocolCores)
                    coreOptions.Add((core.Id, core.Name));

                var currentCoreId = extra.CustomCoreId?.Trim() ?? string.Empty;
                var selectedIdx = coreOptions.FindIndex(o => string.Equals(o.Id, currentCoreId, StringComparison.OrdinalIgnoreCase));
                if (selectedIdx < 0)
                    selectedIdx = 0;

                var coreBox = Choice(T("Running core", "运行核心"), coreOptions.Select(o => o.Label), selectedIdx);
                coreBox.Header = T("Running core", "运行核心");
                coreBox.HorizontalAlignment = HorizontalAlignment.Stretch;
                fields.Children.Add(coreBox);
                apply.Add(() =>
                {
                    var idx = coreBox.SelectedIndex;
                    extra.CustomCoreId = idx > 0 && idx < coreOptions.Count ? coreOptions[idx].Id : null;
                    return null;
                });
            }
        }

        var title = existing == null ? T("Add node", "添加节点") : T("Edit node", "编辑节点");
        if (!await ShowAsync(root, $"{title} · {ServerHelper.GetDisplayName(util)}", fields, T("Save", "保存"), () =>
        {
            ushort number = 0;
            if (protocol == EConfigType.ExternalCli)
            {
                if (!ushort.TryParse(port.Text.Trim(), out number) || number == 0)
                    return T("Enter a port between 1 and 65535.", "请输入 1 至 65535 之间的端口。");
                server.Address = "127.0.0.1";
            }
            else
            {
                if (address.Text.Trim().Length == 0)
                    return T("Enter an address and a valid port.", "请输入地址及有效端口。");
                if (port.Text.Trim().Length > 0)
                {
                    if (!ushort.TryParse(port.Text, out number) || number == 0)
                        return T("Enter an address and a valid port.", "请输入地址及有效端口。");
                }
                else if (protocol != EConfigType.Mieru)
                {
                    return T("Enter an address and a valid port.", "请输入地址及有效端口。");
                }
                server.Address = address.Text.Trim();
            }

            server.Remarks = name.Text.Trim();
            server.Port = number;
            foreach (var read in apply)
                if (read() is { } error)
                    return error;

            ServerEditorHelper.PrepareForSave(server);
            return ServerEditorHelper.ValidateForSave(server, util) is { } validationError
                ? i18N.Translate(validationError)
                : null;
        }, contentMaxWidth: 560))
            return false;

        if (existing != null && Global.Settings.Server.IndexOf(existing) is var index && index >= 0)
            Global.Settings.Server[index] = server;
        else if (existing == null)
        {
            server.Group = Constants.DefaultGroup;
            server.IsSub = false;
            Global.Settings.Server.Add(server);
        }
        await Configuration.SaveAsync();
        return true;
    }

    public static async Task ShareAsync(XamlRoot? root, Server server, bool qr)
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

    public static async Task TestNatAsync(XamlRoot? root, Server server)
    {
        if (root is null || _natDialogActive)
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
