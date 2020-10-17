using System.Collections.Generic;
using Netch.Controllers;
using Newtonsoft.Json.Linq;

namespace Netch.Models
{
    public interface IServerUtil
    {
        /// <summary>
        ///     Collection order basis
        /// </summary>
        ushort Priority { get; }

        /// <summary>
        ///     Server.Type
        /// </summary>
        string TypeName { get; }

        /// <summary>
        ///     Protocol Name
        /// </summary>
        string FullName { get; }

        string ShortName { get; }

        /// <summary>
        ///     Support URI
        /// </summary>
        string[] UriScheme { get; }

        Server ParseJObject(in JObject j);

        public void Edit(Server s);

        public void Create();

        string GetShareLink(Server s);

        public abstract IServerController GetController();

        public abstract IEnumerable<Server> ParseUri(string text);

        bool CheckServer(Server s);
    }
}