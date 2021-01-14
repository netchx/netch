using System;
using System.Linq;
using System.Net;
using System.Windows.Forms;
using Netch.Controllers;
using Netch.Utils;

namespace Netch.Forms
{
    partial class MainForm
    {
        private void CheckUpdate()
        {
            UpdateChecker.NewVersionFound += (_, _) =>
            {
                NotifyTip($"{i18N.Translate(@"New version available", ": ")}{UpdateChecker.LatestVersionNumber}");
                NewVersionLabel.Visible = true;
            };
            UpdateChecker.Check(Global.Settings.CheckBetaUpdate);
        }

        private async void NewVersionLabel_Click(object sender, EventArgs e)
        {
            if (!UpdateChecker.LatestRelease.assets.Any())
            {
                Utils.Utils.Open(UpdateChecker.LatestVersionUrl);
                return;
            }

            if (MessageBoxX.Show(i18N.Translate("Download and install now?"), confirm: true) != DialogResult.OK)
                return;
            NotifyTip(i18N.Translate("Start downloading new version"));

            NewVersionLabel.Enabled = false;
            NewVersionLabel.Text = "...";
            try
            {
                void OnDownloadProgressChanged(object o1, DownloadProgressChangedEventArgs args)
                {
                    BeginInvoke(new Action(() => { NewVersionLabel.Text = $"{args.ProgressPercentage}%"; }));
                }

                await UpdateChecker.UpdateNetch(OnDownloadProgressChanged);
            }
            catch (Exception exception)
            {
                Logging.Error(exception.Message);
                NotifyTip(exception.Message);
            }
        }
    }
}