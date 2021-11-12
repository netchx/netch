namespace Netch.Models.Modes.ShareMode;

public class ShareMode : Mode
{
    public override ModeType Type => ModeType.ShareMode;

    public string Argument = "--preset uu";
}