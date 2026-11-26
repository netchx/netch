using System.Globalization;
using System.Text;
using Netch.Enums;
using Netch.Models;
using Netch.Servers;
using Netch.Services;

namespace Netch.Utils;

public static class CustomCoreHelper
{
    public const string BuiltInCoreId = "__builtin__";

    public static bool IsBuiltInCoreId(string? id) =>
        string.Equals(id?.Trim(), BuiltInCoreId, StringComparison.OrdinalIgnoreCase);

    public static string NormalizeExecutableInput(string? raw)
    {
        var trimmed = (raw ?? string.Empty).Trim();
        if (trimmed.Length >= 2 && trimmed[0] == '"' && trimmed[^1] == '"')
            trimmed = trimmed[1..^1].Trim();

        return trimmed;
    }

    public static string NormalizeArgumentsInput(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        return raw
            .Replace("\r\n", " ", StringComparison.Ordinal)
            .Replace('\r', ' ')
            .Replace('\n', ' ')
            .Trim();
    }

    public static string ResolveExecutablePath(string? raw)
    {
        var normalized = NormalizeExecutableInput(raw);
        if (string.IsNullOrWhiteSpace(normalized))
            throw new MessageException(i18N.Translate("Enter the CLI executable path."));

        if (Path.IsPathRooted(normalized))
        {
            var full = Path.GetFullPath(normalized);
            if (File.Exists(full))
                return full;
        }
        else
        {
            var fromRoot = Path.GetFullPath(Path.Combine(Global.NetchDir, normalized));
            if (File.Exists(fromRoot))
                return fromRoot;

            var fromBin = Path.GetFullPath(Path.Combine(Global.NetchDir, "bin", normalized));
            if (File.Exists(fromBin))
                return fromBin;
        }

        throw new MessageException(i18N.TranslateFormat("External CLI executable not found: {0}", normalized));
    }

    public static IReadOnlyList<CustomCore> GetAll() =>
        Global.Settings.CustomCores ?? [];

    public static IReadOnlyList<CustomCore> GetForProtocol(EConfigType protocol)
    {
        if (protocol == EConfigType.ExternalCli)
            return [];

        return GetAll()
            .Where(c => c.Protocol == protocol)
            .ToArray();
    }

    public static CustomCore? FindById(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        var trimmed = id.Trim();
        return GetAll().FirstOrDefault(c => string.Equals(c.Id, trimmed, StringComparison.OrdinalIgnoreCase));
    }

    public static CustomCore? ResolveForServer(Server server)
    {
        ArgumentNullException.ThrowIfNull(server);
        if (server.ConfigType == EConfigType.ExternalCli)
            return null;

        var nodeCoreId = server.ProtoExtra.CustomCoreId?.Trim();
        if (!string.IsNullOrEmpty(nodeCoreId))
        {
            if (IsBuiltInCoreId(nodeCoreId))
                return null;

            var nodeCore = FindById(nodeCoreId);
            if (nodeCore != null && nodeCore.Protocol == server.ConfigType)
                return nodeCore;
        }

        if (server.IsSub)
        {
            var sub = (Global.Settings.Subscription ?? []).FirstOrDefault(s =>
                NodeGroupHelper.GroupEquals(server.Group, s.Remark));
            var subCoreId = sub?.CustomCoreId?.Trim();
            if (!string.IsNullOrEmpty(subCoreId))
            {
                if (IsBuiltInCoreId(subCoreId))
                    return null;

                var subCore = FindById(subCoreId);
                if (subCore != null && subCore.Protocol == server.ConfigType)
                    return subCore;
            }
        }

        return GetAll().FirstOrDefault(c => c.Protocol == server.ConfigType && c.IsProtocolDefault);
    }

    public static string? Validate(CustomCore core)
    {
        ArgumentNullException.ThrowIfNull(core);
        if (string.IsNullOrWhiteSpace(core.Name))
            return "Enter a custom core name.";
        if (core.Protocol == EConfigType.ExternalCli || !Enum.IsDefined(core.Protocol))
            return "Choose a protocol for the custom core.";
        if (string.IsNullOrWhiteSpace(NormalizeExecutableInput(core.ExecutablePath)))
            return "Enter the CLI executable path.";
        if (!Enum.IsDefined(core.Mode))
            return "Choose a launch mode for the custom core.";
        if (core.Mode == CustomCoreMode.CliArguments && string.IsNullOrWhiteSpace(core.ArgumentsTemplate))
            return "Enter the CLI arguments template.";

        return null;
    }

    public static string GetDefaultCliTemplate(EConfigType protocol) => protocol switch
    {
        EConfigType.Shadowsocks or EConfigType.ShadowsocksR =>
            "-s {address} -p {port} -b {local_addr} -l {local_port} -m {method} -k {password}",
        _ =>
            "-s {address} -p {port} -b {local_addr} -l {local_port}"
    };

    public static string RenderArguments(string? template, Server server, string resolvedAddress, string localAddress, ushort localPort)
    {
        ArgumentNullException.ThrowIfNull(server);
        if (string.IsNullOrWhiteSpace(template))
            return string.Empty;

        var method = server is ShadowsocksRServer ssr
            ? ssr.EncryptMethod ?? string.Empty
            : server.ProtoExtra.SsMethod ?? string.Empty;

        var tokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["address"] = resolvedAddress,
            ["host"] = server.Address ?? string.Empty,
            ["port"] = server.Port.ToString(CultureInfo.InvariantCulture),
            ["local_addr"] = localAddress,
            ["local_port"] = localPort.ToString(CultureInfo.InvariantCulture),
            ["username"] = server.Username ?? string.Empty,
            ["password"] = server.Password ?? string.Empty,
            ["method"] = method,
            ["sni"] = server.Sni ?? string.Empty,
            ["plugin"] = server.ProtoExtra.SsPlugin ?? string.Empty,
            ["plugin_opts"] = server.ProtoExtra.SsPluginOptions ?? string.Empty
        };

        var input = NormalizeArgumentsInput(template);
        if (input.Length == 0)
            return string.Empty;

        var sb = new StringBuilder(input.Length + 32);
        var inQuotes = false;
        var precedingBackslashes = 0;

        for (var i = 0; i < input.Length; i++)
        {
            var ch = input[i];
            if (ch == '\\')
            {
                precedingBackslashes++;
                sb.Append(ch);
                continue;
            }

            if (ch == '"')
            {
                if (precedingBackslashes % 2 == 0)
                    inQuotes = !inQuotes;
                precedingBackslashes = 0;
                sb.Append(ch);
                continue;
            }

            precedingBackslashes = 0;

            if (ch == '{')
            {
                var close = input.IndexOf('}', i + 1);
                if (close > i + 1)
                {
                    var key = input[(i + 1)..close];
                    if (tokens.TryGetValue(key, out var val))
                    {
                        var followedByQuote = inQuotes && close + 1 < input.Length && input[close + 1] == '"';
                        sb.Append(FormatWindowsArgValue(val, inQuotes, followedByQuote));
                        i = close;
                        continue;
                    }
                }
            }

            sb.Append(ch);
        }

        return sb.ToString();
    }

    public static IReadOnlyList<(EConfigType Protocol, IReadOnlyList<Server> Servers, IReadOnlyList<CustomCore> Cores)> GetImportCustomCoreMatches(
        IEnumerable<Server> importedServers)
    {
        ArgumentNullException.ThrowIfNull(importedServers);
        var matches = new List<(EConfigType Protocol, IReadOnlyList<Server> Servers, IReadOnlyList<CustomCore> Cores)>();
        foreach (var group in importedServers
            .Where(s => s != null && s.ConfigType != EConfigType.ExternalCli)
            .GroupBy(s => s.ConfigType)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key))
        {
            var cores = GetForProtocol(group.Key);
            if (cores.Count > 0)
                matches.Add((group.Key, group.ToArray(), cores));
        }
        return matches;
    }

    public static int ApplyCustomCoreSelections(
        IEnumerable<Server> servers,
        IReadOnlyDictionary<EConfigType, string?> selections)
    {
        ArgumentNullException.ThrowIfNull(servers);
        ArgumentNullException.ThrowIfNull(selections);

        var assigned = 0;
        foreach (var server in servers)
        {
            if (server == null || server.ConfigType == EConfigType.ExternalCli)
                continue;
            if (!selections.TryGetValue(server.ConfigType, out var selectedId))
                continue;

            var trimmed = selectedId?.Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                server.ProtoExtra.CustomCoreId = null;
            }
            else if (IsBuiltInCoreId(trimmed))
            {
                server.ProtoExtra.CustomCoreId = BuiltInCoreId;
                assigned++;
            }
            else if (FindById(trimmed) is { } core && core.Protocol == server.ConfigType)
            {
                server.ProtoExtra.CustomCoreId = core.Id;
                assigned++;
            }
        }
        return assigned;
    }

    public static void Save(CustomCore core)
    {
        ArgumentNullException.ThrowIfNull(core);
        var cores = Global.Settings.CustomCores ??= [];

        core.Id = string.IsNullOrWhiteSpace(core.Id) || IsBuiltInCoreId(core.Id)
            ? Guid.NewGuid().ToString("N")
            : core.Id.Trim();
        core.Name = (core.Name ?? string.Empty).Trim();
        core.ExecutablePath = NormalizeExecutableInput(core.ExecutablePath);
        core.ArgumentsTemplate = NormalizeArgumentsInput(core.ArgumentsTemplate);

        foreach (var server in Global.Settings.Server ?? [])
        {
            if (string.Equals(server.ProtoExtra.CustomCoreId, core.Id, StringComparison.OrdinalIgnoreCase) &&
                server.ConfigType != core.Protocol)
            {
                server.ProtoExtra.CustomCoreId = null;
            }
        }

        if (core.IsProtocolDefault)
        {
            foreach (var other in cores)
            {
                if (!string.Equals(other.Id, core.Id, StringComparison.OrdinalIgnoreCase) && other.Protocol == core.Protocol)
                    other.IsProtocolDefault = false;
            }
        }

        var existingIndex = cores.FindIndex(c => string.Equals(c.Id, core.Id, StringComparison.OrdinalIgnoreCase));
        if (existingIndex >= 0)
            cores[existingIndex] = core;
        else
            cores.Add(core);
    }

    public static void Delete(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return;

        var trimmed = id.Trim();
        Global.Settings.CustomCores?.RemoveAll(c => string.Equals(c.Id, trimmed, StringComparison.OrdinalIgnoreCase));

        foreach (var server in Global.Settings.Server ?? [])
        {
            if (string.Equals(server.ProtoExtra.CustomCoreId, trimmed, StringComparison.OrdinalIgnoreCase))
                server.ProtoExtra.CustomCoreId = null;
        }

        foreach (var sub in Global.Settings.Subscription ?? [])
        {
            if (string.Equals(sub.CustomCoreId, trimmed, StringComparison.OrdinalIgnoreCase))
                sub.CustomCoreId = null;
        }
    }

    public static void NormalizeAll(Setting settings)
    {
        settings.CustomCores ??= [];
        settings.CustomCores.RemoveAll(c => c == null || c.Protocol == EConfigType.ExternalCli || !Enum.IsDefined(c.Protocol));

        var defaultProtocols = new HashSet<EConfigType>();
        var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var core in settings.CustomCores)
        {
            var trimmedId = core.Id?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(trimmedId) ||
                IsBuiltInCoreId(trimmedId) ||
                !seenIds.Add(trimmedId))
            {
                core.Id = Guid.NewGuid().ToString("N");
                seenIds.Add(core.Id);
            }
            else
            {
                core.Id = trimmedId;
            }

            core.Name = (core.Name ?? string.Empty).Trim();
            core.ExecutablePath = NormalizeExecutableInput(core.ExecutablePath);
            if (!Enum.IsDefined(core.Mode))
                core.Mode = CustomCoreMode.CliArguments;
            core.ArgumentsTemplate = NormalizeArgumentsInput(core.ArgumentsTemplate);

            if (core.IsProtocolDefault && !defaultProtocols.Add(core.Protocol))
                core.IsProtocolDefault = false;
        }
    }

    private static string FormatWindowsArgValue(string value, bool alreadyQuoted, bool followedByQuote)
    {
        if (alreadyQuoted)
            return EscapeEmbeddedQuotes(value, doubleTrailingBackslashes: followedByQuote);

        if (value.Length > 0 && !value.Any(c => char.IsWhiteSpace(c) || c == '"'))
            return value;

        return "\"" + EscapeEmbeddedQuotes(value, doubleTrailingBackslashes: true) + "\"";
    }

    private static string EscapeEmbeddedQuotes(string value, bool doubleTrailingBackslashes)
    {
        var sb = new StringBuilder(value.Length);
        var backslashes = 0;
        foreach (var c in value)
        {
            if (c == '\\')
            {
                backslashes++;
            }
            else if (c == '"')
            {
                sb.Append('\\', backslashes * 2 + 1);
                sb.Append('"');
                backslashes = 0;
            }
            else
            {
                if (backslashes > 0)
                {
                    sb.Append('\\', backslashes);
                    backslashes = 0;
                }
                sb.Append(c);
            }
        }

        if (backslashes > 0)
            sb.Append('\\', doubleTrailingBackslashes ? backslashes * 2 : backslashes);

        return sb.ToString();
    }
}
