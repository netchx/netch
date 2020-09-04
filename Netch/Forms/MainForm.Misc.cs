using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Windows.Forms;
using Netch.Controllers;
using Netch.Forms.Mode;
using Netch.Utils;
using Process = System.Diagnostics.Process;

namespace Netch.Forms
{
    /// <summary lang="en">
    /// this class is used to disable Designer <para />
    /// </summary>
    /// <summary lang="zh">
    /// 此类用于禁用设计器
    /// </summary>
    [DesignerCategory("")]
    public partial class Dummy
    {
    }

    partial class MainForm
    {
        private readonly UpdateChecker _updater = new UpdateChecker();

        private void CheckUpdate()
        {
            _updater.NewVersionFound += (o, args) =>
            {
                NotifyTip($"{i18N.Translate(@"New version available", ": ")}{_updater.LatestVersionNumber}");
                NewVersionLabel.Visible = true;
            };
            _updater.Check(Global.Settings.CheckBetaUpdate);
        }

        private async void NewVersionLabel_Click(object sender, EventArgs e)
        {
            if (!_updater.LatestVersionDownloadUrl.Contains("Netch"))
            {
                Utils.Utils.Open(_updater.LatestVersionUrl);
                return;
            }

            if (MessageBoxX.Show(i18N.Translate("Download and install now?"), confirm: true) != DialogResult.OK)
                return;
            NotifyTip(i18N.Translate("Start downloading new version"));
            var fileName = Path.GetFileName(new Uri(_updater.LatestVersionDownloadUrl).LocalPath);
            fileName = fileName.Insert(fileName.LastIndexOf('.'), _updater.LatestVersionNumber);
            var fileFullPath = Path.Combine(Global.NetchDir, "data", fileName);

            try
            {
                if (!File.Exists(fileFullPath))
                {
                    await WebUtil.DownloadFileAsync(WebUtil.CreateRequest(_updater.LatestVersionDownloadUrl), fileFullPath);
                }

                RunUpdater();
            }
            catch (Exception exception)
            {
                NotifyTip($"{i18N.Translate("Download update failed")}\n{exception.Message}");
                Logging.Error($"下载更新失败 {exception}");
            }

            void RunUpdater()
            {
                // if debugging process stopped, debugger will kill child processes!!!!
                // 调试进程结束,调试器将会杀死子进程
                // uncomment if(!Debugger.isAttach) block in NetchUpdater Project's main() method and attach to NetchUpdater process to debug
                // 在 NetchUpdater 项目的  main() 方法中取消注释 if（!Debugger.isAttach）块，并附加到 NetchUpdater 进程进行调试
                Process.Start(new ProcessStartInfo
                {
                    FileName = Path.Combine(Global.NetchDir, "NetchUpdater.exe"),
                    Arguments =
                        $"{Global.Settings.UDPSocketPort} {fileFullPath} {Global.NetchDir}"
                });
            }
        }
    }
}