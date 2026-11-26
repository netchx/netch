namespace Netch.Models;

/// <summary>
///     A mode and a fixed node started together from one card in the WinUI front end. Replaces <see cref="Profile" />.
/// </summary>
public class Scene
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Name { get; set; } = string.Empty;

    public string Icon { get; set; } = SceneIcon.Default;

    /// <summary>Mode file path relative to the mode directory.</summary>
    public string ModeId { get; set; } = string.Empty;

    /// <summary><see cref="Server.Id" /> of the node.</summary>
    public string ServerId { get; set; } = string.Empty;

    public string? Hotkey { get; set; }

    public bool PinnedToTray { get; set; }

    public DateTime? LastUsed { get; set; }

    public Scene Duplicate(string name) => new()
    {
        Name = name,
        Icon = Icon,
        ModeId = ModeId,
        ServerId = ServerId,
        PinnedToTray = PinnedToTray
    };
}
