using System.Text.Json.Serialization;
using Netch.Utils;

namespace Netch.Models.Modes;

public abstract class Mode
{
    [JsonPropertyOrder(int.MinValue)]
    public abstract ModeType Type { get; }

    public Dictionary<string, string> Remark { get; set; } = new();

    [JsonIgnore]
    // File FullName
    // TODO maybe make it becomes mode dictionary key
    public string FullName { get; set; } = string.Empty;

    public override string ToString() => $"[{(int)Type + 1}] {i18NRemark}";

    [JsonIgnore]
    public string i18NRemark
    {
        // TODO i18N.Culture to support fallback
        get => Remark.GetValueOrDefault(i18N.LangCode) ?? Remark.GetValueOrDefault("en") ?? "";
        set => Remark[i18N.LangCode] = value;
    }
}