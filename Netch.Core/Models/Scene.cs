namespace Netch.Models;

/// <summary>
///     A mode and a fixed node started together from one card in the WinUI front end. Replaces <see cref="Profile" />.
/// </summary>
public class Scene
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Name { get; set; } = string.Empty;

    public string Icon { get; set; } = SceneIcon.Default;

    /// <summary>Primary mode file path relative to the mode directory. Kept for backward compatibility.</summary>
    public string ModeId { get; set; } = string.Empty;

    /// <summary>Ordered mode file paths relative to the mode directory.</summary>
    public List<string> ModeIds { get; set; } = new();

    /// <summary>Scene-specific incremental process mode created by hot-rule persistence.</summary>
    public string? HotRuleModeId { get; set; }

    /// <summary><see cref="Server.Id" /> of the node.</summary>
    public string ServerId { get; set; } = string.Empty;

    public string? Hotkey { get; set; }

    public bool PinnedToTray { get; set; }

    public DateTime? LastUsed { get; set; }

    public IReadOnlyList<string> GetEffectiveModeIds()
    {
        var source = ModeIds is { Count: > 0 }
            ? (IEnumerable<string>)ModeIds
            : string.IsNullOrWhiteSpace(ModeId) ? Array.Empty<string>() : [ModeId];

        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var raw in source)
        {
            if (string.IsNullOrWhiteSpace(raw))
                continue;

            var trimmed = raw.Trim();
            if (seen.Add(trimmed))
                result.Add(trimmed);
        }

        if (result.Count == 0 && !string.IsNullOrWhiteSpace(ModeId))
            result.Add(ModeId.Trim());

        return result;
    }

    public void NormalizeModes() => SetModeIds(GetEffectiveModeIds());

    public void SetModeIds(IEnumerable<string> modeIds)
    {
        ArgumentNullException.ThrowIfNull(modeIds);

        var normalized = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var raw in modeIds)
        {
            if (string.IsNullOrWhiteSpace(raw))
                continue;

            var trimmed = raw.Trim();
            if (seen.Add(trimmed))
                normalized.Add(trimmed);
        }

        ModeIds = normalized;
        ModeId = normalized.Count > 0 ? normalized[0] : string.Empty;

        if (string.IsNullOrWhiteSpace(HotRuleModeId))
        {
            HotRuleModeId = null;
        }
        else
        {
            var trimmedOverlay = HotRuleModeId.Trim();
            HotRuleModeId = seen.Contains(trimmedOverlay) ? trimmedOverlay : null;
        }
    }

    public bool UsesMode(string? modeId) =>
        !string.IsNullOrWhiteSpace(modeId) &&
        GetEffectiveModeIds().Any(id => string.Equals(id, modeId.Trim(), StringComparison.OrdinalIgnoreCase));

    public bool AddModeId(string modeId)
    {
        if (string.IsNullOrWhiteSpace(modeId))
            return false;

        var trimmed = modeId.Trim();
        var current = GetEffectiveModeIds().ToList();
        if (current.Any(id => string.Equals(id, trimmed, StringComparison.OrdinalIgnoreCase)))
            return false;

        current.Add(trimmed);
        SetModeIds(current);
        return true;
    }

    public bool ReplaceModeId(string oldId, string newId)
    {
        if (string.IsNullOrWhiteSpace(oldId) || string.IsNullOrWhiteSpace(newId))
            return false;

        var oldTrimmed = oldId.Trim();
        var newTrimmed = newId.Trim();
        var current = GetEffectiveModeIds();
        var replaced = false;
        var updated = new List<string>(current.Count);
        foreach (var id in current)
        {
            if (string.Equals(id, oldTrimmed, StringComparison.OrdinalIgnoreCase))
            {
                updated.Add(newTrimmed);
                replaced = true;
            }
            else
            {
                updated.Add(id);
            }
        }

        if (!replaced)
            return false;

        var previousOverlay = HotRuleModeId;
        SetModeIds(updated);
        if (!string.IsNullOrWhiteSpace(previousOverlay) &&
            string.Equals(previousOverlay.Trim(), oldTrimmed, StringComparison.OrdinalIgnoreCase))
        {
            HotRuleModeId = newTrimmed;
        }

        return true;
    }

    public Scene Duplicate(string name)
    {
        var modeIds = GetEffectiveModeIds().ToList();
        return new()
        {
            Name = name,
            Icon = Icon,
            ModeId = modeIds.FirstOrDefault() ?? string.Empty,
            ModeIds = modeIds,
            ServerId = ServerId,
            PinnedToTray = PinnedToTray
        };
    }
}
