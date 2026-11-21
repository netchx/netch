using Netch.Models;
using Netch.Models.Modes;
using Netch.Utils;

namespace Netch.App.Helpers;

/// <summary>Display text shared by pages and dialogs; also called from x:Bind function bindings.</summary>
public static class Format
{
    public static string ModeTypeName(ModeType type) => i18N.Translate(type switch
    {
        ModeType.ProcessMode => "Process",
        ModeType.TunMode => "TUN",
        ModeType.ShareMode => "Share",
        _ => type.ToString()
    });

    public static string ModeTypeLongName(ModeType type) => i18N.Translate(type switch
    {
        ModeType.ProcessMode => "Process mode",
        ModeType.TunMode => "TUN mode",
        ModeType.ShareMode => "Share mode",
        _ => type.ToString()
    });

    public static int ModeGroupOrder(string key) => key switch
    {
        "Game" => 0,
        "Bypass" => 1,
        "TUNTAP" => 2,
        "Other" => 3,
        "Custom" => 4,
        "" => 5,
        _ => 6
    };

    public static string ModeGroupName(string key) => key switch
    {
        "Game" => i18N.Translate("Game"),
        "Bypass" => i18N.Translate("Bypass rules"),
        "TUNTAP" => "TUN",
        "Other" => i18N.Translate("Other"),
        "Custom" => i18N.Translate("Custom"),
        "" => i18N.Translate("Ungrouped"),
        _ => key
    };

    public static string TileTitle(string icon, string name) => $"{icon} {name}";

    public static string NodeName(Server server) => server.Remarks.IsNullOrWhiteSpace() ? $"{server.Address}:{server.Port}" : server.Remarks;

    /// <summary>Server delay as shown next to a node: -1 is untested, -5 is testing, other negatives are failures.</summary>
    public static string Delay(int delay) => delay switch
    {
        DelayStatus.Untested => string.Empty,
        DelayStatus.Testing => "…",
        DelayStatus.DnsFailed => i18N.Translate("DNS failed"),
        DelayStatus.Unreachable => i18N.Translate("Failed"),
        DelayStatus.Timeout => i18N.Translate("Timeout"),
        < 0 => i18N.Translate("Timeout"),
        _ => $"{delay} ms"
    };

    private static readonly string[] TransferUnits = ["B", "KB", "MB", "GB", "TB", "PB", "EB"];

    public static string Bytes(ulong bytes)
    {
        var value = (double)bytes;
        var unit = 0;
        while (value >= 1000 && unit < TransferUnits.Length - 1)
        {
            value /= 1000;
            unit++;
        }

        return $"{value:0.##} {TransferUnits[unit]}";
    }

    public static string Rate(ulong bytesPerSecond) => $"{Bytes(bytesPerSecond)}/s";

    public static string Elapsed(TimeSpan elapsed) => $"{(int)elapsed.TotalHours:00}:{elapsed.Minutes:00}:{elapsed.Seconds:00}";

    public static string LastUsed(DateTime? lastUsed)
    {
        if (lastUsed is not { } time)
            return i18N.Translate("Never used");

        var ago = DateTime.Now - time;
        if (ago.TotalMinutes < 1)
            return i18N.Translate("Just now");
        if (ago.TotalHours < 1)
            return i18N.TranslateFormat("{0} min ago", (int)ago.TotalMinutes);

        return (DateTime.Today - time.Date).Days switch
        {
            0 => i18N.TranslateFormat("{0} h ago", (int)ago.TotalHours),
            1 => i18N.Translate("Yesterday"),
            var days and < 7 => i18N.TranslateFormat("{0} days ago", days),
            _ => time.ToString("yyyy-MM-dd")
        };
    }

    /// <summary>NAT classifications require a completed RFC 5780 probe.</summary>
    public static string Nat(string? result) => result switch
    {
        null => "—",
        "1" => "Full Cone",
        "2" => "Restricted",
        "3" => "Symmetric",
        "UnsupportedServer" or "Incomplete" => i18N.Translate("NAT type unknown"),
        "NoUDP" => i18N.Translate("No STUN response"),
        "STUN hostname lookup failed." => i18N.Translate("STUN hostname lookup failed."),
        _ => i18N.Translate("Test failed")
    };

    public static bool NatFailed(string? result) => result is not (null or "1" or "2" or "3");
}
