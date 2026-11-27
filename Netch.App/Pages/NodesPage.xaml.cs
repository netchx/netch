using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.VisualStudio.Threading;
using Microsoft.Windows.Storage.Pickers;
using Netch.App.Dialogs;
using Netch.App.Helpers;
using Netch.App.Services;
using Netch.App.ViewModels;
using Netch.Enums;
using Netch.Models;
using Netch.Servers;
using Netch.Services;
using Netch.Utils;
using Windows.System;
using static Netch.App.Helpers.Ui;

namespace Netch.App.Pages;

public sealed partial class NodesPage : Page
{
    private readonly ListView _categories = Categories();
    private readonly ListView _nodes = new() { SelectionMode = ListViewSelectionMode.Multiple, Padding = new Thickness(0, 0, Ui.ScrollBarGutter, 0) };
    private readonly ObservableCollection<ListViewItem> _nodeRows = [];
    private readonly Dictionary<Server, TextBlock> _delayLabels = [];
    private readonly TextBox _filter = Input(T("Filter nodes", "筛选节点"), search: true);
    private readonly ContentControl _subscription = new() { HorizontalContentAlignment = HorizontalAlignment.Stretch };
    private readonly TextBlock _status = Caption("");
    private readonly DropDownButton _updateAll;
    private readonly SplitButton _urlTestBtn;
    private readonly SplitButton _tcpingBtn;
    private readonly Border _batchBar;
    private readonly TextBlock _batchCount = Caption("");
    private readonly Button _selectAllBtn;
    private readonly Button _clearSelectionBtn;
    private readonly Button _batchTestBtn;
    private readonly DropDownButton _batchCoreBtn;
    private readonly Button _batchCopyBtn;
    private readonly Button _batchDeleteBtn;
    private enum NodeCategoryKind
    {
        All,
        Subscription,
        ImportedGroup,
        Manual
    }

    private sealed record NodeCategorySelection(NodeCategoryKind Kind, string? GroupName = null, Subscription? Subscription = null);

    private SplitButton? _updateSelected;
    private NodeCategorySelection? SelectedCategory => (_categories.SelectedItem as ListViewItem)?.Tag as NodeCategorySelection;
    private NodeSortField _sortField = NodeSortField.None;
    private bool _sortDescending;
    private readonly Dictionary<NodeSortField, (Button Button, string Label, TextBlock Text)> _sortHeaders = new();
    private bool _refreshing;
    private bool _suppressSelectionChanged;
    private bool _updatingSubscriptions;
    private bool _sawDelayTesting;

    public NodesPage()
    {
        InitializeComponent();
        var import = new DropDownButton { Content = T("Import", "导入") };
        import.Flyout = Menu(
            (T("From clipboard", "从剪贴板"), () => ImportAsync(import, fromFile: false).Forget()),
            (T("From Clash YAML file", "从 Clash YAML 文件"), () => ImportAsync(import, fromFile: true).Forget()));
        _updateAll = new DropDownButton { Content = IconContent(Icons.Refresh, T("Update subscriptions", "更新订阅")) };
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(_updateAll, T("Update subscriptions", "更新订阅"));
        _updateAll.Flyout = Menu(
            (T("Update all (direct)", "直连更新全部订阅"), () => UpdateSubscriptionsAsync(null, useProxy: false).Forget()),
            (T("Update all via proxy", "通过代理更新全部订阅"), () => UpdateSubscriptionsAsync(null, useProxy: true).Forget()));
        var addSubscription = AsyncButton(T("Subscription", "订阅"), () => EditSubscriptionAsync(null), glyph: Icons.Add);
        var add = Button(T("Node", "节点"), () => { }, accent: true, glyph: Icons.Add);
        add.Flyout = Menu(ServerEditorHelper.GetOrderedProtocols()
            .Select(util => (ServerHelper.GetDisplayName(util), (Action)(() => EditNodeAsync(null, util.TypeName).Forget()))).ToArray());
        _urlTestBtn = CreateDelayTestSplitButton(DelayTestMode.Hybrid, isTcping: false);
        _tcpingBtn = CreateDelayTestSplitButton(DelayTestMode.TCPing, isTcping: true);

        _selectAllBtn = Button(T("Select all", "全选"), () => _nodes.SelectAll());
        _clearSelectionBtn = Button(T("Clear selection", "取消选择"), ClearSelection);
        _batchTestBtn = AsyncButton(TF("Test selected ({0})", "测速所选 ({0})", 0), () => DelayTestHelper.PerformTestAsync(SelectedServers(), true), refreshEnabled: UpdateBatchBar);
        _batchCoreBtn = new DropDownButton { Content = T("Set core", "指定核心") };
        _batchCopyBtn = Button(TF("Copy share links ({0})", "复制分享链接 ({0})", 0), () => CopyShareLinks(SelectedServers()));
        _batchDeleteBtn = AsyncButton(TF("Delete selected ({0})", "删除所选 ({0})", 0), () => DeleteServersAsync(SelectedServers()), refreshEnabled: UpdateBatchBar);
        foreach (var btn in new Control[] { _selectAllBtn, _clearSelectionBtn, _batchTestBtn, _batchCoreBtn, _batchCopyBtn, _batchDeleteBtn })
        {
            btn.MinHeight = 28;
            btn.Padding = new Thickness(10, 4, 10, 4);
        }

        _batchBar = Card(Split(Row(_batchCount, _selectAllBtn, _clearSelectionBtn), Row(_batchTestBtn, _batchCoreBtn, _batchCopyBtn, _batchDeleteBtn)), 8);
        _batchBar.Visibility = Visibility.Collapsed;

        _categories.SelectionChanged += (_, _) => { if (!_refreshing) RefreshSelection(); };
        _filter.TextChanged += (_, _) => RefreshNodes();
        _nodes.SelectionChanged += (_, _) =>
        {
            if (!_suppressSelectionChanged)
                UpdateBatchBar();
        };
        var selectAllAccel = new KeyboardAccelerator { Key = VirtualKey.A, Modifiers = VirtualKeyModifiers.Control };
        selectAllAccel.Invoked += (_, e) =>
        {
            _nodes.SelectAll();
            e.Handled = true;
        };
        _nodes.KeyboardAccelerators.Add(selectAllAccel);
        _nodes.KeyDown += (_, e) =>
        {
            if (e.Key == VirtualKey.Enter && _nodes.SelectedItems.Count <= 1)
            {
                var target = _nodes.SelectedItems.Count == 1
                    ? (_nodes.SelectedItem as ListViewItem)?.Tag as Server
                    : XamlRoot != null ? (FocusManager.GetFocusedElement(XamlRoot) as ListViewItem)?.Tag as Server : null;
                if (target != null)
                    EditNodeAsync(target).Forget();
            }
            else if (e.Key == VirtualKey.Delete && SelectedServers() is { Length: > 0 } selected)
            {
                DeleteServersAsync(selected).Forget();
            }
            else if (e.Key == VirtualKey.Escape && _nodes.SelectedItems.Count > 0)
            {
                ClearSelection();
                e.Handled = true;
            }
        };

        var topSection = Stack(_subscription, _status, Split(_filter, Row(_urlTestBtn, _tcpingBtn)), _batchBar);

        var nodeHeaderStrip = BuildNodeHeaderStrip();
        UpdateSortHeaders();
        var listSurface = new Grid
        {
            RowSpacing = 4,
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }
            },
            Children = { nodeHeaderStrip, _nodes }
        };
        Grid.SetRow(_nodes, 1);
        var listCard = Card(listSurface, 2);
        listCard.Padding = new Thickness(2, 6, 2, 2);

        Content = Ui.Page(Body(Header(T("Nodes", "节点"), import, _updateAll, addSubscription, add),
            TwoColumns(_categories, Body(topSection, listCard), new GridLength(165))));
        _nodes.ItemContainerStyle = Resource<Style>("NetchSelectableRow");
        _nodes.ItemsSource = _nodeRows;
        _status.Visibility = Visibility.Collapsed;
        _status.RegisterPropertyChangedCallback(TextBlock.TextProperty,
            (_, _) => _status.Visibility = _status.Text.Length > 0 ? Visibility.Visible : Visibility.Collapsed);
        RefreshCategories();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        AppEvents.SubscriptionChanged += RefreshSubscriptions;
        AppEvents.NodeDelaysChanged += RefreshDelays;
        AppEvents.DelayTestProgressChanged += OnDelayTestProgressChanged;
        UpdateDelayTestUi(DelayTestHelper.IsTesting, DelayTestHelper.CurrentCompleted, DelayTestHelper.CurrentTotal,
            updateStatus: DelayTestHelper.IsTesting);
        if (e.Parameter is true)
            Loaded += ImportSubscriptionOnLoad;
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        AppEvents.SubscriptionChanged -= RefreshSubscriptions;
        AppEvents.NodeDelaysChanged -= RefreshDelays;
        AppEvents.DelayTestProgressChanged -= OnDelayTestProgressChanged;
        Loaded -= ImportSubscriptionOnLoad;
    }

    private async void ImportSubscriptionOnLoad(object sender, RoutedEventArgs e)
    {
        Loaded -= ImportSubscriptionOnLoad;
        await EditSubscriptionAsync(null);
    }

    private void RefreshSubscriptions() => DispatcherQueue.TryEnqueue(() => RefreshCategories());

    private void RefreshDelays() => DispatcherQueue.TryEnqueue(() =>
    {
        if (XamlRoot is not { } root)
            return;

        foreach (var (server, label) in _delayLabels)
            UpdateDelayLabel(server, label);

        if (_sortField != NodeSortField.Delay)
            return;

        var rowsByServer = _nodeRows.ToDictionary(item => (Server)item.Tag);
        var ordered = NodeSortHelper.Sort(rowsByServer.Keys, NodeSortField.Delay, _sortDescending)
            .Select(server => rowsByServer[server])
            .ToArray();
        if (_nodeRows.SequenceEqual(ordered))
            return;

        var selected = SelectedServers().ToHashSet();
        var focused = FocusManager.GetFocusedElement(root) as ListViewItem;
        _suppressSelectionChanged = true;
        try
        {
            for (var i = 0; i < ordered.Length; i++)
                if (_nodeRows[i] != ordered[i])
                    _nodeRows.Move(_nodeRows.IndexOf(ordered[i]), i);

            _nodes.SelectedItems.Clear();
            foreach (var item in _nodeRows)
                if (item.Tag is Server server && selected.Contains(server))
                    _nodes.SelectedItems.Add(item);
        }
        finally
        {
            _suppressSelectionChanged = false;
        }
        UpdateBatchBar();
        if (focused != null && _nodeRows.Contains(focused))
            focused.Focus(FocusState.Keyboard);
    });

    private void OnDelayTestProgressChanged(bool isTesting, int completed, int total) =>
        DispatcherQueue.TryEnqueue(() => UpdateDelayTestUi(isTesting, completed, total, updateStatus: true));

    private void UpdateDelayTestUi(bool isTesting, int completed, int total, bool updateStatus)
    {
        UpdateDelayTestButtons();
        UpdateBatchBar();

        if (!updateStatus)
            return;

        if (!isTesting && DelayTestHelper.IsTesting)
            return;

        if (isTesting)
        {
            _sawDelayTesting = true;
            _status.Text = total > 0
                ? TF("Testing latency… {0} / {1}", "正在测速… {0} / {1}", completed, total)
                : T("Testing latency…", "正在测速…");
        }
        else if (_sawDelayTesting)
        {
            _sawDelayTesting = false;
            if (DelayTestHelper.LastRunCancelled)
                _status.Text = T("Latency test cancelled.", "测速已取消。");
            else if (total > 0)
                _status.Text = TF("Tested {0} nodes ({1} reachable).", "已完成 {0} 个节点测速（{1} 个可用）。",
                    total, DelayTestHelper.LastReachableCount);
        }
    }

    private SplitButton CreateDelayTestSplitButton(DelayTestMode mode, bool isTcping)
    {
        var button = new SplitButton();
        button.Click += (_, _) =>
        {
            var isThisRunning = DelayTestHelper.IsTesting && (isTcping
                ? DelayTestHelper.ActiveMode == DelayTestMode.TCPing
                : DelayTestHelper.ActiveMode != DelayTestMode.TCPing);
            if (isThisRunning)
                DelayTestHelper.Cancel();
            else
                StartDelayTest(TestableDisplayedServers(), mode);
        };

        var menu = new MenuFlyout();
        menu.Opening += (_, _) =>
        {
            menu.Items.Clear();
            var visible = TestableDisplayedServers();
            var all = TestableAllServers();

            var visibleItem = new MenuFlyoutItem
            {
                Text = isTcping
                    ? TF("TCP latency · visible ({0})", "TCP 延迟 · 当前列表 ({0})", visible.Length)
                    : TF("URL test · visible ({0})", "URL 测速 · 当前列表 ({0})", visible.Length),
                IsEnabled = visible.Length > 0
            };
            visibleItem.Click += (_, _) => StartDelayTest(visible, mode);
            menu.Items.Add(visibleItem);

            var allItem = new MenuFlyoutItem
            {
                Text = isTcping
                    ? TF("TCP latency · all ({0})", "TCP 延迟 · 全部节点 ({0})", all.Length)
                    : TF("URL test · all ({0})", "URL 测速 · 全部节点 ({0})", all.Length),
                IsEnabled = all.Length > 0
            };
            allItem.Click += (_, _) => StartDelayTest(all, mode);
            menu.Items.Add(allItem);
        };
        button.Flyout = menu;
        return button;
    }

    private void UpdateDelayTestButtons()
    {
        var isTesting = DelayTestHelper.IsTesting;
        var tcpingActive = isTesting && DelayTestHelper.ActiveMode == DelayTestMode.TCPing;
        var urlActive = isTesting && !tcpingActive;
        var hasAnyTestable = Global.Settings.Server.Any(IsTestableServer);

        SetSplitButtonState(
            _urlTestBtn,
            urlActive ? T("Stop testing", "停止测速") : T("URL test", "URL 测速"),
            urlActive ? Icons.Stop : Icons.Flash,
            urlActive
                ? T("Stop testing", "停止测速")
                : T("Test real URL latency for visible nodes (Smart hybrid)", "测试当前显示列表的真实 URL 延迟（智能混合）"),
            isTesting || hasAnyTestable);

        SetSplitButtonState(
            _tcpingBtn,
            tcpingActive ? T("Stop testing", "停止测速") : T("TCP latency", "TCP 延迟"),
            tcpingActive ? Icons.Stop : Icons.Probe,
            tcpingActive
                ? T("Stop testing", "停止测速")
                : T("Test TCPing latency for visible nodes", "测试当前显示列表的 TCPing 延迟"),
            isTesting || hasAnyTestable);
    }

    private static void SetSplitButtonState(SplitButton button, string label, string glyph, string tooltip, bool isEnabled)
    {
        if (button.Content == null ||
            !string.Equals(Microsoft.UI.Xaml.Automation.AutomationProperties.GetName(button), label, StringComparison.Ordinal))
        {
            button.Content = IconContent(glyph, label);
            Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(button, label);
        }

        if (!string.Equals(ToolTipService.GetToolTip(button) as string, tooltip, StringComparison.Ordinal))
            ToolTipService.SetToolTip(button, tooltip);

        button.IsEnabled = isEnabled;
    }

    private static bool IsTestableServer(Server server) =>
        server.ConfigType != EConfigType.ExternalCli;

    private Server[] DisplayedServers() =>
        _nodeRows.Select(item => item.Tag).OfType<Server>().ToArray();

    private Server[] TestableDisplayedServers() =>
        DisplayedServers().Where(IsTestableServer).ToArray();

    private static Server[] TestableAllServers() =>
        Global.Settings.Server.Where(IsTestableServer).ToArray();

    private void StartDelayTest(Server[] targets, DelayTestMode mode)
    {
        if (targets.Length == 0)
        {
            _status.Text = T("No testable nodes in current list.", "当前列表没有可测速的节点。");
            return;
        }

        DelayTestHelper.PerformTestAsync(targets, waitFinish: true, modeOverride: mode).Forget();
    }

    private static Grid CreateNodeRowGrid(Thickness? margin = null)
    {
        var grid = new Grid { ColumnSpacing = 9 };
        if (margin.HasValue)
            grid.Margin = margin.Value;
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.35, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.75, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(66) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(72) });
        return grid;
    }

    private Grid BuildNodeHeaderStrip()
    {
        var grid = CreateNodeRowGrid(new Thickness(44, 0, 12 + Ui.ScrollBarGutter, 0));
        grid.Children.Add(SortableHeaderCol(T("Name", "名称"), 0, NodeSortField.Name));
        grid.Children.Add(SortableHeaderCol(T("Core", "核心"), 1, NodeSortField.Core));
        grid.Children.Add(SortableHeaderCol(T("Latency", "延迟"), 3, NodeSortField.Delay, HorizontalAlignment.Right));
        return grid;
    }

    private Button SortableHeaderCol(string label, int col, NodeSortField sortField, HorizontalAlignment align = HorizontalAlignment.Left)
    {
        var tb = Caption(label);
        tb.FontSize = 12;
        tb.TextTrimming = TextTrimming.CharacterEllipsis;
        tb.HorizontalAlignment = align;
        tb.VerticalAlignment = VerticalAlignment.Center;

        var btn = new Button
        {
            Content = tb,
            Style = Resource<Style>("NetchWizardTab"),
            BorderThickness = new Thickness(0),
            Padding = new Thickness(3, 2, 3, 2),
            Margin = new Thickness(-3, 0, -3, 0),
            MinWidth = 0,
            MinHeight = 0,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = align,
            VerticalAlignment = VerticalAlignment.Center
        };
        btn.Click += (_, _) => ToggleNodeSort(sortField);
        Grid.SetColumn(btn, col);
        _sortHeaders[sortField] = (btn, label, tb);
        return btn;
    }

    private void ToggleNodeSort(NodeSortField field)
    {
        if (_sortField != field)
        {
            _sortField = field;
            _sortDescending = false;
        }
        else if (!_sortDescending)
        {
            _sortDescending = true;
        }
        else
        {
            _sortField = NodeSortField.None;
            _sortDescending = false;
        }

        UpdateSortHeaders();
        RefreshNodes();
    }

    private void UpdateSortHeaders()
    {
        foreach (var (field, (btn, label, tb)) in _sortHeaders)
        {
            if (field == _sortField)
            {
                var arrow = _sortDescending ? "↓" : "↑";
                tb.Text = $"{label} {arrow}";
                tb.Style = Resource<Style>("NetchAccentText");
                tb.FontWeight = Microsoft.UI.Text.FontWeights.SemiBold;
                var dirText = _sortDescending ? T("descending", "降序") : T("ascending", "升序");
                var nextHint = !_sortDescending
                    ? T("click to sort descending", "点击切换降序")
                    : T("click to reset", "点击恢复默认排序");
                var tip = TF("{0}: {1} ({2})", "{0}：{1}（{2}）", label, dirText, nextHint);
                Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(btn, tip);
                ToolTipService.SetToolTip(btn, tip);
            }
            else
            {
                tb.Text = label;
                tb.Style = Resource<Style>("NetchSecondaryText");
                tb.FontWeight = Microsoft.UI.Text.FontWeights.Normal;
                var tip = TF("Sort by {0}", "点击按{0}排序", label);
                Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(btn, tip);
                ToolTipService.SetToolTip(btn, tip);
            }
        }
    }

    private static void UpdateDelayLabel(Server server, TextBlock label)
    {
        var text = server.Delay == DelayStatus.Untested ? "—" : Format.Delay(server.Delay);
        if (label.Text != text)
            label.Text = text;

        var style = Resource<Style>(server.Delay switch
        {
            DelayStatus.Untested or DelayStatus.Testing => "NetchSecondaryText",
            < 0 => "NetchErrorText",
            < 200 => "NetchSuccessText",
            _ => "NetchWarningText"
        });
        if (!ReferenceEquals(label.Style, style))
            label.Style = style;
    }

    private void RefreshCategories(Subscription? selectSubscription = null, string? selectImportedGroup = null, bool selectManual = false)
    {
        var current = SelectedCategory;
        selectSubscription ??= selectImportedGroup == null && !selectManual ? current?.Subscription : null;
        var importedTarget = selectImportedGroup ?? (selectSubscription == null && !selectManual ? current?.Kind == NodeCategoryKind.ImportedGroup ? current.GroupName : null : null);
        var manualTarget = selectManual || (selectSubscription == null && importedTarget == null && current?.Kind == NodeCategoryKind.Manual);

        _refreshing = true;
        _categories.Items.Clear();
        var nodes = Global.Settings.Server.ToArray();

        var allItem = Category(T("All nodes", "全部节点"), count: nodes.Length.ToString());
        allItem.Tag = new NodeCategorySelection(NodeCategoryKind.All);
        AddCategory(allItem, 35);

        if (Global.Settings.Subscription.Count > 0)
            _categories.Items.Add(CategoryHeading(T("Subscriptions", "订阅")));
        foreach (var sub in Global.Settings.Subscription)
        {
            var detail = sub.UpdateFailed ? T("Update failed", "更新失败")
                : sub.LastUpdated is { } lastUpdated ? Format.LastUsed(lastUpdated.LocalDateTime) : T("Not updated", "尚未更新");
            var statusStyle = sub.UpdateFailed ? "NetchErrorText" : sub.LastUpdated.HasValue ? "NetchSuccessText" : null;
            var detailGlyph = sub.UpdateFailed ? Icons.Warning : sub.LastUpdated.HasValue ? Icons.CheckMark : null;
            var count = nodes.Count(s => s.IsSub && NodeGroupHelper.GroupEquals(s.Group, sub.Remark));
            var item = Category(sub.Remark, detail, count.ToString(), statusStyle, detailGlyph);
            item.Tag = new NodeCategorySelection(NodeCategoryKind.Subscription, sub.Remark, sub);
            item.ContextFlyout = BuildSubscriptionCategoryMenu(sub);
            AddCategory(item, 55.5);
        }

        var importedGroups = NodeGroupHelper.GetImportedGroups(nodes);
        if (importedGroups.Count > 0)
        {
            _categories.Items.Add(CategoryHeading(T("Local imports", "本地导入")));
            foreach (var groupName in importedGroups)
            {
                var count = nodes.Count(s => !s.IsSub && NodeGroupHelper.GroupEquals(s.Group, groupName));
                var item = Category(groupName, count: count.ToString());
                item.Tag = new NodeCategorySelection(NodeCategoryKind.ImportedGroup, groupName);
                item.ContextFlyout = BuildImportedGroupMenu(groupName);
                AddCategory(item, 35);
            }
        }

        _categories.Items.Add(CategoryHeading(T("Local", "本地")));
        var manual = Category(T("Added manually", "手动添加"), count: nodes.Count(s => NodeGroupHelper.IsManualGroup(s.Group)).ToString());
        manual.Tag = new NodeCategorySelection(NodeCategoryKind.Manual);
        AddCategory(manual, 35);

        ListViewItem? match = null;
        if (selectSubscription != null)
        {
            match = _categories.Items.OfType<ListViewItem>()
                .FirstOrDefault(i => i.Tag is NodeCategorySelection sel && ReferenceEquals(sel.Subscription, selectSubscription));
        }
        else if (!string.IsNullOrWhiteSpace(importedTarget))
        {
            match = _categories.Items.OfType<ListViewItem>()
                .FirstOrDefault(i => i.Tag is NodeCategorySelection { Kind: NodeCategoryKind.ImportedGroup } sel &&
                                     string.Equals(sel.GroupName, importedTarget, StringComparison.OrdinalIgnoreCase));
        }
        else if (manualTarget)
        {
            match = manual;
        }

        _categories.SelectedItem = match ?? _categories.Items[0];
        _updateAll.Visibility = Global.Settings.Subscription.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        _updateAll.IsEnabled = !_updatingSubscriptions && Global.Settings.Subscription.Any(s => s.Enable);
        _refreshing = false;
        RefreshSelection();
    }

    private MenuFlyout BuildSubscriptionCategoryMenu(Subscription sub)
    {
        var update = MenuItem(T("Update", "更新"), Icons.Refresh, () => UpdateSubscriptionsAsync(sub, useProxy: false).Forget());
        update.IsEnabled = !_updatingSubscriptions && sub.Enable;
        var menu = IconMenu(
            update,
            MenuItem(T("Rename group", "重命名分组"), Icons.Rename, () => RenameGroupAsync(sub.Remark, isSubscription: true).Forget()),
            MenuItem(T("Edit", "编辑"), Icons.Edit, () => EditSubscriptionAsync(sub).Forget()),
            MenuItem(T("Copy link", "复制链接"), Icons.Copy, () => Copy(sub.Link)),
            new MenuFlyoutSeparator(),
            MenuItem(T("Delete", "删除"), Icons.Delete, () => DeleteSubscriptionAsync(sub).Forget(), destructive: true));
        menu.Opening += (_, _) => update.IsEnabled = !_updatingSubscriptions && sub.Enable;
        return menu;
    }

    private MenuFlyout BuildImportedGroupMenu(string groupName)
    {
        var menu = new MenuFlyout();
        menu.Opening += (_, _) =>
        {
            menu.Items.Clear();
            menu.Items.Add(MenuItem(T("Test latency", "测速"), Icons.Flash, () =>
            {
                var groupNodes = GetImportedGroupServers(groupName);
                if (groupNodes.Length > 0)
                    DelayTestHelper.PerformTestAsync(groupNodes, true).Forget();
            }));
            var groupTargets = GetImportedGroupServers(groupName);
            if (TryBuildCoreAssignSubMenu(groupTargets, out var coreSub))
                menu.Items.Add(coreSub);
            menu.Items.Add(MenuItem(T("Rename group", "重命名分组"), Icons.Rename, () => RenameGroupAsync(groupName, isSubscription: false).Forget()));
            menu.Items.Add(new MenuFlyoutSeparator());
            menu.Items.Add(MenuItem(T("Delete group", "删除分组"), Icons.Delete, () => DeleteImportedGroupAsync(groupName).Forget(), destructive: true));
        };
        return menu;
    }

    private static Server[] GetSubscriptionServers(Subscription sub) =>
        Global.Settings.Server.Where(s => s.IsSub && NodeGroupHelper.GroupEquals(s.Group, sub.Remark)).ToArray();

    private static Server[] GetImportedGroupServers(string groupName) =>
        Global.Settings.Server.Where(s => !s.IsSub && NodeGroupHelper.GroupEquals(s.Group, groupName)).ToArray();

    private async Task RenameGroupAsync(string currentName, bool isSubscription)
    {
        if (XamlRoot is null)
            return;
        var renamed = await NodeEditor.RenameGroupAsync(XamlRoot, currentName, isSubscription);
        if (renamed == null)
            return;
        if (isSubscription)
        {
            var sub = Global.Settings.Subscription.FirstOrDefault(s => s.Remark.Equals(renamed, StringComparison.OrdinalIgnoreCase));
            RefreshCategories(selectSubscription: sub);
        }
        else
        {
            RefreshCategories(selectImportedGroup: renamed);
        }
    }

    private async Task DeleteSubscriptionAsync(Subscription sub)
    {
        var groupNodes = GetSubscriptionServers(sub);
        if (XamlRoot is null || !await ConfirmDeleteGroupAsync(sub.Remark, groupNodes, isSubscription: true))
            return;

        Global.Settings.Subscription.Remove(sub);
        foreach (var node in groupNodes)
            Global.Settings.Server.Remove(node);
        await Configuration.SaveAsync();
        RefreshCategories();
    }

    private async Task DeleteImportedGroupAsync(string groupName)
    {
        var groupNodes = GetImportedGroupServers(groupName);
        if (groupNodes.Length == 0 || XamlRoot is null || !await ConfirmDeleteGroupAsync(groupName, groupNodes, isSubscription: false))
            return;

        foreach (var node in groupNodes)
            Global.Settings.Server.Remove(node);
        await Configuration.SaveAsync();
        RefreshCategories();
    }

    private void AddCategory(ListViewItem item, double height)
    {
        item.Height = height;
        item.Margin = new Thickness(0, 0, 0, 2.4);
        AddSelectionIndicator(item);
        _categories.Items.Add(item);
    }

    private void RefreshSelection()
    {
        var selection = SelectedCategory;
        var filterLabel = selection?.Kind switch
        {
            NodeCategoryKind.Subscription when selection.Subscription is { } s => s.Remark,
            NodeCategoryKind.ImportedGroup when selection.GroupName is { } g => g,
            NodeCategoryKind.Manual => T("Added manually", "手动添加"),
            _ => null
        };
        _filter.PlaceholderText = filterLabel != null
            ? TF("Filter in {0}", "在 {0} 中筛选", filterLabel)
            : T("Filter nodes", "筛选节点");

        if (selection?.Kind == NodeCategoryKind.Subscription && selection.Subscription is { } sub)
        {
            _subscription.Visibility = Visibility.Visible;
            var schedule = sub.UpdateIntervalHours > 0
                ? TF("Updates every {0} h", "每 {0} 小时更新", sub.UpdateIntervalHours)
                : T("Manual updates", "手动更新");
            if (sub.TrafficRemaining is { } remaining)
                schedule += TF(" · {0} remaining", " · 剩余 {0}", Format.Bytes(remaining));
            if (sub.Expiration is { } expiration)
                schedule += TF(" · Expires {0:d}", " · 到期 {0:yyyy-MM-dd}", expiration);
            if (!string.IsNullOrWhiteSpace(sub.CustomCoreId))
            {
                if (CustomCoreHelper.IsBuiltInCoreId(sub.CustomCoreId))
                    schedule += $" · {T("Built-in core", "内置核心")}";
                else if (CustomCoreHelper.FindById(sub.CustomCoreId) is { } subCore)
                    schedule += $" · {subCore.Name}";
            }
            var updateLabel = T("Update", "更新");
            var update = new SplitButton
            {
                Content = IconContent(Icons.Refresh, updateLabel),
                IsEnabled = !_updatingSubscriptions && sub.Enable,
                Flyout = Menu(
                    (T("Update (direct)", "直连更新"), () => UpdateSubscriptionsAsync(sub, useProxy: false).Forget()),
                    (T("Update via proxy", "通过代理更新"), () => UpdateSubscriptionsAsync(sub, useProxy: true).Forget()))
            };
            Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(update, updateLabel);
            update.Click += (_, _) => UpdateSubscriptionsAsync(sub, useProxy: false).Forget();
            _updateSelected = update;
            var menu = IconButton(Icons.More, T("More actions", "更多操作"),
                flyout: IconMenu(
                    MenuItem(T("Rename group", "重命名分组"), Icons.Rename, () => RenameGroupAsync(sub.Remark, isSubscription: true).Forget()),
                    MenuItem(T("Edit", "编辑"), Icons.Edit, () => EditSubscriptionAsync(sub).Forget()),
                    MenuItem(T("Copy link", "复制链接"), Icons.Copy, () => Copy(sub.Link)),
                    new MenuFlyoutSeparator(),
                    MenuItem(T("Delete", "删除"), Icons.Delete, () => DeleteSubscriptionAsync(sub).Forget(), destructive: true)));
            _subscription.Content = Card(Split(new StackPanel { Spacing = 1, Children = { Text(sub.Remark, 15, true), Caption(sub.Link), Caption(schedule) } },
                Row(update, menu)), 10);
        }
        else if (selection?.Kind == NodeCategoryKind.ImportedGroup && selection.GroupName is { } groupName)
        {
            _updateSelected = null;
            _subscription.Visibility = Visibility.Visible;
            var count = GetImportedGroupServers(groupName).Length;
            _subscription.Content = Card(Split(
                new StackPanel
                {
                    Spacing = 1,
                    Children =
                    {
                        Text(groupName, 15, true),
                        Caption(TF("{0} nodes", "{0} 个节点", count))
                    }
                },
                Row(
                    AsyncButton(T("Rename", "重命名"), () => RenameGroupAsync(groupName, isSubscription: false), glyph: Icons.Rename),
                    AsyncButton(T("Delete group", "删除分组"), () => DeleteImportedGroupAsync(groupName), glyph: Icons.Delete))), 10);
        }
        else
        {
            _subscription.Visibility = Visibility.Collapsed;
            _subscription.Content = null;
            _updateSelected = null;
        }
        RefreshNodes();
    }

    private Server[] SelectedServers() =>
        _nodes.SelectedItems.OfType<ListViewItem>().Select(item => item.Tag).OfType<Server>().ToArray();

    private void ClearSelection()
    {
        _suppressSelectionChanged = true;
        try
        {
            _nodes.SelectedItems.Clear();
        }
        finally
        {
            _suppressSelectionChanged = false;
        }
        UpdateBatchBar();
    }

    private void UpdateBatchBar()
    {
        var count = _nodes.SelectedItems.Count;
        var visible = count >= 2;
        _batchBar.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        if (!visible)
            return;

        _batchCount.Text = TF("Selected {0}", "已选 {0} 项", count);
        _selectAllBtn.IsEnabled = _nodeRows.Count > 0 && count < _nodeRows.Count;
        _clearSelectionBtn.IsEnabled = count > 0;
        _batchTestBtn.Content = TF("Test selected ({0})", "测速所选 ({0})", count);
        _batchCopyBtn.Content = TF("Copy share links ({0})", "复制分享链接 ({0})", count);
        _batchDeleteBtn.Content = TF("Delete selected ({0})", "删除所选 ({0})", count);
        _batchDeleteBtn.IsEnabled = count > 0;

        var selected = SelectedServers();
        var eligible = selected.Where(s => s.ConfigType != Netch.Enums.EConfigType.ExternalCli).ToArray();
        _batchTestBtn.IsEnabled = eligible.Length > 0 && !DelayTestHelper.IsTesting;
        _batchCopyBtn.IsEnabled = eligible.Length > 0;

        var showCoreBtn = HasApplicableCustomCores(eligible);
        _batchCoreBtn.Visibility = showCoreBtn ? Visibility.Visible : Visibility.Collapsed;
        if (showCoreBtn)
        {
            var menu = new MenuFlyout();
            PopulateCoreAssignItems(menu.Items, eligible);
            _batchCoreBtn.Flyout = menu;
            _batchCoreBtn.IsEnabled = menu.Items.Count > 0;
        }
    }

    private static bool HasApplicableCustomCores(IReadOnlyList<Server> eligible)
    {
        if (eligible.Count == 0)
            return false;

        var protocols = eligible.Select(s => s.ConfigType).ToHashSet();
        return CustomCoreHelper.GetAll().Any(c => protocols.Contains(c.Protocol))
            || eligible.Any(s => !string.IsNullOrWhiteSpace(s.ProtoExtra.CustomCoreId));
    }

    private bool TryBuildCoreAssignSubMenu(IReadOnlyList<Server> targets, out MenuFlyoutSubItem subMenu)
    {
        subMenu = new MenuFlyoutSubItem
        {
            Text = T("Set core", "指定核心"),
            Icon = new FontIcon { Glyph = Icons.Flash }
        };
        var eligible = targets.Where(s => s.ConfigType != Netch.Enums.EConfigType.ExternalCli).ToArray();
        if (!HasApplicableCustomCores(eligible))
            return false;

        PopulateCoreAssignItems(subMenu.Items, eligible);
        return subMenu.Items.Count > 0;
    }

    private void PopulateCoreAssignItems(IList<MenuFlyoutItemBase> items, IReadOnlyList<Server> targets)
    {
        var eligible = targets.Where(s => s.ConfigType != Netch.Enums.EConfigType.ExternalCli).ToArray();
        if (eligible.Length == 0)
            return;

        var follow = new MenuFlyoutItem { Text = T("Follow default", "跟随默认") };
        follow.Click += (_, _) => AssignCustomCoreAsync(eligible, null).Forget();
        items.Add(follow);

        var builtIn = new MenuFlyoutItem { Text = T("Built-in core", "内置核心") };
        builtIn.Click += (_, _) => AssignCustomCoreAsync(eligible, CustomCoreHelper.BuiltInCoreId).Forget();
        items.Add(builtIn);

        var protocols = eligible.Select(s => s.ConfigType).Distinct().ToHashSet();
        var matchingCores = CustomCoreHelper.GetAll().Where(c => protocols.Contains(c.Protocol)).ToArray();
        if (matchingCores.Length == 0)
            return;

        items.Add(new MenuFlyoutSeparator());
        var multiProto = protocols.Count > 1;
        foreach (var core in matchingCores)
        {
            var label = multiProto ? $"{core.Name} ({ServerHelper.GetDisplayName(core.Protocol)})" : core.Name;
            var item = new MenuFlyoutItem { Text = label };
            item.Click += (_, _) => AssignCustomCoreAsync(eligible, core.Id).Forget();
            items.Add(item);
        }
    }

    private async Task AssignCustomCoreAsync(IReadOnlyList<Server> targets, string? coreId)
    {
        var isBuiltIn = CustomCoreHelper.IsBuiltInCoreId(coreId);
        var hasCustomId = !string.IsNullOrWhiteSpace(coreId) && !isBuiltIn;
        var core = hasCustomId ? CustomCoreHelper.FindById(coreId) : null;
        if (hasCustomId && core == null)
            return;

        var assignedId = isBuiltIn ? CustomCoreHelper.BuiltInCoreId : core?.Id;
        var snapshot = new List<(Server Server, string? OldId)>(targets.Count);
        foreach (var server in targets)
        {
            if (server.ConfigType == Netch.Enums.EConfigType.ExternalCli)
                continue;
            if (core != null && server.ConfigType != core.Protocol)
                continue;

            snapshot.Add((server, server.ProtoExtra.CustomCoreId));
            server.ProtoExtra.CustomCoreId = assignedId;
        }

        if (snapshot.Count == 0)
            return;

        try
        {
            await Configuration.SaveAsync(throwOnError: true);
            RefreshNodes();
        }
        catch (Exception e)
        {
            foreach (var (server, oldId) in snapshot)
                server.ProtoExtra.CustomCoreId = oldId;
            Log.Warning(e, "Assign custom core failed");
            if (XamlRoot is { } root)
                await ShowAsync(root, T("Operation failed", "操作失败"), Text(e.Message));
        }
    }

    private void CopyShareLinks(IReadOnlyList<Server> servers)
    {
        if (servers.Count == 0)
            return;

        var links = new List<string>(servers.Count);
        var skipped = 0;
        foreach (var server in servers)
        {
            try
            {
                links.Add(ShareLink.GetShareLink(server));
            }
            catch
            {
                skipped++;
            }
        }

        if (links.Count > 0)
        {
            Copy(string.Join(Environment.NewLine, links));
            var message = TF("Copied share links for {0} nodes.", "已复制 {0} 个节点的分享链接。", links.Count);
            if (skipped > 0)
                message += " " + TF("Skipped {0} unsupported nodes.", "跳过 {0} 个不支持分享的节点。", skipped);
            _status.Text = message;
        }
        else
        {
            _status.Text = TF("Skipped {0} unsupported nodes.", "跳过 {0} 个不支持分享的节点。", skipped);
        }
    }

    private async Task DeleteServersAsync(IReadOnlyList<Server> servers)
    {
        if (servers.Count == 0 || XamlRoot is null || !await ConfirmDeleteAsync(servers))
            return;

        foreach (var server in servers)
            Global.Settings.Server.Remove(server);
        await Configuration.SaveAsync();
        RefreshCategories();
    }

    private MenuFlyout BuildNodeMenu(ListViewItem item, Server target)
    {
        var menu = new MenuFlyout();
        menu.Opening += (_, _) =>
        {
            menu.Items.Clear();
            var selected = SelectedServers();
            if (item.IsSelected && selected.Length > 1)
            {
                var hasEligible = selected.Any(s => s.ConfigType != Netch.Enums.EConfigType.ExternalCli);
                if (hasEligible)
                {
                    menu.Items.Add(MenuItem(TF("Test selected ({0})", "测速所选 ({0})", selected.Length), "\uE945",
                        () => DelayTestHelper.PerformTestAsync(selected, true).Forget()));
                }
                if (TryBuildCoreAssignSubMenu(selected, out var batchCoreSub))
                    menu.Items.Add(batchCoreSub);
                if (hasEligible)
                {
                    menu.Items.Add(MenuItem(TF("Copy share links ({0})", "复制分享链接 ({0})", selected.Length), "\uE8C8",
                        () => CopyShareLinks(selected)));
                }
                menu.Items.Add(MenuItem(T("Clear selection", "取消选择"), "\uE8E6", ClearSelection));
                menu.Items.Add(new MenuFlyoutSeparator());
                menu.Items.Add(MenuItem(TF("Delete selected ({0})", "删除所选 ({0})", selected.Length), "\uE74D",
                    () => DeleteServersAsync(selected).Forget(), destructive: true));
                return;
            }

            var canShareOrTest = target.ConfigType != Netch.Enums.EConfigType.ExternalCli;
            if (canShareOrTest)
            {
                menu.Items.Add(MenuItem(T("Test latency", "测速"), "\uE945",
                    () => DelayTestHelper.PerformTestAsync([target], true).Forget()));
            }
            menu.Items.Add(MenuItem(T("Edit", "编辑"), "\uE70F", () => EditNodeAsync(target).Forget()));
            if (TryBuildCoreAssignSubMenu([target], out var singleCoreSub))
                menu.Items.Add(singleCoreSub);
            if (canShareOrTest)
            {
                menu.Items.Add(MenuItem(T("Copy share link", "复制分享链接"), "\uE8C8", () => NodeEditor.ShareAsync(XamlRoot, target, false).Forget()));
                menu.Items.Add(MenuItem(T("QR code", "二维码"), "\uED14", () => NodeEditor.ShareAsync(XamlRoot, target, true).Forget()));
            }
            menu.Items.Add(MenuItem(T("Test NAT", "测 NAT"), "\uE9D9", () => NodeEditor.TestNatAsync(XamlRoot, target).Forget()));
            menu.Items.Add(new MenuFlyoutSeparator());
            menu.Items.Add(MenuItem(T("Delete", "删除"), "\uE74D", () => DeleteServersAsync([target]).Forget(), destructive: true));
        };
        return menu;
    }

    private void RefreshNodes()
    {
        var selection = SelectedCategory;
        var nodes = NodeSortHelper.Sort(Global.Settings.Server.ToArray().Where(server =>
        {
            var inCategory = selection?.Kind switch
            {
                NodeCategoryKind.Subscription when selection.Subscription is { } sub =>
                    server.IsSub && NodeGroupHelper.GroupEquals(server.Group, sub.Remark),
                NodeCategoryKind.ImportedGroup when selection.GroupName is { } groupName =>
                    !server.IsSub && NodeGroupHelper.GroupEquals(server.Group, groupName),
                NodeCategoryKind.Manual => NodeGroupHelper.IsManualGroup(server.Group),
                _ => true
            };
            return inCategory &&
                   (Format.NodeName(server).Contains(_filter.Text, StringComparison.OrdinalIgnoreCase) ||
                    (server.Address ?? "").Contains(_filter.Text, StringComparison.OrdinalIgnoreCase));
        }), _sortField, _sortDescending);

        var selected = SelectedServers().ToHashSet();
        _suppressSelectionChanged = true;
        try
        {
            _delayLabels.Clear();
            _nodeRows.Clear();
            foreach (var server in nodes)
            {
                var rowGrid = CreateNodeRowGrid();
                rowGrid.Children.Add(Text(Format.NodeName(server)));

                var coreName = CoreSelector.GetCoreDisplayName(server);
                var typePill = server.ConfigType == EConfigType.ExternalCli
                    ? coreName
                    : $"{ServerHelper.GetDisplayName(server.ConfigType)} · {coreName}";
                var pill = Pill(typePill);
                pill.HorizontalAlignment = HorizontalAlignment.Left;
                ToolTipService.SetToolTip(pill, typePill);
                Grid.SetColumn(pill, 1);
                rowGrid.Children.Add(pill);

                var count = SceneService.Scenes.Count(s => s.ServerId == server.Id);
                var scenes = Caption(count == 0 ? "" : TF("{0} scenes", "{0} 个场景", count));
                scenes.VerticalAlignment = VerticalAlignment.Center;
                Grid.SetColumn(scenes, 2);
                rowGrid.Children.Add(scenes);

                var delay = Caption("");
                delay.FontFamily = new FontFamily("Consolas");
                delay.TextAlignment = TextAlignment.Right;
                delay.HorizontalAlignment = HorizontalAlignment.Stretch;
                delay.VerticalAlignment = VerticalAlignment.Center;
                UpdateDelayLabel(server, delay);
                _delayLabels[server] = delay;
                Grid.SetColumn(delay, 3);
                rowGrid.Children.Add(delay);

                var item = new ListViewItem
                {
                    Tag = server, HorizontalContentAlignment = HorizontalAlignment.Stretch,
                    Content = rowGrid
                };
                item.ContextFlyout = BuildNodeMenu(item, server);
                Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(item, Format.NodeName(server));
                item.DoubleTapped += (_, e) =>
                {
                    if (_nodes.SelectedItems.Count <= 1 && e.GetPosition(item).X >= 44)
                        EditNodeAsync(server).Forget();
                };
                _nodeRows.Add(item);
                if (selected.Contains(server))
                    _nodes.SelectedItems.Add(item);
            }
        }
        finally
        {
            _suppressSelectionChanged = false;
        }
        UpdateDelayTestButtons();
        UpdateBatchBar();
    }

    private async Task EditNodeAsync(Server? server, string? type = null)
    {
        if (await NodeEditor.EditAsync(XamlRoot, server, type))
            RefreshCategories();
    }

    private async Task ImportAsync(Button import, bool fromFile)
    {
        import.IsEnabled = false;
        try
        {
            string text;
            string? filePath = null;
            if (fromFile)
            {
                var picker = new FileOpenPicker(App.MainWindow!.AppWindow.Id);
                picker.FileTypeFilter.Add(".yaml");
                picker.FileTypeFilter.Add(".yml");
                picker.FileTypeFilter.Add(".txt");
                if (await picker.PickSingleFileAsync() is not { } file)
                    return;
                filePath = file.Path;
                text = await File.ReadAllTextAsync(filePath);
            }
            else
                text = await ClipboardTextAsync();

            var (count, skipped, group) = await NodeEditor.ImportTextAsync(text, filePath, XamlRoot);
            _status.Text = count == 0 ? T("No supported nodes found.", "未找到可导入的节点。")
                : TF("Imported {0} nodes.", "已导入 {0} 个节点。", count);
            if (skipped > 0)
                _status.Text += " " + i18N.TranslateFormat("Skipped {0} invalid or unsupported nodes.", skipped);

            if (count > 0)
            {
                _filter.Text = "";
                if (NodeGroupHelper.IsManualGroup(group))
                    RefreshCategories(selectManual: true);
                else
                    RefreshCategories(selectImportedGroup: group);
            }
        }
        catch (Exception e)
        {
            Log.Warning(e, "Import nodes failed");
            if (XamlRoot is { } root)
                await ShowAsync(root, T("Import failed", "导入失败"), Text(e.Message));
        }
        finally
        {
            import.IsEnabled = true;
        }
    }

    private Task<bool> ConfirmDeleteAsync(IEnumerable<Server> nodes)
    {
        var references = NodeGroupHelper.CountAffectedScenes(nodes);
        return ShowAsync(XamlRoot, T("Delete nodes?", "删除节点？"),
            Text(references == 0 ? T("The selected nodes will be deleted.", "将删除选中的节点。")
                : TF("{0} scenes will need another node.", "{0} 个场景需要更换节点。", references)),
            T("Delete", "删除"));
    }

    private Task<bool> ConfirmDeleteGroupAsync(string name, IReadOnlyList<Server> nodes, bool isSubscription)
    {
        var references = NodeGroupHelper.CountAffectedScenes(nodes);
        var title = isSubscription
            ? TF("Delete subscription \"{0}\"?", "删除订阅「{0}」？", name)
            : TF("Delete group \"{0}\"?", "删除分组「{0}」？", name);
        var body = references > 0
            ? TF("This group has {0} nodes. {1} scenes will need another node.", "此分组包含 {0} 个节点，{1} 个场景需要更换节点。", nodes.Count, references)
            : TF("This group has {0} nodes. They will be removed.", "此分组包含 {0} 个节点，删除后将一并移除。", nodes.Count);
        return ShowAsync(XamlRoot, title, Text(body), T("Delete", "删除"));
    }

    private async Task EditSubscriptionAsync(Subscription? existing)
    {
        var subscription = await NodeEditor.EditSubscriptionAsync(XamlRoot, existing);
        if (subscription != null)
            RefreshCategories(subscription);
    }

    private async Task UpdateSubscriptionsAsync(Subscription? single, bool useProxy)
    {
        if (_updatingSubscriptions)
            return;
        if (single == null && Global.Settings.Subscription.Count == 0)
            return;
        if (single != null ? !single.Enable : !Global.Settings.Subscription.Any(s => s.Enable))
            return;

        _updatingSubscriptions = true;
        _updateAll.IsEnabled = false;
        if (_updateSelected != null)
            _updateSelected.IsEnabled = false;
        var executed = false;
        try
        {
            if (!useProxy)
            {
                if (single != null)
                    await SubscriptionUtil.UpdateServerAsync(single);
                else
                    await SubscriptionUtil.UpdateServersAsync();
                executed = true;
            }
            else
            {
                executed = await Session.Current.RunWithProxyAsync(SelectProxyNodeAsync,
                    proxyUri => single != null
                        ? SubscriptionUtil.UpdateServerAsync(single, proxyUri)
                        : SubscriptionUtil.UpdateServersAsync(proxyUri));
            }

            if (executed)
                await Configuration.SaveAsync();
        }
        catch (Exception e)
        {
            Log.Warning(e, "Update subscriptions failed");
            if (XamlRoot is { } root)
                await ShowAsync(root, T("Operation failed", "操作失败"), Text(e.Message));
        }
        finally
        {
            _updatingSubscriptions = false;
            if (executed)
            {
                RefreshCategories();
            }
            else
            {
                _updateAll.IsEnabled = Global.Settings.Subscription.Any(s => s.Enable);
                if (_updateSelected != null)
                    _updateSelected.IsEnabled = SelectedCategory?.Subscription?.Enable ?? false;
            }
        }
    }

    private async Task<Server?> SelectProxyNodeAsync()
    {
        if (Global.Settings.Server.Count == 0)
            throw new InvalidOperationException(T("No nodes available to use as proxy.", "没有可用作代理的节点。"));

        if (XamlRoot is not { } root)
            return null;

        var nodes = Global.Settings.Server
            .OrderBy(server => DelayStatus.IsValid(server.Delay) ? server.Delay : int.MaxValue)
            .ToArray();
        var preferred = (_nodes.SelectedItem as ListViewItem)?.Tag as Server
            ?? (Session.Current.CurrentScene is { } scene ? SceneService.FindServer(scene) : null);
        var index = Math.Max(0, Array.FindIndex(nodes, s => s == preferred));
        var label = T("Select proxy node", "选择代理节点");
        var selector = new ComboBox
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            ItemsSource = nodes.Select(server => new NodeOption(server)).ToArray(),
            ItemTemplate = Resource<DataTemplate>("NetchWizardNodeTemplate"),
            SelectedIndex = index,
            IsTextSearchEnabled = true
        };
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(selector, label);
        var confirmed = await ShowAsync(root, label,
            Stack(Caption(T("Select a node to use as a temporary proxy for updating subscriptions.", "选择一个节点作为临时代理以更新订阅。")), selector),
            T("Update", "更新"));
        return confirmed && selector.SelectedItem is NodeOption option ? option.Server : null;
    }
}
