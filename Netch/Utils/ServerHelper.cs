using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;
using Netch.Interfaces;
using Netch.Models;
using Timer = System.Timers.Timer;

namespace Netch.Utils
{
    public static class ServerHelper
    {
        static ServerHelper()
        {
            var serversUtilsTypes = Assembly.GetExecutingAssembly()
                .GetExportedTypes()
                .Where(type => type.GetInterfaces().Contains(typeof(IServerUtil)));

            ServerUtils = serversUtilsTypes.Select(t => (IServerUtil)Activator.CreateInstance(t)!).OrderBy(util => util.Priority);
        }

        public static Type GetTypeByTypeName(string typeName)
        {
            return ServerUtils.Single(i => i.TypeName.Equals(typeName)).ServerType;
        }

        #region Delay

        public static class DelayTestHelper
        {
            private static readonly Timer Timer;
            private static readonly object TestAllLock = new();

            private static readonly SemaphoreSlim SemaphoreSlim = new(1, 16);

            public static readonly NumberRange Range = new(0, int.MaxValue / 1000);

            static DelayTestHelper()
            {
                Timer = new Timer
                {
                    Interval = 10000,
                    AutoReset = true
                };

                Timer.Elapsed += (_, _) => TestAllDelayAsync().Forget();
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

            public static int Interval => (int)(Timer.Interval / 1000);

            private static bool ValueIsEnabled(int value)
            {
                return value != 0 && Range.InRange(value);
            }

            public static async Task TestAllDelayAsync()
            {
                if (!Monitor.TryEnter(TestAllLock))
                    return;

                try
                {
                    var tasks = Global.Settings.Server.Select(async s =>
                    {
                        await SemaphoreSlim.WaitAsync();
                        try
                        {
                            await s.PingAsync();
                        }
                        finally
                        {
                            SemaphoreSlim.Release();
                        }
                    });

                    await Task.WhenAll(tasks);
                }
                catch (Exception)
                {
                    // ignored
                }
                finally
                {
                    Monitor.Exit(TestAllLock);
                }
            }

            public static void UpdateInterval()
            {
                Timer.Stop();

                if (!ValueIsEnabled(Global.Settings.DetectionTick))
                    return;

                Timer.Interval = Global.Settings.DetectionTick * 1000;
                Timer.Start();

                TestAllDelayAsync().Forget();
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