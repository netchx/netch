using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Threading.Tasks;
using Netch.Models;
using Netch.Servers.Shadowsocks;
using Netch.Servers.Socks5;
using Netch.Utils;
using nfapinet;

namespace Netch.Controllers
{
    public class NFController : IModeController
    {
        private static readonly ServiceController NFService = new("netfilter2");

        private static readonly string BinDriver;
        private static readonly string SystemDriver = $"{Environment.SystemDirectory}\\drivers\\netfilter2.sys";

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
                    throw new MessageException($"不支持的系统版本：{Environment.OSVersion.Version}");
            }

            BinDriver = "bin\\" + fileName;
        }

        public string Name { get; } = "Redirector";

        public void Start(in Mode mode)
        {
            CheckDriver();

            #region aio_dial

            aio_dial((int)NameList.TYPE_FILTERLOOPBACK, "false");
            aio_dial((int)NameList.TYPE_TCPLISN, Global.Settings.RedirectorTCPPort.ToString());

            aio_dial((int)NameList.TYPE_FILTERUDP, (Global.Settings.ProcessProxyProtocol != PortType.TCP).ToString().ToLower());
            aio_dial((int)NameList.TYPE_FILTERTCP, (Global.Settings.ProcessProxyProtocol != PortType.UDP).ToString().ToLower());
            SetServer(Global.Settings.ProcessProxyProtocol);

            if (!CheckRule(mode.FullRule, out var list))
                throw new MessageException($"\"{string.Join("", list.Select(s => s + "\n"))}\" does not conform to C++ regular expression syntax");

            SetName(mode);

            #endregion

            if (Global.Settings.RedirectDNS)
                aio_dial((int)NameList.TYPE_REDIRCTOR_DNS, Global.Settings.RedirectDNSAddr.ToString());

            if (Global.Settings.RedirectICMP)
                aio_dial((int)NameList.TYPE_REDIRCTOR_ICMP, Global.Settings.RedirectICMPAddr.ToString());

            aio_dial((int)NameList.TYPE_FILTERCHILDPROC, Global.Settings.ChildProcessHandle.ToString());

            if (!aio_init())
                throw new MessageException("Redirector Start failed, run Netch with \"-console\" argument");
        }

        public void Stop()
        {
            aio_free();
        }

        /// <summary>
        /// </summary>
        /// <param name="rules"></param>
        /// <param name="incompatibleRule"></param>
        /// <returns>No Problem true</returns>
        public static bool CheckRule(IEnumerable<string> rules, out IEnumerable<string> incompatibleRule)
        {
            incompatibleRule = rules.Where(r => !CheckCppRegex(r, false));
            aio_dial((int)NameList.TYPE_CLRNAME, "");
            return !incompatibleRule.Any();
        }

        /// <summary>
        /// </summary>
        /// <param name="r"></param>
        /// <param name="clear"></param>
        /// <returns>No Problem true</returns>
        public static bool CheckCppRegex(string r, bool clear = true)
        {
            try
            {
                if (r.StartsWith("!"))
                    return aio_dial((int)NameList.TYPE_ADDNAME, r.Substring(1));

                return aio_dial((int)NameList.TYPE_ADDNAME, r);
            }
            finally
            {
                if (clear)
                    aio_dial((int)NameList.TYPE_CLRNAME, "");
            }
        }

        private static void CheckDriver()
        {
            var binFileVersion = Utils.Utils.GetFileVersion(BinDriver);
            var systemFileVersion = Utils.Utils.GetFileVersion(SystemDriver);

            Logging.Info("内置驱动版本: " + binFileVersion);
            Logging.Info("系统驱动版本: " + systemFileVersion);

            if (!File.Exists(SystemDriver))
            {
                InstallDriver();
                return;
            }

            var reinstallFlag = false;
            if (Version.TryParse(binFileVersion, out var binResult) && Version.TryParse(systemFileVersion, out var systemResult))
            {
                if (binResult.CompareTo(systemResult) > 0)
                    // Bin greater than Installed
                    reinstallFlag = true;
                else if (systemResult.Major != binResult.Major)
                    // Installed greater than Bin but Major Version Difference (has breaking changes), do downgrade
                    reinstallFlag = true;
            }
            else
            {
                if (!systemFileVersion.Equals(binFileVersion))
                    reinstallFlag = true;
            }

            if (!reinstallFlag)
                return;

            Logging.Info("更新驱动");
            UninstallDriver();
            InstallDriver();
        }

        private void SetServer(in PortType portType)
        {
            if (portType == PortType.Both)
            {
                SetServer(PortType.TCP);
                SetServer(PortType.UDP);
                return;
            }

            int offset;
            Server server;
            IServerController controller;

            if (portType == PortType.UDP)
            {
                offset = UdpNameListOffset;
                server = MainController.UdpServer!;
                controller = MainController.UdpServerController!;
            }
            else
            {
                offset = 0;
                server = MainController.Server!;
                controller = MainController.ServerController!;
            }

            if (server is Socks5 socks5)
            {
                aio_dial((int)NameList.TYPE_TCPTYPE + offset, "Socks5");
                aio_dial((int)NameList.TYPE_TCPHOST + offset, $"{socks5.AutoResolveHostname()}:{socks5.Port}");
                aio_dial((int)NameList.TYPE_TCPUSER + offset, socks5.Username ?? string.Empty);
                aio_dial((int)NameList.TYPE_TCPPASS + offset, socks5.Password ?? string.Empty);
                aio_dial((int)NameList.TYPE_TCPMETH + offset, string.Empty);
            }
            else if (server is Shadowsocks shadowsocks && !shadowsocks.HasPlugin() && Global.Settings.RedirectorSS)
            {
                aio_dial((int)NameList.TYPE_TCPTYPE + offset, "Shadowsocks");
                aio_dial((int)NameList.TYPE_TCPHOST + offset, $"{shadowsocks.AutoResolveHostname()}:{shadowsocks.Port}");
                aio_dial((int)NameList.TYPE_TCPMETH + offset, shadowsocks.EncryptMethod ?? string.Empty);
                aio_dial((int)NameList.TYPE_TCPPASS + offset, shadowsocks.Password ?? string.Empty);
            }
            else
            {
                aio_dial((int)NameList.TYPE_TCPTYPE + offset, "Socks5");
                aio_dial((int)NameList.TYPE_TCPHOST + offset, $"127.0.0.1:{controller.Socks5LocalPort()}");
                aio_dial((int)NameList.TYPE_TCPUSER + offset, string.Empty);
                aio_dial((int)NameList.TYPE_TCPPASS + offset, string.Empty);
                aio_dial((int)NameList.TYPE_TCPMETH + offset, string.Empty);
            }
        }

        private void SetName(Mode mode)
        {
            aio_dial((int)NameList.TYPE_CLRNAME, "");
            foreach (var rule in mode.FullRule)
            {
                if (rule.StartsWith("!"))
                {
                    aio_dial((int)NameList.TYPE_BYPNAME, rule.Substring(1));
                    continue;
                }

                aio_dial((int)NameList.TYPE_ADDNAME, rule);
            }

            aio_dial((int)NameList.TYPE_ADDNAME, @"NTT\.exe");
            aio_dial((int)NameList.TYPE_BYPNAME, "^" + Global.NetchDir.ToRegexString() + @"((?!NTT\.exe).)*$");
        }

        #region NativeMethods

        private const int UdpNameListOffset = (int)NameList.TYPE_UDPTYPE - (int)NameList.TYPE_TCPTYPE;

        [DllImport("Redirector.bin", CallingConvention = CallingConvention.Cdecl)]
        private static extern bool aio_dial(int name, [MarshalAs(UnmanagedType.LPWStr)] string value);

        [DllImport("Redirector.bin", CallingConvention = CallingConvention.Cdecl)]
        private static extern bool aio_init();

        [DllImport("Redirector.bin", CallingConvention = CallingConvention.Cdecl)]
        private static extern bool aio_free();

        [DllImport("Redirector.bin", CallingConvention = CallingConvention.Cdecl)]
        private static extern ulong aio_getUP();

        [DllImport("Redirector.bin", CallingConvention = CallingConvention.Cdecl)]
        private static extern ulong aio_getDL();

        public enum NameList
        {
            //bool
            TYPE_FILTERLOOPBACK,
            TYPE_FILTERTCP,
            TYPE_FILTERUDP,
            TYPE_FILTERIP,
            TYPE_FILTERCHILDPROC,//子进程捕获

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
            TYPE_ADDFIP,

            TYPE_BYPNAME,

            TYPE_CLRNAME,
            TYPE_CLRFIP,

            //str addr x.x.x.x only ipv4
            TYPE_REDIRCTOR_DNS,
            TYPE_REDIRCTOR_ICMP
        }

        #endregion

        #region Utils

        /// <summary>
        ///     安装 NF 驱动
        /// </summary>
        /// <returns>驱动是否安装成功</returns>
        public static void InstallDriver()
        {
            Logging.Info("安装 NF 驱动");

            if (!File.Exists(BinDriver))
                throw new MessageException(i18N.Translate("builtin driver files missing, can't install NF driver"));

            try
            {
                File.Copy(BinDriver, SystemDriver);
            }
            catch (Exception e)
            {
                Logging.Error("驱动复制失败\n" + e);
                throw new MessageException($"Copy NF driver file failed\n{e.Message}");
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
                throw new MessageException($"Register NF driver failed\n{result}");
            }
        }

        /// <summary>
        ///     卸载 NF 驱动
        /// </summary>
        /// <returns>是否成功卸载</returns>
        public static bool UninstallDriver()
        {
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

            if (!File.Exists(SystemDriver))
                return true;

            NFAPI.nf_unRegisterDriver("netfilter2");
            File.Delete(SystemDriver);

            return true;
        }

        #endregion
    }
}