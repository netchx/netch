using CommunityToolkit.Mvvm.ComponentModel;
using Netch.App.Helpers;
using Netch.App.Services;
using Netch.Enums;
using Netch.Models;
using Netch.Services;
using Netch.Utils;

namespace Netch.App.ViewModels;

/// <summary>One card on the scene wall.</summary>
public sealed partial class SceneCard : ObservableObject
{
    public SceneCard(Scene scene)
    {
        Scene = scene;
        Refresh();
    }

    public Scene Scene { get; }

    [ObservableProperty]
    public partial string Name { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial string Icon { get; private set; } = string.Empty;

    /// <summary>Mode type and node, e.g. "Process · HK 01".</summary>
    [ObservableProperty]
    public partial string Summary { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial string Status { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial string Hotkey { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsPinned { get; private set; }

    /// <summary>Starting, running or stopping.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNormal))]
    public partial bool IsActive { get; private set; }

    /// <summary>The node or the mode no longer exists.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNormal))]
    public partial bool IsUnavailable { get; private set; }

    public bool IsNormal => !IsActive && !IsUnavailable;

    public void Refresh()
    {
        var session = Session.Current;
        var server = SceneService.FindServer(Scene);
        var mode = SceneService.FindMode(Scene);

        Name = Scene.Name;
        Icon = Scene.Icon;
        Hotkey = Scene.Hotkey ?? string.Empty;
        IsPinned = Scene.PinnedToTray;
        Summary = $"{(mode == null ? "—" : Format.ModeTypeName(mode.Type))} · {(server == null ? "—" : Format.NodeName(server))}";
        IsActive = session.ActiveScene == Scene && !session.IsIdle;
        IsUnavailable = !IsActive && (server == null || mode == null);
        Status = session.State switch
        {
            _ when IsUnavailable => "⚠ " + i18N.Translate(server == null ? "Node unavailable" : "Mode unavailable"),
            _ when !IsActive => Format.LastUsed(Scene.LastUsed),
            State.Started => "● " + i18N.Translate("Running"),
            var state => i18N.Translate(state.ToString())
        };
    }
}

/// <summary>The trailing "new scene" tile of the scene wall.</summary>
public sealed class AddSceneCard
{
    public static AddSceneCard Instance { get; } = new();

    private AddSceneCard()
    {
    }
}
