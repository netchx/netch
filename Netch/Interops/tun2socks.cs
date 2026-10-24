using Microsoft.Win32;
using System.Diagnostics;
using System.Text;
using Windows.Win32;

namespace Netch.Interops
{
    public static class TUN2Socks
    {
        private static Process? _process;

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
                var args = BuildArgs(interfaceName, proxyHost, proxyPort, username, password);
                Log.Verbose($"[tun2socks] Args: {args}");
                var processPath = Path.Combine(Global.NetchDir, Constants.TUN2SocksFile);
                Log.Verbose($"[tun2socks] Path: {processPath}");


                ProcessStartInfo info = new ProcessStartInfo(processPath, args)
                {
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                    CreateNoWindow = false,
                    StandardErrorEncoding = Encoding.UTF8,
                    StandardInputEncoding = Encoding.UTF8,
                    StandardOutputEncoding = Encoding.UTF8
                };

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

        private const string ProfilesKeyPath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\NetworkList\Profiles";
        private const string UnmanagedKeyPath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\NetworkList\Signatures\Unmanaged";
        private const string interfaceGUID = "{20214AD5-1D95-4CC1-9233-D850D2C9CF9C}";
        private static string BuildArgs(string interfaceName, string host, int port, string? username, string? password)
        {
            CleanRegeditNetworkProfiles(ProfilesKeyPath, "Description", interfaceName);
            CleanRegeditNetworkProfiles(UnmanagedKeyPath, "Description", interfaceName);
            var sb = new StringBuilder();
            //todo:使用GUID偶尔报 Failed to setup adapter (problem code: 0x1F, ntstatus: 0xC0000035): 当文件已存在时，无法创建该文件。 (Code 0x000000B7)
            //FATAL   engine/engine.go:45     [ENGINE] failed to start: create tun: Error creating interface: Cannot create a file when that file already exists.
            //sb.Append($"-device tun://{interfaceName}?guid={interfaceGUID} ");
            sb.Append($"-device tun://{interfaceName} ");
            // SOCKS5
            sb.Append("-proxy socks5://");
            if (!string.IsNullOrEmpty(username))
            {
                sb.Append($"{username}:{password}@");
            }
            sb.Append($"{host}:{port} ");

            sb.Append("-mtu 1500 ");

            sb.Append("-loglevel debug ");

            return sb.ToString();
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

        #region 注册表操作
        /// <summary>
        /// 删除注册表指定路径的网络配置
        /// </summary>
        /// <param name="path">检查路径</param>
        /// <param name="profileKeyName">要匹配字段的 key</param>
        /// <param name="name">要匹配的字段</param>
        /// <returns></returns>
        private static int CleanRegeditNetworkProfiles(string path, string profileKeyName, string name)
        {
            int cleanedCount = 0;
            try
            {
                Log.Verbose($"正在扫描注册表路径: {path}");
                using (var profilesKey = Registry.LocalMachine.OpenSubKey(path, true))
                {
                    if (profilesKey == null)
                    {
                        Log.Error("注册表路径不存在，可能系统版本不支持。");
                        return 0;
                    }

                    // 获取所有配置的GUID（子键名称）
                    string[] profileGuids = profilesKey.GetSubKeyNames();
                    Log.Verbose($"找到 {profileGuids.Length} 个网络配置");

                    foreach (string guid in profileGuids)
                    {
                        using (var profileKey = profilesKey.OpenSubKey(guid, false))
                        {
                            if (profileKey == null) continue;

                            // 读取配置名称
                            var profileName = profileKey.GetValue(profileKeyName) as string;

                            if (string.IsNullOrEmpty(profileName) || profileName.Contains(name))
                            {
                                try
                                {
                                    // 关闭当前键，以便删除
                                    profileKey.Close();

                                    // 删除该配置
                                    profilesKey.DeleteSubKeyTree(guid);
                                    cleanedCount++;

                                    Log.Verbose($"已删除网络配置: {profileName ?? "未知"} ({guid})");
                                }
                                catch (Exception ex)
                                {
                                    Log.Error($"删除配置 {guid} 失败: {ex.Message}");
                                }
                            }
                        }
                    }
                }

                Log.Verbose($"清理完成，共删除 {cleanedCount} 个网络配置。");
                return cleanedCount;
            }
            catch (Exception ex)
            {
                Log.Error($"清理网络配置时发生错误: {ex.Message}");
                return cleanedCount;
            }
        }
        #endregion       
    }
}
