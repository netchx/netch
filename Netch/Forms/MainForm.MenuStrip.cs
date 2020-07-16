using System;
using System.Linq;
using System.Net;
using System.ServiceProcess;
using System.Threading.Tasks;
using System.Windows.Forms;
using Netch.Controllers;
using Netch.Forms.Mode;
using Netch.Forms.Server;
using Netch.Models;
using Netch.Utils;
using nfapinet;
using Trojan = Netch.Forms.Server.Trojan;
using VMess = Netch.Forms.Server.VMess;
using WebClient = Netch.Override.WebClient;

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
                    MessageBoxX.Show(i18N.Translate("Import servers error!"), info: false);
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
            var bak_State = State;
            var bak_StateText = StatusLabel.Text;

            if (Global.Settings.UseProxyToUpdateSubscription && ServerComboBox.SelectedIndex == -1)
                Global.Settings.UseProxyToUpdateSubscription = false;

            if (Global.Settings.UseProxyToUpdateSubscription)
            {
                // 当前 ServerComboBox 中至少有一项
                if (ServerComboBox.SelectedIndex == -1)
                {
                    MessageBoxX.Show(i18N.Translate("Please select a server first"));
                    return;
                }

                MenuStrip.Enabled = ConfigurationGroupBox.Enabled = ControlButton.Enabled = SettingsButton.Enabled = false;
                ControlButton.Text = "...";
            }

            if (Global.Settings.SubscribeLink.Count > 0)
            {
                StatusText(i18N.Translate("Starting update subscription"));
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
                        _mainController = new MainController();
                        _mainController.Start(ServerComboBox.SelectedItem as Models.Server, mode);
                    }

                    foreach (var item in Global.Settings.SubscribeLink)
                    {
                        using var client = new WebClient();
                        try
                        {
                            if (!string.IsNullOrEmpty(item.UserAgent))
                            {
                                client.Headers.Add("User-Agent", item.UserAgent);
                            }
                            else
                            {
                                client.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/77.0.3865.90 Safari/537.36");
                            }

                            if (Global.Settings.UseProxyToUpdateSubscription)
                            {
                                client.Proxy = new WebProxy($"http://127.0.0.1:{Global.Settings.HTTPLocalPort}");
                            }

                            var response = client.DownloadString(item.Link);

                            try
                            {
                                response = ShareLink.URLSafeBase64Decode(response);
                            }
                            catch (Exception)
                            {
                                // ignored
                            }

                            Global.Settings.Server = Global.Settings.Server.Where(server => server.Group != item.Remark).ToList();
                            var result = ShareLink.Parse(response);

                            if (result != null)
                            {
                                foreach (var x in result)
                                {
                                    x.Group = item.Remark;
                                }

                                Global.Settings.Server.AddRange(result);
                                NotifyIcon.ShowBalloonTip(5,
                                    UpdateChecker.Name,
                                    string.Format(i18N.Translate("Update {1} server(s) from {0}"), item.Remark, result.Count),
                                    ToolTipIcon.Info);
                            }
                            else
                            {
                                NotifyIcon.ShowBalloonTip(5,
                                    UpdateChecker.Name,
                                    string.Format(i18N.Translate("Update servers error from {0}"), item.Remark),
                                    ToolTipIcon.Error);
                            }
                        }
                        catch (Exception)
                        {
                        }
                    }

                    InitServer();
                    DeleteServerPictureBox.Enabled = true;
                    if (Global.Settings.UseProxyToUpdateSubscription)
                    {
                        MenuStrip.Enabled = ConfigurationGroupBox.Enabled = ControlButton.Enabled = SettingsButton.Enabled = true;
                        ControlButton.Text = i18N.Translate("Start");
                        _mainController.Stop();
                        NatTypeStatusLabel.Text = "";
                    }

                    Configuration.Save();
                    StatusText(i18N.Translate("Subscription updated"));

                    MenuStrip.Enabled = ConfigurationGroupBox.Enabled = ControlButton.Enabled = SettingsButton.Enabled = true;
                    UpdateStatus(bak_State);
                    StatusLabel.Text = bak_StateText;
                }).ContinueWith(task => { BeginInvoke(new Action(() => { UpdateServersFromSubscribeLinksToolStripMenuItem.Enabled = true; })); });

                NotifyIcon.ShowBalloonTip(5,
                    UpdateChecker.Name,
                    i18N.Translate("Updating in the background"),
                    ToolTipIcon.Info);
            }
            else
            {
                MessageBoxX.Show(i18N.Translate("No subscription link"));

            }
        }

        #endregion

        #region 选项

        private void RestartServiceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Enabled = false;
            StatusText(i18N.Translate("Restarting service"));

            Task.Run(() =>
            {
                try
                {
                    var service = new ServiceController("netfilter2");
                    if (service.Status == ServiceControllerStatus.Stopped)
                    {
                        service.Start();
                        service.WaitForStatus(ServiceControllerStatus.Running);
                    }
                    else if (service.Status == ServiceControllerStatus.Running)
                    {
                        service.Stop();
                        service.WaitForStatus(ServiceControllerStatus.Stopped);
                        service.Start();
                        service.WaitForStatus(ServiceControllerStatus.Running);
                    }
                }
                catch (Exception)
                {
                    NFAPI.nf_registerDriver("netfilter2");
                }

                MessageBoxX.Show(i18N.Translate("Service has been restarted"), owner: this);
                Enabled = true;
            });
        }

        private void UninstallServiceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Enabled = false;
            StatusText(i18N.Translate("Uninstalling Service"));

            Task.Run(() =>
            {
                try
                {
                    if (NFController.UninstallDriver())
                    {
                        MessageBoxX.Show(i18N.Translate("Service has been uninstalled"), owner: this);
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show(i18N.Translate("Error", e.Message));
                    Console.WriteLine(e);
                    throw;
                }

                StatusText(i18N.Translate(StateExtension.GetStatusString(State.Waiting)));
                Enabled = true;
            });
        }

        private void ReloadModesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Enabled = false;
            SaveConfigs();
            Task.Run(() =>
            {
                InitMode();

                MessageBoxX.Show(i18N.Translate("Modes have been reload"), owner: this);
                Enabled = true;
            });
        }

        private void CleanDNSCacheToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var bak_State = State;
            var bak_StateText = StatusLabel.Text;

            try
            {
                Enabled = false;
                DNS.Cache.Clear();

                MessageBoxX.Show(i18N.Translate("DNS cache cleanup succeeded"), owner: this);
                StatusText(i18N.Translate("DNS cache cleanup succeeded"));
                Enabled = true;
            }
            finally
            {
                UpdateStatus(bak_State);
                StatusLabel.Text = bak_StateText;
            }
        }

        private void OpenDirectoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Utils.Utils.OpenDir(@".\");
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
                    NotifyIcon.ShowBalloonTip(5,
                        UpdateChecker.Name, i18N.Translate("Reinstall TUN/TAP driver successfully"),
                        ToolTipIcon.Info);
                }
                catch
                {
                    NotifyIcon.ShowBalloonTip(5,
                        UpdateChecker.Name, i18N.Translate("Reinstall TUN/TAP driver failed"),
                        ToolTipIcon.Error);
                }
                finally
                {
                    UpdateStatus(State.Waiting);
                    Enabled = true;
                }
            });
        }

        private void updateACLWithProxyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            updateACLWithProxyToolStripMenuItem.Enabled = false;

            // 当前 ServerComboBox 中至少有一项
            if (ServerComboBox.SelectedIndex == -1)
            {
                MessageBoxX.Show(i18N.Translate("Please select a server first"));
                return;
            }

            MenuStrip.Enabled = ConfigurationGroupBox.Enabled = ControlButton.Enabled = SettingsButton.Enabled = false;
            ControlButton.Text = "...";


            Task.Run(() =>
            {
                var mode = new Models.Mode
                {
                    Remark = "ProxyUpdate",
                    Type = 5
                };
                _mainController = new MainController();
                _mainController.Start(ServerComboBox.SelectedItem as Models.Server, mode);

                using var client = new WebClient();

                client.Proxy = new WebProxy($"http://127.0.0.1:{Global.Settings.HTTPLocalPort}");

                StatusText(i18N.Translate("Updating in the background"));
                try
                {
                    client.DownloadFile(Global.Settings.ACL, "bin\\default.acl");
                    NotifyIcon.ShowBalloonTip(5,
                        UpdateChecker.Name, i18N.Translate("ACL updated successfully"),
                        ToolTipIcon.Info);
                }
                catch (Exception e)
                {
                    Logging.Error("使用代理更新 ACL 失败！" + e);
                    MessageBoxX.Show(i18N.Translate("ACL update failed") + "\n" + e);
                }
                finally
                {
                    UpdateStatus(State.Waiting);
                    _mainController.Stop();
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
            System.Diagnostics.Process.Start("https://mega.nz/file/9OQ1EazJ#0pjJ3xt57AVLr29vYEEv15GSACtXVQOGlEOPpi_2Ico");
        }

        private void VersionLabel_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start($"https://github.com/{UpdateChecker.Owner}/{UpdateChecker.Repo}/releases");
        }

        private void AboutToolStripButton_Click(object sender, EventArgs e)
        {
            new AboutForm().Show();
            Hide();
        }

        private void updateACLToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var bak_State = State;
            var bak_StateText = StatusLabel.Text;

            StatusText(i18N.Translate("Starting update ACL"));
            using var client = new WebClient();

            client.DownloadFileTaskAsync(Global.Settings.ACL, "bin\\default.acl");
            client.DownloadFileCompleted += ((sender, args) =>
            {
                try
                {
                    if (args.Error == null)
                    {
                        NotifyIcon.ShowBalloonTip(5,
                            UpdateChecker.Name, i18N.Translate("ACL updated successfully"),
                            ToolTipIcon.Info);
                    }
                    else
                    {
                        Logging.Error("ACL 更新失败！" + args.Error);
                        MessageBoxX.Show(i18N.Translate("ACL update failed") + "\n" + args.Error);
                    }
                }
                finally
                {
                    UpdateStatus(bak_State);
                    StatusLabel.Text = bak_StateText;
                }
            });
        }

        #endregion
    }
}