using Netch.Utils;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Netch.Models
{
    public class Server : ICloneable
    {
        /// <summary>
        ///     延迟
        /// </summary>
        [JsonIgnore]
        public int Delay { get; private set; } = -1;

        /// <summary>
        ///     组
        /// </summary>
        public string Group { get; set; } = "None";

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
        public virtual string Type { get; } = string.Empty;

        [JsonExtensionData]
        // ReSharper disable once CollectionNeverUpdated.Global
        public Dictionary<string, object> ExtensionData { get; set; } = new();

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

            if (Group.Equals("None") || Group.Equals(""))
                Group = "NONE";

            string shortName;
            if (Type == string.Empty)
            {
                shortName = "WTF";
            }
            else
            {
                shortName = ServerHelper.GetUtilByTypeName(Type).ShortName;
            }

            return $"[{shortName}][{Group}] {remark}";
        }

        /// <summary>
        ///     测试延迟
        /// </summary>
        /// <returns>延迟</returns>
        public int Test()
        {
            try
            {
                var destination = DnsUtils.Lookup(Hostname);
                if (destination == null)
                    return Delay = -2;

                var list = new Task<int>[3];
                for (var i = 0; i < 3; i++)
                    list[i] = Task.Run(async () =>
                    {
                        try
                        {
                            return Global.Settings.ServerTCPing
                                ? await Utils.Utils.TCPingAsync(destination, Port)
                                : Utils.Utils.ICMPing(destination, Port);
                        }
                        catch (Exception)
                        {
                            return -4;
                        }
                    });

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
            return Global.Settings.ResolveServerHostname ? DnsUtils.Lookup(server.Hostname)!.ToString() : server.Hostname;
        }

        public static bool Valid(this Server server)
        {
            try
            {
                ServerHelper.GetTypeByTypeName(server.Type);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}