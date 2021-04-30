using System;
using System.Collections.Generic;
using Netch.Models;

namespace Netch.Interfaces
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

        public Type ServerType { get; }

        public void Edit(Server s);

        public void Create();

        string GetShareLink(Server s);

        public IServerController GetController();

        public IEnumerable<Server> ParseUri(string text);

        bool CheckServer(Server s);
    }
}