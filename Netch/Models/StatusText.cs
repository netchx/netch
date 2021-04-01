using Netch.Utils;
using System.Collections.Generic;
using System.Linq;

namespace Netch.Models
{
    public static class StatusPortInfoText
    {
        private static ushort? _socks5Port;
        private static ushort? _httpPort;
        private static bool _shareLan;

        public static ushort HttpPort
        {
            set => _httpPort = value;
        }

        public static ushort Socks5Port
        {
            set => _socks5Port = value;
        }

        public static string Value
        {
            get
            {
                var strings = new List<string>();

                if (_socks5Port != null)
                    strings.Add($"Socks5 {i18N.Translate("Local Port", ": ")}{_socks5Port}");

                if (_httpPort != null)
                    strings.Add($"HTTP {i18N.Translate("Local Port", ": ")}{_httpPort}");

                if (!strings.Any())
                    return string.Empty;

                return $" ({(_shareLan ? i18N.Translate("Allow other Devices to connect") + " " : "")}{string.Join(" | ", strings)})";
            }
        }

        public static void UpdateShareLan()
        {
            _shareLan = Global.Settings.LocalAddress != "127.0.0.1";
        }

        public static void Reset()
        {
            _httpPort = _socks5Port = null;
        }
    }
}