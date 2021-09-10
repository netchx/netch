using System;

namespace Netch.Enums
{
    [Flags]
    public enum ModeFeature
    {
        SupportSocks5 = 0,
        SupportIPv4 = 0,
        SupportSocks5Auth = 0b_0001,
        SupportIPv6 = 0b_0100
    }
}