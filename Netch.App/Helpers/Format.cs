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

    public static string TileTitle(string icon, string name) => $"{icon} {name}";

    public static string NodeName(Server server) => server.Remarks.IsNullOrWhiteSpace() ? $"{server.Address}:{server.Port}" : server.Remarks;

    /// <summary>Server delay as shown next to a node: -1 is untested, other negatives and 1000 are failures.</summary>
    public static string Delay(int delay) => delay switch
    {
        -1 => string.Empty,
        < 0 or >= 1000 => i18N.Translate("Timeout"),
        _ => $"{delay} ms"
    };

    public static string Bytes(ulong bytes) => Bandwidth.Compute(bytes);

    public static string Rate(ulong bytesPerSecond) => $"{Bandwidth.Compute(bytesPerSecond)}/s";

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

    /// <summary>NAT test result: 1–3 from <see cref="Socks5ServerTestUtils" />, otherwise a failure detail such as "Wrong STUN Server!".</summary>
    public static string Nat(string? result) => result switch
    {
        null => "—",
        "1" => "Full Cone",
        "2" => "Restricted",
        "3" => "Symmetric",
        _ => i18N.Translate("Test failed")
    };

    public static bool NatFailed(string? result) => result is not (null or "1" or "2" or "3");
}
