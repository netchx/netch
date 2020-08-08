using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Netch.Models;
using Netch.Utils;

namespace Netch.Controllers
{
    abstract partial class Controller
    {
        /// <summary>
        ///     主程序名
        /// </summary>
        public string MainFile { get; protected set; }

        /// <summary>
        ///     成功启动关键词
        /// </summary>
        protected readonly List<string> StartedKeywords = new List<string>();

        /// <summary>
        ///     启动失败关键词
        /// </summary>
        protected readonly List<string> StoppedKeywords = new List<string>();

        /// <summary>
        ///     进程是否可以重定向输出
        /// </summary>
        protected bool RedirectStd { get; set; } = true;

        /// <summary>
        ///     进程实例
        /// </summary>
        public Process Instance { get; private set; }

        /// <summary>
        ///     日志文件(重定向输出文件)
        /// </summary>
        private string _logPath;

        private FileStream _logFileStream;

        /// <summary>
        ///     程序输出的编码,
        ///     调用于基类的 <see cref="OnOutputDataReceived"/> 
        /// </summary>
        protected string InstanceOutputEncoding { get; set; } = "gbk";

        /// <summary>
        ///     停止进程
        /// </summary>
        protected void StopInstance()
        {
            try
            {
                if (Instance == null || Instance.HasExited) return;
                Instance.Kill();
                Instance.WaitForExit();
            }
            catch (Win32Exception e)
            {
                Logging.Error($"停止 {MainFile} 错误：\n" + e);
            }
            catch
            {
                // ignored
            }
        }

        /// <summary>
        ///     仅初始化 <see cref="Instance"/>,不设定事件处理方法
        /// </summary>
        /// <param name="argument"></param>
        protected void InitInstance(string argument)
        {
            Instance = new Process
            {
                StartInfo =
                {
                    FileName = Path.GetFullPath($"bin\\{MainFile}"),
                    WorkingDirectory = $"{Global.NetchDir}\\bin",
                    Arguments = argument,
                    CreateNoWindow = true,
                    RedirectStandardError = RedirectStd,
                    RedirectStandardInput = RedirectStd,
                    RedirectStandardOutput = RedirectStd,
                    UseShellExecute = !RedirectStd,
                    WindowStyle = ProcessWindowStyle.Hidden
                },
                EnableRaisingEvents = true
            };
        }


        /// <summary>
        ///     默认行为启动主程序
        /// </summary>
        /// <param name="argument">主程序启动参数</param>
        /// <param name="priority">进程优先级</param>
        /// <returns>是否成功启动</returns>
        protected bool StartInstanceAuto(string argument, ProcessPriorityClass priority = ProcessPriorityClass.Normal)
        {
            State = State.Starting;
            try
            {
                // 初始化程序
                InitInstance(argument);
                if (RedirectStd)
                {
                    // 清理日志
                    _logPath ??= Path.Combine(Global.NetchDir, $"logging\\{Name}.log");
                    if (_logFileStream == null && File.Exists(_logPath))
                        File.Delete(_logPath);
                    _logFileStream = new FileStream(_logPath, FileMode.Create, FileAccess.Write);

                    Instance.OutputDataReceived += OnOutputDataReceived;
                    Instance.ErrorDataReceived += OnOutputDataReceived;
                }

                Instance.Exited += OnExited;

                // 启动程序
                Instance.Start();
                if (priority != ProcessPriorityClass.Normal)
                    Instance.PriorityClass = priority;
                if (!RedirectStd || StartedKeywords.Count == 0) return true;
                // 启动日志重定向
                Instance.BeginOutputReadLine();
                Instance.BeginErrorReadLine();
                _writeStreamTimer.Elapsed += SaveStreamTimerEvent;
                _writeStreamTimer.Enabled = true;
                // 等待启动
                for (var i = 0; i < 1000; i++)
                {
                    Thread.Sleep(10);
                    switch (State)
                    {
                        case State.Started:
                            return true;
                        case State.Stopped:
                            Logging.Error($"{Name} 控制器启动失败");
                            Stop();
                            return false;
                    }
                }

                Logging.Error($"{Name} 控制器启动超时");
                Stop();
                return false;
            }
            catch (Exception e)
            {
                Logging.Error($"{Name} 控制器启动失败:\n {e}");
                return false;
            }
        }

        private static System.Timers.Timer _writeStreamTimer = new System.Timers.Timer(300) {AutoReset = true};

        private void OnExited(object sender, EventArgs e)
        {
            if (RedirectStd)
            {
                _writeStreamTimer.Enabled = false;
                Thread.Sleep(100); // 等待 Write() 写入流
                _logFileStream.Close();
                _logFileStream = null;
            }

            State = State.Stopped;
        }

        /// <summary>
        ///     接收输出数据
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <param name="e">数据</param>
        protected void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            // 程序结束, 接收到 null
            if (e.Data == null)
                return;

            var info = Encoding.GetEncoding(InstanceOutputEncoding).GetBytes(e.Data + Global.EOF);
            Task.Run(() => Write(info));
            var str = Encoding.UTF8.GetString(info);
            // 检查启动
            if (State == State.Starting)
            {
                if (StartedKeywords.Any(s => str.Contains(s)))
                    State = State.Started;
                else if (StoppedKeywords.Any(s => str.Contains(s)))
                    State = State.Stopped;
            }
        }

        /// <summary>
        ///     计时器存储日志     
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void SaveStreamTimerEvent(object sender, EventArgs e)
        {
            if (_logFileStream == null) return;
            try
            {
                await _logFileStream.FlushAsync();
            }
            catch
            {
                // ignored
            }
        }

        private readonly object _fileLocker = new object();

        /// <summary>
        ///     写入日志文件流
        /// </summary>
        /// <param name="info"></param>
        /// <returns>转码后的字符串</returns>
        private void Write(byte[] info)
        {
            if (info == null)
                return;

            try
            {
                lock (_fileLocker)
                {
                    _logFileStream.Write(info, 0, info.Length);
                }
            }
            catch (Exception e)
            {
                Logging.Error($"写入 {Name} 日志错误：\n" + e.Message);
            }
        }
    }
}