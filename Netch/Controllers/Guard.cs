using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Netch.Models;
using Netch.Utils;
using Timer = System.Timers.Timer;

namespace Netch.Controllers
{
    public abstract class Guard
    {
        private static readonly Timer SaveBufferTimer = new(300) {AutoReset = true};

        private readonly StringBuilder _logBuffer = new();

        /// <summary>
        ///     日志文件(重定向输出文件)
        /// </summary>
        private string _logPath;

        /// <summary>
        ///     成功启动关键词
        /// </summary>
        protected virtual IEnumerable<string> StartedKeywords { get; } = null;

        /// <summary>
        ///     启动失败关键词
        /// </summary>
        protected virtual IEnumerable<string> StoppedKeywords { get; } = null;

        public virtual string Name { get; }

        /// <summary>
        ///     主程序名
        /// </summary>
        public virtual string MainFile { get; protected set; }

        protected State State { get; set; } = State.Waiting;

        /// <summary>
        ///     进程是否可以重定向输出
        /// </summary>
        protected bool RedirectStd { get; set; } = true;

        /// <summary>
        ///     进程实例
        /// </summary>
        public Process Instance { get; private set; }

        /// <summary>
        ///     程序输出的编码,
        ///     调用于基类的 <see cref="OnOutputDataReceived" />
        /// </summary>
        protected Encoding InstanceOutputEncoding { get; set; } = Encoding.GetEncoding("gbk");

        public abstract void Stop();

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
        ///     仅初始化 <see cref="Instance" />,不设定事件处理方法
        /// </summary>
        /// <param name="argument"></param>
        protected virtual void InitInstance(string argument)
        {
            Instance = new Process
            {
                StartInfo =
                {
                    FileName = Path.GetFullPath($"bin\\{MainFile}"),
                    WorkingDirectory = $"{Global.NetchDir}\\bin",
                    Arguments = argument,
                    CreateNoWindow = true,
                    UseShellExecute = !RedirectStd,
                    RedirectStandardOutput = RedirectStd,
                    StandardOutputEncoding = RedirectStd ? InstanceOutputEncoding : null,
                    RedirectStandardError = RedirectStd,
                    StandardErrorEncoding = RedirectStd ? InstanceOutputEncoding : null,
                    WindowStyle = ProcessWindowStyle.Hidden
                }
            };
        }

        /// <summary>
        ///     默认行为启动主程序
        /// </summary>
        /// <param name="argument">主程序启动参数</param>
        /// <param name="priority">进程优先级</param>
        /// <returns>是否成功启动</returns>
        protected void StartInstanceAuto(string argument, ProcessPriorityClass priority = ProcessPriorityClass.Normal)
        {
            State = State.Starting;
            // 初始化程序
            InitInstance(argument);
            Instance.EnableRaisingEvents = true;
            if (RedirectStd)
            {
                // 清理日志
                _logPath ??= Path.Combine(Global.NetchDir, $"logging\\{Name}.log");
                if (File.Exists(_logPath))
                    File.Delete(_logPath);

                Instance.OutputDataReceived += OnOutputDataReceived;
                Instance.ErrorDataReceived += OnOutputDataReceived;
            }

            Instance.Exited += OnExited;

            // 启动程序
            Instance.Start();
            if (priority != ProcessPriorityClass.Normal)
                Instance.PriorityClass = priority;
            if (!RedirectStd) return;
            // 启动日志重定向
            Instance.BeginOutputReadLine();
            Instance.BeginErrorReadLine();
            SaveBufferTimer.Elapsed += SaveBufferTimerEvent;
            SaveBufferTimer.Enabled = true;
            if (!(StartedKeywords?.Any() ?? false))
            {
                State = State.Started;
                return;
            }

            // 等待启动
            for (var i = 0; i < 1000; i++)
            {
                Thread.Sleep(10);
                switch (State)
                {
                    case State.Started:
                        return;
                    case State.Stopped:
                        Stop();
                        Utils.Utils.Open(_logPath);
                        throw new Exception($"{Name} 控制器启动失败");
                }
            }

            Stop();
            throw new Exception($"{Name} 控制器启动超时");
        }

        private void OnExited(object sender, EventArgs e)
        {
            if (RedirectStd)
                SaveBufferTimer.Enabled = false;

            SaveBufferTimerEvent(null, null);

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

            Write(e.Data);
            // 检查启动
            if (State == State.Starting)
            {
                if (StartedKeywords.Any(s => e.Data.Contains(s)))
                    State = State.Started;
                else if (StoppedKeywords.Any(s => e.Data.Contains(s)))
                    State = State.Stopped;
            }
        }

        /// <summary>
        ///     计时器存储日志
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveBufferTimerEvent(object sender, EventArgs e)
        {
            try
            {
                if (_logPath != null && _logBuffer != null)
                {
                    File.AppendAllText(_logPath, _logBuffer.ToString());
                    _logBuffer.Clear();
                }
            }
            catch (Exception exception)
            {
                Logging.Warning($"写入 {Name} 日志错误：\n" + exception.Message);
            }
        }

        /// <summary>
        ///     写入日志文件缓冲
        /// </summary>
        /// <param name="info"></param>
        /// <returns>转码后的字符串</returns>
        private void Write(string info)
        {
            _logBuffer.Append(info + Global.EOF);
        }
    }
}