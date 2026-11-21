using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.UI.Dispatching;
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
using Windows.Foundation;
using Windows.Graphics;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.HiDpi;
using Point = System.Drawing.Point;

namespace Netch.App;

/// <summary>Borderless window opened by a left click on the tray icon; hides itself when it loses focus.</summary>
public sealed partial class TrayFlyout : Window, INotifyPropertyChanged
{
    private const int ScreenMargin = 12;

    private readonly HWND _hwnd;
    private long _hiddenAt;
    private Point _lastAnchor;
    private bool _hasAnchor;

    public TrayFlyout()
    {
        InitializeComponent();
        var hwnd = Microsoft.UI.Win32Interop.GetWindowFromWindowId(AppWindow.Id);
        _hwnd = new HWND(hwnd);

        var presenter = (OverlappedPresenter)AppWindow.Presenter;
        presenter.SetBorderAndTitleBar(false, false);
        presenter.IsResizable = false;
        presenter.IsMaximizable = false;
        presenter.IsMinimizable = false;
        presenter.IsAlwaysOnTop = true;
        try
        {
            AppWindow.IsShownInSwitchers = false;
        }
        catch (NotImplementedException)
        {
            const int gwlExStyle = -20;
            const nint wsExToolWindow = 0x00000080;
            const nint wsExAppWindow = 0x00040000;
            var exStyle = GetWindowLongPtr(hwnd, gwlExStyle);
            SetWindowLongPtr(hwnd, gwlExStyle, (exStyle & ~wsExAppWindow) | wsExToolWindow);
        }

        Activated += (_, e) =>
        {
            if (e.WindowActivationState != WindowActivationState.Deactivated)
                return;

            AppWindow.Hide();
            _hiddenAt = Environment.TickCount64;
        };
        Root.SizeChanged += (_, _) =>
        {
            if (AppWindow.IsVisible && _hasAnchor)
                UpdateBounds(_lastAnchor);
        };
        Session.PropertyChanged += Session_PropertyChanged;
        Closed += (_, _) => Session.PropertyChanged -= Session_PropertyChanged;
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

        _lastAnchor = anchor;
        _hasAnchor = true;
        LoadTiles();
        UpdateBounds(anchor);

        AppWindow.Show();
        Activate();
        PInvoke.SetForegroundWindow(_hwnd);
        DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
        {
            if (AppWindow.IsVisible && _hasAnchor)
                UpdateBounds(_lastAnchor);
        });
    }

    private void UpdateBounds(Point anchor)
    {
        Root.UpdateLayout();
        Root.Measure(new Size(Root.Width, double.PositiveInfinity));
        var desired = Root.DesiredSize;
        var widthDip = Math.Max(Root.Width, Math.Ceiling(desired.Width));
        var heightDip = desired.Height > 0 ? Math.Ceiling(desired.Height) : Math.Ceiling(Root.ActualHeight);
        if (heightDip <= 0)
            heightDip = HasTiles ? TileHeight + 132 : 148;

        var scale = ScaleAt(anchor);
        var area = DisplayArea.GetFromPoint(new PointInt32(anchor.X, anchor.Y), DisplayAreaFallback.Nearest).WorkArea;
        var margin = (int)Math.Ceiling(ScreenMargin * scale);
        var clientWidth = (int)Math.Ceiling(widthDip * scale);
        var clientHeight = Math.Min((int)Math.Ceiling(heightDip * scale), Math.Max(100, area.Height - margin * 2));
        if (AppWindow.ClientSize.Width != clientWidth || AppWindow.ClientSize.Height != clientHeight)
            AppWindow.ResizeClient(new SizeInt32(clientWidth, clientHeight));

        var size = AppWindow.Size;
        var x = Math.Clamp(anchor.X - size.Width / 2, area.X + margin, area.X + area.Width - size.Width - margin);
        var y = anchor.Y >= area.Y + area.Height / 2 ? area.Y + area.Height - size.Height - margin : area.Y + margin;
        if (AppWindow.Position.X != x || AppWindow.Position.Y != y)
            AppWindow.Move(new PointInt32(x, y));
    }

    private double ScaleAt(Point anchor)
    {
        var monitor = PInvoke.MonitorFromPoint(anchor, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST);
        if (!monitor.IsNull && PInvoke.GetDpiForMonitor(monitor, MONITOR_DPI_TYPE.MDT_EFFECTIVE_DPI, out var dpiX, out _) == 0 && dpiX > 0)
            return dpiX / 96.0;
        var windowDpi = PInvoke.GetDpiForWindow(_hwnd);
        return (windowDpi > 0 ? windowDpi : 96) / 96.0;
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

        if (AppWindow.IsVisible && _hasAnchor &&
            e.PropertyName is nameof(Session.State) or nameof(Session.IsRunning) or nameof(Session.TrafficAvailable) or nameof(Session.ActiveScene) or nameof(Session.Progress))
            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
            {
                if (AppWindow.IsVisible && _hasAnchor)
                    UpdateBounds(_lastAnchor);
            });
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

    [LibraryImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static partial nint GetWindowLongPtr(nint hwnd, int index);

    [LibraryImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static partial nint SetWindowLongPtr(nint hwnd, int index, nint newLong);
}
