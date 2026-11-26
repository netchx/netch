using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Netch.Models;
using Netch.Models.Modes.WebProxyMode;
using Netch.Services;

namespace Netch.Utils;

public sealed record ParsedPacRules(
    HashSet<string> SuffixDomains,
    HashSet<string> ExactDomains,
    List<string> RegexPatterns,
    HashSet<string> BypassSuffixDomains,
    HashSet<string> BypassExactDomains);

public static partial class PacScriptBuilder
{
    public static ParsedPacRules ParseRuleText(string content)
    {
        var suffix = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var exact = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var regex = new List<string>();
        var bypassSuffix = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var bypassExact = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(content))
            return new ParsedPacRules(suffix, exact, regex, bypassSuffix, bypassExact);

        var text = TryDecodeAutoProxyBase64(content) ?? content;
        var isAutoProxy = text.TrimStart().StartsWith("[AutoProxy", StringComparison.OrdinalIgnoreCase);

        foreach (var rawLine in text.Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#') || line.StartsWith("//") || line.StartsWith('!') || line.StartsWith('['))
                continue;

            var targetBypass = false;
            if (line.StartsWith("@@", StringComparison.Ordinal))
            {
                targetBypass = true;
                line = line[2..].Trim();
                isAutoProxy = true;
                if (line.Length == 0)
                    continue;
            }

            if (line.StartsWith("regexp:", StringComparison.OrdinalIgnoreCase))
            {
                var pattern = line["regexp:".Length..].Trim();
                if (!targetBypass && IsValidRegex(pattern))
                    regex.Add(pattern);
                continue;
            }

            if (line.StartsWith("full:", StringComparison.OrdinalIgnoreCase))
            {
                if (NormalizeHost(line["full:".Length..]) is { } host)
                    (targetBypass ? bypassExact : exact).Add(host);
                continue;
            }

            if (line.StartsWith("domain:", StringComparison.OrdinalIgnoreCase))
            {
                if (NormalizeHost(line["domain:".Length..]) is { } host)
                    (targetBypass ? bypassSuffix : suffix).Add(host);
                continue;
            }

            if (line.StartsWith("keyword:", StringComparison.OrdinalIgnoreCase))
            {
                var kw = line["keyword:".Length..].Trim();
                if (!targetBypass && kw.Length > 0)
                    regex.Add(Regex.Escape(kw));
                continue;
            }

            if (line.StartsWith("||", StringComparison.Ordinal))
            {
                isAutoProxy = true;
                if (ExtractAutoProxyHost(line[2..]) is { } host)
                    (targetBypass ? bypassSuffix : suffix).Add(host);
                continue;
            }

            if (line.StartsWith('|'))
            {
                isAutoProxy = true;
                if (ExtractAutoProxyHost(line[1..]) is { } host)
                    (targetBypass ? bypassExact : exact).Add(host);
                continue;
            }

            if (line.StartsWith('/') && line.EndsWith('/') && line.Length > 2)
            {
                var pattern = line[1..^1];
                // Skip URL-only regexes with scheme slashes; retain host-compatible patterns.
                if (!targetBypass && !pattern.Contains("://", StringComparison.Ordinal) && !pattern.Contains("\\/", StringComparison.Ordinal) && IsValidRegex(pattern))
                    regex.Add(pattern);
                continue;
            }

            if (line.StartsWith("+.", StringComparison.Ordinal) || line.StartsWith("*.", StringComparison.Ordinal) || line.StartsWith('.'))
            {
                var stripped = line.StartsWith('.') ? line[1..] : line[2..];
                if (NormalizeHost(stripped) is { } host)
                    (targetBypass ? bypassSuffix : suffix).Add(host);
                continue;
            }

            if (isAutoProxy)
            {
                if (ExtractAutoProxyHost(line) is { } host)
                    (targetBypass ? bypassSuffix : suffix).Add(host);
                continue;
            }

            if (line.Contains('*'))
            {
                if (!targetBypass)
                {
                    var globRegex = GlobToHostRegex(line);
                    if (IsValidRegex(globRegex))
                        regex.Add(globRegex);
                }
                continue;
            }

            if (NormalizeHost(line) is { } plainHost)
                (targetBypass ? bypassSuffix : suffix).Add(plainHost);
        }

        return new ParsedPacRules(suffix, exact, regex, bypassSuffix, bypassExact);
    }

    public static bool ValidateRules(IEnumerable<string> rules, out List<string> invalid)
    {
        invalid = new List<string>();
        foreach (var raw in rules)
        {
            var rule = raw.Trim();
            if (rule.Length == 0)
            {
                invalid.Add(raw);
                continue;
            }

            if (rule.Equals("<local>", StringComparison.OrdinalIgnoreCase))
                continue;

            if (rule.StartsWith("@@", StringComparison.Ordinal))
            {
                invalid.Add(raw);
                continue;
            }

            if (rule.StartsWith("regexp:", StringComparison.OrdinalIgnoreCase))
            {
                var pattern = rule["regexp:".Length..].Trim();
                if (pattern.Length == 0 || !IsValidRegex(pattern))
                    invalid.Add(raw);
                continue;
            }

            if (rule.StartsWith("keyword:", StringComparison.OrdinalIgnoreCase))
            {
                var kw = rule["keyword:".Length..].Trim();
                if (kw.Length == 0 || !IsValidRuleToken(kw, allowSlash: false))
                    invalid.Add(raw);
                continue;
            }

            if (rule.StartsWith("full:", StringComparison.OrdinalIgnoreCase))
            {
                var host = rule["full:".Length..].Trim();
                if (host.Length == 0 || !IsValidRuleToken(host, allowSlash: false))
                    invalid.Add(raw);
                continue;
            }

            if (rule.StartsWith("domain:", StringComparison.OrdinalIgnoreCase))
            {
                var host = rule["domain:".Length..].Trim();
                if (host.Length == 0 || !IsValidRuleToken(host, allowSlash: false))
                    invalid.Add(raw);
                continue;
            }

            var candidate = rule.StartsWith("+.", StringComparison.Ordinal) || rule.StartsWith("*.", StringComparison.Ordinal)
                ? rule[2..]
                : rule.StartsWith('.')
                    ? rule[1..]
                    : rule;

            if (candidate.Length == 0 || !IsValidRuleToken(candidate, allowSlash: false))
                invalid.Add(raw);
        }

        return invalid.Count == 0;
    }

    public static string ResolveScriptPath(string scriptPath, string? baseDir = null)
    {
        var trimmed = scriptPath.Trim();
        if (Path.IsPathRooted(trimmed))
            return Path.GetFullPath(trimmed);

        var root = string.IsNullOrWhiteSpace(baseDir) ? Global.NetchDir : baseDir;
        return Path.GetFullPath(Path.Combine(root, trimmed));
    }

    public static void ValidateMode(WebProxyMode mode, string? baseDir = null)
    {
        ArgumentNullException.ThrowIfNull(mode);

        if (mode.Strategy == WebProxyStrategy.Manual)
            return;

        if (mode.Strategy == WebProxyStrategy.Global)
        {
            if (!ValidateRules(mode.Bypass, out var invalidGlobalBypass))
                throw new InvalidDataException($"{string.Join(", ", invalidGlobalBypass)}: {i18N.Translate("Invalid web proxy rule.")}");
            return;
        }

        if (mode.PacPreset == PacRulePreset.CustomScript)
        {
            if (string.IsNullOrWhiteSpace(mode.PacScriptPath))
                throw new InvalidDataException(i18N.Translate("Select a custom PAC script file."));

            var fullPath = ResolveScriptPath(mode.PacScriptPath, baseDir);
            if (!File.Exists(fullPath))
                throw new InvalidDataException(i18N.Translate("The custom PAC script file does not exist."));

            var script = File.ReadAllText(fullPath, Encoding.UTF8);
            if (!script.Contains("FindProxyForURL", StringComparison.Ordinal))
                throw new InvalidDataException(i18N.Translate("The PAC script must define FindProxyForURL."));
            return;
        }

        if (!ValidateRules(mode.Handle, out var invalidHandle))
            throw new InvalidDataException($"{string.Join(", ", invalidHandle)}: {i18N.Translate("Invalid web proxy rule.")}");

        if (!ValidateRules(mode.Bypass, out var invalidBypass))
            throw new InvalidDataException($"{string.Join(", ", invalidBypass)}: {i18N.Translate("Invalid web proxy rule.")}");

        if (mode.PacPreset is PacRulePreset.ProxyList or PacRulePreset.GfwList or PacRulePreset.BypassChina &&
            !string.IsNullOrWhiteSpace(mode.PacRuleUrl))
        {
            if (!Uri.TryCreate(mode.PacRuleUrl.Trim(), UriKind.Absolute, out var uri) ||
                uri.Scheme is not ("http" or "https"))
            {
                throw new InvalidDataException(i18N.Translate("Enter an HTTP or HTTPS PAC rule URL."));
            }
        }
    }

    public static string BuildPacScript(WebProxyMode mode, string proxyDirective = "__PROXY__", string? baseDir = null)
    {
        ArgumentNullException.ThrowIfNull(mode);
        var effectiveProxy = string.IsNullOrEmpty(proxyDirective) ? "__PROXY__" : proxyDirective;

        if (mode.PacPreset == PacRulePreset.CustomScript)
            return BuildCustomPacScript(mode, effectiveProxy, baseDir);

        ParsedPacRules? baseRules = null;
        if (mode.PacPreset is PacRulePreset.ProxyList or PacRulePreset.GfwList or PacRulePreset.BypassChina)
        {
            var path = PacRuleService.ResolveRuleFilePath(mode.PacPreset, baseDir);
            if (path == null)
            {
                var file = PacRuleService.GetFileName(mode.PacPreset) ?? "pac";
                throw new MessageException(i18N.TranslateFormat(
                    "PAC rule list \"{0}\" was not found. Download it in Downloads first.", file));
            }

            baseRules = ParseRuleText(File.ReadAllText(path, Encoding.UTF8));
        }

        var modeHandle = ParseRuleList(mode.Handle);
        var modeBypass = ParseRuleList(mode.Bypass);

        var modeBypassSuffix = PruneSuffixDomains(modeBypass.SuffixDomains);
        var modeBypassExact = PruneExactDomains(modeBypass.ExactDomains, modeBypassSuffix);
        var modeHandleSuffix = PruneSuffixDomains(modeHandle.SuffixDomains);
        var modeHandleExact = PruneExactDomains(modeHandle.ExactDomains, modeHandleSuffix);

        HashSet<string> baseBypassSuffix = baseRules != null ? PruneSuffixDomains(baseRules.BypassSuffixDomains) : [];
        HashSet<string> baseBypassExact = baseRules != null ? PruneExactDomains(baseRules.BypassExactDomains, baseBypassSuffix) : [];
        HashSet<string> baseSuffix = baseRules != null ? PruneSuffixDomains(baseRules.SuffixDomains) : [];
        HashSet<string> baseExact = baseRules != null ? PruneExactDomains(baseRules.ExactDomains, baseSuffix) : [];
        IReadOnlyList<string> baseRegex = baseRules?.RegexPatterns ?? [];

        var defaultAction = mode.PacPreset switch
        {
            PacRulePreset.BypassChina => "proxy",
            PacRulePreset.CustomRulesOnly => mode.DefaultProxy ? "proxy" : "direct",
            _ => "direct"
        };

        var invertBase = mode.PacPreset == PacRulePreset.BypassChina;

        var sb = new StringBuilder(4096);
        sb.Append("var proxy = ").Append(JsonSerializer.Serialize(effectiveProxy)).Append(";\n");
        sb.Append("var direct = \"DIRECT\";\n");
        sb.Append("var bypassLan = ").Append(mode.BypassLan ? "true" : "false").Append(";\n");
        sb.Append("var invertBase = ").Append(invertBase ? "true" : "false").Append(";\n");
        sb.Append("var defaultResult = ").Append(defaultAction).Append(";\n");
        sb.Append(
            """
            function decodeDomains(packed) {
              var map = {};
              for (var tail in packed) {
                if (!Object.prototype.hasOwnProperty.call(packed, tail)) continue;
                var suffix = tail ? "." + tail : "";
                var parts = packed[tail].split(" ");
                var prev = "";
                for (var i = 0; i < parts.length; i++) {
                  var w = parts[i];
                  var c = w.charCodeAt(0);
                  if (c >= 65 && c <= 90) w = prev.substring(0, c - 64) + w.substring(1);
                  prev = w;
                  map[w + suffix] = 1;
                }
              }
              return map;
            }

            """);

        AppendDomainMap(sb, "modeBypassExact", modeBypassExact);
        AppendDomainMap(sb, "modeBypassSuffix", modeBypassSuffix);
        AppendRegexList(sb, "modeBypassRegex", modeBypass.RegexPatterns);

        AppendDomainMap(sb, "modeHandleExact", modeHandleExact);
        AppendDomainMap(sb, "modeHandleSuffix", modeHandleSuffix);
        AppendRegexList(sb, "modeHandleRegex", modeHandle.RegexPatterns);

        AppendDomainMap(sb, "baseBypassExact", baseBypassExact);
        AppendDomainMap(sb, "baseBypassSuffix", baseBypassSuffix);
        AppendDomainMap(sb, "baseExact", baseExact);
        AppendDomainMap(sb, "baseSuffix", baseSuffix);
        AppendRegexList(sb, "baseRegex", baseRegex);

        sb.Append(
            """
            function compileRegexes(patterns) {
              var list = [];
              for (var i = 0; i < patterns.length; i++) {
                try { list.push(new RegExp(patterns[i], "i")); } catch (e) {}
              }
              return list;
            }
            var modeBypassRe = compileRegexes(modeBypassRegex);
            var modeHandleRe = compileRegexes(modeHandleRegex);
            var baseRe = compileRegexes(baseRegex);

            function stripHostPort(host) {
              if (!host) return "";
              var h = host.toLowerCase();
              if (h.charAt(0) === "[") {
                var end = h.indexOf("]");
                h = end > 0 ? h.substring(1, end) : h;
              } else {
                var idx = h.indexOf(":");
                if (idx > 0 && h.lastIndexOf(":") === idx) h = h.substring(0, idx);
              }
              if (h.length > 1 && h.charAt(h.length - 1) === ".") h = h.substring(0, h.length - 1);
              return h;
            }

            function isLocalHost(host) {
              if (!host) return true;
              if (host.indexOf(":") >= 0) {
                return host === "::" || host === "::1" || host === "0:0:0:0:0:0:0:1" || /^fe[89ab][0-9a-f]:/.test(host) || /^f[cd][0-9a-f]{2}:/.test(host);
              }
              if (isPlainHostName(host) || host === "localhost" || (host.length > 6 && host.substring(host.length - 6) === ".local") || host === "0.0.0.0") return true;
              if (/^127\.\d{1,3}\.\d{1,3}\.\d{1,3}$/.test(host)) return true;
              if (/^10\.\d{1,3}\.\d{1,3}\.\d{1,3}$/.test(host)) return true;
              if (/^192\.168\.\d{1,3}\.\d{1,3}$/.test(host)) return true;
              if (/^169\.254\.\d{1,3}\.\d{1,3}$/.test(host)) return true;
              var m172 = /^172\.(\d{1,3})\.\d{1,3}\.\d{1,3}$/.exec(host);
              if (m172) {
                var o2 = parseInt(m172[1], 10);
                if (o2 >= 16 && o2 <= 31) return true;
              }
              var m100 = /^100\.(\d{1,3})\.\d{1,3}\.\d{1,3}$/.exec(host);
              if (m100) {
                var c2 = parseInt(m100[1], 10);
                if (c2 >= 64 && c2 <= 127) return true;
              }
              return false;
            }

            function hasDomain(host, exactMap, suffixMap) {
              if (Object.prototype.hasOwnProperty.call(exactMap, host)) return true;
              var cur = host;
              while (cur) {
                if (Object.prototype.hasOwnProperty.call(suffixMap, cur)) return true;
                var dot = cur.indexOf(".");
                if (dot < 0) break;
                cur = cur.substring(dot + 1);
              }
              return false;
            }

            function matchesRegex(host, list) {
              for (var i = 0; i < list.length; i++) {
                if (list[i].test(host)) return true;
              }
              return false;
            }

            function FindProxyForURL(url, host) {
              var h = stripHostPort(host);
              if (bypassLan && isLocalHost(h)) return direct;
              if (hasDomain(h, modeBypassExact, modeBypassSuffix) || matchesRegex(h, modeBypassRe)) return direct;
              if (hasDomain(h, modeHandleExact, modeHandleSuffix) || matchesRegex(h, modeHandleRe)) return proxy;
              if (hasDomain(h, baseBypassExact, baseBypassSuffix)) return invertBase ? proxy : direct;
              if (hasDomain(h, baseExact, baseSuffix) || matchesRegex(h, baseRe)) return invertBase ? direct : proxy;
              return defaultResult;
            }
            """);

        return sb.ToString();
    }

    private static string BuildCustomPacScript(WebProxyMode mode, string proxyDirective, string? baseDir)
    {
        if (string.IsNullOrWhiteSpace(mode.PacScriptPath))
            throw new MessageException(i18N.Translate("Select a custom PAC script file."));

        var fullPath = ResolveScriptPath(mode.PacScriptPath, baseDir);
        if (!File.Exists(fullPath))
            throw new MessageException(i18N.Translate("The custom PAC script file does not exist."));

        var script = File.ReadAllText(fullPath, Encoding.UTF8);
        if (!script.Contains("FindProxyForURL", StringComparison.Ordinal))
            throw new MessageException(i18N.Translate("The PAC script must define FindProxyForURL."));

        if (script.Contains("__PROXY__", StringComparison.Ordinal))
            return script.Replace("__PROXY__", proxyDirective, StringComparison.Ordinal);

        return VarProxyRegex().Replace(script, $"${{1}}{proxyDirective}${{2}}", 1);
    }

    private static ParsedPacRules ParseRuleList(IEnumerable<string> rules)
    {
        var joined = string.Join('\n', rules.Where(r => !string.IsNullOrWhiteSpace(r)));
        return ParseRuleText(joined);
    }

    private static HashSet<string> PruneSuffixDomains(IEnumerable<string> domains)
    {
        var raw = new HashSet<string>(domains, StringComparer.Ordinal);
        if (raw.Count <= 1)
            return raw;

        var result = new HashSet<string>(raw.Count, StringComparer.Ordinal);
        foreach (var domain in raw)
        {
            var cur = domain;
            var covered = false;
            while (true)
            {
                var dot = cur.IndexOf('.');
                if (dot < 0)
                    break;
                cur = cur[(dot + 1)..];
                if (raw.Contains(cur))
                {
                    covered = true;
                    break;
                }
            }

            if (!covered)
                result.Add(domain);
        }

        return result;
    }

    private static HashSet<string> PruneExactDomains(IEnumerable<string> exactDomains, HashSet<string> coveringSuffixes)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);
        foreach (var domain in exactDomains)
        {
            var cur = domain;
            var covered = false;
            while (cur.Length > 0)
            {
                if (coveringSuffixes.Contains(cur))
                {
                    covered = true;
                    break;
                }

                var dot = cur.IndexOf('.');
                if (dot < 0)
                    break;
                cur = cur[(dot + 1)..];
            }

            if (!covered)
                result.Add(domain);
        }

        return result;
    }

    private static void AppendDomainMap(StringBuilder sb, string variableName, IReadOnlyCollection<string> domains)
    {
        if (domains.Count < 64)
        {
            sb.Append("var ").Append(variableName).Append(" = {");
            var first = true;
            foreach (var domain in domains)
            {
                if (!first)
                    sb.Append(',');
                first = false;
                sb.Append(JsonSerializer.Serialize(domain)).Append(":1");
            }
            sb.Append("};\n");
            return;
        }

        var groups = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        var singles = new List<string>();
        foreach (var domain in domains)
        {
            var dot = domain.IndexOf('.');
            if (dot <= 0 || dot == domain.Length - 1)
            {
                singles.Add(domain);
                continue;
            }

            var head = domain[..dot];
            var tail = domain[(dot + 1)..];
            if (!groups.TryGetValue(tail, out var list))
                groups[tail] = list = new List<string>();
            list.Add(head);
        }

        var packed = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (tail, list) in groups)
        {
            if (list.Count >= 2 && tail != "__proto__")
            {
                packed[tail] = FrontCodeJoin(list);
            }
            else
            {
                foreach (var item in list)
                    singles.Add($"{item}.{tail}");
            }
        }

        if (singles.Count > 0)
            packed[string.Empty] = FrontCodeJoin(singles);

        sb.Append("var ").Append(variableName).Append(" = decodeDomains(").Append(JsonSerializer.Serialize(packed)).Append(");\n");
    }

    private static string FrontCodeJoin(List<string> items)
    {
        items.Sort(StringComparer.Ordinal);
        var sb = new StringBuilder();
        var prev = string.Empty;
        foreach (var item in items)
        {
            if (sb.Length > 0)
                sb.Append(' ');

            var max = Math.Min(Math.Min(prev.Length, item.Length), 26);
            var prefix = 0;
            while (prefix < max && prev[prefix] == item[prefix])
                prefix++;

            if (prefix >= 2)
            {
                sb.Append((char)('A' + prefix - 1));
                sb.Append(item.AsSpan(prefix));
            }
            else
            {
                sb.Append(item);
            }

            prev = item;
        }

        return sb.ToString();
    }

    private static void AppendRegexList(StringBuilder sb, string variableName, IReadOnlyList<string> patterns)
    {
        sb.Append("var ").Append(variableName).Append(" = ");
        sb.Append(JsonSerializer.Serialize(patterns));
        sb.Append(";\n");
    }

    private static string? TryDecodeAutoProxyBase64(string content)
    {
        var trimmed = content.Trim();
        if (trimmed.Length < 16 ||
            trimmed.StartsWith("[AutoProxy", StringComparison.OrdinalIgnoreCase) ||
            trimmed.Contains('.') ||
            trimmed.Contains('#') ||
            trimmed.Contains('!'))
            return null;

        var compact = new StringBuilder(trimmed.Length);
        foreach (var c in trimmed)
        {
            if (char.IsWhiteSpace(c))
                continue;
            if (!(char.IsAsciiLetterOrDigit(c) || c is '+' or '/' or '=' or '-' or '_'))
                return null;
            compact.Append(c switch { '-' => '+', '_' => '/', _ => c });
        }

        var normalized = compact.ToString();
        var rem = normalized.Length % 4;
        if (rem == 1)
            return null;
        if (rem > 0)
            normalized = normalized.PadRight(normalized.Length + 4 - rem, '=');

        try
        {
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(normalized));
            if (decoded.Contains("[AutoProxy", StringComparison.OrdinalIgnoreCase) ||
                decoded.Contains("||", StringComparison.Ordinal) ||
                decoded.Contains('\n'))
                return decoded;
        }
        catch (FormatException)
        {
            // Treat as plain text if decoding fails.
        }

        return null;
    }

    private static string? ExtractAutoProxyHost(string token)
    {
        var s = token.Trim();
        if (s.Length == 0)
            return null;

        if (s.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            s = s["https://".Length..];
        else if (s.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            s = s["http://".Length..];

        if (s.StartsWith("*.", StringComparison.Ordinal))
            s = s[2..];
        else if (s.StartsWith('.'))
            s = s[1..];

        var cut = s.IndexOfAny(['/', '?', '#']);
        if (cut >= 0)
            s = s[..cut];

        if (s.Contains('*') || s.Contains('%'))
            return null;

        return NormalizeHost(s);
    }

    private static string? NormalizeHost(string raw)
    {
        var s = raw.Trim().TrimEnd('.');
        if (s.Length == 0)
            return null;

        if (s.StartsWith('[') && s.EndsWith(']'))
            s = s[1..^1];
        else
        {
            var colon = s.IndexOf(':');
            if (colon > 0 && s.LastIndexOf(':') == colon && int.TryParse(s[(colon + 1)..], out _))
                s = s[..colon];
        }

        s = s.ToLowerInvariant();
        if (!IsValidRuleToken(s, allowSlash: false) || s.Contains('*') || s.Equals("<local>", StringComparison.Ordinal))
            return null;

        return s;
    }

    private static string GlobToHostRegex(string glob)
    {
        var escaped = Regex.Escape(glob.Trim().ToLowerInvariant()).Replace("\\*", ".*");
        return $"^{escaped}$";
    }

    private static bool IsValidRuleToken(string value, bool allowSlash)
    {
        if (value.Contains("://", StringComparison.Ordinal))
            return false;

        foreach (var c in value)
        {
            if (char.IsWhiteSpace(c) || char.IsControl(c) || c is ';' or '?' or '#')
                return false;
            if (!allowSlash && c == '/')
                return false;
        }

        return true;
    }

    private static bool IsValidRegex(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            return false;

        try
        {
            _ = new Regex(pattern, RegexOptions.None, TimeSpan.FromSeconds(1));
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    [GeneratedRegex("""(var\s+proxy\s*=\s*['"])[^'"]*(['"])""")]
    private static partial Regex VarProxyRegex();
}
