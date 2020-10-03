using System.Collections.Generic;
using Netch.Controllers;
using Netch.Models;
using Netch.Servers.VMess.Form;
using Netch.Servers.VMess.Models;
using Netch.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Netch.Servers.VMess
{
    public class VMessUtil : IServerUtil
    {
        public ushort Priority { get; } = 2;
        public string TypeName { get; } = "VMess";
        public string FullName { get; } = "VMess";
        public string ShortName { get; } = "V2";
        public string[] UriScheme { get; } = {"vmess"};

        public Server ParseJObject(JObject j)
        {
            return j.ToObject<VMess>();
        }

        public void Edit(Server s)
        {
            new VMessForm((VMess) s).ShowDialog();
        }

        public void Create()
        {
            new VMessForm().ShowDialog();
        }

        public string GetShareLink(Server s)
        {
            var server = (VMess) s;

            var vmessJson = JsonConvert.SerializeObject(new
            {
                v = "2",
                ps = server.Remark,
                add = server.Hostname,
                port = server.Port,
                id = server.UserID,
                aid = server.AlterID,
                net = server.TransferProtocol,
                type = server.FakeType,
                host = server.Host,
                path = server.Path,
                tls = server.TLSSecure ? "tls" : ""
            });
            return "vmess://" + ShareLink.URLSafeBase64Encode(vmessJson);
        }

        public IServerController GetController()
        {
            return new VMessController();
        }

        public IEnumerable<Server> ParseUri(string text)
        {
            var data = new VMess();

            text = text.Substring(8);
            var vmess = JsonConvert.DeserializeObject<VMessJObject>(ShareLink.URLSafeBase64Decode(text));

            data.Remark = vmess.ps;
            data.Hostname = vmess.add;
            data.Port = vmess.port;
            data.UserID = vmess.id;
            data.AlterID = vmess.aid;
            data.TransferProtocol = vmess.net;
            data.FakeType = vmess.type;

            if (vmess.v == null || vmess.v == "1")
            {
                var info = vmess.host.Split(';');
                if (info.Length == 2)
                {
                    vmess.host = info[0];
                    vmess.path = info[1];
                }
            }

            if (data.TransferProtocol == "quic")
            {
                if (VMessGlobal.QUIC.Contains(vmess.host))
                {
                    data.QUICSecure = vmess.host;
                    data.QUICSecret = vmess.path;
                }
            }
            else
            {
                data.Host = vmess.host;
                data.Path = vmess.path;
            }

            data.TLSSecure = vmess.tls == "tls";

            if (vmess.mux == null)
            {
                data.UseMux = false;
            }
            else
            {
                if (vmess.mux.enabled is bool enabled)
                {
                    data.UseMux = enabled;
                }
                else if (vmess.mux.enabled is string muxEnabled)
                {
                    data.UseMux = muxEnabled == "true"; // 针对使用字符串当作布尔值的情况
                }
                else
                {
                    data.UseMux = false;
                }
            }

            data.EncryptMethod = "auto"; // V2Ray 加密方式不包括在链接中，主动添加一个
            return CheckServer(data) ? new[] {data} : null;
        }

        public bool CheckServer(Server s)
        {
            var server = (VMess) s;
            if (!VMessGlobal.TransferProtocols.Contains(server.TransferProtocol))
            {
                Logging.Error($"不支持的 VMess 传输协议：{server.TransferProtocol}");
                return false;
            }

            if (server.FakeType.Length != 0 && !VMessGlobal.FakeTypes.Contains(server.FakeType))
            {
                Logging.Error($"不支持的 VMess 伪装类型：{server.FakeType}");
                return false;
            }

            if (server.TransferProtocol == "quic")
            {
                if (!VMessGlobal.QUIC.Contains(server.QUICSecure))
                {
                    Logging.Error($"不支持的 VMess QUIC 加密方式：{server.QUICSecure}");
                    return false;
                }
            }

            return true;
        }
    }
}