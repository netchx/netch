using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.Win32;
using Netch.Controllers;

namespace Netch.Utils
{
    public static class TUNTAP
    {
        public static string TUNTAP_COMPONENT_ID_0901 = "tap0901";
        public static string TUNTAP_COMPONENT_ID_0801 = "tap0801";
        public static string NETWORK_KEY = @"SYSTEM\CurrentControlSet\Control\Network\{4D36E972-E325-11CE-BFC1-08002BE10318}";
        public static string ADAPTER_KEY = @"SYSTEM\CurrentControlSet\Control\Class\{4D36E972-E325-11CE-BFC1-08002BE10318}";

        /// <summary>
        ///     获取 TUN/TAP 适配器 ID
        /// </summary>
        /// <returns>适配器 ID</returns>
        public static string? GetComponentID()
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

                    if (componentId == TUNTAP_COMPONENT_ID_0901 || componentId == TUNTAP_COMPONENT_ID_0801)
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

        /// <summary>
        ///     获取 TUN/TAP 适配器名称
        /// </summary>
        /// <param name="componentId">适配器 ID</param>
        /// <returns>适配器名称</returns>
        public static string GetName(string componentId)
        {
            var registry = Registry.LocalMachine.OpenSubKey($"{NETWORK_KEY}\\{componentId}\\Connection");

            return registry.GetValue("Name", "").ToString();
        }

        /// <summary>
        ///     创建 TUN/TAP 适配器
        /// </summary>
        /// <returns></returns>
        public static bool Create()
        {
            return false;
        }

        /// <summary>
        ///     卸载tap网卡
        /// </summary>
        public static void deltapall()
        {
            Logging.Info("卸载 TUN/TAP 适配器");
            var installProcess = new Process
                {StartInfo = {WindowStyle = ProcessWindowStyle.Hidden, FileName = Path.Combine("bin/tap-driver", "deltapall.bat")}};

            installProcess.Start();
            installProcess.WaitForExit();
            installProcess.Close();
        }

        /// <summary>
        ///     安装tap网卡
        /// </summary>
        public static void AddTap()
        {
            Logging.Info("安装 TUN/TAP 适配器");
            //安装Tap Driver
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = Path.Combine("bin/tap-driver", "addtap.bat"),
                WindowStyle = ProcessWindowStyle.Hidden
            })!;

            process.WaitForExit();

            Thread.Sleep(1000);
            if (GetComponentID() == null)
                throw new MessageException("TAP 驱动安装失败，找不到 ComponentID 注册表项");
        }
    }
}