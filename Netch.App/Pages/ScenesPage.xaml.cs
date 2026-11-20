using System.Collections.ObjectModel;
using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Netch.App.Dialogs;
using Netch.App.Helpers;
using Netch.App.Services;
using Netch.App.ViewModels;
using Netch.Enums;
using Netch.Models;
using Netch.Services;
using Netch.Utils;
using Windows.Foundation;

namespace Netch.App.Pages;

public sealed partial class ScenesPage : Page, INotifyPropertyChanged
{
    public ScenesPage()
    {
        InitializeComponent();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public Session Session => Session.Current;

    public Tray Tray => Tray.Current;

    public ObservableCollection<SceneCard> Cards { get; } = new();

    public bool HasScenes => SceneService.Scenes.Count > 0;

    public bool IsEmpty => !HasScenes;

    public bool HasNodes => Global.Settings.Server.Count > 0;

    public bool NeedsNodes => !HasNodes;

    private void ScenesPage_Loaded(object sender, RoutedEventArgs e)
    {
        if (App.MainWindow is { } window)
            window.Activated += MainWindow_Activated;
        UpdateEmptyActions();
    }

    private void ScenesPage_Unloaded(object sender, RoutedEventArgs e)
    {
        if (App.MainWindow is { } window)
            window.Activated -= MainWindow_Activated;
    }

    private void MainWindow_Activated(object sender, WindowActivatedEventArgs e)
    {
        if (e.WindowActivationState != WindowActivationState.Deactivated)
            UpdateEmptyActions();
    }

    private void UpdateEmptyActions()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasNodes)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NeedsNodes)));
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        LoadCards();
        DrawSparkline();
        Session.PropertyChanged += Session_PropertyChanged;
        Tray.PropertyChanged += Tray_PropertyChanged;
        AppEvents.SubscriptionChanged += RefreshSubscriptionCards;
        UpdateAlerts();
        Downloads.Changed += UpdateRulesNotice;
        UpdateRulesNotice();
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        Session.PropertyChanged -= Session_PropertyChanged;
        Tray.PropertyChanged -= Tray_PropertyChanged;
        AppEvents.SubscriptionChanged -= RefreshSubscriptionCards;
        Downloads.Changed -= UpdateRulesNotice;
    }

    /// <summary>Rebuilds the wall after scenes were added, removed or edited.</summary>
    private void LoadCards()
    {
        Tray.RegisterHotkeys();
        Cards.Clear();
        var scenes = SceneService.Scenes.AsEnumerable();
        if (SortByRecent.IsChecked == true)
            scenes = scenes.OrderByDescending(scene => scene.LastUsed);
        foreach (var scene in scenes)
            Cards.Add(new SceneCard(scene));

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasScenes)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsEmpty)));
    }

    private void RefreshSubscriptionCards() => DispatcherQueue.TryEnqueue(() =>
    {
        RefreshCards();
        UpdateEmptyActions();
    });

    private void RefreshCards()
    {
        foreach (var card in Cards)
            card.Refresh();
    }

    private void Session_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(Session.State):
            case nameof(Session.ActiveScene):
                RefreshCards();
                break;
            case nameof(Session.ErrorMessage):
                UpdateAlerts();
                break;
            case nameof(Session.RateHistory):
                DrawSparkline();
                break;
        }
    }

    private void DrawSparkline()
    {
        var history = Session.RateHistory.ToArray();
        var max = Math.Max(history.Max(), 1);
        var points = new PointCollection();
        for (var i = 0; i < history.Length; i++)
            points.Add(new Point(i * Sparkline.Width / (history.Length - 1), Sparkline.Height - 2 - history[i] / max * (Sparkline.Height - 4)));

        Sparkline.Points = points;
    }

    #region Status card

    public Style PowerButtonStyle(State state) =>
        (Style)Application.Current.Resources[state is State.Started or State.Starting ? "AccentButtonStyle" : "DefaultButtonStyle"];

    public bool CanUsePower(bool busy, bool hasScenes) => !busy && hasScenes;

    public string PowerLabel(State state) => i18N.Translate(state == State.Started ? "Stop" : "Start");

    public string Headline(State state, Scene? scene) =>
        state is State.Waiting or State.Stopped || scene == null ? i18N.Translate("Not connected") : scene.Name;

    public string Subline(State state, Scene? scene, TimeSpan elapsed, string? progress)
    {
        if (scene == null || state is State.Waiting or State.Stopped)
            return i18N.Translate("Click a scene to connect");

        if (state != State.Started)
            return progress ?? i18N.Translate(state.ToString());

        var mode = SceneService.FindMode(scene);
        var server = SceneService.FindServer(scene);
        return string.Join(" · ",
            mode == null ? "—" : Format.ModeTypeLongName(mode.Type),
            server == null ? "—" : Format.NodeName(server),
            i18N.TranslateFormat("Running for {0}", Format.Elapsed(elapsed)));
    }

    public string LatencyText(int? latency, bool testing) =>
        testing ? "…" : latency is { } ms ? $"{ms} ms" : i18N.Translate("Timeout");

    public string NatText(string? result, bool testing) => testing ? "…" : Format.Nat(result);

    public string NatTip(string? result) =>
        Format.NatFailed(result) ? $"{result}\n{i18N.Translate("Test again")}" : i18N.Translate("Test again");

    public string TotalText(ulong totalBytes) => $"↓ {Format.Bytes(totalBytes)}";

    public bool HasText(string? text) => !string.IsNullOrEmpty(text);

    private void Tray_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Tray.HotkeyConflicts))
            UpdateAlerts();
    }

    private void UpdateAlerts() =>
        Alerts.Visibility = HasText(Session.ErrorMessage) || HasText(Tray.HotkeyConflicts) ? Visibility.Visible : Visibility.Collapsed;

    public string HotkeyConflictText(string conflicts) => i18N.TranslateFormat("These hotkeys are already in use and do not work: {0}", conflicts);

    private async void PowerButton_Click(object sender, RoutedEventArgs e)
    {
        if (Session.IsRunning)
            await Session.StopAsync();
        else if (Session.CurrentScene is { } scene)
            await Session.StartAsync(scene);
    }

    private async void Latency_Click(object sender, RoutedEventArgs e) => await Session.RetestLatencyAsync();

    private async void Nat_Click(object sender, RoutedEventArgs e) => await Session.RetestNatAsync();

    private void OpenLogs_Click(object sender, RoutedEventArgs e) => App.MainWindow?.Navigate("Logs", true);

    private void DownloadRules_Click(object sender, RoutedEventArgs e) => App.MainWindow?.Navigate("Downloads", true);

    private void UpdateRulesNotice()
    {
        RulesNotice.Visibility = Downloads.Rules.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

        var added = Downloads.Rules.Count(rule => rule.Kind == RuleChangeKind.Added);
        var updated = Downloads.Rules.Count(rule => rule.Kind == RuleChangeKind.Updated);
        var conflicts = Downloads.Rules.Count(rule => rule.Kind == RuleChangeKind.Conflict);
        var counts = Ui.T($"{added} new, {updated} updated", $"{added} 个新增、{updated} 个更新");
        if (conflicts > 0)
            counts += Ui.T($", {conflicts} conflicts", $"、{conflicts} 个冲突");

        var months = Downloads.LastRulesSynchronized is { } last ? (int)((DateTimeOffset.Now - last).TotalDays / 30) : 0;
        var label = string.Equals(Global.Settings.ModeRepository, "netchx/netch", StringComparison.OrdinalIgnoreCase)
            && Global.Settings.ModeBranch == "main" && Global.Settings.ModeRepositoryPath == "Storage/mode"
                ? Ui.T("Bundled mode rules", "内置模式规则")
                : Ui.T("Mode rules", "模式规则");
        RulesNoticeText.Text = months > 0
            ? Ui.T($"{label} were last synced {months} months ago. Remote: {counts}.",
                $"{label}已有 {months} 个月未更新，远端有 {counts}。")
            : Ui.T($"Online mode rules: {counts}.", $"在线规则库有 {counts}。");
    }

    private void OpenWizard_Click(object sender, RoutedEventArgs e) => App.OpenWizard();

    private void ErrorBar_Closed(InfoBar sender, InfoBarClosedEventArgs args) => Session.ErrorMessage = null;

    #endregion

    #region Scene wall

    private async void SceneWall_ItemInvoked(ItemsView sender, ItemsViewItemInvokedEventArgs args)
    {
        if (args.InvokedItem is SceneCard card)
            await Session.ToggleAsync(card.Scene);
    }

    private void SortByRecent_Click(object sender, RoutedEventArgs e) => LoadCards();

    private async void NewScene_Click(object sender, RoutedEventArgs e) => await EditSceneAsync(null);

    private void ImportSubscription_Click(object sender, RoutedEventArgs e) => App.MainWindow?.Navigate("Nodes", true);

    private async void Edit_Click(object sender, RoutedEventArgs e) => await EditSceneAsync(CardOf(sender).Scene);

    private async void ChangeNode_Click(object sender, RoutedEventArgs e) => await EditSceneAsync(CardOf(sender).Scene, true);

    private async void Duplicate_Click(object sender, RoutedEventArgs e)
    {
        var scene = CardOf(sender).Scene;
        var scenes = SceneService.Scenes;
        scenes.Insert(scenes.IndexOf(scene) + 1, scene.Duplicate(i18N.TranslateFormat("{0} (copy)", scene.Name)));
        LoadCards();
        await Configuration.SaveAsync();
    }

    private async void Pin_Click(object sender, RoutedEventArgs e)
    {
        var card = CardOf(sender);
        card.Scene.PinnedToTray = !card.Scene.PinnedToTray;
        card.Refresh();
        await Configuration.SaveAsync();
    }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        var scene = CardOf(sender).Scene;
        var confirm = new ContentDialog
        {
            Title = i18N.TranslateFormat("Delete scene \"{0}\"?", scene.Name),
            PrimaryButtonText = i18N.Translate("Delete"),
            CloseButtonText = i18N.Translate("Cancel"),
            DefaultButton = ContentDialogButton.Close
        };
        if (await ShowDialogAsync(confirm) != ContentDialogResult.Primary)
            return;

        if (Session.ActiveScene == scene && Session.IsRunning)
            await Session.StopAsync();

        SceneService.Scenes.Remove(scene);
        LoadCards();
        await Configuration.SaveAsync();
    }

    private async Task EditSceneAsync(Scene? scene, bool changeNode = false)
    {
        var oldMode = scene?.ModeId;
        var oldServer = scene?.ServerId;

        var dialog = new SceneEditorDialog(scene, changeNode);
        dialog.PrimaryButtonClick += async (_, args) =>
        {
            var deferral = args.GetDeferral();
            try
            {
                var apply = dialog.ViewModel.CaptureEdits();
                var draft = scene == null ? new Scene() : new Scene { Id = scene.Id, LastUsed = scene.LastUsed };
                apply(draft);
                var settings = Global.Settings.ShallowCopy();
                settings.Scenes = SceneService.Scenes.Select(item => item == scene ? draft : item).ToList();
                if (scene == null)
                    settings.Scenes.Add(draft);

                await Configuration.SaveAsync(settings, throwOnError: true);
                if (scene == null)
                    SceneService.Scenes.Add(draft);
                else
                    apply(scene);
            }
            catch
            {
                args.Cancel = true;
                dialog.ShowSaveError(Ui.T("Could not save the scene. Check the configuration file and try again.",
                    "无法保存场景，请检查配置文件后重试。"));
            }
            finally
            {
                deferral.Complete();
            }
        };
        Tray.UnregisterHotkeys();
        ContentDialogResult result;
        try
        {
            SceneSurface.Opacity = 0;
            result = await ShowDialogAsync(dialog);
        }
        finally
        {
            SceneSurface.Opacity = 1;
            Tray.RegisterHotkeys();
        }

        if (dialog.ModeToOpen != null)
        {
            App.MainWindow?.Navigate("Modes", dialog.ModeToOpen);
            return;
        }

        if (result != ContentDialogResult.Primary)
            return;

        LoadCards();

        // A changed node or mode only takes effect on a restart; do it for the running scene.
        if (scene != null && Session.IsRunning && Session.ActiveScene == scene && (scene.ModeId != oldMode || scene.ServerId != oldServer))
            await Session.StartAsync(scene);
    }

    private Task<ContentDialogResult> ShowDialogAsync(ContentDialog dialog)
    {
        dialog.XamlRoot = XamlRoot;
        // Dialogs live in the popup layer and do not inherit the theme set on the window content.
        dialog.RequestedTheme = ((FrameworkElement)XamlRoot.Content).RequestedTheme;
        dialog.Style ??= (Style)Application.Current.Resources["DefaultContentDialogStyle"];
        return dialog.ShowAsync().AsTask();
    }

    private static SceneCard CardOf(object sender) => (SceneCard)((FrameworkElement)sender).Tag;

    #endregion
}
