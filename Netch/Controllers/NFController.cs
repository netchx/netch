using System;
using System.IO;
using System.ServiceProcess;

namespace Netch.Controllers
{
    public class NFController
    {
        /// <summary>
        ///		启动
        /// </summary>
        /// <param name="server">服务器</param>
        /// <param name="mode">模式</param>
        /// <returns>是否成功</returns>
        public bool Start(Objects.Server server, Objects.Mode mode)
        {
            if (!File.Exists("NetchCore.dll"))
            {
                Utils.Logging.Info("核心文件 NetchCore.dll 丢失");
                return false;
            }

            // 生成驱动文件路径
            var driver = String.Format("{0}\\drivers\\netfilter2.sys", Environment.SystemDirectory);

            // 检查驱动是否存在
            if (!File.Exists(driver))
            {
                // 生成系统版本
                var version = $"{Environment.OSVersion.Version.Major.ToString()}.{Environment.OSVersion.Version.Minor.ToString()}";

                // 检查系统版本并复制对应驱动
                try
                {
                    switch (version)
                    {
                        case "10.0":
                            File.Copy("bin\\Win-10.sys", driver);
                            Utils.Logging.Info("已复制 Win10 驱动");
                            break;
                        case "6.3":
                        case "6.2":
                            File.Copy("bin\\Win-8.sys", driver);
                            Utils.Logging.Info("已复制 Win8 驱动");
                            break;
                        case "6.1":
                        case "6.0":
                            File.Copy("bin\\Win-7.sys", driver);
                            Utils.Logging.Info("已复制 Win7 驱动");
                            break;
                        default:
                            Utils.Logging.Info($"不支持的系统版本：{version}");
                            return false;
                    }
                }
                catch (Exception e)
                {
                    Utils.Logging.Info("复制驱动文件失败");
                    Utils.Logging.Info(e.ToString());
                    return false;
                }

                // 注册驱动文件
                var result = nfapinet.NFAPI.nf_registerDriver("netfilter2");
                if (result != nfapinet.NF_STATUS.NF_STATUS_SUCCESS)
                {
                    Utils.Logging.Info($"注册驱动失败，返回值：{result}");
                    return false;
                }
            }

            try
            {
                var service = new ServiceController("netfilter2");
                if (service.Status == ServiceControllerStatus.Running)
                {
                    service.Stop();
                    service.WaitForStatus(ServiceControllerStatus.Stopped);
                }

                service.Start();
                service.WaitForStatus(ServiceControllerStatus.Running);
            }
            catch (Exception e)
            {
                Utils.Logging.Info(e.ToString());

                var result = nfapinet.NFAPI.nf_registerDriver("netfilter2");
                if (result != nfapinet.NF_STATUS.NF_STATUS_SUCCESS)
                {
                    Utils.Logging.Info($"注册驱动失败，返回值：{result}");
                    return false;
                }
            }

            // 初始化
            if (!Win32Native.srn_init())
            {
                Utils.Logging.Info("初始化失败");
                return false;
            }

            // 设置驱动名
            Win32Native.srn_addOption(Win32Native.OptionType.OT_DRIVER_NAME, "netfilter2");

            // 绕过 IPv4 环路地址
            Win32Native.srn_startRule();
            Win32Native.srn_addOption(Win32Native.OptionType.OT_REMOTE_ADDRESS, "127.0.0.0/8");
            Win32Native.srn_addOption(Win32Native.OptionType.OT_ACTION, "bypass");
            Win32Native.srn_endRule();

            // 绕过 IPv6 环路地址
            Win32Native.srn_startRule();
            Win32Native.srn_addOption(Win32Native.OptionType.OT_REMOTE_ADDRESS, "[::1]/128");
            Win32Native.srn_addOption(Win32Native.OptionType.OT_ACTION, "bypass");
            Win32Native.srn_endRule();

            // 设置需要劫持的进程
            foreach (var proc in mode.Rule)
            {
                Win32Native.srn_startRule();
                Win32Native.srn_addOption(Win32Native.OptionType.OT_PROCESS_NAME, proc);
                Win32Native.srn_addOption(Win32Native.OptionType.OT_PROXY_ADDRESS, "127.0.0.1:2801");

                if (!String.IsNullOrWhiteSpace(server.Username) && !String.IsNullOrWhiteSpace(server.Password))
                {
                    Win32Native.srn_addOption(Win32Native.OptionType.OT_PROXY_USER_NAME, server.Username);
                    Win32Native.srn_addOption(Win32Native.OptionType.OT_PROXY_PASSWORD, server.Password);
                }

                Win32Native.srn_endRule();
            }

            if (!Win32Native.srn_enable(1))
            {
                Win32Native.srn_free();
                return false;
            }

            return true;
        }

        /// <summary>
        ///		停止
        /// </summary>
        public void Stop()
        {
            try
            {
                Win32Native.srn_enable(0);
                Win32Native.srn_free();

                var service = new ServiceController("netfilter2");
                service.Stop();
                service.WaitForStatus(ServiceControllerStatus.Stopped);
            }
            catch (Exception e)
            {
                Utils.Logging.Info(e.ToString());
            }
        }
    }
}
