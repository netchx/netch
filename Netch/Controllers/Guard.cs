using Netch.Models;
using Netch.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Timer = System.Timers.Timer;

namespace Netch.Controllers
{
    public abstract class Guard
    {
        private readonly Timer _flushFileStreamTimer = new(300) { AutoReset = true };

        private FileStream? _logFileStream;

        private StreamWriter? _logStreamWriter;
        private bool _redirectToFile = true;

        /// <summary>
        ///     日志文件(重定向输出文件)
        /// </summary>
        protected string LogPath => Path.Combine(Global.NetchDir, $"logging\\{Name}.log");

        /// <summary>
        ///     成功启动关键词
        /// </summary>
        protected virtual IEnumerable<string> StartedKeywords { get; set; } = new List<string>();

        /// <summary>
        ///     启动失败关键词
        /// </summary>
        protected virtual IEnumerable<string> StoppedKeywords { get; set; } = new List<string>();

        public abstract string Name { get; }

        /// <summary>
        ///     主程序名
        /// </summary>
        public abstract string MainFile { get; protected set; }

        protected State State { get; set; } = State.Waiting;

        /// <summary>
        ///     进程是否可以重定向输出
        /// </summary>
        protected bool RedirectStd { get; set; } = true;

        protected bool RedirectToFile
        {
            get => RedirectStd && _redirectToFile;
            set => _redirectToFile = value;
        }

        /// <summary>
        ///     进程实例
        /// </summary>
        public Process? Instance { get; private set; }

        /// <summary>
        ///     程序输出的编码,
        /// </summary>
        protected virtual Encoding? InstanceOutputEncoding { get; } = null;

        public abstract void Stop();

        /// <summary>
        ///     停止进程
        /// </summary>
        protected void StopInstance()
        {
            try
            {
                if (Instance == null || Instance.HasExited)
                    return;

                Instance.Kill();
                Instance.WaitForExit();
            }
            catch (Win32Exception e)
            {
                Global.Logger.Error($"停止 {MainFile} 错误：\n" + e);
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

            if (!File.Exists(Instance.StartInfo.FileName))
                throw new MessageException(i18N.Translate($"bin\\{MainFile} file not found!"));
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

            if (RedirectToFile)
                OpenLogFile();

            // 启动程序
            Instance!.Start();
            if (priority != ProcessPriorityClass.Normal)
                Instance.PriorityClass = priority;

            if (RedirectStd)
            {
                Task.Run(() => ReadOutput(Instance.StandardOutput));
                Task.Run(() => ReadOutput(Instance.StandardError));

                if (!StartedKeywords.Any())
                {
                    State = State.Started;
                    return;
                }
            }
            else
            {
                return;
            }

            // 等待启动
            for (var i = 0; i < 1000; i++)
            {
                Thread.Sleep(10);
                switch (State)
                {
                    case State.Started:
                        Task.Run(OnKeywordStarted);
                        return;
                    case State.Stopped:
                        Stop();
                        CloseLogFile();
                        OnKeywordStopped();
                        throw new MessageException($"{Name} 控制器启动失败");
                }
            }

            Stop();
            OnKeywordTimeout();
            throw new MessageException($"{Name} 控制器启动超时");
        }

        #region FileStream

        private void OpenLogFile()
        {
            if (!RedirectToFile)
                return;

            _logFileStream = File.Open(LogPath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
            _logStreamWriter = new StreamWriter(_logFileStream);

            _flushFileStreamTimer.Elapsed += FlushFileStreamTimerEvent;
            _flushFileStreamTimer.Enabled = true;
        }

        private void WriteLog(string line)
        {
            if (!RedirectToFile)
                return;

            _logStreamWriter!.WriteLine(line);
        }

        private void CloseLogFile()
        {
            if (!RedirectToFile)
                return;

            _flushFileStreamTimer.Enabled = false;
            _logStreamWriter?.Close();
            _logFileStream?.Close();
            _logStreamWriter = _logStreamWriter = null;
        }

        #endregion

        #region virtual

        protected virtual void OnReadNewLine(string line)
        {
        }

        protected virtual void OnKeywordStarted()
        {
        }

        protected virtual void OnKeywordStopped()
        {
            Utils.Utils.Open(LogPath);
        }

        protected virtual void OnKeywordTimeout()
        {
        }

        #endregion

        protected void ReadOutput(TextReader reader)
        {
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                WriteLog(line);
                OnReadNewLine(line);

                // State == State.Started if !StartedKeywords.Any() 
                if (State == State.Starting)
                {
                    if (StartedKeywords.Any(s => line.Contains(s)))
                        State = State.Started;
                    else if (StoppedKeywords.Any(s => line.Contains(s)))
                        State = State.Stopped;
                }
            }

            CloseLogFile();
            State = State.Stopped;
        }

        /// <summary>
        ///     计时器存储日志
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FlushFileStreamTimerEvent(object sender, EventArgs e)
        {
            try
            {
                _logStreamWriter!.Flush();
            }
            catch (Exception exception)
            {
                Global.Logger.Warning($"写入 {Name} 日志错误：\n" + exception.Message);
            }
        }
    }
}