using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Timers;
using Netch.Models;

namespace Netch.Utils
{
    public static class ServerHelper
    {
        static ServerHelper()
        {
            var serversUtilsTypes = Assembly.GetExecutingAssembly()
                .GetExportedTypes()
                .Where(type => type.GetInterfaces().Contains(typeof(IServerUtil)));

            ServerUtils = serversUtilsTypes.Select(t => (IServerUtil) Activator.CreateInstance(t)).OrderBy(util => util.Priority);
        }

        public static Type GetTypeByTypeName(string typeName)
        {
            return ServerUtils.Single(i => i.TypeName.Equals(typeName)).ServerType;
        }

        #region Delay

        public static class DelayTestHelper
        {
            private static readonly Timer Timer;
            private static bool _mux;

            public static readonly Range Range = new(0, int.MaxValue / 1000);

            static DelayTestHelper()
            {
                Timer = new Timer
                {
                    Interval = 10000,
                    AutoReset = true
                };

                Timer.Elapsed += (_, _) => TestAllDelay();
            }

            public static bool Enabled
            {
                get => Timer.Enabled;
                set
                {
                    if (!ValueIsEnabled(Global.Settings.DetectionTick))
                        return;

                    Timer.Enabled = value;
                }
            }

            public static int Interval => (int) (Timer.Interval / 1000);

            private static bool ValueIsEnabled(int value)
            {
                return value != 0 && Range.InRange(value);
            }

            public static event EventHandler? TestDelayFinished;

            public static void TestAllDelay()
            {
                if (_mux)
                    return;

                try
                {
                    _mux = true;
                    Parallel.ForEach(Global.Settings.Server, new ParallelOptions {MaxDegreeOfParallelism = 16}, server => { server.Test(); });
                    _mux = false;
                    TestDelayFinished?.Invoke(null, new EventArgs());
                }
                catch (Exception)
                {
                    // ignored
                }
            }

            public static void UpdateInterval()
            {
                Timer.Stop();

                if (!ValueIsEnabled(Global.Settings.DetectionTick))
                    return;

                Timer.Interval = Global.Settings.DetectionTick * 1000;
                Task.Run(TestAllDelay);
                Timer.Start();
            }
        }

        #endregion

        #region Handler

        public static readonly IEnumerable<IServerUtil> ServerUtils;

        public static IServerUtil GetUtilByTypeName(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                throw new ArgumentNullException();

            return ServerUtils.Single(i => i.TypeName.Equals(typeName));
        }

        public static IServerUtil GetUtilByFullName(string fullName)
        {
            if (string.IsNullOrEmpty(fullName))
                throw new ArgumentNullException();

            return ServerUtils.Single(i => i.FullName.Equals(fullName));
        }

        public static IServerUtil? GetUtilByUriScheme(string typeName)
        {
            return ServerUtils.SingleOrDefault(i => i.UriScheme.Any(s => s.Equals(typeName)));
        }

        #endregion
    }
}