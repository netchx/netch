using System.Net;
using System.Net.Sockets;

namespace Netch.Utils;

public static class EndpointPresetHelper
{
    public static string FormatStunEndpoint(string host, int port)
    {
        var trimmedHost = (host ?? string.Empty).Trim();
        if (trimmedHost.StartsWith('[') && trimmedHost.EndsWith(']') && trimmedHost.Length > 2)
            trimmedHost = trimmedHost[1..^1];

        var validPort = port is >= 1 and <= ushort.MaxValue ? port : Constants.DefaultStunPort;
        if (IPAddress.TryParse(trimmedHost, out var ip) && ip.AddressFamily == AddressFamily.InterNetworkV6)
            return validPort == Constants.DefaultStunPort ? ip.ToString() : $"[{ip}]:{validPort}";

        return validPort == Constants.DefaultStunPort ? trimmedHost : $"{trimmedHost}:{validPort}";
    }

    public static bool TryParseStunEndpoint(string? raw, out string host, out int port)
    {
        host = string.Empty;
        port = Constants.DefaultStunPort;

        var trimmed = (raw ?? string.Empty).Trim();
        if (trimmed.Length == 0 ||
            trimmed.Contains("://", StringComparison.Ordinal) ||
            trimmed.Contains('/') ||
            trimmed.Any(char.IsWhiteSpace))
        {
            return false;
        }

        // Validate explicit trailing port on IPv4/hostname or bracketed IPv6 so :0 or :70000 is rejected.
        if (trimmed.StartsWith('['))
        {
            var closeIdx = trimmed.IndexOf(']');
            if (closeIdx <= 1)
                return false;
            if (closeIdx < trimmed.Length - 1)
            {
                if (trimmed[closeIdx + 1] != ':' ||
                    !ushort.TryParse(trimmed[(closeIdx + 2)..], out var explicitV6Port) ||
                    explicitV6Port == 0)
                    return false;
            }
        }
        else
        {
            var firstColon = trimmed.IndexOf(':');
            var lastColon = trimmed.LastIndexOf(':');
            if (firstColon >= 0 && firstColon == lastColon)
            {
                if (!ushort.TryParse(trimmed[(lastColon + 1)..], out var explicitPort) || explicitPort == 0)
                    return false;
            }
        }

        if (!StunServer.TryParse(trimmed, out var parsed) || parsed.Port == 0)
            return false;

        var cleanHost = parsed.Hostname.Trim();
        if (Uri.CheckHostName(cleanHost) == UriHostNameType.Unknown)
            return false;

        host = cleanHost;
        port = parsed.Port;
        return true;
    }

    public static List<string> BuildOptions(IEnumerable<string> presets, IEnumerable<string>? customItems, string? currentValue)
    {
        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void AddIfNew(string? item)
        {
            var value = item?.Trim();
            if (!string.IsNullOrEmpty(value) && seen.Add(value))
                result.Add(value);
        }

        foreach (var preset in presets)
            AddIfNew(preset);

        if (customItems != null)
            foreach (var custom in customItems)
                AddIfNew(custom);

        AddIfNew(currentValue);
        return result;
    }

    public static List<string> NormalizeCustomList(
        IEnumerable<string>? customItems,
        IEnumerable<string> presets,
        Func<string, string?>? normalize = null)
    {
        var presetSet = presets
            .Select(p => p.Trim())
            .Where(p => p.Length > 0)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (customItems == null)
            return result;

        foreach (var item in customItems)
        {
            var trimmed = item?.Trim();
            if (string.IsNullOrEmpty(trimmed))
                continue;

            var normalized = normalize != null ? normalize(trimmed) : trimmed;
            if (string.IsNullOrWhiteSpace(normalized))
                continue;

            if (!presetSet.Contains(normalized) && seen.Add(normalized))
                result.Add(normalized);
        }

        return result;
    }

    public static bool RecordCustomValue(List<string> customItems, IEnumerable<string> presets, string? value)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return false;

        if (presets.Any(p => string.Equals(p.Trim(), trimmed, StringComparison.OrdinalIgnoreCase)))
            return false;

        if (customItems.Any(c => string.Equals(c.Trim(), trimmed, StringComparison.OrdinalIgnoreCase)))
            return false;

        customItems.Add(trimmed);
        return true;
    }

    public static bool RemoveCustomValue(List<string> customItems, IEnumerable<string> presets, string? value)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return false;

        if (presets.Any(p => string.Equals(p.Trim(), trimmed, StringComparison.OrdinalIgnoreCase)))
            return false;

        return customItems.RemoveAll(c => string.Equals(c.Trim(), trimmed, StringComparison.OrdinalIgnoreCase)) > 0;
    }

    public static bool IsCustomValue(IEnumerable<string>? customItems, IEnumerable<string> presets, string? value)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed) || customItems == null)
            return false;

        if (presets.Any(p => string.Equals(p.Trim(), trimmed, StringComparison.OrdinalIgnoreCase)))
            return false;

        return customItems.Any(c => string.Equals(c.Trim(), trimmed, StringComparison.OrdinalIgnoreCase));
    }
}
