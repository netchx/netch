using System;
using System.Linq;
using Microsoft.Win32;

namespace Netch.Utils
{
    public static class AdapterUtils
    {
        public const string NETWORK_KEY = @"SYSTEM\CurrentControlSet\Control\Network\{4D36E972-E325-11CE-BFC1-08002BE10318}";
        public const string ADAPTER_KEY = @"SYSTEM\CurrentControlSet\Control\Class\{4D36E972-E325-11CE-BFC1-08002BE10318}";

        /// <summary>
        ///     获取 TUN/TAP 适配器名称
        /// </summary>
        /// <param name="componentId">适配器 ID</param>
        /// <returns>适配器名称</returns>
        public static string GetName(string componentId)
        {
            return Registry.LocalMachine.OpenSubKey($"{NETWORK_KEY}\\{componentId}\\Connection")?.GetValue("Name")?.ToString() ?? "";
        }

        public static string? GetAdapterId(params string[] componentIds)
        {
            try
            {
                var adaptersRegistry = Registry.LocalMachine.OpenSubKey(ADAPTER_KEY)!;

                foreach (var keyName in adaptersRegistry.GetSubKeyNames().Where(s => s is not ("Configuration" or "Properties")))
                {
                    var adapterRegistry = adaptersRegistry.OpenSubKey(keyName)!;
                    var componentId = adapterRegistry.GetValue("ComponentId")?.ToString();
                    if (componentId == null)
                        continue;

                    if (componentIds.Contains(componentId))
                        return (string) (adapterRegistry.GetValue("NetCfgInstanceId") ??
                                         throw new Exception("Tap adapter have no NetCfgInstanceId key"));
                }
            }
            catch (Exception e)
            {
                Logging.Warning(e.ToString());
            }

            return null;
        }
    }
}