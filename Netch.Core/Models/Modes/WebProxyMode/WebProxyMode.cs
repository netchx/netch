namespace Netch.Models.Modes.WebProxyMode;

public enum WebProxyStrategy
{
    Pac,
    Global,
    Manual
}

public enum PacRulePreset
{
    ProxyList,
    GfwList,
    BypassChina,
    CustomRulesOnly,
    CustomScript
}

public class WebProxyMode : Mode
{
    public override ModeType Type => ModeType.WebProxyMode;

    public WebProxyStrategy Strategy { get; set; } = WebProxyStrategy.Pac;

    public PacRulePreset PacPreset { get; set; } = PacRulePreset.ProxyList;

    public bool DefaultProxy { get; set; }

    public bool BypassLan { get; set; } = true;

    public List<string> Handle { get; set; } = new();

    public List<string> Bypass { get; set; } = new();

    public string? PacScriptPath { get; set; }

    public string? PacRuleUrl { get; set; }
}
