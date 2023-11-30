namespace Netch.Models;

public class V2rayConfig
{
    public bool UseMux { get; set; } = false;

    public bool Sniffing { get; set; } = false;

    public bool AllowHttp { get; set; } = false;

    public bool XrayFullCone { get; set; } = true;

    public bool TCPFastOpen { get; set; } = false;

    public bool AllowInsecure { get; set; } = false;

    public KcpConfig KcpConfig { get; set; } = new();

    public bool V2rayNShareLink { get; set; } = true;

    public string Fingerprint { get; set; } = "chrome";

    public string[] Alpn { get; set; } = { "h2", "http/1.1", "h2,http/1.1" };
}