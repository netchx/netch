using Netch.Models.Modes.ProcessMode;

namespace Netch.Models;

public enum HotRuleKind
{
    HandleProcess,
    BypassProcess,
    HandleIp,
    BypassIp
}

public sealed record HotRuleEntry(HotRuleKind Kind, string Pattern, string DisplayLabel = "")
{
    public string EffectiveLabel => string.IsNullOrEmpty(DisplayLabel) ? Pattern : DisplayLabel;

    public bool Equals(HotRuleEntry? other) =>
        other is not null && Kind == other.Kind && string.Equals(Pattern, other.Pattern, StringComparison.OrdinalIgnoreCase);

    public override int GetHashCode() => HashCode.Combine(Kind, StringComparer.OrdinalIgnoreCase.GetHashCode(Pattern));
}

public sealed class HotRuleOverlay
{
    private readonly object _sync = new();
    private readonly List<HotRuleEntry> _entries = new();

    public event Action? Changed;

    public bool HasChanges
    {
        get
        {
            lock (_sync)
                return _entries.Count > 0;
        }
    }

    public int Count
    {
        get
        {
            lock (_sync)
                return _entries.Count;
        }
    }

    public IReadOnlyList<HotRuleEntry> Entries
    {
        get
        {
            lock (_sync)
                return _entries.ToArray();
        }
    }

    public bool Add(HotRuleKind kind, string pattern, string displayLabel = "")
    {
        if (string.IsNullOrEmpty(pattern))
            return false;

        var changed = false;
        lock (_sync)
        {
            var entry = new HotRuleEntry(kind, pattern, displayLabel);
            var opposite = OppositeKind(kind);
            var removedOpposite = _entries.RemoveAll(e =>
                e.Kind == opposite && string.Equals(e.Pattern, pattern, StringComparison.OrdinalIgnoreCase));

            if (!_entries.Contains(entry))
            {
                _entries.Add(entry);
                changed = true;
            }
            else if (removedOpposite > 0)
            {
                changed = true;
            }
        }

        if (changed)
            Changed?.Invoke();

        return changed;
    }

    public bool Remove(HotRuleEntry entry)
    {
        bool removed;
        lock (_sync)
            removed = _entries.RemoveAll(e => e.Equals(entry)) > 0;

        if (removed)
            Changed?.Invoke();

        return removed;
    }

    public bool Clear()
    {
        bool hadAny;
        lock (_sync)
        {
            hadAny = _entries.Count > 0;
            _entries.Clear();
        }

        if (hadAny)
            Changed?.Invoke();

        return hadAny;
    }

    public (List<string> Handle, List<string> Bypass, List<string> HandleIPs, List<string> BypassIPs) BuildEffectiveRules(Redirector baseMode)
    {
        var handle = new List<string>(baseMode.Handle);
        var bypass = new List<string>(baseMode.Bypass);
        var handleIps = new List<string>(baseMode.HandleIPs);
        var bypassIps = new List<string>(baseMode.BypassIPs);

        HotRuleEntry[] snapshot;
        lock (_sync)
            snapshot = _entries.ToArray();

        foreach (var entry in snapshot)
        {
            switch (entry.Kind)
            {
                case HotRuleKind.HandleProcess:
                    RemoveIgnoreCase(bypass, entry.Pattern);
                    AddUniqueIgnoreCase(handle, entry.Pattern);
                    break;
                case HotRuleKind.BypassProcess:
                    RemoveIgnoreCase(handle, entry.Pattern);
                    AddUniqueIgnoreCase(bypass, entry.Pattern);
                    break;
                case HotRuleKind.HandleIp:
                    RemoveIgnoreCase(bypassIps, entry.Pattern);
                    AddUniqueIgnoreCase(handleIps, entry.Pattern);
                    break;
                case HotRuleKind.BypassIp:
                    RemoveIgnoreCase(handleIps, entry.Pattern);
                    AddUniqueIgnoreCase(bypassIps, entry.Pattern);
                    break;
            }
        }

        return (handle, bypass, handleIps, bypassIps);
    }

    public void ApplyToMode(Redirector targetMode)
    {
        var (handle, bypass, handleIps, bypassIps) = BuildEffectiveRules(targetMode);
        targetMode.Handle = handle;
        targetMode.Bypass = bypass;
        targetMode.HandleIPs = handleIps;
        targetMode.BypassIPs = bypassIps;
    }

    private static HotRuleKind OppositeKind(HotRuleKind kind) => kind switch
    {
        HotRuleKind.HandleProcess => HotRuleKind.BypassProcess,
        HotRuleKind.BypassProcess => HotRuleKind.HandleProcess,
        HotRuleKind.HandleIp => HotRuleKind.BypassIp,
        HotRuleKind.BypassIp => HotRuleKind.HandleIp,
        _ => kind
    };

    private static void AddUniqueIgnoreCase(List<string> list, string value)
    {
        if (!list.Exists(item => string.Equals(item, value, StringComparison.OrdinalIgnoreCase)))
            list.Add(value);
    }

    private static void RemoveIgnoreCase(List<string> list, string value)
    {
        list.RemoveAll(item => string.Equals(item, value, StringComparison.OrdinalIgnoreCase));
    }
}
