using System;
using System.Collections.Generic;
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
                    Global.Settings.Server.AddRange(result);
                }
                else
                {
                    MessageBoxX.Show(i18N.Translate("Import servers error!"), LogLevel.ERROR);
                }

                InitServer();
                Configuration.Save();
            }
        }

        private void AddSocks5ServerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new Socks5().Show();
            Hide();
        }

        private void AddShadowsocksServerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new Shadowsocks().Show();
            Hide();
        }

        private void AddShadowsocksRServerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new ShadowsocksR().Show();
            Hide();
        }

        private void AddVMessServerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new VMess().Show();
            Hide();
        }

        private void AddTrojanServerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new Trojan().Show();
            Hide();
        }

        #endregion

        #region 模式

        private void CreateProcessModeToolStripButton_Click(object sender, EventArgs e)
        {
            new Process().Show();
            Hide();
        }

        #endregion

        #region 订阅

        private void ManageSubscribeLinksToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new SubscribeForm().Show();
            Hide();
        }

        private void UpdateServersFromSubscribeLinksToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Global.Settings.UseProxyToUpdateSubscription && ServerComboBox.SelectedIndex == -1)
                Global.Settings.UseProxyToUpdateSubscription = false;

            if (Global.Settings.UseProxyToUpdateSubscription && ServerComboBox.SelectedIndex == -1)
            {
                MessageBoxX.Show(i18N.Translate("Please select a server first"));
                return;
            }

            if (Global.Settings.SubscribeLink.Count > 0)
            {
                DeleteServerPictureBox.Enabled = false;
                UpdateServersFromSubscribeLinksToolStripMenuItem.Enabled = false;

                Task.Run(() =>
                {
                    if (Global.Settings.UseProxyToUpdateSubscription)
                    {
                        var mode = new Models.Mode
                        {
                            Remark = "ProxyUpdate",
                            Type = 5
                        };
                        State = State.Starting;
                        if (_mainController.Start(ServerComboBox.SelectedItem as Models.Server, mode))
                            StatusText(i18N.Translate("Starting update subscription"));
                    }
                    else
                        StatusText(i18N.Translate("Starting update subscription"));

                    Task.WaitAll(Global.Settings.SubscribeLink.Select(item => Task.Factory.StartNew(() =>
                    {
                        try
                        {
                            var request = WebUtil.CreateRequest(item.Link);

                            if (!string.IsNullOrEmpty(item.UserAgent)) request.UserAgent = item.UserAgent;
                            if (Global.Settings.UseProxyToUpdateSubscription) request.Proxy = new WebProxy($"http://127.0.0.1:{Global.Settings.HTTPLocalPort}");

                            var str = WebUtil.DownloadString(request);

                            try
                            {
                                str = ShareLink.URLSafeBase64Decode(str);
                            }
                            catch
                            {
                                // ignored
                            }

                            Global.Settings.Server = Global.Settings.Server.Where(server => server.Group != item.Remark).ToList();
                            var result = ShareLink.Parse(str);

                            if (result != null)
                            {
                                foreach (var x in result) x.Group = item.Remark;

                                Global.Settings.Server.AddRange(result);
                                NotifyTip(i18N.TranslateFormat("Update {1} server(s) from {0}", item.Remark, result.Count));
                            }
                        }
                        catch (WebException e)
                        {
                            NotifyTip($"{i18N.TranslateFormat("Update servers error from {0}", item.Remark)}\n{e.Message}", info: false);
                        }
                    })).ToArray());

                    InitServer();

                    Configuration.Save();
                    NotifyTip(i18N.Translate("Subscription updated"));

                    if (Global.Settings.UseProxyToUpdateSubscription)
                    {
                        _mainController.Stop();
                        State = State.Stopped;
                    }

                    State = State.Waiting;
                    DeleteModePictureBox.Enabled = true;
                    MenuStrip.Enabled = ConfigurationGroupBox.Enabled = ControlButton.Enabled = SettingsButton.Enabled = true;
                    UpdateServersFromSubscribeLinksToolStripMenuItem.Enabled = true;
                });

                NotifyTip(i18N.Translate("Updating in the background"));
            }
            else
            {
                MessageBoxX.Show(i18N.Translate("No subscription link"));
            }
        }

        #endregion

        #region 选项

        private void OpenDirectoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Utils.Utils.Open(".\\");
        }

        private void CleanDNSCacheToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Task.Run(() =>
            {
                DNS.Cache.Clear();
                NotifyTip(i18N.Translate("DNS cache cleanup succeeded"));
            });
        }

        private void ReloadModesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Enabled = false;
            SaveConfigs();
            Task.Run(() =>
            {
                InitMode();

                NotifyTip(i18N.Translate("Modes have been reload"));
                Enabled = true;
            });
        }

        private void updateACLWithProxyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UpdateACL(true, sender);
        }

        private void updateACLToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UpdateACL(false, sender);
        }

        private void UpdateACL(bool useProxy, object sender)
        {
            if (useProxy && ServerComboBox.SelectedIndex == -1)
            {
                MessageBoxX.Show(i18N.Translate("Please select a server first"));
                return;
            }

            ((ToolStripMenuItem) sender).Enabled = false;

            Task.Run(async () =>
            {
                if (useProxy)
                {
                    var mode = new Models.Mode
                    {
                        Remark = "ProxyUpdate",
                        Type = 5
                    };
                    State = State.Starting;
                    if (_mainController.Start(ServerComboBox.SelectedItem as Models.Server, mode))
                        StatusText(i18N.Translate("Updating in the background"));
                }

                try
                {
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
                    if (!(e is WebException))
                        throw;
                }
                finally
                {
                    ((ToolStripMenuItem) sender).Enabled = true;
                    if (useProxy)
                    {
                        _mainController.Stop();
                        State = State.Stopped;
                    }

                    State = State.Waiting;
                }
            });
        }


        private void UninstallServiceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Enabled = false;
            StatusText(i18N.Translate("Uninstalling NF Service"));

            Task.Run(() =>
            {
                try
                {
                    if (NFController.UninstallDriver())
                    {
                        StatusText(i18N.Translate("Service has been uninstalled"));
                    }
                }
                catch (Exception e)
                {
                    MessageBoxX.Show(e.ToString(), LogLevel.ERROR);
                    Console.WriteLine(e);
                    throw;
                }

                Enabled = true;
            });
        }

        private void reinstallTapDriverToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Task.Run(() =>
            {
                StatusText(i18N.Translate("Reinstalling TUN/TAP driver"));
                Enabled = false;
                try
                {
                    Configuration.deltapall();
                    Configuration.addtap();
                    NotifyTip(i18N.Translate("Reinstall TUN/TAP driver successfully"));
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
            });
        }

        #endregion


        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Exit(true);
        }

        private void RelyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Utils.Utils.Open("https://mega.nz/file/9OQ1EazJ#0pjJ3xt57AVLr29vYEEv15GSACtXVQOGlEOPpi_2Ico");
        }

        private void VersionLabel_Click(object sender, EventArgs e)
        {
            Utils.Utils.Open($"https://github.com/{UpdateChecker.Owner}/{UpdateChecker.Repo}/releases");
        }

        private void AboutToolStripButton_Click(object sender, EventArgs e)
        {
            new AboutForm().Show();
            Hide();
        }

        #endregion
    }
}