using System;
using System.Diagnostics;
using System.IO;
using Netch.Models;
using Netch.Utils;

namespace Netch.Controllers
{
    public class Controller
    {
        protected string[] ExtFiles;

        /// <summary>
        ///     进程实例
        /// </summary>
        public Process Instance;

        /// <summary>
        ///     程序名
        /// </summary>
        public string MainName;

        public bool ready;

        /// <summary>
        ///     当前状态
        /// </summary>
        protected State State = State.Waiting;

        /// <summary>
        ///     停止
        /// </summary>
        public void Stop()
        {
            try
            {
                if (Instance == null || Instance.HasExited) return;
                Instance.Kill();
                Instance.WaitForExit();
            }
            catch (Exception e)
            {
                Logging.Info(e.ToString());
            }
        }


        /// <summary>
        ///     杀残留进程，清除日志，检查文件
        /// </summary>
        /// <returns></returns>
        protected bool BeforeStartProgress()
        {
            var result = false;
            // 杀残留
            MainController.KillProcessByName(MainName);
            // 清日志
            try
            {
                if (File.Exists($"logging\\{MainName}.log")) File.Delete($"logging\\{MainName}.log");
            }
            catch (Exception)
            {
                // ignore
            }

            // 检查文件
            if (!File.Exists($"bin\\{MainName}.exe")) Logging.Info($"bin\\{MainName}.exe 文件不存在");

            if (ExtFiles == null)
                result = true;
            else
                foreach (var f in ExtFiles)
                    if (!File.Exists($"bin\\{f}"))
                        Logging.Info($"bin\\{f}.exe 文件不存在");

            if (!ready) Logging.Info(MainName + "未能就绪");
            return result;
        }

        /// <summary>
        ///     写日志
        /// </summary>
        /// <param name="e"></param>
        /// <returns>e是否为空</returns>
        public bool WriteLog(DataReceivedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(e.Data)) return false;
            try
            {
                File.AppendAllText($"logging\\{MainName}.log", $@"{e.Data}{Global.EOF}");
            }
            catch (Exception exception)
            {
                Logging.Info($"写入{MainName}日志失败" + exception);
            }

            return true;
        }
    }
}