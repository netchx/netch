using System.ComponentModel;
using System.Drawing;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.Threading;
using Netch.App.Helpers;
using Netch.Models;
using Netch.Services;
using Netch.Utils;

namespace Netch.App.Services;

/// <summary>The tray icon with its flyout and menu, and the scene hotkeys.</summary>
public sealed partial class Tray : ObservableObject
{
    public static Tray Current { get; } = new();

    private readonly Dictionary<int, Scene> _hotkeys = new();
    private TrayIcon? _icon;
    private TrayFlyout? _flyout;

    private Tray()
    {
    }

    /// <summary>Hotkeys another program already owns, e.g. "Ctrl+Alt+1"; empty when all are registered.</summary>
    [ObservableProperty]
    public partial string HotkeyConflicts { get; private set; } = string.Empty;

    /// <summary>Must run on the UI thread after <see cref="Session.Attach" />.</summary>
    public void Initialize()
    {
        _icon = new TrayIcon(Path.Combine(AppContext.BaseDirectory, "Assets", "Netch.ico"), "Netch");
        _icon.Selected += anchor => (_flyout ??= CreateFlyout()).Toggle(anchor);
        _icon.ContextMenuRequested += ShowMenu;
        _icon.HotkeyPressed += OnHotkeyPressed;

        Session.Current.PropertyChanged += Session_PropertyChanged;
        AppEvents.NotificationRequested += (text, info) => App.MainWindow?.DispatcherQueue.TryEnqueue(() => _icon?.ShowBalloon("Netch", text, !info));

        RegisterHotkeys();
    }

    public void ShowBalloon(string text) => _icon?.ShowBalloon("Netch", text);

    public void ApplyTheme(ElementTheme theme) => _flyout?.ApplyTheme(theme);

    /// <summary>Registers the hotkeys of all scenes again; call after scenes change.</summary>
    public void RegisterHotkeys()
    {
        if (_icon == null)
            return;

        foreach (var id in _hotkeys.Keys)
            _icon.UnregisterHotkey(id);
        _hotkeys.Clear();

        var conflicts = new List<string>();
        var nextId = 1;
        foreach (var scene in SceneService.Scenes)
        {
            if (!Hotkey.TryParse(scene.Hotkey, out var hotkey))
                continue;

            if (_icon.RegisterHotkey(nextId, hotkey.Modifiers, (uint)hotkey.Key))
                _hotkeys[nextId++] = scene;
            else
                conflicts.Add(hotkey.ToString());
        }

        HotkeyConflicts = string.Join(", ", conflicts);
    }

    /// <summary>Removes the icon and closes the flyout; used on exit.</summary>
    public void Close()
    {
        _flyout?.Close();
        _icon?.Dispose();
        _icon = null;
    }

    private TrayFlyout CreateFlyout()
    {
        var flyout = new TrayFlyout();
        flyout.ApplyTheme(App.MainWindow?.Theme ?? ElementTheme.Default);
        return flyout;
    }

    private void ShowMenu(Point at)
    {
        var session = Session.Current;
        var entries = new List<MenuEntry?>();
        var actions = new List<Action?>();

        void Add(MenuEntry? entry, Action? action)
        {
            entries.Add(entry);
            actions.Add(action);
        }

        foreach (var scene in SceneService.Scenes)
            Add(new MenuEntry(scene.Name, session.ActiveScene == scene && !session.IsIdle, !session.IsBusy),
                () => session.ToggleAsync(scene).Forget());

        if (entries.Count > 0)
            Add(null, null);

        Add(new MenuEntry(i18N.Translate("Disconnect"), Enabled: session.IsRunning), () => session.StopAsync().Forget());
        Add(new MenuEntry(i18N.Translate("Open Netch")), App.ShowMainWindow);
        Add(null, null);
        Add(new MenuEntry(i18N.Translate("Exit")), () => App.ExitAsync().Forget());

        var chosen = _icon!.ShowMenu(at, entries);
        if (chosen >= 0)
            actions[chosen]?.Invoke();
    }

    private async void OnHotkeyPressed(int id)
    {
        if (_hotkeys.TryGetValue(id, out var scene))
            await Session.Current.ToggleAsync(scene);
    }

    private void Session_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_icon == null || e.PropertyName is not (nameof(Session.State) or nameof(Session.ActiveScene)))
            return;

        var session = Session.Current;
        _icon.Tooltip = session.IsRunning && session.ActiveScene is { } scene ? $"Netch · {scene.Name}" : "Netch";
    }
}
