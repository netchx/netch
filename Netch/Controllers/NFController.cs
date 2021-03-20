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

        private const string BinDriver = "bin\\nfdriver.sys";
        private static readonly string SystemDriver = $"{Environment.SystemDirectory}\\drivers\\netfilter2.sys";

        public string Name { get; } = "Redirector";

        public void Start(in Mode mode)
        {
            CheckDriver();

            aio_dial((int) NameList.TYPE_FILTERLOOPBACK, "false");
            aio_dial((int) NameList.TYPE_TCPLISN, Global.Settings.RedirectorTCPPort.ToString());

            // Server
            aio_dial((int) NameList.TYPE_FILTERUDP, (Global.Settings.ProcessProxyProtocol != PortType.TCP).ToString().ToLower());
            aio_dial((int) NameList.TYPE_FILTERTCP, (Global.Settings.ProcessProxyProtocol != PortType.UDP).ToString().ToLower());
            dial_Server(Global.Settings.ProcessProxyProtocol);

            // Mode Rule
            dial_Name(mode);

            // Features
            aio_dial((int) NameList.TYPE_REDIRCTOR_DNS, Global.Settings.RedirectDNS ? Global.Settings.RedirectDNSAddr : "");
            aio_dial((int) NameList.TYPE_REDIRCTOR_ICMP, Global.Settings.RedirectICMP ? Global.Settings.RedirectICMPAddr : "");
            aio_dial((int) NameList.TYPE_FILTERCHILDPROC, Global.Settings.ChildProcessHandle.ToString().ToLower());

            if (!aio_init())
                throw new MessageException("Redirector Start failed, run Netch with \"-console\" argument");
        }

        public void Stop()
        {
            aio_free();
        }

        #region CheckRule

        /// <summary>
        /// </summary>
        /// <param name="r"></param>
        /// <param name="clear"></param>
        /// <returns>No Problem true</returns>
        private static bool CheckCppRegex(string r, bool clear = true)
        {
            try
            {
                if (r.StartsWith("!"))
                    return aio_dial((int) NameList.TYPE_ADDNAME, r.Substring(1));

                return aio_dial((int) NameList.TYPE_ADDNAME, r);
            }
            finally
            {
                if (clear)
                    aio_dial((int) NameList.TYPE_CLRNAME, "");
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="rules"></param>
        /// <param name="results"></param>
        /// <returns>No Problem true</returns>
        public static bool CheckRules(IEnumerable<string> rules, out IEnumerable<string> results)
        {
            results = rules.Where(r => !CheckCppRegex(r, false));
            aio_dial((int) NameList.TYPE_CLRNAME, "");
            return !results.Any();
        }

        public static string GenerateInvalidRulesMessage(IEnumerable<string> rules)
        {
            return $"{string.Join("\n", rules)}\nAbove rules does not conform to C++ regular expression syntax";
        }

        #endregion

        private void dial_Server(in PortType portType)
        {
            if (portType == PortType.Both)
            {
                dial_Server(PortType.TCP);
                dial_Server(PortType.UDP);
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
                aio_dial((int) NameList.TYPE_TCPTYPE + offset, "Socks5");
                aio_dial((int) NameList.TYPE_TCPHOST + offset, $"{socks5.AutoResolveHostname()}:{socks5.Port}");
                aio_dial((int) NameList.TYPE_TCPUSER + offset, socks5.Username ?? string.Empty);
                aio_dial((int) NameList.TYPE_TCPPASS + offset, socks5.Password ?? string.Empty);
                aio_dial((int) NameList.TYPE_TCPMETH + offset, string.Empty);
            }
            else if (server is Shadowsocks shadowsocks && !shadowsocks.HasPlugin() && Global.Settings.RedirectorSS)
            {
                aio_dial((int) NameList.TYPE_TCPTYPE + offset, "Shadowsocks");
                aio_dial((int) NameList.TYPE_TCPHOST + offset, $"{shadowsocks.AutoResolveHostname()}:{shadowsocks.Port}");
                aio_dial((int) NameList.TYPE_TCPMETH + offset, shadowsocks.EncryptMethod);
                aio_dial((int) NameList.TYPE_TCPPASS + offset, shadowsocks.Password);
            }
            else
            {
                aio_dial((int) NameList.TYPE_TCPTYPE + offset, "Socks5");
                aio_dial((int) NameList.TYPE_TCPHOST + offset, $"127.0.0.1:{controller.Socks5LocalPort()}");
                aio_dial((int) NameList.TYPE_TCPUSER + offset, string.Empty);
                aio_dial((int) NameList.TYPE_TCPPASS + offset, string.Empty);
                aio_dial((int) NameList.TYPE_TCPMETH + offset, string.Empty);
            }
        }

        private void dial_Name(Mode mode)
        {
            aio_dial((int) NameList.TYPE_CLRNAME, "");
            var list = new List<string>();
            foreach (var s in mode.FullRule)
            {
                if (s.StartsWith("!"))
                {
                    if (!aio_dial((int) NameList.TYPE_BYPNAME, s.Substring(1)))
                        list.Add(s);

                    continue;
                }

                if (!aio_dial((int) NameList.TYPE_ADDNAME, s))
                    list.Add(s);
            }

            if (list.Any())
                throw new MessageException(GenerateInvalidRulesMessage(list));

            aio_dial((int) NameList.TYPE_ADDNAME, @"NTT\.exe");
            aio_dial((int) NameList.TYPE_BYPNAME, "^" + Global.NetchDir.ToRegexString() + @"((?!NTT\.exe).)*$");
        }

        #region DriverUtil

        private static void CheckDriver()
        {
            var binFileVersion = Utils.Utils.GetFileVersion(BinDriver);
            var systemFileVersion = Utils.Utils.GetFileVersion(SystemDriver);

            Logging.Info("内置驱动版本: " + binFileVersion);
            Logging.Info("系统驱动版本: " + systemFileVersion);

            if (!File.Exists(SystemDriver))
            {
                // Install
                InstallDriver();
                return;
            }

            var reinstall = false;
            if (Version.TryParse(binFileVersion, out var binResult) && Version.TryParse(systemFileVersion, out var systemResult))
            {
                if (binResult.CompareTo(systemResult) > 0)
                    // Update
                    reinstall = true;
                else if (systemResult.Major != binResult.Major)
                    // Downgrade when Major version different (may have breaking changes)
                    reinstall = true;
            }
            else
            {
                // Parse File versionName to Version failed
                if (!systemFileVersion.Equals(binFileVersion))
                    // versionNames are different, Reinstall
                    reinstall = true;
            }

            if (!reinstall)
                return;

            Logging.Info("更新驱动");
            UninstallDriver();
            InstallDriver();
        }

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

        #region NativeMethods

        private const int UdpNameListOffset = (int) NameList.TYPE_UDPTYPE - (int) NameList.TYPE_TCPTYPE;

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
            TYPE_FILTERCHILDPROC, //子进程捕获

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
    }
}