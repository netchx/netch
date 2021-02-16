using System.Collections.Generic;
using Netch.Controllers;
using Netch.Models;
using Netch.Servers.V2ray;
using Newtonsoft.Json.Linq;

namespace Netch.Servers.VLESS
{
    public class VLESSUtil : IServerUtil
    {
        public ushort Priority { get; } = 2;
        public string TypeName { get; } = "VLESS";
        public string FullName { get; } = "VLESS";
        public string ShortName { get; } = "VL";
        public string[] UriScheme { get; } = {"vless"};

        public Server ParseJObject(in JObject j)
        {
            return j.ToObject<VLESS>();
        }

        public void Edit(Server s)
        {
            new VLESSForm.VLESSForm((VLESS) s).ShowDialog();
        }

        public void Create()
        {
            new VLESSForm.VLESSForm().ShowDialog();
        }

        public string GetShareLink(Server s)
        {
            return V2rayUtils.GetVShareLink(s, "vless");
        }

        public IServerController GetController()
        {
            return new V2rayController();
        }

        public IEnumerable<Server> ParseUri(string text)
        {
            return V2rayUtils.ParseVUri(text);
        }

        public bool CheckServer(Server s)
        {
            // TODO
            return true;
        }
    }
}