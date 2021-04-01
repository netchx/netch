using Netch.Servers.VMess;
using System.Collections.Generic;

namespace Netch.Servers.VLESS
{
    public class VLESS : VMess.VMess
    {
        public override string Type { get; } = "VLESS";

        /// <summary>
        ///     加密方式
        /// </summary>
        public override string EncryptMethod { get; set; } = "none";

        /// <summary>
        ///     传输协议
        /// </summary>
        public override string TransferProtocol { get; set; } = VLESSGlobal.TransferProtocols[0];

        /// <summary>
        ///     伪装类型
        /// </summary>
        public override string FakeType { get; set; } = VLESSGlobal.FakeTypes[0];

        /// <summary>
        /// </summary>
        public string? Flow { get; set; }
    }

    public class VLESSGlobal
    {
        public static readonly List<string> TLSSecure = new()
        {
            "none",
            "tls",
            "xtls"
        };

        public static List<string> FakeTypes => VMessGlobal.FakeTypes;

        public static List<string> TransferProtocols => VMessGlobal.TransferProtocols;

        public static List<string> QUIC => VMessGlobal.QUIC;
    }
}