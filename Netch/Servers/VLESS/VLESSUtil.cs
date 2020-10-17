using System.Collections.Generic;
using Netch.Controllers;
using Netch.Models;
using Newtonsoft.Json.Linq;

namespace Netch.Servers.VLESS
{
    public class VLESSUtil : IServerUtil
    {
        public ushort Priority { get; } = 2;
        public string TypeName { get; } = "VLESS";
        public string FullName { get; } = "VLESS";
        public string ShortName { get; } = "VL";
        public string[] UriScheme { get; } = { };

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

        public string GetShareLink(Server server)
        {
            // TODO
            return "";
        }

        public IServerController GetController()
        {
            return new VLESSController();
        }

        public IEnumerable<Server> ParseUri(string text)
        {
            throw new System.NotImplementedException();
        }

        public bool CheckServer(Server s)
        {
            // TODO
            return true;
        }
    }
}