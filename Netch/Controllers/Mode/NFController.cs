using System;
using System.IO;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Threading.Tasks;
using Netch.Models;
using Netch.Utils;
using nfapinet;

namespace Netch.Controllers
{
    public class NFController : ModeController
    {
        private static readonly ServiceController NFService = new ServiceController("netfilter2");

        private static readonly string BinDriver = string.Empty;
        private static readonly string SystemDriver = $"{Environment.SystemDirectory}\\drivers\\netfilter2.sys";
        private static string _sysDns;

        static NFController()
        {
            switch ($"{Environment.OSVersion.Version.Major}.{Environment.OSVersion.Version.Minor}")
            {
                case "10.0":
                    BinDriver = "Win-10.sys";
                    break;
                case "6.3":
                case "6.2":
                    BinDriver = "Win-8.sys";
                    break;
                case "6.1":
                case "6.0":
                    BinDriver = "Win-7.sys";
                    break;
                default:
                    Logging.Error($"不支持的系统版本：{Environment.OSVersion.Version}");
                    return;
            }

            BinDriver = "bin\\" + BinDriver;
        }

        public NFController()
        {
            Name = "Redirector";
        }

        public override bool Start(Server server, Mode mode)
        {
            Logging.Info("内置驱动版本: " + Utils.Utils.FileVersion(BinDriver));
            if (Utils.Utils.FileVersion(SystemDriver) != Utils.Utils.FileVersion(BinDriver))
            {
                if (File.Exists(SystemDriver))
                {
                    Logging.Info("系统驱动版本: " + Utils.Utils.FileVersion(SystemDriver));
                    Logging.Info("更新驱动");
                    UninstallDriver();
                }

                if (!InstallDriver())
                    return false;
            }

            aio_dial((int) NameList.TYPE_CLRNAME, "");
            foreach (var rule in mode.Rule)
            {
                aio_dial((int) NameList.TYPE_ADDNAME, rule);
            }

            aio_dial((int) NameList.TYPE_ADDNAME, "NTT.exe");

            if (server.Type != "Socks5")
            {
                aio_dial((int) NameList.TYPE_TCPHOST, $"127.0.0.1:{Global.Settings.Socks5LocalPort}");
                aio_dial((int) NameList.TYPE_UDPHOST, $"127.0.0.1:{Global.Settings.Socks5LocalPort}");
            }
            else
            {
                var result = DNS.Lookup(server.Hostname);
                if (result == null)
                {
                    Logging.Info("无法解析服务器 IP 地址");
                    return false;
                }

                aio_dial((int) NameList.TYPE_TCPHOST, $"{result}:{server.Port}");
                aio_dial((int) NameList.TYPE_UDPHOST, $"{result}:{server.Port}");
            }

            if (Global.Settings.ModifySystemDNS)
            {
                // 备份并替换系统 DNS
                _sysDns = DNS.OutboundDNS;
                DNS.OutboundDNS = "1.1.1.1,8.8.8.8";
            }

            return aio_init();
        }

        public override void Stop()
        {
            Task.Run(() =>
            {
                if (Global.Settings.ModifySystemDNS)
                    //恢复系统DNS
                    DNS.OutboundDNS = _sysDns;
            });

            aio_free();
        }

        #region NativeMethods

        [DllImport("Redirector.bin", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool aio_dial(int name, [MarshalAs(UnmanagedType.LPWStr)] string value);

        [DllImport("Redirector.bin", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool aio_init();

        [DllImport("Redirector.bin", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool aio_free();

        [DllImport("Redirector.bin", CallingConvention = CallingConvention.Cdecl)]
        public static extern ulong aio_getUP();

        [DllImport("Redirector.bin", CallingConvention = CallingConvention.Cdecl)]
        public static extern ulong aio_getDL();

        #endregion

        #region Utils

        /// <summary>
        ///     安装 NF 驱动
        /// </summary>
        /// <returns>驱动是否安装成功</returns>
        public static bool InstallDriver()
        {
            Logging.Info("安装 NF 驱动");
            try
            {
                File.Copy(BinDriver, SystemDriver);
            }
            catch (Exception e)
            {
                Logging.Error("驱动复制失败\n" + e);
                return false;
            }

            Global.MainForm.StatusText(i18N.Translate("Register driver"));
            // 注册驱动文件
            var result = NFAPI.nf_registerDriver("netfilter2");
            if (result == NF_STATUS.NF_STATUS_SUCCESS)
            {
                Logging.Info("驱动安装成功");
            }
            else
            {
                Logging.Error($"注册驱动失败，返回值：{result}");
                return false;
            }

            return true;
        }

        /// <summary>
        ///     卸载 NF 驱动
        /// </summary>
        /// <returns>是否成功卸载</returns>
        public static bool UninstallDriver()
        {
            Global.MainForm.StatusText(i18N.Translate("Uninstalling NF Service"));
            Logging.Info("卸载 NF 驱动");
            try
            {
                if (NFService.Status == ServiceControllerStatus.Running)
                {
                    NFService.Stop();
                    NFService.WaitForStatus(ServiceControllerStatus.Stopped);
                }
            }
            catch (Exception)
            {
                // ignored
            }

            if (!File.Exists(SystemDriver)) return true;
            NFAPI.nf_unRegisterDriver("netfilter2");
            File.Delete(SystemDriver);

            return true;
        }

        #endregion
    }
}