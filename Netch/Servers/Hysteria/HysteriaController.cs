using System.Net;
using System.Text.Json;
using Netch.Controllers;
using Netch.Interfaces;
using Netch.Models;
using Netch.Utils;

namespace Netch.Servers;

public class HysteriaController:Guard,IServerController
{
    public HysteriaController():base("hysteria.exe")
    {
    }

    public override string Name => "Hysteria";

    public ushort? Socks5LocalPort { get; set; }

    public string? LocalAddress { get; set; }

    public async Task<Socks5Server> StartAsync(Server s)
    {
        var server = (HysteriaServer)s;

        var hysteriaConfig = new HysteriaConfig
        {
            server = server.Hostname + ":" + server.Port,
            protocol = server.Protocol,
            obfs = server.OBFS,
            alpn = server.ALPN,
            auth = server.AuthType != "DISABLED" ? (server.AuthType == "BASE64" ? server.AuthPayload : null) : null,
            auth_str = server.AuthType != "DISABLED" ? (server.AuthType == "STR" ? server.AuthPayload : null) : null,
            server_name = server.ServerName,
            insecure = Convert.ToBoolean(server.Insecure),
            up_mbps = server.UpMbps,
            down_mbps = server.DownMbps,
            recv_window_conn = server.RecvWindowConn,
            recv_window = server.RecvWindow,
            disable_mtu_discovery = Convert.ToBoolean(server.DisableMTUDiscovery),
            socks5 = new Socks5Config
            {
                listen = Global.Settings.LocalAddress + ":" + Global.Settings.Socks5LocalPort
            }
        };
        
        await using (var fileStream = new FileStream(Constants.TempConfig, FileMode.Create, FileAccess.Write, FileShare.Read))
        {
            await JsonSerializer.SerializeAsync(fileStream, hysteriaConfig, Global.NewCustomJsonSerializerOptions());
        }

        await StartGuardAsync("-c ..\\data\\last.json");
        return new Socks5Server(IPAddress.Loopback.ToString(), this.Socks5LocalPort(), server.Hostname);
    }
}