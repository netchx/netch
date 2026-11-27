using CommunityToolkit.Mvvm.ComponentModel;
using Netch.App.Helpers;
using Netch.App.Services;
using Netch.Enums;
using Netch.Models;
using Netch.Models.Modes;
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
    public partial string Icon { get; private set; } = SceneIcon.Default;

    [ObservableProperty]
    public partial string IconGlyph { get; private set; } = SceneIcon.ToGlyph(SceneIcon.Default);

    /// <summary>Mode type and node, e.g. "Process · HK 01".</summary>
    [ObservableProperty]
    public partial string Summary { get; private set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasModeBadges), nameof(NoModeBadges))]
    public partial IReadOnlyList<SceneModeBadge> ModeBadges { get; private set; } = Array.Empty<SceneModeBadge>();

    [ObservableProperty]
    public partial string NodeNameText { get; private set; } = string.Empty;

    public bool HasModeBadges => ModeBadges.Count > 0;

    public bool NoModeBadges => ModeBadges.Count == 0;

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

    /// <summary>Second line of the tray tile: latency while running, otherwise the node.</summary>
    [ObservableProperty]
    public partial string TileDetail { get; private set; } = string.Empty;

    /// <summary>Visual state of the tray tile: Normal, Active or Unavailable.</summary>
    [ObservableProperty]
    public partial string TileState { get; private set; } = "Normal";

    public void Refresh()
    {
        var session = Session.Current;
        var server = SceneService.FindServer(Scene);
        var modes = SceneService.FindModes(Scene);
        var missingModes = SceneService.FindMissingModeIds(Scene);
        var modeUnavailable = modes.Count == 0 || missingModes.Count > 0;

        Name = Scene.Name;
        Icon = SceneIcon.NormalizeKey(Scene.Icon);
        IconGlyph = SceneIcon.ToGlyph(Icon);
        Hotkey = Scene.Hotkey ?? string.Empty;
        IsPinned = Scene.PinnedToTray;
        var nodeName = server == null ? "—" : Format.NodeName(server);
        Summary = $"{(modes.Count == 0 ? "—" : Format.ModeTypesSummary(modes))} · {nodeName}";
        NodeNameText = nodeName;
        var badges = Format.GetModeTypeBadges(modes).Select(b => new SceneModeBadge(b.Type, b.Label)).ToArray();
        if (!ModeBadges.SequenceEqual(badges))
            ModeBadges = badges;
        IsActive = session.ActiveScene == Scene && !session.IsIdle;
        IsUnavailable = !IsActive && (server == null || modeUnavailable);
        Status = session.State switch
        {
            _ when IsUnavailable => i18N.Translate(server == null ? "Node unavailable" : "Mode unavailable"),
            _ when !IsActive => Format.LastUsed(Scene.LastUsed),
            State.Started => i18N.Translate("Running"),
            var state => i18N.Translate(state.ToString())
        };

        TileState = IsActive ? "Active" : IsUnavailable ? "Unavailable" : "Normal";
        TileDetail = IsActive
            ? session.IsRunning && session.Latency is { } latency ? $"{latency} ms" : Status
            : IsUnavailable ? Status : server == null ? "—" : Format.NodeName(server);
    }
}

public sealed record SceneModeBadge(ModeType Type, string Label)
{
    public bool IsProcess => Type == ModeType.ProcessMode;
    public bool IsWebProxy => Type == ModeType.WebProxyMode;
    public bool IsTun => Type == ModeType.TunMode;
    public bool IsShare => Type == ModeType.ShareMode;
}
