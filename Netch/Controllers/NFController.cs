using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Threading.Tasks;
using Netch.Interfaces;
using Netch.Interops;
using Netch.Models;
using Netch.Servers;
using Netch.Servers.Shadowsocks;
using Netch.Utils;
using Serilog;
using static Netch.Interops.Redirector;

namespace Netch.Controllers
{
    public class NFController : IModeController
    {
        private Server? _server;
        private Mode? _mode;
        private RedirectorConfig _rdrConfig = null!;

        private static readonly ServiceController NFService = new("netfilter2");

        private static readonly string SystemDriver = $"{Environment.SystemDirectory}\\drivers\\netfilter2.sys";

        public string Name => "Redirector";

        public async Task StartAsync(Server server, Mode mode)
        {
            _server = server;
            _mode = mode;
            _rdrConfig = Global.Settings.Redirector;
            CheckDriver();

            Dial(NameList.TYPE_FILTERLOOPBACK, "false");
            Dial(NameList.TYPE_FILTERICMP, "true");
            var p = PortHelper.GetAvailablePort();
            Dial(NameList.TYPE_TCPLISN, p.ToString());
            Dial(NameList.TYPE_UDPLISN, p.ToString());

            // Server
            Dial(NameList.TYPE_FILTERUDP, _rdrConfig.FilterProtocol.HasFlag(PortType.UDP).ToString().ToLower());
            Dial(NameList.TYPE_FILTERTCP, _rdrConfig.FilterProtocol.HasFlag(PortType.TCP).ToString().ToLower());
            await DialServerAsync(_rdrConfig.FilterProtocol, _server);

            // Mode Rule
            dial_Name(_mode);

            // Features
            Dial(NameList.TYPE_DNSHOST, _rdrConfig.DNSHijack ? _rdrConfig.DNSHijackHost : "");

            if (!await InitAsync())
                throw new MessageException("Redirector start failed.");
        }

        public async Task StopAsync()
        {
            await FreeAsync();
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
                    return Dial(NameList.TYPE_ADDNAME, r.Substring(1));

                return Dial(NameList.TYPE_ADDNAME, r);
            }
            finally
            {
                if (clear)
                    Dial(NameList.TYPE_CLRNAME, "");
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
            Dial(NameList.TYPE_CLRNAME, "");
            return !results.Any();
        }

        public static string GenerateInvalidRulesMessage(IEnumerable<string> rules)
        {
            return $"{string.Join("\n", rules)}\nAbove rules does not conform to C++ regular expression syntax";
        }

        #endregion

        private async Task DialServerAsync(PortType portType, Server server)
        {
            if (portType == PortType.Both)
            {
                await DialServerAsync(PortType.TCP, server);
                await DialServerAsync(PortType.UDP, server);
                return;
            }

            var offset = portType == PortType.UDP ? UdpNameListOffset : 0;

            if (server is Socks5 socks5)
            {
                Dial(NameList.TYPE_TCPTYPE + offset, "Socks5");
                Dial(NameList.TYPE_TCPHOST + offset, $"{await socks5.AutoResolveHostnameAsync()}:{socks5.Port}");
                Dial(NameList.TYPE_TCPUSER + offset, socks5.Username ?? string.Empty);
                Dial(NameList.TYPE_TCPPASS + offset, socks5.Password ?? string.Empty);
                Dial(NameList.TYPE_TCPMETH + offset, string.Empty);
            }
            else if (server is Shadowsocks shadowsocks && !shadowsocks.HasPlugin() && _rdrConfig.RedirectorSS)
            {
                Dial(NameList.TYPE_TCPTYPE + offset, "Shadowsocks");
                Dial(NameList.TYPE_TCPHOST + offset, $"{await shadowsocks.AutoResolveHostnameAsync()}:{shadowsocks.Port}");
                Dial(NameList.TYPE_TCPMETH + offset, shadowsocks.EncryptMethod);
                Dial(NameList.TYPE_TCPPASS + offset, shadowsocks.Password);
            }
            else
            {
                Trace.Assert(false);
            }
        }

        private void dial_Name(Mode mode)
        {
            Dial(NameList.TYPE_CLRNAME, "");
            var invalidList = new List<string>();
            foreach (var s in mode.GetRules())
            {
                if (s.StartsWith("!"))
                {
                    if (!Dial(NameList.TYPE_BYPNAME, s.Substring(1)))
                        invalidList.Add(s);

                    continue;
                }

                if (!Dial(NameList.TYPE_ADDNAME, s))
                    invalidList.Add(s);
            }

            if (invalidList.Any())
                throw new MessageException(GenerateInvalidRulesMessage(invalidList));

            Dial(NameList.TYPE_ADDNAME, @"NTT\.exe");
            Dial(NameList.TYPE_BYPNAME, "^" + Global.NetchDir.ToRegexString() + @"((?!NTT\.exe).)*$");
        }

        #region DriverUtil

        private static void CheckDriver()
        {
            var binFileVersion = Utils.Utils.GetFileVersion(Constants.NFDriver);
            var systemFileVersion = Utils.Utils.GetFileVersion(SystemDriver);

            Log.Information("内置驱动版本: {Name}", binFileVersion);
            Log.Information("系统驱动版本: {Name}", systemFileVersion);

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

            Log.Information("更新驱动");
            UninstallDriver();
            InstallDriver();
        }

        /// <summary>
        ///     安装 NF 驱动
        /// </summary>
        /// <returns>驱动是否安装成功</returns>
        private static void InstallDriver()
        {
            Log.Information("安装 NF 驱动");

            if (!File.Exists(Constants.NFDriver))
                throw new MessageException(i18N.Translate("builtin driver files missing, can't install NF driver"));

            try
            {
                File.Copy(Constants.NFDriver, SystemDriver);
            }
            catch (Exception e)
            {
                Log.Error(e, "驱动复制失败\n");
                throw new MessageException($"Copy NF driver file failed\n{e.Message}");
            }

            Global.MainForm.StatusText(i18N.Translate("Register driver"));
            // 注册驱动文件
            var result = NFAPI.nf_registerDriver("netfilter2");
            if (result == NF_STATUS.NF_STATUS_SUCCESS)
            {
                Log.Information("驱动安装成功");
            }
            else
            {
                Log.Error("注册驱动失败: {Result}", result);
                throw new MessageException($"Register NF driver failed\n{result}");
            }
        }

        /// <summary>
        ///     卸载 NF 驱动
        /// </summary>
        /// <returns>是否成功卸载</returns>
        public static bool UninstallDriver()
        {
            Log.Information("卸载 NF 驱动");
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