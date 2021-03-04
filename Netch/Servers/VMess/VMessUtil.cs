using System;
using System.Collections.Generic;
using System.Text.Encodings.Web;
using System.Text.Json;
using Netch.Controllers;
using Netch.Models;
using Netch.Servers.V2ray;
using Netch.Servers.V2ray.Models;
using Netch.Servers.VMess.Form;
using Netch.Utils;

namespace Netch.Servers.VMess
{
    public class VMessUtil : IServerUtil
    {
        public ushort Priority { get; } = 3;

        public string TypeName { get; } = "VMess";

        public string FullName { get; } = "VMess";

        public string ShortName { get; } = "V2";

        public string[] UriScheme { get; } = {"vmess"};

        public Type ServerType { get; } = typeof(VMess);

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
            if (Global.Settings.V2RayConfig.V2rayNShareLink)
            {
                var server = (VMess) s;

                var vmessJson = JsonSerializer.Serialize(new V2rayNSharing
                    {
                        v = "2",
                        ps = server.Remark,
                        add = server.Hostname,
                        port = server.Port.ToString(),
                        id = server.UserID,
                        aid = server.AlterID.ToString(),
                        net = server.TransferProtocol,
                        type = server.FakeType,
                        host = server.Host,
                        path = server.Path,
                        tls = server.TLSSecureType
                    },
                    new JsonSerializerOptions
                    {
                        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                    });

                return "vmess://" + ShareLink.URLSafeBase64Encode(vmessJson);
            }

            return V2rayUtils.GetVShareLink(s);
        }

        public IServerController GetController()
        {
            return new V2rayController();
        }

        public IEnumerable<Server> ParseUri(string text)
        {
            var data = new VMess();

            string s;
            try
            {
                s = ShareLink.URLSafeBase64Decode(text.Substring(8));
            }
            catch
            {
                return V2rayUtils.ParseVUri(text);
            }

            V2rayNSharing vmess = JsonSerializer.Deserialize<V2rayNSharing>(s)!;

            data.Remark = vmess.ps;
            data.Hostname = vmess.add;
            data.Port = ushort.Parse(vmess.port);
            data.UserID = vmess.id;
            data.AlterID = int.Parse(vmess.aid);
            data.TransferProtocol = vmess.net;
            data.FakeType = vmess.type;

            if (data.TransferProtocol == "quic")
            {
                if (VMessGlobal.QUIC.Contains(vmess.host!))
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

            data.TLSSecureType = vmess.tls;
            data.EncryptMethod = "auto"; // V2Ray 加密方式不包括在链接中，主动添加一个

            return new[] {data};
        }

        public bool CheckServer(Server s)
        {
            return true;
        }
    }
}