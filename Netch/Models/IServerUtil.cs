using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Netch.Models
{
    public interface IServerUtil
    {
        ushort Priority { get; }

        /// <summary>
        /// Server.Type
        /// </summary>
        string TypeName { get; }

        string FullName { get; }

        string[] UriScheme { get; }

        bool ParseUriScheme(string scheme);

        Server ParseJObject(JObject j);

        public void Edit(Server s);

        public void Create();

        string GetShareLink(Server server);

        public abstract ServerController GetController();

        /// <summary>
        ///     SSR链接解析器
        ///     Copy From https://github.com/HMBSbige/ShadowsocksR-Windows/blob/d9dc8d032a6e04c14b9dc6c8f673c9cc5aa9f607/shadowsocks-csharp/Model/Server.cs#L428
        ///     Thx :D
        /// </summary>
        /// <param name="ssrUrl"></param>
        /// <returns></returns>
        public abstract IEnumerable<Server> ParseUri(string ssrUrl);

        bool CheckServer(Server s);
    }
}