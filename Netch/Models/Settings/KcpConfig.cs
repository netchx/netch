namespace Netch.Models;

public class KcpConfig
{
    public bool congestion { get; set; } = false;

    public int downlinkCapacity { get; set; } = 100;

    public int mtu { get; set; } = 1350;

    public int readBufferSize { get; set; } = 2;

    public int tti { get; set; } = 50;

    public int uplinkCapacity { get; set; } = 12;

    public int writeBufferSize { get; set; } = 2;
}