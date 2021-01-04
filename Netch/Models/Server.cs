using System;
using System.Threading.Tasks;
using Netch.Utils;

namespace Netch.Models
{
    public class Server : ICloneable
    {
        /// <summary>
        ///     备注
        /// </summary>
        public string Remark;

        /// <summary>
        ///     组
        /// </summary>
        public string Group = "None";

        /// <summary>
        ///     代理类型
        /// </summary>
        public string Type;

        /// <summary>
        ///     倍率
        /// </summary>
        public double Rate = 1.0;

        /// <summary>
        ///     地址
        /// </summary>
        public string Hostname;

        /// <summary>
        ///     端口
        /// </summary>
        public ushort Port;

        /// <summary>
        ///     延迟
        /// </summary>
        public int Delay = -1;

        /// <summary>
        ///		获取备注
        /// </summary>
        /// <returns>备注</returns>
        public override string ToString()
        {
            var remark = string.IsNullOrWhiteSpace(Remark) ? $"{Hostname}:{Port}" : Remark;

            if (Group.Equals("None") || Group.Equals(""))
                Group = "NONE";

            return $"[{ServerHelper.GetUtilByTypeName(Type)?.ShortName ?? "WTF"}][{Group}] {remark}";
        }

        public object Clone()
        {
            return MemberwiseClone();
        }

        /// <summary>
        ///		测试延迟
        /// </summary>
        /// <returns>延迟</returns>
        public int Test()
        {
            try
            {
                var destination = DNS.Lookup(Hostname);
                if (destination == null)
                {
                    return Delay = -2;
                }

                var list = new Task<int>[3];
                for (var i = 0; i < 3; i++)
                {
                    list[i] = Task.Run(async () =>
                    {
                        try
                        {
                            return Global.Settings.ServerTCPing ? await Utils.Utils.TCPingAsync(destination, Port) : await Utils.Utils.ICMPing(destination, Port);
                        }
                        catch (Exception)
                        {
                            return -4;
                        }
                    });
                }

                Task.WaitAll(list[0], list[1], list[2]);

                var min = Math.Min(list[0].Result, list[1].Result);
                min = Math.Min(min, list[2].Result);
                return Delay = min;
            }
            catch (Exception)
            {
                return Delay = -4;
            }
        }
    }

    public static class ServerExtension
    {
        public static string AutoResolveHostname(this Server server)
        {
            return Global.Settings.ResolveServerHostname ? DNS.Lookup(server.Hostname).ToString() : server.Hostname;
        }
    }
}