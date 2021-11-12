namespace Netch.Models.Modes.TunMode;

public class TunMode : Mode
{
    public override ModeType Type => ModeType.TunMode;

    public List<string> Bypass { get; set; } = new();

    public List<string> Handle { get; set; } = new();
}