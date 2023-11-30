namespace Netch.Servers;

public class VisionServer : VMessServer
{
    public override string Type { get; } = "Vision";

    public string SpiderX { get; set; } = string.Empty;

    public string ShortId { get; set; } = string.Empty;

    public string PublicKey { get; set; } = string.Empty;

    public string Flow { get; set; } = VisionGlobal.Flows[0];

    public override string EncryptMethod { get; set; } = "none";

    public string Fingerprint { get; set; } = VisionGlobal.Fingerprints[0];

    public override string TransferProtocol { get; set; } = VisionGlobal.TransferProtocols[0];
}

public class VisionGlobal
{
    public static List<string> TransferProtocols => new() { "tcp" };

    public static readonly List<string> TLSSecure = new() { "none", "tls", "reality" };

    public static readonly List<string> Alpns = new() { "h2", "http/1.1", "h2,http/1.1" };

    public static readonly List<string> Flows = new() { "xtls-rprx-vision", "xtls-rprx-vision-udp443" };

    public static readonly List<string> Fingerprints = new() { "chrome","firefox","safari","ios","android","edge","360","qq" };
}