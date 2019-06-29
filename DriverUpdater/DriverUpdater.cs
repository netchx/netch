using System;
using System.IO;
using System.ServiceProcess;

namespace DriverUpdater
{
    public class DriverUpdater
    {
        public static void Print(string text)
        {
            Console.WriteLine("[{0}] {1}", DateTime.Now.ToString(), text);
        }

        public static void Pause()
        {
            Print("请按任意键继续");
            Console.ReadKey();
        }

        public static void Main(string[] args)
        {
            try
            {
                var driver = $"{Environment.SystemDirectory}\\drivers\\netfilter2.sys";

                Print("检查已存在的驱动文件中");
                if (File.Exists(driver))
                {
                    // 停止服务
                    Print("正在停止服务中");

                    try
                    {
                        var service = new ServiceController("netfilter2");
                        if (service.Status == ServiceControllerStatus.Running)
                        {
                            service.Stop();
                            service.WaitForStatus(ServiceControllerStatus.Stopped);
                        }
                    }
                    catch (Exception)
                    {
                        // 跳过
                    }

                    // 取消注册驱动
                    Print("正在取消注册驱动中");
                    nfapinet.NFAPI.nf_unRegisterDriver("netfilter2");

                    // 生成系统版本
                    var version = $"{Environment.OSVersion.Version.Major.ToString()}.{Environment.OSVersion.Version.Minor.ToString()}";
                    Print($"当前系统版本：{version}");

                    // 复制新的驱动
                    Print("正在复制新的驱动中");
                    switch (version)
                    {
                        case "10.0":
                            File.Copy("bin\\Win-10.sys", driver, true);
                            Print("已复制 Win10 驱动");
                            break;
                        case "6.3":
                        case "6.2":
                            File.Copy("bin\\Win-8.sys", driver, true);
                            Print("已复制 Win8 驱动");
                            break;
                        case "6.1":
                        case "6.0":
                            File.Copy("bin\\Win-7.sys", driver, true);
                            Print("已复制 Win7 驱动");
                            break;
                        default:
                            Print($"不支持的系统版本：{version}");
                            Pause();
                            return;
                    }

                    // 注册新的驱动
                    Print("正在注册新的驱动中");
                    var result = nfapinet.NFAPI.nf_registerDriver("netfilter2");
                    if (result != nfapinet.NF_STATUS.NF_STATUS_SUCCESS)
                    {
                        Print($"注册驱动失败，返回值：{result}");
                        Pause();
                        return;
                    }

                    Print("驱动更新完毕");
                    Pause();
                    return;
                }

                Print("未检测到驱动文件");
                Pause();
            }
            catch (Exception e)
            {
                Print(e.ToString());
                Print("发生错误");
                Pause();
            }
        }
    }
}
