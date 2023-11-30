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

        // 先声明变量，再进行赋值
        var mode = JsonSerializer.Deserialize<Mode>(fs, JsonSerializerOptions);

        // 检查是否为null，如果是，则抛出异常
        if (mode == null)
        {
            throw new ArgumentNullException(nameof(mode), "Deserialization result is null.");
        }

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
        var ls = File.ReadAllLines(file);
        string modeTypeNum;

        if (ls.First().First() != '#')
            throw new FormatException("Not a valid txt mode that begins with meta line");

        var heads = ls[0][1..].Split(",", StringSplitOptions.TrimEntries);
        Mode mode = (modeTypeNum = heads.ElementAtOrDefault(1) ?? "0") switch
        {
            "0" => new Redirector { FullName = file },
            "1" or "2" => new TunMode { FullName = file },
            "6" => new ShareMode { FullName = file },
            _ => throw new ArgumentOutOfRangeException(nameof(modeTypeNum), "Invalid modeTypeNum value"),
        };
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
                includeMode = ReadTxtMode(ModeService.GetFullPath(relativePath));
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
                    throw new ArgumentOutOfRangeException(nameof(mode), "Invalid mode value");
            }
        }

        if (modeTypeNum == "2")
            ((TunMode)mode).Handle.Add("0.0.0.0/0");

        return mode;
    }
}