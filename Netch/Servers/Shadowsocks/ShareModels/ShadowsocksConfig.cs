#nullable disable
namespace Netch.Servers;

/// <summary>
///     Import Shadowsocks Server from Json Configuration
///     <see cref="Utils.ShareLink.ParseText"/>
/// </summary>
public class ShadowsocksConfig
{
    public string server { get; set; }

    public ushort server_port { get; set; }

    public string password { get; set; }

    public string method { get; set; }

    public string remarks { get; set; }

    public string plugin { get; set; }

    public string plugin_opts { get; set; }
}