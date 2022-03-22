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
        if (file.EndsWith(".json"))
            return LoadJsonMode(file);

        if (file.EndsWith(".txt"))
            return ReadTxtMode(file);

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
        using var fs = new FileStream(mode.FullName, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true);
        JsonSerializer.Serialize(fs, mode, JsonSerializerOptions);
    }

    private static Mode ReadTxtMode(string file)
    {
        Mode mode;
        var ls = File.ReadAllLines(file);
        string modeTypeNum;

        if (ls.First().First() != '#')
            throw new FormatException("Not a valid txt mode that begins with meta line");

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
                var relativePath = l["#include ".Length..].Replace("<", "").Replace(">", "").Replace(".h", ".txt").Trim();
                includeMode = ReadTxtMode(ModeService.Instance.GetFullPath(relativePath));
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
                        processMode.Bypass.Add(l);
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
                        tMode.Handle.AddRange(tMode.Handle);
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
            ((TunMode)mode).Handle.Add("0.0.0.0/0");

        return mode;
    }
}