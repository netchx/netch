namespace Netch.Models;

public class V2rayConfig
{
    public CoreBasicItem CoreBasicItem { get; set; } = new();
    public KcpItem KcpItem { get; set; } = new();
    public Mux4RayItem Mux4RayItem { get; set; } = new();
    public HysteriaItem HysteriaItem { get; set; } = new();
    public GrpcItem GrpcItem { get; set; } = new();
}

[Serializable]
public class CoreBasicItem
{
    public bool MuxEnabled { get; set; } = false;

    public bool DefAllowInsecure { get; set; } = false;

    public string DefFingerprint { get; set; } = Constants.Fingerprints[2];

    public bool EnableFragment { get; set; } = false;
}

[Serializable]
public class KcpItem
{
    public int Mtu { get; set; } = 1350;

    public int Tti { get; set; } = 50;

    public int UplinkCapacity { get; set; } = 12;

    public int DownlinkCapacity { get; set; } = 100;

    public bool Congestion { get; set; } = false;

    public int ReadBufferSize { get; set; } = 2;

    public int WriteBufferSize { get; set; } = 2;
}


[Serializable]
public class Mux4RayItem
{
    public int? Concurrency { get; set; } = 8;
    public int? XudpConcurrency { get; set; } = 16;
    public string? XudpProxyUDP443 { get; set; } = "reject";
}

[Serializable]
public class HysteriaItem
{
    public int UpMbps { get; set; } = 0;
    public int DownMbps { get; set; } = 0;
    public int HopInterval { get; set; } = 30;
}

[Serializable]
public class GrpcItem
{
    public int? IdleTimeout { get; set; } = 60;
    public int? HealthCheckTimeout { get; set; } = 20;
    public bool? PermitWithoutStream { get; set; } = false;
    public int? InitialWindowsSize { get; set; } = 0;
}
