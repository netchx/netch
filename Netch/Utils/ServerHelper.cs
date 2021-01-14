using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Timers;
using Netch.Models;
using Newtonsoft.Json.Linq;

namespace Netch.Utils
{
    public static class ServerHelper
    {
        static ServerHelper()
        {
            var serversUtilsTypes = Assembly.GetExecutingAssembly().GetExportedTypes().Where(type => type.GetInterfaces().Contains(typeof(IServerUtil)));
            ServerUtils = serversUtilsTypes.Select(t => (IServerUtil) Activator.CreateInstance(t)).OrderBy(util => util.Priority);

            Timer = new Timer
            {
                Interval = 10000,
                AutoReset = true,
                Enabled = false
            };

            Timer.Elapsed += (_, _) => TestAllDelay();
            Timer.Start();
        }

        #region Handler

        public static readonly IEnumerable<IServerUtil> ServerUtils;


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

        #endregion

        #region Delay

        public static readonly Timer Timer;

        private static bool _mux;
        public static event EventHandler TestDelayFinished;

        public static void TestAllDelay()
        {
            if (_mux)
                return;
            try
            {
                _mux = true;
                Parallel.ForEach(Global.Settings.Server, new ParallelOptions {MaxDegreeOfParallelism = 16},
                    server => { server.Test(); });
                _mux = false;
                TestDelayFinished?.Invoke(null, new EventArgs());
            }
            catch (Exception)
            {
                // ignored
            }
        }

        #endregion
    }
}