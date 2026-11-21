using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Netch.Utils;

public sealed record ParsedLogLine(string Time, int Level, string Source, string Message)
{
    public string LevelLabel => Level switch
    {
        0 => "TRACE",
        1 => "DEBUG",
        3 => "WARN",
        4 => "ERROR",
        _ => "INFO"
    };

    public string DisplayTime
    {
        get
        {
            if (string.IsNullOrEmpty(Time))
                return string.Empty;

            var space = Time.LastIndexOf(' ');
            return space >= 0 && space + 1 < Time.Length ? Time[(space + 1)..] : Time;
        }
    }

    public override string ToString() => $"{Time}  {LevelLabel,-5}  [{Source}]  {Message}";
}

public static partial class LogLineParser
{
    [GeneratedRegex(@"\x1B\[[0-9;?]*[ -/]*[@-~]")]
    private static partial Regex AnsiEscapeRegex();

    [GeneratedRegex(@"^time=(?:""(?<time>[^""]+)""|(?<time>\S+))\s+level=(?:""(?<level>[^""]+)""|(?<level>\S+))(?:\s+msg=(?:""(?<qmsg>(?:\\.|[^""\\])*)""|(?<umsg>.*)))?$", RegexOptions.IgnoreCase)]
    private static partial Regex LogfmtRegex();

    [GeneratedRegex(@"^(?:(?<tz>[+-]\d{2}:?\d{2})\s+)?(?<date>\d{4}[-/]\d{2}[-/]\d{2})[T ](?<clock>\d{2}:\d{2}:\d{2}(?:\.\d+)?)(?<tz2>Z|[+-]\d{2}:?\d{2})?\s+(?<level>TRACE|TRCE|DEBUG|DBUG|INFO|INFORMATION|WARN|WARNING|ERROR|ERR|FATAL|PANIC)\b[:\s-]*(?<message>.*)$", RegexOptions.IgnoreCase)]
    private static partial Regex DatedLevelRegex();

    [GeneratedRegex(@"^(?<date>\d{4}[-/]\d{2}[-/]\d{2})[T ](?<clock>\d{2}:\d{2}:\d{2}(?:\.\d+)?)(?<tz>Z|[+-]\d{2}:?\d{2})?\s+(?:\[(?<level>[^\]]+)\]\s*)?(?<message>.*)$")]
    private static partial Regex DatedOptionalBracketRegex();

    [GeneratedRegex(@"^\[(?:(?<date>\d{4}[-/]\d{2}[-/]\d{2})[T ])?(?<clock>\d{2}:\d{2}:\d{2}(?:\.\d+)?)\]\s*(?:\[(?<level>[^\]]+)\]\s*)?(?<message>.*)$")]
    private static partial Regex BracketedTimeRegex();

    [GeneratedRegex(@"^(?:\[(?<level>TRACE|TRCE|DEBUG|DBUG|INFO|INFORMATION|WARN|WARNING|ERROR|ERR|FATAL|PANIC)\]\s*|(?<level>TRACE|TRCE|DEBUG|DBUG|DEBU|INFO|WARN|ERRO|FATA)\[\d+\]\s*|(?<level>TRACE|DEBUG|INFO|WARN|WARNING|ERROR|FATAL)\s*:\s*)(?<message>.*)$", RegexOptions.IgnoreCase)]
    private static partial Regex LeadingLevelRegex();

    [GeneratedRegex(@"\b(error|fatal|panic|exception|failed)\b", RegexOptions.IgnoreCase)]
    private static partial Regex ErrorKeywordRegex();

    [GeneratedRegex(@"\b(warn|warning)\b", RegexOptions.IgnoreCase)]
    private static partial Regex WarnKeywordRegex();

    public static ParsedLogLine Parse(string rawLine, string fileLabel, ParsedLogLine? previous, DateTime fallbackLocalTime)
    {
        var line = AnsiEscapeRegex().Replace(rawLine, string.Empty).TrimEnd('\r');
        var fallbackDate = fallbackLocalTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var fallbackStamp = fallbackLocalTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

        var payload = StripWrapperTag(line, fileLabel);

        if (LogfmtRegex().Match(payload) is { Success: true } logfmt)
        {
            var time = NormalizeDateTime(logfmt.Groups["time"].Value) ?? previous?.Time ?? fallbackStamp;
            var level = ParseLevel(logfmt.Groups["level"].Value);
            var message = logfmt.Groups["qmsg"].Success
                ? UnescapeQuoted(logfmt.Groups["qmsg"].Value)
                : logfmt.Groups["umsg"].Value;
            return new ParsedLogLine(time, level, ResolveSource(fileLabel, message), message);
        }

        if (DatedLevelRegex().Match(payload) is { Success: true } datedLevel)
        {
            var time = NormalizeParts(datedLevel.Groups["date"].Value, datedLevel.Groups["clock"].Value,
                datedLevel.Groups["tz2"].Success ? datedLevel.Groups["tz2"].Value : datedLevel.Groups["tz"].Value);
            var message = datedLevel.Groups["message"].Value;
            return new ParsedLogLine(time, ParseLevel(datedLevel.Groups["level"].Value), ResolveSource(fileLabel, message), message);
        }

        if (DatedOptionalBracketRegex().Match(payload) is { Success: true } datedBracket)
        {
            var time = NormalizeParts(datedBracket.Groups["date"].Value, datedBracket.Groups["clock"].Value,
                datedBracket.Groups["tz"].Success ? datedBracket.Groups["tz"].Value : null);
            var message = datedBracket.Groups["message"].Value;
            int level;
            if (datedBracket.Groups["level"].Success && TryParseLevel(datedBracket.Groups["level"].Value, out var parsed))
            {
                level = parsed;
            }
            else
            {
                if (datedBracket.Groups["level"].Success)
                    message = $"[{datedBracket.Groups["level"].Value}] {message}".TrimEnd();
                level = InferLevel(message, 2);
            }
            return new ParsedLogLine(time, level, ResolveSource(fileLabel, message), message);
        }

        if (BracketedTimeRegex().Match(payload) is { Success: true } bracketTime)
        {
            var date = bracketTime.Groups["date"].Success ? bracketTime.Groups["date"].Value : fallbackDate;
            var time = NormalizeParts(date, bracketTime.Groups["clock"].Value, null);
            var message = bracketTime.Groups["message"].Value;
            int level;
            if (bracketTime.Groups["level"].Success && TryParseLevel(bracketTime.Groups["level"].Value, out var parsed))
            {
                level = parsed;
            }
            else
            {
                if (bracketTime.Groups["level"].Success)
                    message = $"[{bracketTime.Groups["level"].Value}] {message}".TrimEnd();
                level = InferLevel(message, 2);
            }
            return new ParsedLogLine(time, level, ResolveSource(fileLabel, message), message);
        }

        if (LeadingLevelRegex().Match(payload) is { Success: true } leadingLevel)
        {
            var message = leadingLevel.Groups["message"].Value;
            return new ParsedLogLine(previous?.Time ?? fallbackStamp, ParseLevel(leadingLevel.Groups["level"].Value),
                ResolveSource(fileLabel, message), message);
        }

        if (previous != null)
            return new ParsedLogLine(previous.Time, previous.Level, previous.Source, payload);

        return new ParsedLogLine(fallbackStamp, InferLevel(payload, 2), ResolveSource(fileLabel, payload), payload);
    }

    public static int ParseLevel(string label) => TryParseLevel(label, out var level) ? level : 2;

    private static bool TryParseLevel(string label, out int level)
    {
        switch (label.Trim().ToLowerInvariant())
        {
            case "verbose" or "vrb" or "trace" or "trce":
                level = 0;
                return true;
            case "debug" or "dbg" or "dbug" or "debu":
                level = 1;
                return true;
            case "info" or "inf" or "information" or "notice":
                level = 2;
                return true;
            case "warning" or "warn" or "wrn":
                level = 3;
                return true;
            case "error" or "erro" or "err" or "fatal" or "ftl" or "fata" or "panic" or "crit" or "critical":
                level = 4;
                return true;
            default:
                level = 2;
                return false;
        }
    }

    private static int InferLevel(string message, int fallback) =>
        ErrorKeywordRegex().IsMatch(message) ? 4 :
        WarnKeywordRegex().IsMatch(message) ? 3 :
        fallback;

    private static string ResolveSource(string fileLabel, string message) =>
        fileLabel == "netch" && (message.Contains("netfilter2", StringComparison.OrdinalIgnoreCase)
                                 || message.StartsWith("Redirector", StringComparison.OrdinalIgnoreCase))
            ? "nf"
            : fileLabel;

    private static string StripWrapperTag(string line, string fileLabel)
    {
        if (fileLabel.Equals("tun2socks", StringComparison.OrdinalIgnoreCase)
            && line.StartsWith("[tun2socks] ", StringComparison.OrdinalIgnoreCase))
            return line["[tun2socks] ".Length..];

        if (fileLabel.Equals("pcap2socks", StringComparison.OrdinalIgnoreCase)
            && line.StartsWith("[pcap2socks] ", StringComparison.OrdinalIgnoreCase))
            return line["[pcap2socks] ".Length..];

        return line;
    }

    private static string? NormalizeDateTime(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        if ((raw.Contains('T') || raw.Contains('Z') || raw.IndexOf('+', 10) > 0 || raw.IndexOf('-', 10) > 0)
            && DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dto))
        {
            var local = dto.ToLocalTime();
            return HasFraction(raw)
                ? local.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture)
                : local.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        }

        var separator = raw.IndexOfAny([' ', 'T']);
        if (separator > 0 && separator + 1 < raw.Length)
            return NormalizeParts(raw[..separator], raw[(separator + 1)..], null);

        return raw.Replace('/', '-');
    }

    private static string NormalizeParts(string datePart, string clockPart, string? offsetPart)
    {
        var normalizedDate = datePart.Replace('/', '-');
        var dot = clockPart.IndexOf('.');
        var trimmedClock = dot >= 0 && clockPart.Length - dot - 1 > 3
            ? clockPart[..(dot + 4)]
            : clockPart;

        if (!string.IsNullOrWhiteSpace(offsetPart))
        {
            var normalizedOffset = offsetPart.Length == 5
                && (offsetPart[0] is '+' or '-')
                && char.IsDigit(offsetPart[1]) && char.IsDigit(offsetPart[2])
                && char.IsDigit(offsetPart[3]) && char.IsDigit(offsetPart[4])
                ? $"{offsetPart[..3]}:{offsetPart[3..]}"
                : offsetPart;
            var combined = $"{normalizedDate}T{trimmedClock}{normalizedOffset}";
            if (DateTimeOffset.TryParse(combined, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dto))
            {
                var local = dto.ToLocalTime();
                return dot >= 0
                    ? local.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture)
                    : local.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            }
        }

        return $"{normalizedDate} {trimmedClock}";
    }

    private static bool HasFraction(string raw)
    {
        var dot = raw.IndexOf('.');
        return dot >= 0 && dot + 1 < raw.Length && char.IsDigit(raw[dot + 1]);
    }

    private static string UnescapeQuoted(string value)
    {
        if (!value.Contains('\\'))
            return value;

        var sb = new StringBuilder(value.Length);
        for (var i = 0; i < value.Length; i++)
        {
            if (value[i] == '\\' && i + 1 < value.Length)
            {
                var next = value[++i];
                switch (next)
                {
                    case '"': sb.Append('"'); break;
                    case '\\': sb.Append('\\'); break;
                    case 't' or 'n' or 'r': sb.Append(' '); break;
                    default:
                        sb.Append('\\');
                        sb.Append(next);
                        break;
                }
            }
            else
            {
                sb.Append(value[i]);
            }
        }
        return sb.ToString();
    }
}
