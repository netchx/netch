using System.Text.Json;
using System.Text.Json.Serialization;
using Netch.JsonConverter;
using Netch.Models.Modes;
using Netch.Models.Modes.ProcessMode;
using Netch.Models.Modes.ShareMode;
using Netch.Models.Modes.TunMode;
using Netch.Services;

namespace Netch.Utils;

public static class ModeHelper
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = Global.NewCustomJsonSerializerOptions();

    static ModeHelper()
    {
        JsonSerializerOptions.Converters.Add(new ModeConverterWithTypeDiscriminator());
        JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.Never;
    }

    public static Mode LoadMode(string file)
    {
        if (file.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            return LoadJsonMode(file);

        if (file.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
            return ReadTxtMode(file, new HashSet<string>(StringComparer.OrdinalIgnoreCase));

        throw new NotSupportedException();
    }

    private static Mode LoadJsonMode(string file)
    {
        using var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
        var mode = JsonSerializer.Deserialize<Mode>(fs, JsonSerializerOptions) ?? throw new ArgumentNullException();
        mode.FullName = file;
        return mode;
    }

    public static void WriteFile(this Mode mode)
    {
        var temporary = mode.FullName + ".tmp";
        try
        {
            using (var fs = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
                JsonSerializer.Serialize(fs, mode, JsonSerializerOptions);
            File.Move(temporary, mode.FullName, overwrite: true);
        }
        finally
        {
            File.Delete(temporary);
        }
    }

    // Use the previous global values when a legacy process or TUN mode omitted a setting.
    public static void CompleteSceneDefaults(Mode mode)
    {
        if (mode is TunMode tun)
        {
            tun.Network ??= Global.Settings.TUNTAP;
            return;
        }

        if (mode is not Redirector process)
            return;

        var defaults = Global.Settings.Redirector;
        process.ICMPDelay = process.FilterICMP == null ? defaults.ICMPDelay : process.ICMPDelay ?? 10;
        process.DNSHost = process.FilterDNS == null ? defaults.DNSHost : process.DNSHost ?? $"{Constants.DefaultPrimaryDNS}:53";
        process.FilterTCP ??= defaults.FilterTCP;
        process.FilterUDP ??= defaults.FilterUDP;
        process.FilterICMP ??= defaults.FilterICMP;
        process.FilterDNS ??= defaults.FilterDNS;
        process.FilterParent ??= defaults.FilterParent;
        process.DNSProxy ??= defaults.DNSProxy;
        process.HandleOnlyDNS ??= defaults.HandleOnlyDNS;
    }

    public static IEnumerable<string> ScanExecutables(string directory, int limit) =>
        Directory.EnumerateFiles(directory, "*.exe", new EnumerationOptions
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true,
            AttributesToSkip = FileAttributes.ReparsePoint
        }).Take(limit);

    private static Mode ReadTxtMode(string file, HashSet<string> includes)
    {
        file = Path.GetFullPath(file);
        if (!includes.Add(file))
            throw new FormatException($"Circular mode include: {Path.GetFileName(file)}.");
        try { return ReadTxtModeCore(file, includes); }
        finally { includes.Remove(file); }
    }

    private static Mode ReadTxtModeCore(string file, HashSet<string> includes)
    {
        Mode mode;
        var ls = File.ReadAllLines(file);
        string modeTypeNum;

        if (ls.Length == 0 || !ls[0].StartsWith('#'))
            throw new FormatException(i18N.Translate("A mode TXT must begin with a # metadata line."));

        var heads = ls[0][1..].Split(",", StringSplitOptions.TrimEntries);
        switch (modeTypeNum = heads.ElementAtOrDefault(1) ?? "0")
        {
            case "0":
                mode = new Redirector { FullName = file };
                break;
            case "1":
            case "2":
                mode = new TunMode { FullName = file };
                break;
            case "6":
                mode = new ShareMode { FullName = file };
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        mode.Remark.Add("en", heads[0]);

        foreach (var l in ls.Skip(1))
        {
            if (l.IsNullOrWhiteSpace())
                continue;

            if (l.StartsWith("//"))
                continue;

            Mode? includeMode = null;
            if (l.StartsWith("#include"))
            {
                var relativePath = l["#include ".Length..].Replace("<", "").Replace(">", "").Trim();
                if (relativePath.EndsWith(".h", StringComparison.OrdinalIgnoreCase))
                    relativePath = relativePath[..^2] + ".txt";
                includeMode = ReadTxtMode(ModeService.Instance.GetFullPath(relativePath), includes);
            }

            switch (mode)
            {
                case Redirector processMode:
                    if (includeMode is Redirector pMode)
                    {
                        processMode.Bypass.AddRange(pMode.Bypass);
                        processMode.Handle.AddRange(pMode.Handle);
                    }
                    else if (l.StartsWith("!"))
                    {
                        if (l.Length == 1)
                            throw new FormatException("Empty process bypass rule");
                        processMode.Bypass.Add(l[1..]);
                    }
                    else
                        processMode.Handle.Add(l);

                    break;
                case ShareMode shareMode:
                    shareMode.Argument = l;
                    break;
                case TunMode tunMode:
                    if (includeMode is TunMode tMode)
                    {
                        tunMode.Bypass.AddRange(tMode.Bypass);
                        tunMode.Handle.AddRange(tMode.Handle);
                        break;
                    }

                    switch (modeTypeNum)
                    {
                        case "1":
                            tunMode.Handle.Add(l);
                            break;
                        case "2":
                            tunMode.Bypass.Add(l);
                            break;
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        if (modeTypeNum == "2")
        {
            var tunMode = (TunMode)mode;
            if (!tunMode.Handle.Contains("0.0.0.0/0"))
                tunMode.Handle.Add("0.0.0.0/0");
            if (!tunMode.Handle.Contains("::/0"))
                tunMode.Handle.Add("::/0");
        }

        return mode;
    }
}
