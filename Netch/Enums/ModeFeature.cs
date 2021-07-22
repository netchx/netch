using System;

namespace Netch.Enums
{
    [Flags]
    public enum ModeFeature
    {
        SupportSocks5 = 0,
        SupportIPv4 = 0,
        SupportSocks5Auth = 0b_0001,
        [Obsolete]
        SupportShadowsocks = 0b_0010,
        SupportIPv6 = 0b_0100,
        RequireTestNat = 0b_1000
    }
}