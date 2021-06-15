using System.Collections.Generic;

namespace Netch.Models.Server.Shadowsocks
{
    public static class Global
    {
        public static readonly List<string> Methods = new List<string>()
        {
            "bf-cfb",
            "rc4-md5",
            "aes-128-cfb",
            "aes-192-cfb",
            "aes-256-cfb",
            "aes-128-ctr",
            "aes-192-ctr",
            "aes-256-ctr",
            "aes-128-gcm",
            "aes-192-gcm",
            "aes-256-gcm",
            "camellia-128-cfb",
            "camellia-192-cfb",
            "camellia-256-cfb",
            "salsa20",
            "chacha20",
            "chacha20-ietf",
            "chacha20-ietf-poly1305",
            "xchacha20-ietf-poly1305",
        };
    }
}
