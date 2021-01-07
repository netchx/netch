using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Netch.Models;
using Newtonsoft.Json.Linq;

namespace Netch.Utils
{
    public static class ServerHelper
    {
        public static readonly IEnumerable<IServerUtil> ServerUtils;

        static ServerHelper()
        {
            var serversUtilsTypes = Assembly.GetExecutingAssembly().GetExportedTypes().Where(type => type.GetInterfaces().Contains(typeof(IServerUtil)));
            ServerUtils = serversUtilsTypes.Select(t => (IServerUtil) Activator.CreateInstance(t)).OrderBy(util => util.Priority);
        }

        public static Server ParseJObject(JObject o)
        {
            var handle = GetUtilByTypeName((string) o["Type"]);
            if (handle == null)
            {
                Logging.Warning($"不支持的服务器类型: {o["Type"]}");
                return null;
            }

            return handle.ParseJObject(o);
        }

        public static IServerUtil GetUtilByTypeName(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return null;
            return ServerUtils.FirstOrDefault(i => (i.TypeName ?? "").Equals(typeName));
        }

        public static IServerUtil GetUtilByFullName(string fullName)
        {
            if (string.IsNullOrEmpty(fullName))
                return null;
            return ServerUtils.FirstOrDefault(i => (i.FullName ?? "").Equals(fullName));
        }

        public static IServerUtil GetUtilByUriScheme(string typeName)
        {
            return ServerUtils.FirstOrDefault(i => i.UriScheme.Any(s => s.Equals(typeName)));
        }
    }
}