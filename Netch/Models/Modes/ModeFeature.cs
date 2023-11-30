namespace Netch.Models.Modes;

[Flags]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "CA1069:Enums values should not be duplicated", Justification = "Intentional duplication for specific scenario.")]
public enum ModeFeature
{
    SupportSocks5 = 0,
    SupportIPv4 = 0,
    SupportSocks5Auth = 0b_0001,
    SupportIPv6 = 0b_0100
}