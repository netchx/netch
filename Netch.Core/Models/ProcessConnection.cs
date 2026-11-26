using System.Text.Json.Serialization;

namespace Netch.Models;

public sealed class ProcessConnectionSnapshot
{
    [JsonPropertyName("id")]
    public ulong Id { get; set; }

    [JsonPropertyName("protocol")]
    public string Protocol { get; set; } = string.Empty;

    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;

    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;

    [JsonPropertyName("pid")]
    public uint Pid { get; set; }

    [JsonPropertyName("processName")]
    public string ProcessName { get; set; } = string.Empty;

    [JsonPropertyName("processPath")]
    public string ProcessPath { get; set; } = string.Empty;

    [JsonPropertyName("parentName")]
    public string ParentName { get; set; } = string.Empty;

    [JsonPropertyName("remoteIp")]
    public string RemoteIp { get; set; } = string.Empty;

    [JsonPropertyName("remotePort")]
    public ushort RemotePort { get; set; }

    [JsonPropertyName("upBytes")]
    public ulong UpBytes { get; set; }

    [JsonPropertyName("downBytes")]
    public ulong DownBytes { get; set; }

    [JsonPropertyName("startUnixMs")]
    public ulong StartUnixMs { get; set; }

    [JsonPropertyName("endUnixMs")]
    public ulong EndUnixMs { get; set; }

    [JsonPropertyName("matchedRule")]
    public string MatchedRule { get; set; } = string.Empty;

    [JsonPropertyName("ruleSource")]
    public string RuleSource { get; set; } = string.Empty;

    [JsonPropertyName("closeReason")]
    public string CloseReason { get; set; } = string.Empty;

    [JsonIgnore]
    public ulong UpSpeedBps { get; set; }

    [JsonIgnore]
    public ulong DownSpeedBps { get; set; }

    [JsonIgnore]
    public bool IsActive => string.Equals(State, "Active", StringComparison.OrdinalIgnoreCase);

    [JsonIgnore]
    public bool IsDirect => string.Equals(Action, "DIRECT", StringComparison.OrdinalIgnoreCase);

    [JsonIgnore]
    public bool CanClose => IsActive && !IsDirect;

    [JsonIgnore]
    public string Endpoint => string.IsNullOrEmpty(RemoteIp)
        ? "-"
        : RemoteIp.Contains(':')
            ? $"[{RemoteIp}]:{RemotePort}"
            : $"{RemoteIp}:{RemotePort}";

    [JsonIgnore]
    public DateTime StartTimeLocal => StartUnixMs == 0
        ? DateTime.Now
        : DateTimeOffset.FromUnixTimeMilliseconds((long)StartUnixMs).LocalDateTime;

    [JsonIgnore]
    public DateTime? EndTimeLocal => EndUnixMs == 0
        ? null
        : DateTimeOffset.FromUnixTimeMilliseconds((long)EndUnixMs).LocalDateTime;

    [JsonIgnore]
    public TimeSpan Duration
    {
        get
        {
            if (StartUnixMs == 0)
                return TimeSpan.Zero;

            var endMs = EndUnixMs != 0
                ? EndUnixMs
                : (ulong)Math.Max(0, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            return endMs >= StartUnixMs
                ? TimeSpan.FromMilliseconds(endMs - StartUnixMs)
                : TimeSpan.Zero;
        }
    }
}

public sealed class ProcessConnectionsDump
{
    [JsonPropertyName("active")]
    public List<ProcessConnectionSnapshot> Active { get; set; } = new();

    [JsonPropertyName("closed")]
    public List<ProcessConnectionSnapshot> Closed { get; set; } = new();
}

public sealed class ProcessConnectionEvent
{
    [JsonPropertyName("seq")]
    public ulong Seq { get; set; }

    [JsonPropertyName("timestampMs")]
    public ulong TimestampMs { get; set; }

    [JsonPropertyName("connectionId")]
    public ulong ConnectionId { get; set; }

    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;

    [JsonPropertyName("protocol")]
    public string Protocol { get; set; } = string.Empty;

    [JsonPropertyName("pid")]
    public uint Pid { get; set; }

    [JsonPropertyName("processName")]
    public string ProcessName { get; set; } = string.Empty;

    [JsonPropertyName("processPath")]
    public string ProcessPath { get; set; } = string.Empty;

    [JsonPropertyName("parentName")]
    public string ParentName { get; set; } = string.Empty;

    [JsonPropertyName("remoteIp")]
    public string RemoteIp { get; set; } = string.Empty;

    [JsonPropertyName("remotePort")]
    public ushort RemotePort { get; set; }

    [JsonPropertyName("matchedRule")]
    public string MatchedRule { get; set; } = string.Empty;

    [JsonPropertyName("ruleSource")]
    public string RuleSource { get; set; } = string.Empty;

    [JsonPropertyName("upBytes")]
    public ulong UpBytes { get; set; }

    [JsonPropertyName("downBytes")]
    public ulong DownBytes { get; set; }

    [JsonPropertyName("durationMs")]
    public ulong DurationMs { get; set; }

    [JsonPropertyName("detail")]
    public string Detail { get; set; } = string.Empty;

    [JsonIgnore]
    public DateTime TimestampLocal => TimestampMs == 0
        ? DateTime.Now
        : DateTimeOffset.FromUnixTimeMilliseconds((long)TimestampMs).LocalDateTime;

    [JsonIgnore]
    public string Endpoint => string.IsNullOrEmpty(RemoteIp)
        ? "*"
        : RemotePort == 0
            ? RemoteIp
            : RemoteIp.Contains(':')
                ? $"[{RemoteIp}]:{RemotePort}"
                : $"{RemoteIp}:{RemotePort}";

    public string FormatLogLine()
    {
        var time = TimestampLocal.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var proc = string.IsNullOrEmpty(ProcessName) ? $"pid:{Pid}" : $"{ProcessName}({Pid})";
        var rule = string.IsNullOrEmpty(MatchedRule) ? RuleSource : MatchedRule;
        var extra = string.IsNullOrEmpty(Detail) ? string.Empty : $" | {Detail}";
        return $"[{time}] #{Seq} {Action} {Protocol} {proc} -> {Endpoint} [{rule}]{extra}";
    }
}

public sealed class ProcessConnectionEventBatch
{
    [JsonPropertyName("nextSeq")]
    public ulong NextSeq { get; set; }

    [JsonPropertyName("hasMore")]
    public bool HasMore { get; set; }

    [JsonPropertyName("events")]
    public List<ProcessConnectionEvent> Events { get; set; } = new();
}
