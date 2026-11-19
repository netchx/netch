using System.Collections.ObjectModel;
using System.ComponentModel;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Netch.App.Helpers;
using Netch.App.Services;
using Netch.App.ViewModels;
using Netch.Enums;
using Netch.Models;
using Netch.Services;
using Netch.Utils;
using Windows.Graphics;
using Windows.Win32;
using Windows.Win32.Foundation;
using Point = System.Drawing.Point;

namespace Netch.App;

/// <summary>Borderless window opened by a left click on the tray icon; hides itself when it loses focus.</summary>
public sealed partial class TrayFlyout : Window, INotifyPropertyChanged
{
    private const int FlyoutWidth = 310;
    private const int ScreenMargin = 12;

    private readonly HWND _hwnd;
    private long _hiddenAt;

    public TrayFlyout()
    {
        InitializeComponent();
        _hwnd = new HWND(Microsoft.UI.Win32Interop.GetWindowFromWindowId(AppWindow.Id));

        var presenter = (OverlappedPresenter)AppWindow.Presenter;
        presenter.SetBorderAndTitleBar(false, false);
        presenter.IsResizable = false;
        presenter.IsMaximizable = false;
        presenter.IsMinimizable = false;
        presenter.IsAlwaysOnTop = true;
        AppWindow.IsShownInSwitchers = false;

        Activated += (_, e) =>
        {
            if (e.WindowActivationState != WindowActivationState.Deactivated)
                return;

            AppWindow.Hide();
            _hiddenAt = Environment.TickCount64;
        };
        Session.PropertyChanged += Session_PropertyChanged;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public Session Session => Session.Current;

    /// <summary>Scenes pinned to the tray.</summary>
    public ObservableCollection<SceneCard> Tiles { get; } = new();

    public bool HasTiles => Tiles.Count > 0;

    public bool NoTiles => !HasTiles;

    public double TileHeight => Math.Max(0, Math.Min(4, (Tiles.Count + 1) / 2) * 60 - 6);

    public void ApplyTheme(ElementTheme theme) => Root.RequestedTheme = theme;

    /// <summary>Shows the flyout next to the tray icon at <paramref name="anchor" />, or hides it.</summary>
    public void Toggle(Point anchor)
    {
        if (AppWindow.IsVisible)
        {
            AppWindow.Hide();
            return;
        }

        // The click on the tray icon that took focus away from the open flyout arrives right after it hid.
        if (Environment.TickCount64 - _hiddenAt < 300)
            return;

        LoadTiles();

        var scale = PInvoke.GetDpiForWindow(_hwnd) / 96.0;
        var border = WindowSize.InvisibleBorder(this);
        var height = (HasTiles ? TileHeight : 37) + (Session.IsRunning ? 132 : 101);
        AppWindow.Resize(new SizeInt32((int)Math.Ceiling(FlyoutWidth * scale) + border.Width,
            (int)Math.Ceiling(height * scale) + border.Height));

        // The outer size includes the window frame, which is what has to fit on screen.
        var size = AppWindow.Size;
        var area = DisplayArea.GetFromPoint(new PointInt32(anchor.X, anchor.Y), DisplayAreaFallback.Nearest).WorkArea;
        var margin = (int)(ScreenMargin * scale);
        var x = Math.Clamp(anchor.X - size.Width / 2, area.X + margin, area.X + area.Width - size.Width - margin);
        var y = anchor.Y >= area.Y + area.Height / 2 ? area.Y + area.Height - size.Height - margin : area.Y + margin;
        AppWindow.Move(new PointInt32(x, y));

        AppWindow.Show();
        Activate();
        PInvoke.SetForegroundWindow(_hwnd);
    }

    private void LoadTiles()
    {
        Tiles.Clear();
        foreach (var scene in SceneService.Scenes.Where(s => s.PinnedToTray))
            Tiles.Add(new SceneCard(scene));

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasTiles)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NoTiles)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TileHeight)));
    }

    private void Session_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(Session.State) or nameof(Session.ActiveScene) or nameof(Session.Latency))
            foreach (var tile in Tiles)
                tile.Refresh();
    }

    public string Headline(State state) => i18N.Translate(state switch
    {
        State.Started => "Connected",
        State.Starting or State.Stopping => state.ToString(),
        _ => "Not connected"
    });

    public string Subline(State state, Scene? scene, string? progress)
    {
        if (scene == null || state is State.Waiting or State.Stopped)
            return i18N.Translate("Click a scene to connect");

        if (state != State.Started)
            return progress ?? scene.Name;

        var server = SceneService.FindServer(scene);
        return server == null ? scene.Name : $"{scene.Name} · {Format.NodeName(server)}";
    }

    public string DisconnectLabel() => Ui.T("Disconnect", "断开");

    public string DownloadRate(ulong bytesPerSecond) => $"↓ {Format.Rate(bytesPerSecond)}";

    public string UploadRate(ulong bytesPerSecond) => $"↑ {Format.Rate(bytesPerSecond)}";

    public string NatText(string? result) => $"NAT {Format.Nat(result)}";

    private async void Tiles_ItemInvoked(ItemsView sender, ItemsViewItemInvokedEventArgs args)
    {
        if (args.InvokedItem is SceneCard card)
            await Session.ToggleAsync(card.Scene);
    }

    private async void Disconnect_Click(object sender, RoutedEventArgs e) => await Session.StopAsync();

    private void Open_Click(object sender, RoutedEventArgs e)
    {
        AppWindow.Hide();
        App.ShowMainWindow();
    }

    private async void Exit_Click(object sender, RoutedEventArgs e) => await App.ExitAsync();
}
