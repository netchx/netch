using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Netch.App.Helpers;
using Netch.App.Services;
using Netch.Controllers;
using Netch.Models.Modes.WebProxyMode;
using Netch.Services;
using Netch.Utils;
using static Netch.App.Helpers.Ui;

namespace Netch.App.Pages;

public sealed partial class DownloadsPage : Page
{
    private const int TabCores = 0;
    private const int TabRules = 1;
    private const int TabPac = 2;
    private const int TabHistory = 4;

    private readonly ListView _tabs = Categories();
    private readonly TextBlock _coreCount = Caption("");
    private readonly TextBlock _coreDetail = Caption("");
    private readonly TextBlock _rulesCount = Caption("");
    private readonly TextBlock _rulesDetail = Caption("");
    private readonly TextBlock _pacCount = Caption("");
    private readonly TextBlock _pacDetail = Caption("");
    private readonly ContentControl _pane = new() { HorizontalContentAlignment = HorizontalAlignment.Stretch, VerticalContentAlignment = VerticalAlignment.Stretch };
    private readonly TextBlock _checked = Caption("");
    private readonly Button _check;
    private readonly Button _apply;
    private readonly InfoBar _notice = new() { IsClosable = false };
    private readonly List<(RuleChange Rule, CheckBox Check, ComboBox? Conflict)> _selection = [];
    private readonly Dictionary<string, (bool Checked, int Action)> _selectedRules = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<RuleChange, Task<(string[] Added, string[] Removed)>> _rulePreviews = [];
    private ProgressBar? _progress;
    private TextBlock? _progressLabel;
    private bool _present;

    public DownloadsPage()
    {
        InitializeComponent();
        _check = AsyncButton(T("Check updates", "检查更新"), () => Downloads.CheckAsync(isAuto: false), glyph: Icons.Refresh);
        _apply = AsyncButton("", ApplyAsync, true, ChangeApply);
        _tabs.Items.Add(DownloadCategory(T("App and cores", "程序与核心"), _coreCount, _coreDetail));
        _tabs.Items.Add(DownloadCategory(T("Mode rules", "模式规则"), _rulesCount, _rulesDetail));
        _tabs.Items.Add(DownloadCategory(T("PAC rules", "PAC 规则"), _pacCount, _pacDetail));
        _tabs.Items.Add(CategoryHeading(T("History", "历史")));
        var history = Category(T("Download history", "下载记录"));
        history.Height = 36;
        AddSelectionIndicator(history);
        _tabs.Items.Add(history);
        _tabs.SelectionChanged += (_, _) => Refresh();
        _tabs.SelectedIndex = TabCores;
        var tabs = Body(_notice, TwoColumns(_tabs, _pane, new GridLength(165)));
        tabs.RowSpacing = 0;
        Content = Ui.Page(Body(Header(T("Downloads", "下载"), _checked, _check, _apply), tabs));
    }

    private static ListViewItem DownloadCategory(string label, TextBlock count, TextBlock detail)
    {
        var item = Category(label);
        item.Content = new StackPanel { Spacing = 1, Children = { Split(Text(label, strong: true), count), detail } };
        item.Height = 54;
        item.Margin = new Thickness(0, 0, 0, 2.5);
        AddSelectionIndicator(item);
        return item;
    }

    private void RefreshCategories()
    {
        var transfer = Downloads.ActiveTransfer;
        var pending = AvailableCores().Count(item => item.Core != transfer?.Core);
        var active = transfer is { PacPreset: null } ? 1 : 0;
        _coreCount.Text = (pending + active).ToString();
        _coreDetail.Text = active == 0 ? TF("{0} updates", "{0} 个更新", pending)
            : TF("{0} updates · {1} downloading", "{0} 个更新 · {1} 个下载中", pending, active);
        _rulesCount.Text = Downloads.Rules.Count.ToString();
        if (Downloads.LastRulesSynchronized is not { } last)
        {
            _rulesDetail.Text = T("Not yet synced", "尚未同步");
        }
        else
        {
            var months = (int)((DateTimeOffset.Now - last).TotalDays / 30);
            _rulesDetail.Text = months > 0 ? TF("Synced {0} mo ago", "{0} 个月前同步", months)
                : TF("Synced {0}", "{0}同步", Format.LastUsed(last.LocalDateTime));
        }

        var pacPending = PacRuleService.RemotePresets.Count(p =>
            p != transfer?.PacPreset && Downloads.IsPacUpdateAvailable(p));
        var pacActive = transfer?.PacPreset != null ? 1 : 0;
        _pacCount.Text = (pacPending + pacActive).ToString();
        if (pacActive > 0)
        {
            _pacDetail.Text = TF("{0} updates · {1} downloading", "{0} 个更新 · {1} 个下载中", pacPending, pacActive);
        }
        else if (pacPending > 0 && Downloads.RemotePacVersion != null)
        {
            _pacDetail.Text = TF("{0} updates", "{0} 个更新", pacPending);
        }
        else if (Downloads.LastPacSynchronized is { } pacLast)
        {
            var pacMonths = (int)((DateTimeOffset.Now - pacLast).TotalDays / 30);
            _pacDetail.Text = pacMonths > 0
                ? TF("Synced {0} mo ago", "{0} 个月前同步", pacMonths)
                : TF("Synced {0}", "{0}同步", Format.LastUsed(pacLast.LocalDateTime));
        }
        else
        {
            var hasBundled = PacRuleService.RemotePresets.Any(p => PacRuleService.ResolveRuleFilePath(p) != null);
            _pacDetail.Text = hasBundled ? T("Bundled", "随包内置") : T("Not yet synced", "尚未同步");
        }
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        _present = true;
        Downloads.Changed += Refresh;
        Downloads.ProgressChanged += UpdateProgress;
        if (e.Parameter is true) _tabs.SelectedIndex = TabRules;
        else if (e.Parameter is "pac") _tabs.SelectedIndex = TabPac;
        else if (e.Parameter is "check") _ = Downloads.CheckAsync(isAuto: false);
        _ = Downloads.PopulateInstalledAsync();
        Refresh();
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        _present = false;
        Downloads.Changed -= Refresh;
        Downloads.ProgressChanged -= UpdateProgress;
        _progress = null;
        _progressLabel = null;
    }

    private void Refresh()
    {
        if (!_present)
            return;
        _checked.Text = Downloads.Checking ? T("Checking…", "正在检查…")
            : Downloads.LastChecked is { } time ? TF("Last checked {0:HH:mm}", "上次检查 {0:HH:mm}", time) : "";
        _check.IsEnabled = !Downloads.Checking && !Downloads.Applying && Downloads.ActiveTransfer == null;
        var reasons = _tabs.SelectedIndex switch
        {
            TabCores => Downloads.Errors.Where(error => error.Key is not ("Rules" or "PAC")),
            TabRules => Downloads.Errors.Where(error => error.Key == "Rules"),
            TabPac => Downloads.Errors.Where(error => error.Key == "PAC"),
            _ => []
        };
        var warningParts = reasons.Select(item => item.Key + " · " + item.Value).ToList();
        if (Downloads.FormatRateLimitError() is { } rateMsg)
        {
            if (_tabs.SelectedIndex == TabRules && !Downloads.Errors.ContainsKey("Rules"))
                warningParts.Add(rateMsg);
            else if (_tabs.SelectedIndex == TabCores && Global.Settings.CheckBetaUpdate && warningParts.Count == 0)
                warningParts.Add(rateMsg);
        }
        var warning = string.Join(" · ", warningParts);
        _notice.Severity = InfoBarSeverity.Warning;
        _notice.Message = warning;
        _notice.IsOpen = warning.Length > 0;
        _notice.Margin = new Thickness(0, 0, 0, _notice.IsOpen ? 11 : 0);
        RefreshCategories();
        foreach (var stale in _rulePreviews.Keys.Except(Downloads.Rules).ToArray())
            _rulePreviews.Remove(stale);
        _selection.Clear();
        _progress = null;
        _progressLabel = null;
        _pane.Content = _tabs.SelectedIndex switch
        {
            TabRules => RulesPane(),
            TabPac => PacPane(),
            TabHistory => HistoryPane(),
            _ => CoresPane()
        };
        ChangeApply();
    }

    private void ChangeApply()
    {
        var tab = _tabs.SelectedIndex;
        var isRules = tab == TabRules;
        var count = tab switch
        {
            TabRules => _selection.Count(entry => entry.Check.IsChecked == true && entry.Conflict?.SelectedIndex != 0),
            TabPac => Downloads.RemotePacVersion != null ? Downloads.PendingPacCount : PacRuleService.RemotePresets.Length,
            TabHistory => 0,
            _ => AvailableCores().Count()
        };
        _apply.Visibility = tab == TabHistory ? Visibility.Collapsed : Visibility.Visible;
        _apply.IsEnabled = count > 0 && !Downloads.Checking && !Downloads.Applying && Downloads.ActiveTransfer == null;
        _apply.Content = isRules ? TF("Sync selected ({0})", "同步所选 ({0})", count) : TF("Update all ({0})", "全部更新 ({0})", count);
    }

    private static IEnumerable<(CorePackage Core, UpdateChoice Choice)> AvailableCores() =>
        Downloads.Cores.Select(core => (Core: core, Choice: Downloads.AvailableCore(core)))
            .Where(item => item.Choice != null)
            .Select(item => (item.Core, item.Choice!));

    private FrameworkElement CoresPane()
    {
        var transfer = Downloads.ActiveTransfer;
        var canUpdate = transfer == null && !Downloads.Checking && !Downloads.Applying;
        var rows = new StackPanel
        {
            Children = { new Border
            {
                Height = 30, Padding = new Thickness(12, 0, 12, 0), Child = Caption(T("Cores", "核心"))
            } }
        };
        foreach (var core in Downloads.Cores)
        {
            var current = Downloads.Installed.GetValueOrDefault(core) ?? "";
            var versions = Downloads.Versions.GetValueOrDefault(core)?.Where(choice => !choice.Prerelease ||
                Global.Settings.CheckBetaUpdate && !string.Equals(choice.Version, current, StringComparison.OrdinalIgnoreCase)).ToList();
            var next = Downloads.LatestStable(core);
            var update = Downloads.AvailableCore(core);
            var inUse = Downloads.InUse(core);
            var options = new List<(string, Action)>();
            if (core.Repo.Length > 0 || versions?.Count > 0)
                options.Add((T("Choose version", "指定版本"), () => ChooseVersionAsync(core).Forget()));
            if (Downloads.HasRollback(core))
                options.Add((T("Rollback", "回滚"), () => RollbackCoreAsync(core).Forget()));
            options.Add((T("Open release", "打开 Release"), () => Utils.Utils.Open(next?.ReleaseUrl ??
                (core.Repo.Length == 0 ? "https://www.wintun.net/" : $"https://github.com/{core.Repo}/releases"))));
            var version = Caption(current.Length == 0 ? T("Not installed", "未安装") : current);
            version.FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas");
            var detail = Row(version);
            if (update != null)
            {
                if (current.Length > 0)
                {
                    var target = Caption(update.Version);
                    target.Style = Resource<Style>("NetchAccentText");
                    target.FontFamily = version.FontFamily;
                    var arrow = GlyphIcon(Icons.ChevronRight, 10, target.Foreground);
                    arrow.VerticalAlignment = VerticalAlignment.Center;
                    var delta = Row(arrow, target);
                    delta.Spacing = 4;
                    detail.Children.Add(delta);
                }
                var button = AsyncButton(current.Length == 0 ? T("Download", "下载") : T("Update", "更新"),
                    () => Downloads.InstallAsync(core, update));
                button.IsEnabled = canUpdate;
                detail.Children.Add(button);
            }
            else if (next != null && string.Equals(next.Version, current, StringComparison.OrdinalIgnoreCase)
                && !Downloads.Errors.ContainsKey(core.Name))
            {
                var latest = StatusRow(Icons.CheckMark, T("Latest", "最新"), "NetchSuccessText", Resource<Brush>("SystemFillColorSuccessBrush"), 11);
                latest.Width = 82;
                latest.HorizontalAlignment = HorizontalAlignment.Right;
                detail.Children.Add(latest);
            }
            var more = IconButton(Icons.More, T("More actions", "更多操作"),
                flyout: Menu(options.ToArray()), style: Resource<Style>("NetchInlineButton"));
            more.IsEnabled = canUpdate;
            detail.Children.Add(more);
            if (transfer?.Core == core)
            {
                detail = Row(ProgressView(), PauseButton(transfer));
            }
            var role = core.Name switch
            {
                "sing-box" => T("Extended protocols", "扩展协议"),
                "Xray-core" => "XHTTP",
                "Mihomo" => T("Default proxy", "默认代理"),
                "tun2socks" => "TUN",
                "WinTUN" => T("Driver", "驱动"),
                _ => T("Share mode", "共享模式")
            };
            var row = Split(Row(Text(core.Name, strong: true), Pill(role)), detail);
            var surface = new Border
            {
                Height = transfer?.Core == core ? 44 : 45, Padding = new Thickness(12, 0, 12, 0),
                CornerRadius = new CornerRadius(6), Child = row
            };
            if (inUse)
                ToolTipService.SetToolTip(row, T("In use", "使用中"));
            if (transfer?.Core == core)
            {
                surface.Style = Resource<Style>("NetchActiveRow");
                var indicator = RowIndicator(25);
                indicator.Visibility = Visibility.Visible;
                row.Children.Add(indicator);
            }
            rows.Children.Add(surface);
        }

        var running = Downloads.Cores.Where(Downloads.InUse).Select(core => core.Name).ToArray();
        var reconnectText = Text(TF("{0} in use by “{1}”. Updating will disconnect and reconnect the scene.",
            "{0} 正被场景「{1}」使用，更新时会先断开，完成后自动重连。", string.Join(" / ", running), Session.Current.ActiveScene?.Name ?? ""));
        reconnectText.TextWrapping = TextWrapping.Wrap;
        var symbol = GlyphIcon(Icons.Warning, 12, Resource<Brush>("TextOnAccentFillColorPrimaryBrush"));
        symbol.HorizontalAlignment = HorizontalAlignment.Center;
        symbol.VerticalAlignment = VerticalAlignment.Center;
        var icon = new Border
        {
            Width = 21, Height = 21, VerticalAlignment = VerticalAlignment.Center,
            CornerRadius = new CornerRadius(11), Style = Resource<Style>("NetchWarningIcon"),
            Child = symbol
        };
        var reconnect = new Border
        {
            Style = Resource<Style>("NetchWarningStrip"), Padding = new Thickness(13, 5, 13, 5),
            MinHeight = 40, Child = TwoColumns(icon, reconnectText, GridLength.Auto)
        };
        var currentVersion = "v" + UpdateChecker.Version;
        var release = Downloads.AppRelease is { } r && (!r.prerelease || Global.Settings.CheckBetaUpdate) ? r : null;
        var appAvailable = release != null && Downloads.AppAsset != null && Downloads.IsNewer(release.tag_name, UpdateChecker.Version);
        UIElement versionLine = appAvailable
            ? new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 5,
                Children = { Caption(currentVersion), GlyphIcon(Icons.ChevronRight, 10, Resource<Brush>("TextFillColorSecondaryBrush")), Caption("v" + release!.tag_name.TrimStart('v', 'V')) }
            }
            : Caption(currentVersion);
        var appMeta = Stack(Text("Netch", 15, true), versionLine);
        if (release != null)
        {
            var releaseNotes = new HyperlinkButton
            {
                Content = T("Release notes", "更新日志"), Padding = new Thickness(0), FontSize = 13,
                Height = 19, MinHeight = 0
            };
            releaseNotes.Click += (_, _) => Utils.Utils.Open(release.html_url);
            appMeta.Children.Add(releaseNotes);
        }
        appMeta.Spacing = 2;
        var appButtons = Row();
        if (transfer is { IsApplication: true })
        {
            appButtons.Children.Add(ProgressView());
            appButtons.Children.Add(PauseButton(transfer));
        }
        else if (appAvailable)
        {
            var button = AsyncButton(T("Update and restart", "更新并重启"), Downloads.UpdateApplicationAsync, true);
            button.IsEnabled = canUpdate;
            appButtons.Children.Add(button);
        }
        var app = Card(Split(Row(new Image { Width = 35, Height = 35, Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///Assets/Netch.png")) }, appMeta), appButtons));
        app.Padding = new Thickness(13, 7, 13, 7);
        var listCard = Card(Scroll(rows), 2);
        var layout = running.Length == 0 ? Body(app, listCard) : Body(reconnect, app, listCard);
        layout.RowSpacing = 9.5;
        return layout;
    }

    private FrameworkElement RulesPane()
    {
        var s = Global.Settings;
        var heading = Stack(Text(s.ModeRepository, 15, true), Caption($"{s.ModeBranch} · {s.ModeRepositoryPath}" +
                (Downloads.RemoteTree.Length >= 7 ? $" · {Downloads.RemoteTree[..7]}" : "")),
            Caption(TF("{0} local · {1} remote", "本地 {0} 个 · 远端 {1} 个", Global.Modes.Count, Downloads.RemoteRuleCount)));
        heading.Spacing = 1;
        var changeSource = AsyncButton(T("Change source", "更换来源"), ChangeSourceAsync);
        changeSource.IsEnabled = Downloads.ActiveTransfer == null && !Downloads.Applying && !Downloads.Checking;
        var card = Card(Split(heading, changeSource));
        card.Padding = new Thickness(13, 8, 13, 8);
        var list = new StackPanel();
        foreach (var group in Downloads.Rules.GroupBy(rule => rule.Kind).OrderBy(group => group.Key))
        {
            var title = group.Key switch
            {
                RuleChangeKind.Added => T("Added", "新增"),
                RuleChangeKind.Updated => T("Updated", "更新"),
                _ => T("Conflict", "冲突")
            };
            var entries = new List<CheckBox>();
            var groupName = $"{title} · {group.Count()}";
            var groupBox = RuleCheckBox(groupName);
            groupBox.IsThreeState = true;
            var updatingGroup = false;
            var headingText = Caption(groupName);
            headingText.Tapped += (_, _) => { if (groupBox.IsEnabled) ToggleGroup(); };
            list.Children.Add(new Border
            {
                Height = 31, Padding = new Thickness(12, 0, 12, 0),
                Child = TwoColumns(groupBox, headingText, GridLength.Auto, 6)
            });
            groupBox.Checked += (_, _) => ToggleGroup();
            groupBox.Unchecked += (_, _) => ToggleGroup();
            groupBox.Indeterminate += (_, _) => ToggleGroup();
            void ToggleGroup()
            {
                if (updatingGroup)
                    return;
                var eligible = entries.Where(entry => entry.IsEnabled).ToArray();
                var check = eligible.Any(entry => entry.IsChecked != true);
                updatingGroup = true;
                foreach (var entry in eligible)
                    entry.IsChecked = check;
                updatingGroup = false;
                UpdateGroup();
            }
            void UpdateGroup()
            {
                if (updatingGroup)
                    return;
                var eligible = entries.Count(entry => entry.IsEnabled);
                var selected = entries.Count(entry => entry.IsEnabled && entry.IsChecked == true);
                updatingGroup = true;
                groupBox.IsEnabled = eligible > 0;
                groupBox.IsChecked = selected == 0 ? false : selected == eligible ? true : null;
                updatingGroup = false;
            }
            foreach (var rule in group.OrderBy(rule => rule.Path, StringComparer.OrdinalIgnoreCase))
            {
                var checkedBox = RuleCheckBox(rule.Path);
                var previous = _selectedRules.GetValueOrDefault(rule.Path, (rule.Kind != RuleChangeKind.Conflict, 0));
                checkedBox.IsChecked = previous.Item1;
                entries.Add(checkedBox);
                ComboBox? action = null;
                var actions = Row();
                if (rule.Kind == RuleChangeKind.Conflict)
                {
                    checkedBox.IsEnabled = previous.Item2 != 0;
                    action = Choice(T("Resolve conflict", "冲突处理"),
                        [T("Keep local", "保留本地"), T("Use remote", "使用远端"), T("Save a copy", "另存为副本")], previous.Item2);
                    action.Width = 98;
                    action.Padding = new Thickness(8, 0, 2, 0);
                    action.MinWidth = 0;
                    action.SelectionChanged += (_, _) =>
                    {
                        checkedBox.IsEnabled = action.SelectedIndex > 0;
                        checkedBox.IsChecked = checkedBox.IsEnabled;
                        SaveSelection();
                    };
                    actions.Children.Add(action);
                }

                var preview = Caption("");
                preview.TextAlignment = TextAlignment.Right;
                var removedPreview = Caption("");
                var previewColumn = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 5, Width = 106,
                    HorizontalAlignment = HorizontalAlignment.Right };
                var previewEnd = new Grid { Width = 106 };
                previewEnd.Children.Add(previewColumn);
                if (rule.Kind != RuleChangeKind.Conflict)
                    actions.Children.Add(previewEnd);
                async Task ShowDiffAsync()
                {
                    var (added, removed) = await Downloads.DiffAsync(rule);
                    if (rule.Kind != RuleChangeKind.Conflict)
                    {
                        _rulePreviews[rule] = Task.FromResult((added, removed));
                        SetPreview(added.Length, removed.Length);
                    }
                    var diff = Input($"+{added.Length}  −{removed.Length}", string.Join(Environment.NewLine,
                        removed.Select(line => "− " + line).Concat(added.Select(line => "+ " + line))), multiline: true);
                    diff.IsReadOnly = true;
                    await ShowAsync(XamlRoot, rule.Path, diff);
                }

                if (rule.Kind == RuleChangeKind.Updated)
                {
                    preview.FontFamily = removedPreview.FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas");
                    removedPreview.Style = Resource<Style>("NetchErrorText");
                    var delta = Row(preview, removedPreview);
                    delta.Spacing = 4;
                    delta.HorizontalAlignment = HorizontalAlignment.Right;
                    previewColumn.Children.Add(new Grid { Width = 72, Children = { delta } });
                    var link = AsyncButton(T("Diff", "差异"), ShowDiffAsync);
                    link.Style = Resource<Style>("NetchInlineButton");
                    previewColumn.Children.Add(link);
                }
                else if (rule.Kind == RuleChangeKind.Added)
                {
                    preview.Width = 106;
                    previewColumn.Children.Add(preview);
                }
                var titleText = Text(rule.Path);
                titleText.Tapped += (_, _) => { if (checkedBox.IsEnabled) checkedBox.IsChecked = checkedBox.IsChecked != true; };
                var titlePanel = rule.Kind == RuleChangeKind.Conflict
                    ? new StackPanel { Children = { titleText, Caption(T("Both local and remote changed", "本地已修改，远端也有更新")) } }
                    : (UIElement)titleText;
                ((FrameworkElement)titlePanel).VerticalAlignment = VerticalAlignment.Center;
                actions.VerticalAlignment = VerticalAlignment.Center;
                var nameAndCheck = TwoColumns(checkedBox, titlePanel, GridLength.Auto, 6);
                var row = Split(nameAndCheck, actions);
                var surface = new Border
                {
                    Child = row, Height = rule.Kind == RuleChangeKind.Conflict ? 51 : 33,
                    Padding = new Thickness(12, 0, 12, 0), CornerRadius = new CornerRadius(6)
                };
                if (action != null)
                {
                    var indicator = RowIndicator(31);
                    row.Children.Add(indicator);
                    action.GotFocus += (_, _) =>
                    {
                        surface.Style = Resource<Style>("NetchActiveRow");
                        indicator.Visibility = Visibility.Visible;
                    };
                    action.LostFocus += (_, _) =>
                    {
                        surface.ClearValue(FrameworkElement.StyleProperty);
                        indicator.Visibility = Visibility.Collapsed;
                    };
                    surface.ContextFlyout = Menu((T("Diff", "差异"), () => ShowRuleDiffAsync().Forget()));
                    async Task ShowRuleDiffAsync()
                    {
                        try { await ShowDiffAsync(); }
                        catch (Exception ex) { await ShowAsync(XamlRoot, rule.Path, Text(ex.Message)); }
                    }
                }
                else
                    row.EffectiveViewportChanged += LoadPreview;
                list.Children.Add(surface);
                async void LoadPreview(FrameworkElement sender, EffectiveViewportChangedEventArgs args)
                {
                    if (sender.ActualHeight <= 0 || args.BringIntoViewDistanceY >= sender.ActualHeight)
                        return;
                    sender.EffectiveViewportChanged -= LoadPreview;
                    try
                    {
                        if (!_rulePreviews.TryGetValue(rule, out var pending))
                            _rulePreviews[rule] = pending = Downloads.DiffAsync(rule);
                        var (added, removed) = await pending;
                        SetPreview(added.Length, removed.Length);
                    }
                    catch (Exception e)
                    {
                        _rulePreviews.Remove(rule);
                        Log.Debug(e, "Read rule preview {Rule}", rule.Path);
                    }
                }
                void SetPreview(int added, int removed)
                {
                    if (rule.Kind == RuleChangeKind.Added)
                    {
                        preview.Text = TF("{0} rules", "{0} 条规则", added);
                        return;
                    }

                    preview.Style = Resource<Style>(added > 0 ? "NetchSuccessText" : "NetchSecondaryText");
                    preview.Text = added > 0 ? $"+{added}" : removed == 0 ? T("Rules unchanged", "规则未变") : "";
                    preview.Visibility = preview.Text.Length == 0 ? Visibility.Collapsed : Visibility.Visible;
                    removedPreview.Text = removed > 0 ? $"−{removed}" : "";
                    removedPreview.Visibility = removed > 0 ? Visibility.Visible : Visibility.Collapsed;
                }
                _selection.Add((rule, checkedBox, action));
                checkedBox.Checked += (_, _) => SaveSelection();
                checkedBox.Unchecked += (_, _) => SaveSelection();

                void SaveSelection()
                {
                    _selectedRules[rule.Path] = (checkedBox.IsChecked == true, action?.SelectedIndex ?? 0);
                    UpdateGroup();
                    ChangeApply();
                }
            }
            UpdateGroup();
        }
        if (list.Children.Count == 0)
            list.Children.Add(Caption(Downloads.LastChecked == null ? T("Check for updates to load online rules.", "检查更新以读取在线规则。")
                : T("No rule updates.", "没有可同步的规则。")));
        return Body(card, Card(Scroll(list), 2));
    }

    private FrameworkElement PacPane()
    {
        var transfer = Downloads.ActiveTransfer;
        var canUpdate = transfer == null && !Downloads.Checking && !Downloads.Applying;
        var branchInfo = $"{PacRuleService.Branch}" +
            (string.IsNullOrWhiteSpace(Downloads.RemotePacVersion) ? "" : $" · {Downloads.RemotePacVersion}");
        var heading = Stack(
            Text(PacRuleService.Repository, 15, true),
            Caption(branchInfo),
            Caption(T("Proxy list, GFWList, and China direct domain lists for WebProxy PAC.",
                "网页代理 PAC 使用的海外代理、精简 GFW 与中国大陆直连域名表。")));
        heading.Spacing = 1;
        var openRepo = Button(T("Open repository", "打开仓库"), () => Utils.Utils.Open(Downloads.RemotePacReleaseUrl));
        var sourceCard = Card(Split(heading, openRepo));
        sourceCard.Padding = new Thickness(13, 8, 13, 8);

        var rows = new StackPanel
        {
            Children =
            {
                new Border
                {
                    Height = 30, Padding = new Thickness(12, 0, 12, 0),
                    Child = Caption(T("PAC rule lists", "PAC 规则表"))
                }
            }
        };

        var font = new Microsoft.UI.Xaml.Media.FontFamily("Consolas");
        foreach (var preset in PacRuleService.RemotePresets)
        {
            var file = PacRuleService.GetFileName(preset) ?? "";
            var status = PacRuleService.GetRuleStatus(preset);
            var installedVer = Downloads.InstalledPacVersion(preset, status);
            var hasUpdate = PacRuleService.IsUpdateAvailable(status, installedVer, Downloads.RemotePacVersion);

            var presetName = preset switch
            {
                PacRulePreset.ProxyList => T("Proxy list (Loyalsoldier · overseas)", "海外代理列表（Loyalsoldier · proxy-list）"),
                PacRulePreset.GfwList => T("GFWList (Loyalsoldier · lightweight)", "精简 GFWList（Loyalsoldier · gfw）"),
                _ => T("Bypass China (Loyalsoldier · direct-list)", "绕过中国大陆（Loyalsoldier · direct-list）")
            };
            var statusSummary = status.Exists
                ? TF("{0:N0} rules · {1}", "{0:N0} 条规则 · {1}",
                    status.RuleCount,
                    status.IsUserUpdated && status.LastWriteTime is { } dt
                        ? Format.LastUsed(dt)
                        : T("Bundled", "随包内置"))
                : T("Not downloaded yet", "尚未下载");
            var subtitle = string.IsNullOrWhiteSpace(installedVer)
                ? $"{file} · {statusSummary}"
                : $"{file} · {installedVer} · {statusSummary}";

            var info = Stack(Text(presetName, strong: true), Caption(subtitle));
            info.Spacing = 1;
            info.VerticalAlignment = VerticalAlignment.Center;

            StackPanel detail;
            if (transfer?.PacPreset == preset)
            {
                detail = Row(ProgressView(), PauseButton(transfer));
            }
            else
            {
                detail = Row();
                if (!status.Exists || hasUpdate || Downloads.RemotePacVersion == null)
                {
                    if (status.Exists && Downloads.RemotePacVersion is { } remoteVer)
                    {
                        var target = Caption(remoteVer);
                        target.Style = Resource<Style>("NetchAccentText");
                        target.FontFamily = font;
                        var arrow = GlyphIcon(Icons.ChevronRight, 10, target.Foreground);
                        arrow.VerticalAlignment = VerticalAlignment.Center;
                        var delta = Row(arrow, target);
                        delta.Spacing = 4;
                        detail.Children.Add(delta);
                    }

                    var button = AsyncButton(status.Exists ? T("Update", "更新") : T("Download", "下载"),
                        () => DownloadPacAsync(preset));
                    button.IsEnabled = canUpdate;
                    detail.Children.Add(button);
                }
                else
                {
                    var latest = StatusRow(Icons.CheckMark, T("Latest", "最新"),
                        "NetchSuccessText", Resource<Brush>("SystemFillColorSuccessBrush"), 11);
                    latest.Width = 82;
                    latest.HorizontalAlignment = HorizontalAlignment.Right;
                    detail.Children.Add(latest);
                }

                var more = IconButton(Icons.More, T("More actions", "更多操作"),
                    flyout: Menu(
                        (T("Redownload", "重新下载"), () => DownloadPacAsync(preset).Forget()),
                        (T("Open release", "打开 Release"), () => Utils.Utils.Open(Downloads.RemotePacReleaseUrl))),
                    style: Resource<Style>("NetchInlineButton"));
                more.IsEnabled = canUpdate;
                detail.Children.Add(more);
            }
            detail.VerticalAlignment = VerticalAlignment.Center;

            var row = Split(info, detail);
            var surface = new Border
            {
                Height = 52, Padding = new Thickness(12, 0, 12, 0),
                CornerRadius = new CornerRadius(6), Child = row
            };
            if (transfer?.PacPreset == preset)
            {
                surface.Style = Resource<Style>("NetchActiveRow");
                var indicator = RowIndicator(28);
                indicator.Visibility = Visibility.Visible;
                row.Children.Add(indicator);
            }
            rows.Children.Add(surface);
        }

        return Body(sourceCard, Card(Scroll(rows), 2));
    }

    private async Task DownloadPacAsync(PacRulePreset preset)
    {
        try { await Downloads.UpdatePacRuleAsync(preset); }
        catch (Exception e)
        {
            Log.Warning(e, "Download PAC rule {Preset} failed", preset);
            if (_present && XamlRoot is { } root)
                await ShowAsync(root, PacRuleService.GetFileName(preset) ?? "PAC", Text(e.Message));
        }
    }

    private static CheckBox RuleCheckBox(string label)
    {
        var check = new CheckBox { MinWidth = 0, Width = 20, VerticalAlignment = VerticalAlignment.Center,
            RenderTransformOrigin = new Windows.Foundation.Point(0, 0.5),
            RenderTransform = new Microsoft.UI.Xaml.Media.ScaleTransform { ScaleX = 0.85, ScaleY = 0.85 } };
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(check, label);
        return check;
    }

    private FrameworkElement HistoryPane()
    {
        var rows = new ListView { Padding = new Thickness(0, 0, Ui.ScrollBarGutter, 0) };
        foreach (var item in Downloads.History)
            rows.Items.Add(new ListViewItem
            {
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                Content = Split(Stack(Text($"{item.Name} · {item.Version}"), Caption(item.Message ?? "")),
                    Caption($"{item.Time:g} · {(item.Success ? T("Completed", "完成") : T("Incomplete", "未完成"))}"))
            });
        if (rows.Items.Count == 0)
            rows.Items.Add(Caption(T("No downloads yet.", "暂无下载记录。")));
        return Card(rows, 4);
    }

    private async Task ChangeSourceAsync()
    {
        var s = Global.Settings;
        var repo = Input(T("GitHub repository (owner/repo)", "GitHub 仓库（所有者/仓库）"), s.ModeRepository);
        var branch = Input(T("Branch", "分支"), s.ModeBranch);
        var folder = Input(T("Directory", "目录"), s.ModeRepositoryPath);
        if (await ShowAsync(XamlRoot, T("Mode rule source", "模式规则来源"), Stack(repo, branch, folder), T("Save", "保存"), () =>
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(repo.Text.Trim(), @"^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$") ||
                branch.Text.Trim().Length == 0 || folder.Text.Trim(' ', '/').Length == 0)
                return T("Enter a valid repository, branch, and directory.", "请输入有效的仓库、分支和目录。");
            s.ModeRepository = repo.Text.Trim();
            s.ModeBranch = branch.Text.Trim();
            s.ModeRepositoryPath = folder.Text.Trim(' ', '/');
            return null;
        }))
        {
            _selectedRules.Clear();
            _rulePreviews.Clear();
            Downloads.SourceChanged();
            await Configuration.SaveAsync();
            await Downloads.CheckAsync();
        }
    }

    private async Task ChooseVersionAsync(CorePackage core)
    {
        List<UpdateChoice> loaded;
        try
        {
            loaded = await Downloads.LoadCoreHistoryAsync(core);
        }
        catch (Exception e)
        {
            if (_present && XamlRoot is { } root)
                await ShowAsync(root, core.Name, Text(e.Message));
            return;
        }

        var current = Downloads.Installed.GetValueOrDefault(core) ?? "";
        var choices = loaded.Where(choice => !choice.Prerelease ||
            Global.Settings.CheckBetaUpdate && !string.Equals(choice.Version, current, StringComparison.OrdinalIgnoreCase)).ToList();
        if (choices.Count == 0)
        {
            if (_present && XamlRoot is { } root)
                await ShowAsync(root, core.Name, Text(T("No versions available.", "暂无可用版本。")));
            return;
        }

        var version = Choice(T("Version", "版本"), choices.Select(choice => choice.Version));
        if (_present && XamlRoot is { } dialogRoot &&
            await ShowAsync(dialogRoot, core.Name, version, T("Download", "下载")) &&
            version.SelectedIndex >= 0)
            await Downloads.InstallAsync(core, choices[version.SelectedIndex]);
    }

    private async Task RollbackCoreAsync(CorePackage core)
    {
        try { await Downloads.RollbackAsync(core); }
        catch (Exception e) { await ShowAsync(XamlRoot, T("Rollback failed", "回滚失败"), Text(e.Message)); }
    }

    private async Task ApplyAsync()
    {
        if (_tabs.SelectedIndex == TabRules)
        {
            var selected = _selection.Where(entry => entry.Check.IsEnabled && entry.Check.IsChecked == true)
                .Select(entry => (entry.Rule, Action: entry.Conflict == null ? ConflictAction.Replace : (ConflictAction)entry.Conflict.SelectedIndex)).ToArray();
            await Downloads.SyncAsync(selected, path => _selectedRules.Remove(path));
            return;
        }

        if (_tabs.SelectedIndex == TabPac)
        {
            var targets = PacRuleService.RemotePresets
                .Where(p => Downloads.RemotePacVersion == null || Downloads.IsPacUpdateAvailable(p))
                .ToArray();
            var updated = new List<PacRulePreset>();
            try
            {
                foreach (var preset in targets)
                {
                    if (!await Downloads.UpdatePacRuleAsync(preset, reloadActiveScene: false))
                        break;
                    updated.Add(preset);
                }
            }
            finally
            {
                Downloads.ReloadActivePacSceneIfNeeded(updated);
            }
            return;
        }

        foreach (var (core, choice) in AvailableCores().ToArray())
            if (!await Downloads.InstallAsync(core, choice))
                break;
    }

    private StackPanel ProgressView()
    {
        _progress = new ProgressBar { Minimum = 0, Maximum = 100, Width = 165, Height = 5 };
        _progressLabel = Caption("");
        _progressLabel.FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas");
        UpdateProgress();
        var panel = Stack(_progress, _progressLabel);
        panel.Spacing = 3;
        panel.VerticalAlignment = VerticalAlignment.Center;
        return panel;
    }

    private void UpdateProgress()
    {
        if (_progress == null || _progressLabel == null || Downloads.ActiveTransfer is not { } transfer)
            return;
        var progress = transfer.Progress;
        _progress.IsIndeterminate = progress.Percent < 0;
        if (progress.Percent >= 0)
            _progress.Value = progress.Percent;
        var percent = progress.Percent >= 0 ? $"{progress.Percent:0}%" : "";
        var speed = progress.BytesPerSecond > 0 && !transfer.IsPaused ? Format.Rate(progress.BytesPerSecond) : "";
        _progressLabel.Text = string.Join(" · ", new[] { percent, speed }.Where(text => text.Length > 0));
    }

    private Button PauseButton(DownloadTransfer transfer)
    {
        var icon = new FontIcon { FontSize = 14 };
        var button = new Button { Content = icon, Style = Resource<Style>("NetchInlineButton"), IsEnabled = transfer.CanPause };
        UpdateButton();
        button.Click += (_, _) =>
        {
            transfer.TogglePause();
            UpdateButton();
            UpdateProgress();
        };
        return button;

        void UpdateButton()
        {
            var label = transfer.IsPaused ? T("Resume", "继续") : T("Pause", "暂停");
            icon.Glyph = transfer.IsPaused ? Icons.Play : Icons.Pause;
            AutomationProperties.SetName(button, label);
            ToolTipService.SetToolTip(button, label);
        }
    }

}
