using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Netch.Models;
using Netch.Models.Modes;
using WindowsJobAPI;

namespace Netch;

public static class Global
{
    /// <summary>
    ///     用于读取和写入的配置
    /// </summary>
    public static Setting Settings = new();
    public static readonly JobObject Job = new();

    /// <summary>
    ///     用于存储模式
    /// </summary>
    public static readonly List<Mode> Modes = new();

    public static readonly string NetchDir;
    public static readonly string NetchExecutable;

    static Global()
    {
        NetchExecutable = Environment.ProcessPath ?? Path.Combine(AppContext.BaseDirectory, "Netch.exe");
        // A WinUI single-file bundle extracts its runtime to AppContext.BaseDirectory.
        NetchDir = Path.TrimEndingDirectorySeparator(Path.GetDirectoryName(NetchExecutable) ?? AppContext.BaseDirectory);
    }

    public static JsonSerializerOptions NewCustomJsonSerializerOptions() => new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
}
