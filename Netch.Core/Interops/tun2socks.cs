using System.Diagnostics;
using System.Text;
using Netch.Utils;
using Windows.Win32;

namespace Netch.Interops
{
    public static class TUN2Socks
    {
        private static Process? _process;
        internal static Process? ActiveProcess => _process;

        public static bool Init(string interfaceName, string proxyHost, int proxyPort, string? username = null, string? password = null)
        {
            Log.Verbose("[tun2socks] init 开始");
            if (_process != null)
            {
                try
                {
                    if (!_process.HasExited)
                    {
                        Log.Warning("[tun2socks] 已经在运行了");
                        return false;
                    }
                }
                catch (InvalidOperationException)
                {
                    // Process.Start failed previously, so the Process object has no associated process.
                }

                _process.Dispose();
                _process = null;
            }

            Process? process = null;
            try
            {
                var processPath = Path.Combine(Global.NetchDir, Constants.TUN2SocksFile);
                Log.Verbose("[tun2socks] Path: {Path}", processPath);

                var info = BuildStartInfo(processPath, interfaceName, proxyHost, proxyPort, username, password);

                process = new Process()
                {
                    StartInfo = info,
                    EnableRaisingEvents = true
                };

                if (process.Start())
                {
                    Log.Verbose("[tun2socks] 启动成功，正在创建虚拟网卡。");
                }
                else
                {
                    Log.Error("[tun2socks] 启动失败。");
                    process.Dispose();
                    return false;
                }

                _process = process;
                process.StandardInput.AutoFlush = true;

                Global.Job.AddProcess(process);
                ProcessTuning.Boost(process, "tun2socks");

                // 可选：异步读取输出
                _ = Task.Run(async () => ReadOutputAsync(process));

                return true;
            }
            catch (Exception e)
            {
                Log.Error(e, $"[tun2socks] 尝试启动失败");
                if (process != null)
                {
                    try
                    {
                        if (!process.HasExited)
                            process.Kill();
                    }
                    catch
                    {
                        // The process may not have started or may have already exited.
                    }

                    process.Dispose();
                }

                _process = null;
                return false;
            }

        }

        /// <summary>
        /// 杀掉 tun2socks 进程
        /// </summary>
        /// <returns></returns>
        public static async Task<bool> FreeAsync()
        {
            bool isSuccess;
            try
            {
                if (_process != null)
                {
                    try
                    {
                        if (!_process.HasExited)
                        {
                            //SendCtrlC(_process);
                            _process.Kill();
                            await _process.WaitForExitAsync();
                            Log.Warning("[tun2socks] 已退出");
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        // Process.Start failed and there is no associated process to stop.
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "[tun2socks] FreeAsync 失败");
                isSuccess = false;
            }
            finally
            {
                // 释放进程资源
                _process?.Dispose();
                _process = null;
                isSuccess = true;
            }
            return isSuccess;
        }

        private const int CTRL_C_EVENT = 0;

        private static bool SendCtrlC(Process process)
        {
            if (process.HasExited) return true;

            var consoleWindowHandle = PInvoke.GetConsoleWindow();
            if (consoleWindowHandle == IntPtr.Zero)
            {
                Log.Error("输出类型为 Windows应用程序");
            }
            else
            {
                Log.Error("输出类型为 控制台应用程序");
            }

            var mainWindowHandle = Process.GetCurrentProcess().MainWindowHandle;
            if (consoleWindowHandle == mainWindowHandle)
            {
                Log.Error("输出类型为 控制台应用程序,主窗口也是控制台窗口");
            }

            // 先释放当前进程可能关联的控制台
            //FreeConsole();


            // 绑定到 tun2socks 的控制台
            if (!PInvoke.AttachConsole((uint)process.Id))
            {

                Log.Verbose("绑定失败");
                return false;
            }

            try
            {
                // 防止 Ctrl+C 把自己干掉
                PInvoke.SetConsoleCtrlHandler(null, true);

                // 0 = 当前控制台的整个进程组
                return PInvoke.GenerateConsoleCtrlEvent(CTRL_C_EVENT, 0);
            }
            finally
            {
                PInvoke.FreeConsole();
                PInvoke.SetConsoleCtrlHandler(null, false);
            }
        }

        public static ProcessStartInfo BuildStartInfo(string processPath, string interfaceName, string proxyHost, int proxyPort, string? username = null, string? password = null)
        {
            var info = new ProcessStartInfo(processPath)
            {
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                StandardErrorEncoding = Encoding.UTF8,
                StandardInputEncoding = Encoding.UTF8,
                StandardOutputEncoding = Encoding.UTF8
            };

            foreach (var argument in BuildArgs(interfaceName, proxyHost, proxyPort, username, password))
                info.ArgumentList.Add(argument);

            return info;
        }

        private static string[] BuildArgs(string interfaceName, string host, int port, string? username, string? password)
        {
            var proxy = new UriBuilder("socks5", host, port);
            if (!string.IsNullOrEmpty(username))
            {
                proxy.UserName = Uri.EscapeDataString(username);
                proxy.Password = Uri.EscapeDataString(password ?? "");
            }

            return ["--device", $"tun://{interfaceName}", "--proxy", proxy.Uri.AbsoluteUri,
                "--mtu", "1500", "--loglevel", "debug"];
        }

        private static async Task ReadOutputAsync(Process p)
        {
            string logPath = Path.Combine(Global.NetchDir, "logging", "tun2socks.log");
            await using var _logFileStream = new FileStream(logPath, FileMode.Create, FileAccess.Write, FileShare.Read, 4096, true);
            await using var _logStreamWriter = new StreamWriter(_logFileStream, Encoding.UTF8) { AutoFlush = true };

            Task ReadStreamAsync(StreamReader reader, Action<string> logAction)
            {
                var prefix = "tun2socks";
                return Task.Run(async () =>
                {
                    string? line;
                    try
                    {
                        while ((line = await reader.ReadLineAsync()) != null)
                        {
                            await _logStreamWriter.WriteLineAsync($"[{prefix}] {line}");
                            //logAction?.Invoke($"[{prefix}] {line}");
                        }
                    }
                    catch (Exception ex)
                    {
                        await _logStreamWriter.WriteLineAsync($"[{prefix}] Stream read exception: {ex}");
                        Log.Error(ex.ToString());
                    }
                });
            }
            var stdoutTask = ReadStreamAsync(p.StandardOutput, Log.Verbose);
            var stderrTask = ReadStreamAsync(p.StandardError, Log.Verbose);

            await Task.WhenAll(stdoutTask, stderrTask);

            await _logStreamWriter.WriteLineAsync("[tun2socks] Process exited.");

            await _logStreamWriter.DisposeAsync();
            await _logFileStream.DisposeAsync();
        }
    }
}
