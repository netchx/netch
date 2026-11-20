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
using Netch.Models;
using Netch.Services;
using Netch.Utils;
using static Netch.App.Helpers.Ui;

namespace Netch.App.Pages;

public sealed partial class NodesPage : Page
{
    private readonly ListView _categories = Categories();
    private readonly ListView _nodes = new();
    private readonly ObservableCollection<ListViewItem> _nodeRows = [];
    private readonly Dictionary<Server, TextBlock> _delayLabels = [];
    private readonly TextBox _filter = Input(T("Filter nodes", "筛选节点"), search: true);
    private readonly ContentControl _subscription = new() { HorizontalContentAlignment = HorizontalAlignment.Stretch };
    private readonly ContentControl _details = new() { HorizontalContentAlignment = HorizontalAlignment.Stretch };
    private readonly TextBlock _status = Caption("");
    private readonly Button _speed;
    private Subscription? SelectedSubscription => (_categories.SelectedItem as ListViewItem)?.Tag as Subscription;
    private bool _byDelay;
    private bool _refreshing;

    public NodesPage()
    {
        InitializeComponent();
        var import = new DropDownButton { Content = T("Import", "导入") };
        import.Flyout = Menu(
            (T("From clipboard", "从剪贴板"), () => ImportAsync(import, fromFile: false).Forget()),
            (T("From Clash YAML file", "从 Clash YAML 文件"), () => ImportAsync(import, fromFile: true).Forget()));
        var addSubscription = AsyncButton(T("+ Subscription", "＋ 订阅"), () => EditSubscriptionAsync(null));
        var add = Button(T("+ Node", "＋ 节点"), () => { }, accent: true);
        add.Flyout = Menu(ServerHelper.ServerUtilDictionary.Values.OrderBy(util => util.Priority)
            .Select(util => (ServerHelper.GetDisplayName(util), (Action)(async () => await EditNodeAsync(null, util.TypeName)))).ToArray());
        _speed = AsyncButton(T("⚡ Test latency", "⚡ 测速"), () => DelayTestHelper.PerformTestAsync(true));
        _categories.SelectionChanged += (_, _) => { if (!_refreshing) RefreshSelection(); };
        _filter.TextChanged += (_, _) => RefreshNodes();
        _nodes.KeyDown += async (_, e) =>
        {
            if (e.Key == Windows.System.VirtualKey.Enter && _nodes.SelectedItem is ListViewItem { Tag: Server server })
                await EditNodeAsync(server);
        };

        Content = Ui.Page(Body(Header(T("Nodes", "节点"), import, addSubscription, add),
            TwoColumns(_categories, Body(_details,
                Split(_filter, Row(_speed, Button(T("⇅ Latency", "⇅ 延迟"), () =>
                {
                    _byDelay = !_byDelay;
                    RefreshNodes();
                }))),
                Card(_nodes, 2)), new GridLength(165))));
        _nodes.ItemContainerStyle = Resource<Style>("NetchCompactRow");
        _nodes.ItemsSource = _nodeRows;
        _details.Content = new StackPanel { Children = { _subscription, _status } };
        _status.Visibility = Visibility.Collapsed;
        _status.RegisterPropertyChangedCallback(TextBlock.TextProperty,
            (_, _) => _status.Visibility = _status.Text.Length > 0 ? Visibility.Visible : Visibility.Collapsed);
        RefreshCategories();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        AppEvents.SubscriptionChanged += RefreshSubscriptions;
        AppEvents.NodeDelaysChanged += RefreshDelays;
        if (e.Parameter is true)
            Loaded += ImportSubscriptionOnLoad;
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        AppEvents.SubscriptionChanged -= RefreshSubscriptions;
        AppEvents.NodeDelaysChanged -= RefreshDelays;
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

        if (!_byDelay)
            return;

        var ordered = _nodeRows.OrderBy(item => DelayOrder((Server)item.Tag)).ToArray();
        if (_nodeRows.SequenceEqual(ordered))
            return;

        var selected = _nodes.SelectedItem;
        var focused = FocusManager.GetFocusedElement(root) as ListViewItem;
        for (var i = 0; i < ordered.Length; i++)
            if (_nodeRows[i] != ordered[i])
                _nodeRows.Move(_nodeRows.IndexOf(ordered[i]), i);

        if (selected != null)
            _nodes.SelectedItem = selected;
        if (focused != null && _nodeRows.Contains(focused))
            focused.Focus(FocusState.Keyboard);
    });

    private static int DelayOrder(Server server) => server.Delay < 0 ? int.MaxValue : server.Delay;

    private static void UpdateDelayLabel(Server server, TextBlock label)
    {
        var text = server.Delay == -1 ? "—" : Format.Delay(server.Delay);
        if (label.Text == text)
            return;

        label.Text = text;
        label.Style = Resource<Style>(server.Delay switch
        {
            -1 => "NetchSecondaryText",
            < 0 or >= 1000 => "NetchErrorText",
            < 100 => "NetchSuccessText",
            _ => "NetchWarningText"
        });
    }

    private void RefreshCategories(Subscription? select = null, bool selectLocal = false)
    {
        select ??= SelectedSubscription;
        var local = Equals((_categories.SelectedItem as ListViewItem)?.Tag, Constants.DefaultGroup);
        _refreshing = true;
        _categories.Items.Clear();
        var nodes = Global.Settings.Server.ToArray();
        AddCategory(Category(T("All nodes", "全部节点"), count: nodes.Length.ToString()), 35);
        if (Global.Settings.Subscription.Count > 0)
            _categories.Items.Add(CategoryHeading(T("Subscriptions", "订阅")));
        foreach (var sub in Global.Settings.Subscription)
        {
            var detail = sub.UpdateFailed ? T("⚠ Update failed", "⚠ 更新失败")
                : sub.LastUpdated is { } lastUpdated ? $"✓ {Format.LastUsed(lastUpdated.LocalDateTime)}" : T("Not updated", "尚未更新");
            var statusStyle = sub.UpdateFailed ? "NetchErrorText" : sub.LastUpdated.HasValue ? "NetchSuccessText" : null;
            var item = Category(sub.Remark, detail, nodes.Count(s => s.Group == sub.Remark).ToString(), statusStyle);
            item.Tag = sub;
            AddCategory(item, 55.5);
        }

        _categories.Items.Add(CategoryHeading(T("Local", "本地")));
        var manual = Category(T("Added manually", "手动添加"), count: nodes.Count(s => s.Group == Constants.DefaultGroup).ToString());
        manual.Tag = Constants.DefaultGroup;
        AddCategory(manual, 35);
        _categories.SelectedItem = selectLocal ? manual : select == null ? (local ? manual : _categories.Items[0])
            : _categories.Items.OfType<ListViewItem>().FirstOrDefault(i => ReferenceEquals(i.Tag, select)) ?? _categories.Items[0];
        _refreshing = false;
        RefreshSelection();
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
        _filter.PlaceholderText = SelectedSubscription is { } subscription
            ? TF("Filter in {0}", "在 {0} 中筛选", subscription.Remark)
            : T("Filter nodes", "筛选节点");
        _subscription.Visibility = SelectedSubscription == null ? Visibility.Collapsed : Visibility.Visible;
        if (SelectedSubscription is { } sub)
        {
            var schedule = sub.UpdateIntervalHours > 0
                ? TF("Updates every {0} h", "每 {0} 小时更新", sub.UpdateIntervalHours)
                : T("Manual updates", "手动更新");
            if (sub.TrafficRemaining is { } remaining)
                schedule += TF(" · {0} remaining", " · 剩余 {0}", Format.Bytes(remaining));
            if (sub.Expiration is { } expiration)
                schedule += TF(" · Expires {0:d}", " · 到期 {0:yyyy-MM-dd}", expiration);
            var menu = new Button
            {
                Content = "⋯",
                Flyout = Menu((T("Edit", "编辑"), async () => await EditSubscriptionAsync(sub)),
                    (T("Copy link", "复制链接"), () => Copy(sub.Link)),
                    (T("Delete", "删除"), async () =>
                    {
                        if (!await ConfirmDeleteAsync(Global.Settings.Server.Where(s => s.Group == sub.Remark)))
                            return;
                        Global.Settings.Subscription.Remove(sub);
                        Global.Settings.Server.RemoveAll(s => s.Group == sub.Remark);
                        await Configuration.SaveAsync();
                        RefreshCategories();
                    }))
            };
            _subscription.Content = Card(Split(new StackPanel { Spacing = 1, Children = { Text(sub.Remark, 15, true), Caption(sub.Link), Caption(schedule) } }, Row(
                AsyncButton(T("⟳ Update", "⟳ 更新"), async () =>
                {
                    await SubscriptionUtil.UpdateServerAsync(sub);
                    await Configuration.SaveAsync();
                    RefreshCategories(sub);
                }), menu)), 10);
        }
        RefreshNodes();
    }

    private void RefreshNodes()
    {
        var group = SelectedSubscription?.Remark ?? (_categories.SelectedItem as ListViewItem)?.Tag as string;
        var nodes = Global.Settings.Server.ToArray().Where(server =>
            (group == null || server.Group == group) &&
            (Format.NodeName(server).Contains(_filter.Text, StringComparison.OrdinalIgnoreCase) ||
             (server.Address ?? "").Contains(_filter.Text, StringComparison.OrdinalIgnoreCase)));
        if (_byDelay)
            nodes = nodes.OrderBy(DelayOrder);

        var selected = (_nodes.SelectedItem as ListViewItem)?.Tag as Server;
        _delayLabels.Clear();
        _nodeRows.Clear();
        foreach (var server in nodes)
        {
            var count = SceneService.Scenes.Count(s => s.ServerId == server.Id);
            var delay = Caption("");
            delay.Width = 59;
            delay.FontFamily = new FontFamily("Consolas");
            delay.TextAlignment = TextAlignment.Right;
            UpdateDelayLabel(server, delay);
            _delayLabels[server] = delay;
            var scenes = Caption(count == 0 ? "" : TF("{0} scenes", "{0} 个场景", count));
            scenes.Width = 66;
            var item = new ListViewItem
            {
                Tag = server, HorizontalContentAlignment = HorizontalAlignment.Stretch,
                Content = Split(Text(Format.NodeName(server)),
                    Row(Pill(ServerHelper.GetDisplayName(server.ConfigType)), scenes, delay)),
                ContextFlyout = Menu(
                    (T("Edit", "编辑"), async () => await EditNodeAsync(server)),
                    (T("Copy share link", "复制分享链接"), async () => await NodeEditor.ShareAsync(XamlRoot, server, false)),
                    (T("QR code", "二维码"), async () => await NodeEditor.ShareAsync(XamlRoot, server, true)),
                    (T("Test NAT", "测 NAT"), async () => await NodeEditor.TestNatAsync(XamlRoot, server)),
                    (T("Delete", "删除"), async () =>
                    {
                        if (!await ConfirmDeleteAsync([server]))
                            return;
                        Global.Settings.Server.Remove(server);
                        await Configuration.SaveAsync();
                        RefreshCategories();
                    }))
            };
            Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(item, Format.NodeName(server));
            item.DoubleTapped += async (_, _) => await EditNodeAsync(server);
            AddSelectionIndicator(item);
            _nodeRows.Add(item);
            if (server == selected)
                _nodes.SelectedItem = item;
        }
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
            if (fromFile)
            {
                var picker = new FileOpenPicker(App.MainWindow!.AppWindow.Id);
                picker.FileTypeFilter.Add(".yaml");
                picker.FileTypeFilter.Add(".yml");
                if (await picker.PickSingleFileAsync() is not { } file)
                    return;
                text = await File.ReadAllTextAsync(file.Path);
            }
            else
                text = await ClipboardTextAsync();

            var (count, skipped) = await NodeEditor.ImportTextAsync(text);
            _status.Text = count == 0 ? T("No supported nodes found.", "未找到可导入的节点。")
                : TF("Imported {0} nodes.", "已导入 {0} 个节点。", count);
            if (skipped > 0)
                _status.Text += " " + i18N.TranslateFormat("Skipped {0} invalid or unsupported nodes.", skipped);

            if (count > 0)
            {
                _filter.Text = "";
                RefreshCategories(selectLocal: true);
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
        var ids = nodes.Select(s => s.Id).ToHashSet();
        var references = SceneService.Scenes.Count(s => ids.Contains(s.ServerId));
        return ShowAsync(XamlRoot, T("Delete nodes?", "删除节点？"),
            Text(references == 0 ? T("The selected nodes will be deleted.", "将删除选中的节点。")
                : TF("{0} scenes will need another node.", "{0} 个场景需要更换节点。", references)),
            T("Delete", "删除"));
    }

    private async Task EditSubscriptionAsync(Subscription? existing)
    {
        var subscription = await NodeEditor.EditSubscriptionAsync(XamlRoot, existing);
        if (subscription != null)
            RefreshCategories(subscription);
    }
}
