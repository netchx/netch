using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;
using Netch.Controllers;
using Netch.Forms.Mode;
using Netch.Models;
using Netch.Utils;

namespace Netch.Forms
{
    partial class Dummy
    {
    }

    partial class MainForm
    {
        #region MenuStrip

        #region 服务器

        private void ImportServersFromClipboardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var texts = Clipboard.GetText();
            if (!string.IsNullOrWhiteSpace(texts))
            {
                var servers = ShareLink.ParseText(texts);
                Global.Settings.Server.AddRange(servers);
                NotifyTip(i18N.TranslateFormat("Import {0} server(s) form Clipboard", servers.Count));

                InitServer();
                Configuration.Save();
            }
        }

        private void AddServerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var s = ((ToolStripMenuItem) sender).Text;

            var start = s.IndexOf("[", StringComparison.Ordinal) + 1;
            var end = s.IndexOf("]", start, StringComparison.Ordinal);
            var result = s.Substring(start, end - start);

            Hide();
            ServerHelper.GetUtilByFullName(result).Create();

            InitServer();
            Configuration.Save();
            Show();
        }

        #endregion

        #region 模式

        private void CreateProcessModeToolStripButton_Click(object sender, EventArgs e)
        {
            Hide();
            new Process().ShowDialog();
            Show();
        }

        private void ReloadModesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Enabled = false;
            try
            {
                ModeHelper.Load();
                InitMode();
                NotifyTip(i18N.Translate("Modes have been reload"));
            }
            catch (Exception)
            {
                // ignored
            }
            finally
            {
                Enabled = true;
            }
        }

        #endregion

        #region 订阅

        private void ManageSubscribeLinksToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Hide();
            new SubscribeForm().ShowDialog();
            InitServer();
            Show();
        }

        private async void UpdateServersFromSubscribeLinksToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Global.Settings.UseProxyToUpdateSubscription = false;
            await UpdateServersFromSubscribe();
        }

        private async void UpdateServersFromSubscribeLinksWithProxyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Global.Settings.UseProxyToUpdateSubscription = true;
            await UpdateServersFromSubscribe(true);
        }

        private async Task UpdateServersFromSubscribe(bool useProxy = false)
        {
            void DisableItems(bool v)
            {
                MenuStrip.Enabled = ConfigurationGroupBox.Enabled = ProfileGroupBox.Enabled = ControlButton.Enabled = v;
            }

            if (useProxy && ServerComboBox.SelectedIndex == -1)
            {
                MessageBoxX.Show(i18N.Translate("Please select a server first"));
                return;
            }

            if (Global.Settings.SubscribeLink.Count <= 0)
            {
                MessageBoxX.Show(i18N.Translate("No subscription link"));
                return;
            }

            StatusText(i18N.Translate("Starting update subscription"));
            DisableItems(false);
            try
            {
                string proxyServer = null;
                if (useProxy)
                {
                    var mode = new Models.Mode
                    {
                        Remark = "ProxyUpdate",
                        Type = 5
                    };
                    await MainController.Start(ServerComboBox.SelectedItem as Server, mode);
                    proxyServer = $"http://127.0.0.1:{Global.Settings.HTTPLocalPort}";
                }

                await Subscription.UpdateServersAsync(proxyServer);

                InitServer();
                Configuration.Save();
                StatusText(i18N.Translate("Subscription updated"));
            }
            catch (Exception)
            {
                // ignored
            }
            finally
            {
                if (useProxy)
                    try
                    {
                        await MainController.Stop();
                    }
                    catch
                    {
                        // ignored
                    }

                DisableItems(true);
            }
        }

        #endregion

        #region 选项

        private void CheckForUpdatesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Task.Run(() =>
            {
                void OnNewVersionNotFound(object o, EventArgs args)
                {
                    _updater.NewVersionNotFound -= OnNewVersionNotFound;
                    NotifyTip(i18N.Translate("Already latest version"));
                }

                void OnNewVersionFoundFailed(object o, EventArgs args)
                {
                    _updater.NewVersionFoundFailed -= OnNewVersionFoundFailed;
                    NotifyTip(i18N.Translate("New version found failed"), info: false);
                }

                _updater.NewVersionNotFound += OnNewVersionNotFound;
                _updater.NewVersionFoundFailed += OnNewVersionFoundFailed;
                CheckUpdate();
            });
        }

        private void OpenDirectoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Utils.Utils.Open(".\\");
        }

        private async void CleanDNSCacheToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                await Task.Run(() =>
                {
                    NativeMethods.FlushDNSResolverCache();
                    DNS.Cache.Clear();
                });

                NotifyTip(i18N.Translate("DNS cache cleanup succeeded"));
            }
            catch (Exception)
            {
                // ignored
            }
            finally
            {
                StatusText();
            }
        }

        private void updateACLWithProxyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UpdateACL(true);
        }

        private void updateACLToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UpdateACL(false);
        }

        private async void UpdateACL(bool useProxy)
        {
            if (useProxy && ServerComboBox.SelectedIndex == -1)
            {
                MessageBoxX.Show(i18N.Translate("Please select a server first"));
                return;
            }

            Enabled = false;
            StatusText(i18N.TranslateFormat("Updating {0}", "ACL"));
            try
            {
                if (useProxy)
                {
                    var mode = new Models.Mode
                    {
                        Remark = "ProxyUpdate",
                        Type = 5
                    };
                    State = State.Starting;
                    await MainController.Start(ServerComboBox.SelectedItem as Server, mode);
                }

                var req = WebUtil.CreateRequest(Global.Settings.ACL);
                if (useProxy)
                    req.Proxy = new WebProxy($"http://127.0.0.1:{Global.Settings.HTTPLocalPort}");

                await WebUtil.DownloadFileAsync(req, Path.Combine(Global.NetchDir, "bin\\default.acl"));
                NotifyTip(i18N.Translate("ACL updated successfully"));
            }
            catch (Exception e)
            {
                NotifyTip(i18N.Translate("ACL update failed") + "\n" + e.Message, info: false);
                Logging.Error("更新 ACL 失败！" + e);
            }
            finally
            {
                if (useProxy)
                {
                    await MainController.Stop();
                    State = State.Stopped;
                }

                StatusText();
                Enabled = true;
            }
        }

        private async void updatePACToolStripMenuItem_Click(object sender, EventArgs eventArgs)
        {
            Enabled = false;

            StatusText(i18N.TranslateFormat("Updating {0}", "PAC"));
            try
            {
                var req = WebUtil.CreateRequest(Global.Settings.PAC);

                var pac = Path.Combine(Global.NetchDir, "bin\\pac.txt");

                await WebUtil.DownloadFileAsync(req, pac);

                NotifyTip(i18N.Translate("PAC updated successfully"));
            }
            catch (Exception e)
            {
                NotifyTip(i18N.Translate("PAC update failed") + "\n" + e.Message, info: false);
                Logging.Error("更新 PAC 失败！" + e);
            }
            finally
            {
                StatusText();
                Enabled = true;
            }
        }

        private async void UninstallServiceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Enabled = false;
            StatusText(i18N.TranslateFormat("Uninstalling {0}", "NF Service"));
            try
            {
                await Task.Run(() =>
                {
                    if (NFController.UninstallDriver())
                        NotifyTip(i18N.TranslateFormat("{0} has been uninstalled", "NF Service"));
                });
            }
            finally
            {
                StatusText();
                Enabled = true;
            }
        }

        private async void UninstallTapDriverToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Enabled = false;
            StatusText(i18N.TranslateFormat("Uninstalling {0}", "TUN/TAP driver"));
            try
            {
                await Task.Run(TUNTAP.deltapall);
                NotifyTip(i18N.TranslateFormat("{0} has been uninstalled", "TUN/TAP driver"));
            }
            catch (Exception exception)
            {
                Logging.Error($"卸载 TUN/TAP 适配器失败: {exception}");
            }
            finally
            {
                StatusText();
                Enabled = true;
            }
        }

        #endregion


        /// <summary>
        ///     菜单栏强制退出
        /// </summary>
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Exit(true);
        }

        private void VersionLabel_Click(object sender, EventArgs e)
        {
            Utils.Utils.Open($"https://github.com/{UpdateChecker.Owner}/{UpdateChecker.Repo}/releases");
        }

        private void AboutToolStripButton_Click(object sender, EventArgs e)
        {
            Hide();
            new AboutForm().ShowDialog();
            Show();
        }

        private void fAQToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Utils.Utils.Open("https://netch.org/#/docs/zh-CN/faq");
        }

        #endregion
    }
}