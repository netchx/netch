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

    /// <summary>Scene cards followed by the "new scene" tile.</summary>
    public ObservableCollection<object> Cards { get; } = new();

    public bool HasScenes => SceneService.Scenes.Count > 0;

    public bool IsEmpty => !HasScenes;

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        LoadCards();
        DrawSparkline();
        Session.PropertyChanged += Session_PropertyChanged;
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        Session.PropertyChanged -= Session_PropertyChanged;
    }

    /// <summary>Rebuilds the wall after scenes were added, removed or edited.</summary>
    private void LoadCards()
    {
        Tray.RegisterHotkeys();
        Cards.Clear();
        foreach (var scene in SceneService.Scenes)
            Cards.Add(new SceneCard(scene));
        Cards.Add(AddSceneCard.Instance);

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasScenes)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsEmpty)));
    }

    private void RefreshCards()
    {
        foreach (var card in Cards.OfType<SceneCard>())
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

    public bool CanUsePower(State state, bool hasScenes) => state is not (State.Starting or State.Stopping) && hasScenes;

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

    private void ErrorBar_Closed(InfoBar sender, InfoBarClosedEventArgs args) => Session.ErrorMessage = null;

    #endregion

    #region Scene wall

    private async void SceneWall_ItemInvoked(ItemsView sender, ItemsViewItemInvokedEventArgs args)
    {
        switch (args.InvokedItem)
        {
            case SceneCard card:
                await Session.ToggleAsync(card.Scene);
                break;
            case AddSceneCard:
                await EditSceneAsync(null);
                break;
        }
    }

    private async void NewScene_Click(object sender, RoutedEventArgs e) => await EditSceneAsync(null);

    private void ImportSubscription_Click(object sender, RoutedEventArgs e) => App.MainWindow?.Navigate("Nodes");

    private async void Edit_Click(object sender, RoutedEventArgs e) => await EditSceneAsync(CardOf(sender).Scene);

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

    private async Task EditSceneAsync(Scene? scene)
    {
        var oldMode = scene?.ModeId;
        var oldServer = scene?.ServerId;

        var dialog = new SceneEditorDialog(scene);
        Tray.UnregisterHotkeys();
        ContentDialogResult result;
        try
        {
            result = await ShowDialogAsync(dialog);
        }
        finally
        {
            Tray.RegisterHotkeys();
        }

        if (dialog.ModeToOpen != null)
        {
            App.MainWindow?.Navigate("Modes");
            return;
        }

        if (result != ContentDialogResult.Primary)
            return;

        var edited = dialog.ViewModel.Apply();
        if (scene == null)
            SceneService.Scenes.Add(edited);

        LoadCards();
        await Configuration.SaveAsync();

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
