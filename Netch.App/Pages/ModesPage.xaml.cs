using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.Storage.Pickers;
using Netch.App.Helpers;
using Netch.App.Services;
using Netch.Models.Modes;
using Netch.Models.Modes.ProcessMode;
using Netch.Models.Modes.TunMode;
using Netch.Services;
using Netch.Utils;
using static Netch.App.Helpers.Ui;

namespace Netch.App.Pages;

public sealed partial class ModesPage : Page
{
    private readonly ListView _groups = Categories();
    private readonly ListView _modes = new();
    private readonly TextBox _query = Input(T("Search modes or executable names", "搜索模式或程序名，例如 steam.exe"), search: true);
    private readonly ComboBox _source;
    private readonly Border _updates;
    private readonly TextBlock _updateText = Text("");
    private bool _loading;

    public ModesPage()
    {
        InitializeComponent();
        _source = Choice(T("Source", "来源"), new[] { T("All sources", "全部来源") }.Concat(Enum.GetValues<ModeOrigin>().Select(OriginName)));
        _source.Width = 97;
        _source.Padding = new Thickness(9, 0, 2, 0);
        _source.SelectionChanged += (_, _) => Refresh();
        _groups.SelectionChanged += (_, _) => Refresh();
        _query.TextChanged += (_, _) => Refresh();
        _modes.ItemContainerStyle = Resource<Style>("NetchCompactRow");
        _modes.DoubleTapped += (_, _) => OpenSelected();
        _modes.KeyDown += (_, e) => { if (e.Key == Windows.System.VirtualKey.Enter) OpenSelected(); };
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
        var results = Body(Split(_query, _source), Card(_modes, 2));
        results.RowSpacing = 10;
        var interior = Body(_updates, TwoColumns(_groups, results, new GridLength(165)));
        interior.RowSpacing = 0;
        Content = Ui.Page(Body(Header(T("Modes", "模式"),
                AsyncButton(T("📂 Import from file", "📂 从文件导入"), ImportAsync),
                Button(T("+ Routing rules", "＋ 路由规则"), () => OpenEditor(ModeType.TunMode)),
                Button(T("+ Process mode", "＋ 进程模式"), () => OpenEditor(ModeType.ProcessMode), true)),
            interior));
        LoadGroups();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        if (e.Parameter is Mode mode)
            Content = new ModeEditor(mode.Type, mode);
        else
        {
            Downloads.Changed += RefreshUpdates;
            AppEvents.ModesChanged += LoadGroups;
            RefreshUpdates();
        }
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        Downloads.Changed -= RefreshUpdates;
        AppEvents.ModesChanged -= LoadGroups;
    }

    private void RefreshUpdates()
    {
        var additions = Downloads.Rules.Count(rule => rule.Kind == RuleChangeKind.Added);
        var updates = Downloads.Rules.Count(rule => rule.Kind == RuleChangeKind.Updated);
        var conflicts = Downloads.Rules.Count(rule => rule.Kind == RuleChangeKind.Conflict);
        var visible = additions + updates + conflicts > 0;
        _updates.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        _updates.Margin = new Thickness(0, 0, 0, visible ? 11 : 0);
        _updateText.Text = T($"{additions} new rules, {updates} updates, {conflicts} conflicts",
            $"在线规则库有 {additions} 个新增、{updates} 个更新、{conflicts} 个冲突");
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
        foreach (var group in Global.Modes.GroupBy(GroupOf).OrderBy(g => GroupOrder(g.Key)).ThenBy(g => g.Key))
        {
            var name = group.Key switch
            {
                "Game" => T("Game", "游戏"),
                "Bypass" => T("Bypass rules", "绕过规则"),
                "TUNTAP" => "TUN",
                "Other" => T("Other", "其他"),
                "" => T("Ungrouped", "未分组"),
                _ => group.Key
            };
            var item = GroupCategory(name, group.Count());
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

    private static int GroupOrder(string key) => key switch
    {
        "Game" => 0, "Bypass" => 1, "TUNTAP" => 2, "Other" => 3, _ => 4
    };

    internal static string GroupOf(Mode mode) => Path.GetDirectoryName(SceneService.GetModeId(mode)) ?? "";

    private void Refresh()
    {
        if (_loading)
            return;
        var group = (_groups.SelectedItem as ListViewItem)?.Tag as string;
        var previous = (_modes.SelectedItem as ListViewItem)?.Tag as Mode;
        var previousPath = previous == null ? null : SceneService.GetModeId(previous);
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
            var references = Caption(count == 0 ? "" : T($"{count} scenes", $"{count} 个场景"));
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
                Content = Split(Text(mode.i18NRemark), Row(Pill(Format.ModeTypeName(mode.Type)), status, referenceColumn)),
                ContextFlyout = Menu((T("Edit", "编辑"), () => OpenEditor(mode.Type, mode)),
                    (T("Delete", "删除"), async () =>
                    {
                        if (!await ShowAsync(XamlRoot, T("Delete mode?", "删除模式？"),
                                Text(count == 0 ? mode.i18NRemark : T($"{count} scenes use this mode.", $"{count} 个场景引用此模式。")), T("Delete", "删除")))
                            return;
                        try
                        {
                            ModeService.Delete(mode);
                        }
                        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
                        {
                            await ShowAsync(XamlRoot, T("Could not delete mode", "无法删除模式"), Text(e.Message));
                            return;
                        }
                        LoadGroups();
                    }))
            };
            Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(item, mode.i18NRemark);
            AddSelectionIndicator(item);
            _modes.Items.Add(item);
            if (string.Equals(path, previousPath, StringComparison.OrdinalIgnoreCase))
                _modes.SelectedItem = item;
        }
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
        if (_modes.SelectedItem is ListViewItem { Tag: Mode mode })
            OpenEditor(mode.Type, mode);
    }

    private void OpenEditor(ModeType type, Mode? mode = null) => Content = new ModeEditor(type, mode);

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
