using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.VisualStudio.Threading;
using Microsoft.Windows.Storage.Pickers;
using Netch.App.Helpers;
using Netch.App.Services;
using Netch.Models.Modes;
using Netch.Models.Modes.ProcessMode;
using Netch.Models.Modes.TunMode;
using Netch.Services;
using Netch.Utils;
using Windows.System;
using static Netch.App.Helpers.Ui;

namespace Netch.App.Pages;

public sealed partial class ModesPage : Page
{
    private readonly ListView _groups = Categories();
    private readonly ListView _modes = new() { SelectionMode = ListViewSelectionMode.Multiple };
    private readonly TextBox _query = Input(T("Search modes or executable names", "搜索模式或程序名，例如 steam.exe"), search: true);
    private readonly ComboBox _source;
    private readonly Border _batchBar;
    private readonly TextBlock _batchCount = Caption("");
    private readonly Button _selectAllBtn;
    private readonly Button _clearSelectionBtn;
    private readonly Button _batchDeleteBtn;
    private readonly Border _updates;
    private readonly TextBlock _updateText = Text("");
    private readonly UIElement _listContent;
    private bool _loading;
    private bool _suppressSelectionChanged;
    private bool _inEditor;
    private bool _needsReload;

    public ModesPage()
    {
        InitializeComponent();
        _source = Choice(T("Source", "来源"), new[] { T("All sources", "全部来源") }.Concat(Enum.GetValues<ModeOrigin>().Select(OriginName)));
        _source.MinWidth = 112;
        _source.Height = 36;
        _source.SelectionChanged += (_, _) => Refresh();
        _groups.SelectionChanged += (_, _) => Refresh();
        _query.TextChanged += (_, _) => Refresh();

        _selectAllBtn = Button(T("Select all", "全选"), () => _modes.SelectAll());
        _clearSelectionBtn = Button(T("Clear selection", "取消选择"), ClearSelection);
        _batchDeleteBtn = AsyncButton(TF("Delete selected ({0})", "删除所选 ({0})", 0), () => DeleteModesAsync(SelectedModes()), refreshEnabled: UpdateBatchBar);
        foreach (var btn in new[] { _selectAllBtn, _clearSelectionBtn, _batchDeleteBtn })
        {
            btn.MinHeight = 28;
            btn.Padding = new Thickness(10, 4, 10, 4);
        }

        _batchBar = Card(Split(Row(_batchCount, _selectAllBtn, _clearSelectionBtn), _batchDeleteBtn), 8);
        _batchBar.Margin = new Thickness(0, 10, 0, 0);
        _batchBar.Visibility = Visibility.Collapsed;

        _modes.ItemContainerStyle = Resource<Style>("NetchSelectableRow");
        _modes.SelectionChanged += (_, _) =>
        {
            if (!_suppressSelectionChanged)
                UpdateBatchBar();
        };
        var selectAllAccel = new KeyboardAccelerator { Key = VirtualKey.A, Modifiers = VirtualKeyModifiers.Control };
        selectAllAccel.Invoked += (_, e) =>
        {
            _modes.SelectAll();
            e.Handled = true;
        };
        _modes.KeyboardAccelerators.Add(selectAllAccel);
        _modes.KeyDown += (_, e) =>
        {
            if (e.Key == VirtualKey.Enter && _modes.SelectedItems.Count <= 1)
            {
                OpenSelected();
            }
            else if (e.Key == VirtualKey.Delete && SelectedModes() is { Length: > 0 } selected)
            {
                DeleteModesAsync(selected).Forget();
            }
            else if (e.Key == VirtualKey.Escape && _modes.SelectedItems.Count > 0)
            {
                ClearSelection();
                e.Handled = true;
            }
        };

        _updateText.TextWrapping = TextWrapping.Wrap;
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetLiveSetting(_updateText,
            Microsoft.UI.Xaml.Automation.Peers.AutomationLiveSetting.Polite);
        var go = Button(T("Go to Downloads", "去下载"), () => App.MainWindow?.Navigate("Downloads", true));
        go.Width = 68;
        go.Height = 29;
        go.MinHeight = 0;
        go.Padding = new Thickness(0);
        go.VerticalAlignment = VerticalAlignment.Center;
        var icon = Pill("↻", "NetchAccentText");
        icon.Width = 23;
        icon.Height = 22;
        icon.Padding = new Thickness(0);
        ((TextBlock)icon.Child).HorizontalAlignment = HorizontalAlignment.Center;
        var noticeText = TwoColumns(icon, _updateText, GridLength.Auto);
        _updates = new Border
        {
            Name = "ModeUpdates", MinHeight = 49, Padding = new Thickness(14, 5, 14, 5),
            Style = Resource<Style>("NetchUpdateNotice"), Child = Split(noticeText, go)
        };
        _query.Height = 36;
        var searchSection = new StackPanel
        {
            Children =
            {
                Split(_query, _source),
                _batchBar
            }
        };
        var results = Body(searchSection, Card(_modes, 2));
        results.RowSpacing = 10;
        var interior = Body(_updates, TwoColumns(_groups, results, new GridLength(165)));
        interior.RowSpacing = 0;
        _listContent = Ui.Page(Body(Header(T("Modes", "模式"),
                AsyncButton(T("📂 Import from file", "📂 从文件导入"), ImportAsync),
                Button(T("+ Routing rules", "＋ 路由规则"), () => OpenEditor(ModeType.TunMode)),
                Button(T("+ Process mode", "＋ 进程模式"), () => OpenEditor(ModeType.ProcessMode), true)),
            interior));
        Content = _listContent;
        LoadGroups();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        Downloads.Changed += RefreshUpdates;
        AppEvents.ModesChanged += OnModesChanged;
        if (e.Parameter is Mode mode)
        {
            OpenEditor(mode.Type, mode);
            return;
        }
        if (_inEditor)
            ReturnToList();
        RefreshUpdates();
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        Downloads.Changed -= RefreshUpdates;
        AppEvents.ModesChanged -= OnModesChanged;
    }

    private void OnModesChanged() => DispatcherQueue.TryEnqueue(() =>
    {
        if (_inEditor)
        {
            _needsReload = true;
            return;
        }
        LoadGroups();
    });

    internal void ReturnToList(bool reload = false)
    {
        if (!_inEditor)
            return;
        _inEditor = false;
        Content = _listContent;
        if (reload || _needsReload)
        {
            _needsReload = false;
            LoadGroups();
        }
    }

    private void RefreshUpdates()
    {
        var additions = Downloads.Rules.Count(rule => rule.Kind == RuleChangeKind.Added);
        var updates = Downloads.Rules.Count(rule => rule.Kind == RuleChangeKind.Updated);
        var conflicts = Downloads.Rules.Count(rule => rule.Kind == RuleChangeKind.Conflict);
        var visible = additions + updates + conflicts > 0;
        _updates.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        _updates.Margin = new Thickness(0, 0, 0, visible ? 11 : 0);
        _updateText.Text = TF("{0} new rules, {1} updates, {2} conflicts",
            "在线规则库有 {0} 个新增、{1} 个更新、{2} 个冲突", additions, updates, conflicts);
        if (!_inEditor)
            Refresh();
    }

    private void LoadGroups()
    {
        var selectedGroup = (_groups.SelectedItem as ListViewItem)?.Tag as string;
        _loading = true;
        _groups.Items.Clear();
        _groups.Items.Add(GroupCategory(T("All modes", "全部模式"), Global.Modes.Count));
        var heading = CategoryHeading(T("Groups", "分组"));
        heading.Height = 24;
        _groups.Items.Add(heading);
        foreach (var group in Global.Modes.GroupBy(GroupOf).OrderBy(g => Format.ModeGroupOrder(g.Key)).ThenBy(g => g.Key))
        {
            var item = GroupCategory(Format.ModeGroupName(group.Key), group.Count());
            item.Tag = group.Key;
            _groups.Items.Add(item);
        }
        _groups.SelectedItem = _groups.Items.OfType<ListViewItem>()
            .FirstOrDefault(item => item.Tag is string key && string.Equals(key, selectedGroup, StringComparison.OrdinalIgnoreCase))
            ?? _groups.Items[0];
        _loading = false;
        Refresh();
    }

    private static ListViewItem GroupCategory(string label, int count)
    {
        var item = Category(label, count: count.ToString());
        item.Height = 36;
        item.Margin = new Thickness(0, 0, 0, 2);
        AddSelectionIndicator(item);
        return item;
    }

    internal static string GroupOf(Mode mode) => Path.GetDirectoryName(SceneService.GetModeId(mode)) ?? "";

    private Mode[] SelectedModes() =>
        _modes.SelectedItems.OfType<ListViewItem>().Select(item => item.Tag).OfType<Mode>().ToArray();

    private void ClearSelection()
    {
        _suppressSelectionChanged = true;
        try
        {
            _modes.SelectedItems.Clear();
        }
        finally
        {
            _suppressSelectionChanged = false;
        }
        UpdateBatchBar();
    }

    private void UpdateBatchBar()
    {
        var count = _modes.SelectedItems.Count;
        var visible = count >= 2;
        _batchBar.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        if (!visible)
            return;

        _batchCount.Text = TF("Selected {0}", "已选 {0} 项", count);
        _selectAllBtn.IsEnabled = _modes.Items.Count > 0 && count < _modes.Items.Count;
        _clearSelectionBtn.IsEnabled = count > 0;
        _batchDeleteBtn.Content = TF("Delete selected ({0})", "删除所选 ({0})", count);
        _batchDeleteBtn.IsEnabled = count > 0;
    }

    private void OpenModePath(string? fullName, bool selectInExplorer)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return;

        try
        {
            var fullPath = Path.GetFullPath(fullName);
            if (selectInExplorer)
            {
                if (File.Exists(fullPath))
                {
                    Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{fullPath}\"") { UseShellExecute = true });
                }
                else if (Path.GetDirectoryName(fullPath) is { } dir && Directory.Exists(dir))
                {
                    Process.Start(new ProcessStartInfo("explorer.exe", $"\"{dir}\"") { UseShellExecute = true });
                }
            }
            else if (File.Exists(fullPath))
            {
                Process.Start(new ProcessStartInfo(fullPath) { UseShellExecute = true });
            }
        }
        catch (Exception e) when (XamlRoot != null)
        {
            ShowAsync(XamlRoot, T("Operation failed", "操作失败"), Text(e.Message)).Forget();
        }
    }

    private async Task DeleteModesAsync(IReadOnlyList<Mode> targets)
    {
        if (targets.Count == 0 || XamlRoot is null)
            return;

        var modeIds = targets.Select(SceneService.GetModeId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var refCount = SceneService.Scenes.Count(s => modeIds.Contains(s.ModeId));
        string body;
        if (targets.Count == 1)
        {
            body = refCount == 0
                ? targets[0].i18NRemark
                : TF("{0} scenes use this mode.", "{0} 个场景引用此模式。", refCount);
        }
        else
        {
            body = TF("{0} modes selected.", "已选 {0} 个模式。", targets.Count);
            if (refCount > 0)
                body += " " + TF("{0} scenes use these modes.", "{0} 个场景引用这些模式。", refCount);
        }

        if (!await ShowAsync(XamlRoot, T("Delete mode?", "删除模式？"), Text(body), T("Delete", "删除")))
            return;

        var errors = new List<string>();
        foreach (var mode in targets)
        {
            try
            {
                ModeService.Delete(mode);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                errors.Add($"{mode.i18NRemark}: {e.Message}");
            }
        }

        if (errors.Count > 0)
        {
            var title = targets.Count == 1 ? T("Could not delete mode", "无法删除模式") : T("Could not delete modes", "无法删除部分模式");
            await ShowAsync(XamlRoot, title, Text(string.Join(Environment.NewLine, errors)));
        }

        LoadGroups();
    }

    private MenuFlyout BuildModeMenu(ListViewItem item, Mode target)
    {
        var menu = new MenuFlyout();
        menu.Opening += (_, _) =>
        {
            menu.Items.Clear();
            var selected = SelectedModes();
            if (item.IsSelected && selected.Length > 1)
            {
                menu.Items.Add(MenuItem(T("Clear selection", "取消选择"), "\uE8E6", ClearSelection));
                menu.Items.Add(new MenuFlyoutSeparator());
                menu.Items.Add(MenuItem(TF("Delete selected ({0})", "删除所选 ({0})", selected.Length), "\uE74D",
                    () => DeleteModesAsync(selected).Forget(), destructive: true));
                return;
            }

            menu.Items.Add(MenuItem(T("Edit", "编辑"), "\uE70F",
                () => DispatcherQueue.TryEnqueue(() => OpenEditor(target.Type, target))));
            menu.Items.Add(MenuItem(T("Create duplicate", "创建副本"), "\uE8C8",
                () => DispatcherQueue.TryEnqueue(() => OpenDuplicate(target))));
            menu.Items.Add(new MenuFlyoutSeparator());
            menu.Items.Add(MenuItem(T("Open file", "打开文件"), "\uE8E5", () => OpenModePath(target.FullName, selectInExplorer: false)));
            menu.Items.Add(MenuItem(T("Open folder", "打开所在目录"), "\uE838", () => OpenModePath(target.FullName, selectInExplorer: true)));
            menu.Items.Add(new MenuFlyoutSeparator());
            menu.Items.Add(MenuItem(T("Delete", "删除"), "\uE74D",
                () => DeleteModesAsync([target]).Forget(), destructive: true));
        };
        return menu;
    }

    private void Refresh()
    {
        if (_loading)
            return;
        var group = (_groups.SelectedItem as ListViewItem)?.Tag as string;
        var selectedIds = SelectedModes().Select(SceneService.GetModeId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        _suppressSelectionChanged = true;
        try
        {
            _modes.Items.Clear();
            foreach (var mode in Global.Modes)
            {
                if (group != null && !group.Equals(GroupOf(mode), StringComparison.OrdinalIgnoreCase))
                    continue;
                if (_query.Text is { Length: > 0 } query && !mode.i18NRemark.Contains(query, StringComparison.OrdinalIgnoreCase) &&
                    (mode is not Redirector process || !process.Handle.Concat(process.Bypass).Any(r =>
                        r.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                        r.Replace(@"\.", ".", StringComparison.Ordinal).Contains(query, StringComparison.OrdinalIgnoreCase))))
                    continue;
                var path = SceneService.GetModeId(mode);
                var origin = Downloads.Origin(path);
                if (_source.SelectedIndex > 0 && _source.SelectedIndex - 1 != (int)origin)
                    continue;
                var count = SceneService.Scenes.Count(s => string.Equals(s.ModeId, path, StringComparison.OrdinalIgnoreCase));
                var references = Caption(count == 0 ? "" : TF("{0} scenes", "{0} 个场景", count));
                references.TextAlignment = TextAlignment.Right;
                var referenceColumn = new Grid
                {
                    Name = "ModeReferences", Width = 59, VerticalAlignment = VerticalAlignment.Center, Children = { references }
                };
                var status = Pill(OriginName(origin),
                    origin == ModeOrigin.Update ? "NetchAccentText" : origin == ModeOrigin.Modified ? "NetchErrorText" : null);
                if (origin is ModeOrigin.Update or ModeOrigin.Modified)
                    status.Style = Resource<Style>(origin == ModeOrigin.Update ? "NetchUpdatedPill" : "NetchModifiedPill");
                var item = new ListViewItem
                {
                    Tag = mode, HorizontalContentAlignment = HorizontalAlignment.Stretch,
                    Content = Split(Text(mode.i18NRemark), Row(Pill(Format.ModeTypeName(mode.Type)), status, referenceColumn))
                };
                item.ContextFlyout = BuildModeMenu(item, mode);
                Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(item, mode.i18NRemark);
                item.DoubleTapped += (_, e) =>
                {
                    if (_modes.SelectedItems.Count <= 1 && e.GetPosition(item).X >= 44)
                        OpenEditor(mode.Type, mode);
                };
                _modes.Items.Add(item);
                if (selectedIds.Contains(path))
                    _modes.SelectedItems.Add(item);
            }
        }
        finally
        {
            _suppressSelectionChanged = false;
        }
        UpdateBatchBar();
    }

    private static string OriginName(ModeOrigin origin) => origin switch
    {
        ModeOrigin.Bundled => T("Built-in", "内置"),
        ModeOrigin.Update => T("Update available", "有更新"),
        ModeOrigin.Modified => T("Modified locally", "本地已修改"),
        _ => T("Custom", "自定义")
    };

    private void OpenSelected()
    {
        var item = _modes.SelectedItems.Count == 1
            ? _modes.SelectedItem as ListViewItem
            : _modes.SelectedItems.Count == 0 && XamlRoot != null
                ? FocusManager.GetFocusedElement(XamlRoot) as ListViewItem
                : null;
        if (item?.Tag is Mode mode && _modes.Items.Contains(item))
            OpenEditor(mode.Type, mode);
    }

    private void OpenEditor(ModeType type, Mode? mode = null, Mode? template = null)
    {
        _inEditor = true;
        _needsReload = false;
        Content = new ModeEditor(type, mode, saved => ReturnToList(saved), template);
    }

    private void OpenDuplicate(Mode mode) => OpenEditor(mode.Type, null, mode);

    private async Task ImportAsync()
    {
        var picker = new FileOpenPicker(App.MainWindow!.AppWindow.Id);
        picker.FileTypeFilter.Add(".json");
        picker.FileTypeFilter.Add(".txt");
        if (await picker.PickSingleFileAsync() is not { } file)
            return;

        var mode = ModeHelper.LoadMode(file.Path);
        mode.FullName = ModeEditor.NewPath("Custom", Path.GetFileNameWithoutExtension(file.Path));
        Directory.CreateDirectory(Path.GetDirectoryName(mode.FullName)!);
        ModeService.Instance.Add(mode);
        LoadGroups();
        OpenEditor(mode.Type, mode);
    }
}
