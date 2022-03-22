using System.Net.Sockets;
using System.Text.Json.Serialization;
using Netch.Utils;

namespace Netch.Models;

public abstract class Server : ICloneable
{
    /// <summary>
    ///     延迟
    /// </summary>
    [JsonIgnore]
    public int Delay { get; private set; } = -1;

    /// <summary>
    ///     组
    /// </summary>
    public string Group { get; set; } = Constants.DefaultGroup;

    /// <summary>
    ///     地址
    /// </summary>
    public string Hostname { get; set; } = string.Empty;

    /// <summary>
    ///     端口
    /// </summary>
    public ushort Port { get; set; }

    /// <summary>
    ///     倍率
    /// </summary>
    public double Rate { get; } = 1.0;

    /// <summary>
    ///     备注
    /// </summary>
    public string Remark { get; set; } = "";

    /// <summary>
    ///     代理类型
    /// </summary>
    [JsonPropertyOrder(int.MinValue)]
    public abstract string Type { get; }

    public object Clone()
    {
        return MemberwiseClone();
    }

    /// <summary>
    ///     获取备注
    /// </summary>
    /// <returns>备注</returns>
    public override string ToString()
    {
        var remark = string.IsNullOrWhiteSpace(Remark) ? $"{Hostname}:{Port}" : Remark;

        var shortName = ServerHelper.GetUtilByTypeName(Type).ShortName;

        return $"[{shortName}][{Group}] {remark}";
    }

    public abstract string MaskedData();

    /// <summary>
    ///     测试延迟
    /// </summary>
    /// <returns>延迟</returns>
    public async Task<int> PingAsync()
    {
        try
        {
            var destination = await DnsUtils.LookupAsync(Hostname);
            if (destination == null)
                return Delay = -2;

            var list = new Task<int>[3];
            for (var i = 0; i < 3; i++)
            {
                Task<int> PingCoreAsync()
                {
                    try
                    {
                        return Global.Settings.ServerTCPing ? Utils.Utils.TCPingAsync(destination, Port) : Utils.Utils.ICMPingAsync(destination);
                    }
                    catch (Exception)
                    {
                        return Task.FromResult(-4);
                    }
                }

                list[i] = PingCoreAsync();
            }

            var resTask = await Task.WhenAny(list[0], list[1], list[2]);

            return Delay = await resTask;
        }
        catch (Exception)
        {
            return Delay = -4;
        }
    }
}

public static class ServerExtension
{
    public static async Task<string> AutoResolveHostnameAsync(this Server server, AddressFamily inet = AddressFamily.Unspecified)
    {
        // ! MainController cached
        return (await DnsUtils.LookupAsync(server.Hostname, inet))!.ToString();
    }

    public static bool IsInGroup(this Server server)
    {
        return server.Group is not Constants.DefaultGroup;
    }
}