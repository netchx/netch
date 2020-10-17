using System;
using System.IO;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Threading.Tasks;
using Netch.Forms;
using Netch.Models;
using Netch.Servers.Socks5;
using Netch.Utils;
using nfapinet;

namespace Netch.Controllers
{
    public class NFController : IModeController
    {
        private static readonly ServiceController NFService = new ServiceController("netfilter2");

        private static readonly string BinDriver = string.Empty;
        private static readonly string SystemDriver = $"{Environment.SystemDirectory}\\drivers\\netfilter2.sys";
        private static string _sysDns;

        public string Name { get; } = "Redirector";

        static NFController()
        {
            string fileName;
            switch ($"{Environment.OSVersion.Version.Major}.{Environment.OSVersion.Version.Minor}")
            {
                case "10.0":
                    fileName = "Win-10.sys";
                    break;
                case "6.3":
                case "6.2":
                    fileName = "Win-8.sys";
                    break;
                case "6.1":
                case "6.0":
                    fileName = "Win-7.sys";
                    break;
                default:
                    Logging.Error($"不支持的系统版本：{Environment.OSVersion.Version}");
                    return;
            }

            BinDriver = "bin\\" + fileName;
        }

        public bool Start(Mode mode)
        {
            Logging.Info("内置驱动版本: " + Utils.Utils.GetFileVersion(BinDriver));
            if (Utils.Utils.GetFileVersion(SystemDriver) != Utils.Utils.GetFileVersion(BinDriver))
            {
                if (File.Exists(SystemDriver))
                {
                    Logging.Info("系统驱动版本: " + Utils.Utils.GetFileVersion(SystemDriver));
                    Logging.Info("更新驱动");
                    UninstallDriver();
                }

                if (!InstallDriver())
                    return false;
            }

            aio_dial((int) NameList.TYPE_FILTERLOOPBACK, "false");
            aio_dial((int) NameList.TYPE_FILTERTCP, "true");
            aio_dial((int) NameList.TYPE_FILTERUDP, "true");
            aio_dial((int) NameList.TYPE_TCPLISN, Global.Settings.RedirectorTCPPort.ToString());

            SetServer(MainController.ServerController, PortType.Both);

            SetName(mode);

            if (Global.Settings.ModifySystemDNS)
            {
                // 备份并替换系统 DNS
                _sysDns = DNS.OutboundDNS;
                DNS.OutboundDNS = "1.1.1.1,8.8.8.8";
            }

            return aio_init();
        }

        private void SetServer(IServerController controller, PortType portType)
        {
            if (portType == PortType.Both)
            {
                SetServer(controller, PortType.TCP);
                SetServer(controller, PortType.UDP);
                return;
            }

            var offset = portType == PortType.UDP ? UdpNameListOffset : 0;

            aio_dial((int) NameList.TYPE_TCPTYPE + offset, "Socks5");

            if (controller.Server is Socks5 socks5)
            {
                aio_dial((int) NameList.TYPE_TCPHOST + offset, $"{socks5.AutoResolveHostname()}:{socks5.Port}");
                aio_dial((int) NameList.TYPE_TCPUSER + offset, socks5.Username ?? string.Empty);
                aio_dial((int) NameList.TYPE_TCPPASS + offset, socks5.Password ?? string.Empty);
            }
            else
            {
                aio_dial((int) NameList.TYPE_TCPHOST + offset, $"127.0.0.1:{controller.Socks5LocalPort()}");
                aio_dial((int) NameList.TYPE_TCPUSER + offset, string.Empty);
                aio_dial((int) NameList.TYPE_TCPPASS + offset, string.Empty);
            }

            aio_dial((int) NameList.TYPE_TCPMETH + offset, string.Empty);
        }

        private void SetName(Mode mode)
        {
            aio_dial((int) NameList.TYPE_CLRNAME, "");
            foreach (var rule in mode.FullRule)
            {
                aio_dial((int) NameList.TYPE_ADDNAME, rule);
            }

            aio_dial((int) NameList.TYPE_ADDNAME, "NTT.exe");
        }

        public void Stop()
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

        private const int UdpNameListOffset = (int) NameList.TYPE_UDPTYPE - (int) NameList.TYPE_TCPTYPE;

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


        public enum NameList : int
        {
            TYPE_FILTERLOOPBACK,
            TYPE_FILTERTCP,
            TYPE_FILTERUDP,
            TYPE_TCPLISN,
            TYPE_TCPTYPE,
            TYPE_TCPHOST,
            TYPE_TCPUSER,
            TYPE_TCPPASS,
            TYPE_TCPMETH,
            TYPE_UDPTYPE,
            TYPE_UDPHOST,
            TYPE_UDPUSER,
            TYPE_UDPPASS,
            TYPE_UDPMETH,
            TYPE_ADDNAME,
            TYPE_BYPNAME,
            TYPE_CLRNAME
        }

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
            Global.MainForm.StatusText(i18N.TranslateFormat("Uninstalling {0}", "NF Service"));
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