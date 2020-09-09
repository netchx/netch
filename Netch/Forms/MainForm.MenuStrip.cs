using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;
using Netch.Controllers;
using Netch.Forms.Mode;
using Netch.Forms.Server;
using Netch.Models;
using Netch.Utils;
using Trojan = Netch.Forms.Server.Trojan;
using VMess = Netch.Forms.Server.VMess;

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
                var result = ShareLink.Parse(texts);

                if (result != null)
                {
                    foreach (var server in result)
                    {
                        Global.Settings.Server.Add(server);
                    }
                }
                else
                {
                    MessageBoxX.Show(i18N.Translate("Import servers error!"), LogLevel.ERROR);
                }

                InitServer();
                Configuration.Save();
            }
        }

        private void AddServerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form form = ((ToolStripMenuItem) sender).Name switch
            {
                "AddSocks5ServerToolStripMenuItem" => new Socks5(),
                "AddShadowsocksServerToolStripMenuItem" => new Shadowsocks(),
                "AddShadowsocksRServerToolStripMenuItem" => new ShadowsocksR(),
                "AddVMessServerToolStripMenuItem" => new VMess(),
                "AddTrojanServerToolStripMenuItem" => new Trojan(),
                _ => null
            };

            Hide();
            form?.ShowDialog();
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
                Modes.Load();
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
            await UpdateServersFromSubscribe();
        }

        public async Task UpdateServersFromSubscribe()
        {
            void DisableItems(bool v)
            {
                MenuStrip.Enabled = ConfigurationGroupBox.Enabled = ProfileGroupBox.Enabled = ControlButton.Enabled = v;
            }

            if (Global.Settings.UseProxyToUpdateSubscription && ServerComboBox.SelectedIndex == -1)
                Global.Settings.UseProxyToUpdateSubscription = false;

            if (Global.Settings.UseProxyToUpdateSubscription && ServerComboBox.SelectedIndex == -1)
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
                if (Global.Settings.UseProxyToUpdateSubscription)
                {
                    var mode = new Models.Mode
                    {
                        Remark = "ProxyUpdate",
                        Type = 5
                    };
                    await MainController.Start(ServerComboBox.SelectedItem as Models.Server, mode);
                }

                var serverLock = new object();

                await Task.WhenAll(Global.Settings.SubscribeLink.Select(async item => await Task.Run(async () =>
                {
                    try
                    {
                        var request = WebUtil.CreateRequest(item.Link);

                        if (!string.IsNullOrEmpty(item.UserAgent)) request.UserAgent = item.UserAgent;
                        if (Global.Settings.UseProxyToUpdateSubscription)
                            request.Proxy = new WebProxy($"http://127.0.0.1:{Global.Settings.HTTPLocalPort}");

                        var str = await WebUtil.DownloadStringAsync(request);

                        try
                        {
                            str = ShareLink.URLSafeBase64Decode(str);
                        }
                        catch
                        {
                            // ignored
                        }

                        lock (serverLock)
                        {
                            Global.Settings.Server.RemoveAll(server => server.Group == item.Remark);

                            var result = ShareLink.Parse(str);
                            if (result != null)
                            {
                                foreach (var server in result)
                                {
                                    server.Group = item.Remark;
                                    Global.Settings.Server.Add(server);
                                }
                            }

                            NotifyTip(i18N.TranslateFormat("Update {1} server(s) from {0}", item.Remark, result?.Count ?? 0));
                        }
                    }
                    catch (WebException e)
                    {
                        NotifyTip($"{i18N.TranslateFormat("Update servers error from {0}", item.Remark)}\n{e.Message}", info: false);
                    }
                    catch (Exception e)
                    {
                        Logging.Error(e.ToString());
                    }
                })).ToArray());

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
                if (Global.Settings.UseProxyToUpdateSubscription)
                {
                    await MainController.Stop();
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
                await Task.Run(() => DNS.Cache.Clear());
                StatusText(i18N.Translate("DNS cache cleanup succeeded"));
            }
            catch (Exception)
            {
                // ignored
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
            void DisableItems(bool v)
            {
                UpdateACLToolStripMenuItem.Enabled = updateACLWithProxyToolStripMenuItem.Enabled = v;
            }

            if (useProxy && ServerComboBox.SelectedIndex == -1)
            {
                MessageBoxX.Show(i18N.Translate("Please select a server first"));
                return;
            }

            DisableItems(false);


            NotifyTip(i18N.Translate("Updating in the background"));
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
                    await MainController.Start(ServerComboBox.SelectedItem as Models.Server, mode);
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

                DisableItems(true);
            }
        }

        private async void UninstallServiceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Enabled = false;
            StatusText(i18N.Translate("Uninstalling NF Service"));
            try
            {
                await Task.Run(() =>
                {
                    if (NFController.UninstallDriver())
                    {
                        StatusText(i18N.Translate("Service has been uninstalled"));
                    }
                });
            }
            finally
            {
                Enabled = true;
            }
        }

        private async void reinstallTapDriverToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StatusText(i18N.Translate("Reinstalling TUN/TAP driver"));
            Enabled = false;
            try
            {
                await Task.Run(() =>
                {
                    Configuration.deltapall();
                    Configuration.addtap();
                });
                StatusText(i18N.Translate("Reinstall TUN/TAP driver successfully"));
            }
            catch
            {
                NotifyTip(i18N.Translate("Reinstall TUN/TAP driver failed"), info: false);
            }
            finally
            {
                State = State.Waiting;
                Enabled = true;
            }
        }

        #endregion


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
            Utils.Utils.Open($"https://netch.org/#/docs/zh-CN/faq");
        }

        #endregion
    }
}