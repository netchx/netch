using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Netch.Tools
{
    public class Guard
    {
        /// <summary>
        ///     启动信息
        /// </summary>
        public ProcessStartInfo StartInfo;

        /// <summary>
        ///     标准模式
        /// </summary>
        public bool Standard = true;

        /// <summary>
        ///     判定启动的字符串
        /// </summary>
        public List<string> JudgmentStarted;

        /// <summary>
        ///     判定停止的字符串
        /// </summary>
        public List<string> JudgmentStopped;

        /// <summary>
        ///     自动重启
        /// </summary>
        public bool AutoRestart;

        /// <summary>
        ///     启动
        /// </summary>
        /// <returns></returns>
        public bool Create()
        {
            this.instance = new Process
            {
                StartInfo = this.StartInfo,
                EnableRaisingEvents = true
            };
            this.instance.StartInfo.RedirectStandardError = true;
            this.instance.StartInfo.RedirectStandardOutput = true;

            this.instance.Exited += this.OnExited;
            this.instance.ErrorDataReceived += this.OnOutputDataReceived;
            this.instance.OutputDataReceived += this.OnOutputDataReceived;

            this.Started = false;
            this.Starting = true;
            this.instance.Start();

            if (!this.Standard)
            {
                this.Started = true;
                this.instance.BeginErrorReadLine();
                this.instance.BeginOutputReadLine();

                return true;
            }

            this.instance.BeginErrorReadLine();
            this.instance.BeginOutputReadLine();

            for (var i = 0; i < 1000; i++)
            {
                Thread.Sleep(10);

                if (this.Started)
                {
                    return true;
                }

                if (!this.Starting)
                {
                    return false;
                }
            }

            this.Delete();
            return false;
        }

        /// <summary>
        ///     停止
        /// </summary>
        public void Delete()
        {
            this.AutoRestart = false;

            try
            {
                this.instance?.Kill();
                this.instance?.WaitForExit();
            }
            catch (Exception)
            {
                // ignore
            }
        }

        /// <summary>
        ///     进程
        /// </summary>
        private Process instance;

        /// <summary>
        ///     是否已启动
        /// </summary>
        private bool Started = false;

        /// <summary>
        ///     是否正在启动中
        /// </summary>
        private bool Starting = false;

        private void OnExited(object sender, EventArgs e)
        {
            if (this.Started && this.AutoRestart)
            {
                Thread.Sleep(200);

                this.Create();
            }
        }

        private void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (this.Starting)
            {
                if (this.instance.HasExited)
                {
                    this.Starting = false;
                }

                for (int i = 0; i < this.JudgmentStarted.Count; i++)
                {
                    if (e.Data.ToLower().Contains(this.JudgmentStarted[i]))
                    {
                        this.Started = true;
                        this.Starting = false;
                        return;
                    }
                }

                for (int i = 0; i < this.JudgmentStopped.Count; i++)
                {
                    if (e.Data.ToLower().Contains(this.JudgmentStopped[i]))
                    {
                        this.Starting = false;
                        return;
                    }
                }
            }

            Console.WriteLine($"[Netch][Tools.Guard] {e.Data}");
        }
    }
}
