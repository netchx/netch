using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Netch.Forms
{
    public partial class MainForm : Form
    {
        /// <summary>
        ///     当前状态
        /// </summary>
        public Objects.State State = Objects.State.Waiting;

        /// <summary>
        ///     主控制器
        /// </summary>
        public Controllers.MainController MainController;

        // <summary>
        ///		当前正在处理的模式文件
        /// </summary>
        public static List<string> _changedFiles = new List<string>();

        // <summary>
        ///		用于上锁的对象
        /// </summary>
        static object lockObj = new object();

        public MainForm()
        {
            InitializeComponent();

            CheckForIllegalCrossThreadCalls = false;
            ToolStrip.Renderer = new Override.ToolStripProfessionalRender();
        }

        public void TestServer()
        {
            foreach (var server in Global.Server)
            {
                Task.Run(server.Test);
            }

            Task.Run(() =>
            {
                Thread.Sleep(2000);

                Refresh();
            });
        }

        public void ScanMode()
        {
            DirectoryInfo ModeDir = new DirectoryInfo(Global.MODE_DIR);
            List<Objects.Mode> ModeNew = new List<Objects.Mode>();
            
            foreach (var fileInfo in ModeDir.GetFiles(Global.MODE_EXT))
            {
                var Index = Global.Mode.FindIndex(x => x.FileName == fileInfo.Name);
                // 如果文件名在 Mode 列表内
                if (Index != -1)
                {
                    if (Global.Mode[Index].LastWriteTime != fileInfo.LastWriteTime)
                    {
                        System.Security.Cryptography.SHA256 modeSHA256 = System.Security.Cryptography.SHA256.Create();
                        var fs = fileInfo.Create();
                        var FileSHA256 = modeSHA256.ComputeHash(fs);

                        // mode 文件发生了改变，进行修改
                        if (Global.Mode[Index].SHA256 != FileSHA256)
                        {
                            var mode = new Objects.Mode(fileInfo.FullName, out bool ok);
                            if (ok)
                            {
                                ModeNew.Add(mode);
                                
                            }
                            // 新文件格式有误
                            else
                            {
                                // 跳过
                            }
                            continue;
                        }
                    }
                    ModeNew.Add(Global.Mode[Index]);
                }

                // 如果文件名不在 Mode 列表内
                else
                {
                    var mode = new Objects.Mode(fileInfo.FullName, out bool ok);
                    if (ok)
                    {
                        ModeNew.Add(mode);
                    }
                    // 新文件格式有误
                    else
                    {
                        // 跳过
                    }
                }
            }

            if (ModeNew.Count > 0)
            {
                ModeNew.Sort();
                string SelectedFileName;
                if (ModeComboBox.SelectedIndex > 0)
                {
                    SelectedFileName = Global.Mode[ModeComboBox.SelectedIndex].FileName;
                    ModeComboBox.Items.Clear();
                    Global.Mode.Clear();
                    Global.Mode = ModeNew;
                    ModeComboBox.Items.AddRange(ModeNew.ToArray());
                    var Index = ModeNew.FindIndex(x => x.FileName == SelectedFileName);
                    ModeComboBox.SelectedIndex = (Index > -1 ? Index : 0);
                }
                else
                {
                    ModeComboBox.Items.Clear();
                    Global.Mode.Clear();
                    Global.Mode = ModeNew;
                    ModeComboBox.Items.AddRange(ModeNew.ToArray());
                    ModeComboBox.SelectedIndex = 0;
                }
            }
            else
            {
                ModeComboBox.Items.Clear();
                Global.Mode.Clear();
            }
        }

        public void InitServer()
        {
            ServerComboBox.Items.Clear();
            ServerComboBox.Items.AddRange(Global.Server.ToArray());

            // 查询设置中是否正常加载了上次存储的服务器位置
            if (Global.Settings.TryGetValue("ServerComboBoxSelectedIndex", out int count))
            {
                // 如果值合法，选中该位置
                if (count > 0 && count < ServerComboBox.Items.Count)
                {
                    ServerComboBox.SelectedIndex = count;
                }
                else if (ServerComboBox.Items.Count > 0) // 如果值非法，且当前 ServerComboBox 中有元素，选择第一个位置
                {
                    ServerComboBox.SelectedIndex = 0;
                }

                // 如果当前 ServerComboBox 中没元素，不做处理
            }
            else // 如果设置中没有加载上次的位置，给设置添加元素
            {
                Global.Settings.Add("ServerComboBoxSelectedIndex", 0);
                
                // 如果当前 ServerComboBox 中有元素，选择第一个位置
                if (ServerComboBox.Items.Count > 0)
                {
                    ServerComboBox.SelectedIndex = 0;
                }

                // 如果当前 ServerComboBox 中没元素，不做处理
            }
        }

        public void InitMode()
        {
            ModeComboBox.Items.Clear();

            if (Directory.Exists(Global.MODE_DIR))
            {

                // 读取所有在文件夹 Global.RULE_DIR 中的 Mode 文件
                foreach (var FullPathName in Directory.GetFiles(Global.MODE_DIR, Global.MODE_EXT))
                {
                    var mode = new Objects.Mode(FullPathName, out bool ok);
                    if (ok)
                    {
                        Global.Mode.Add(mode);
                    }
                }

                Global.Mode.Sort();

                ModeComboBox.Items.AddRange(Global.Mode.ToArray());

                Global.ModeWatch.Path = Global.MODE_DIR;
                Global.ModeWatch.Filter = Global.MODE_EXT;
                Global.ModeWatch.NotifyFilter = NotifyFilters.FileName;
                Global.ModeWatch.Renamed += OnModeRenamed;
                Global.ModeWatch.EnableRaisingEvents = true;
            }

            // 查询设置中是否正常加载了上次存储的服务器位置
            if (Global.Settings.TryGetValue("ModeComboBoxSelectedIndex", out int count))
            {
                // 如果值合法，选中该位置
                if (count > 0 && count < ModeComboBox.Items.Count)
                {
                    ModeComboBox.SelectedIndex = count;
                }
                // 如果值非法，且当前 ModeComboBox 中有元素，选择第一个位置
                else if (ModeComboBox.Items.Count > 0)
                {
                    ModeComboBox.SelectedIndex = 0;
                }

                // 如果当前 ModeComboBox 中没元素，不做处理
            }
            else // 如果设置中没有加载上次的位置，给Settings添加元素
            {
                Global.Settings.Add("ModeComboBoxSelectedIndex", 0);

                // 如果当前 ModeComboBox 中有元素，选择第一个位置
                if (ModeComboBox.Items.Count > 0)
                {
                    ModeComboBox.SelectedIndex = 0;
                }

                // 如果当前 ModeComboBox 中没元素，不做处理
            }
        }

        public void UpdateMode(int ModePos)
        {
            ModeComboBox.Items.Clear();
            ModeComboBox.Items.AddRange(Global.Mode.ToArray());
            ModeComboBox.SelectedIndex = ModePos;
        }

        private static void OnModeRenamed(object source, RenamedEventArgs e)
        {
            // 给资源上锁
            lock(lockObj)
            {
                if (MainForm._changedFiles.Contains(e.FullPath))
                {
                    return;
                }
                MainForm._changedFiles.Add(e.FullPath);

                var Index = Global.Mode.FindIndex(x => x.FileName == Path.GetFileName(e.OldFullPath));
                if (Index > -1)
                {
                    Global.Mode[Index].FileName = Path.GetFileName(e.FullPath);
                }
            }

            // 等待至少 5000 毫秒以后再释放资源
            System.Timers.Timer timer = new System.Timers.Timer(5000) { AutoReset = false };
            timer.Elapsed += (timerElapsedSender, timerElapsedArgs) =>
            {
                lock(lockObj)
                {
                    MainForm._changedFiles.Remove(e.FullPath);
                }
            };
            timer.Start();
        }

        private void ComboBox_DrawItem(object sender, DrawItemEventArgs e)
        {
            var cbx = sender as ComboBox;

            // 绘制背景颜色
            e.Graphics.FillRectangle(new SolidBrush(Color.White), e.Bounds);

            if (e.Index >= 0)
            {
                // 绘制 备注/名称 字符串
                e.Graphics.DrawString(cbx.Items[e.Index].ToString(), cbx.Font, new SolidBrush(Color.Black), e.Bounds);

                if (cbx.Items[e.Index] is Objects.Server)
                {
                    var item = cbx.Items[e.Index] as Objects.Server;

                    // 计算延迟底色
                    SolidBrush brush;
                    if (item.Delay == -1)
                    {
                        // 灰色
                        brush = new SolidBrush(Color.Gray);
                    }
                    else if (item.Delay > 200)
                    {
                        // 红色
                        brush = new SolidBrush(Color.Red);
                    }
                    else if (item.Delay > 80)
                    {
                        // 黄色
                        brush = new SolidBrush(Color.Yellow);
                    }
                    else
                    {
                        // 绿色
                        brush = new SolidBrush(Color.FromArgb(50, 255, 56));
                    }

                    // 绘制延迟底色
                    e.Graphics.FillRectangle(brush, ServerComboBox.Size.Width - 60, e.Bounds.Y, 60, e.Bounds.Height);

                    // 绘制延迟字符串
                    e.Graphics.DrawString(item.Delay.ToString(), cbx.Font, new SolidBrush(Color.Black), ServerComboBox.Size.Width - 58, e.Bounds.Y);
                }
                else if (cbx.Items[e.Index] is Objects.Mode)
                {
                    var item = cbx.Items[e.Index] as Objects.Mode;

                    // 绘制延迟底色
                    e.Graphics.FillRectangle(new SolidBrush(Color.Gray), ServerComboBox.Size.Width - 60, e.Bounds.Y, 60, e.Bounds.Height);

                    // 绘制延迟字符串
                    e.Graphics.DrawString(item.Rule.Count.ToString(), cbx.Font, new SolidBrush(Color.Black), ServerComboBox.Size.Width - 58, e.Bounds.Y);
                }
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            // 加载配置
            Utils.Configuration.Load();

            // 加载服务器
            InitServer();

            // 加载模式
            InitMode();

            // 加载翻译
            ServerToolStripDropDownButton.Text = Utils.i18N.Translate("Server");
            ImportServersFromClipboardToolStripMenuItem.Text = Utils.i18N.Translate("Import Servers From Clipboard");
            AddSocks5ServerToolStripMenuItem.Text = Utils.i18N.Translate("Add [Socks5] Server");
            AddShadowsocksServerToolStripMenuItem.Text = Utils.i18N.Translate("Add [Shadowsocks] Server");
            AddShadowsocksRServerToolStripMenuItem.Text = Utils.i18N.Translate("Add [ShadowsocksR] Server");
            AddVMessServerToolStripMenuItem.Text = Utils.i18N.Translate("Add [VMess] Server");
            ModeToolStripDropDownButton.Text = Utils.i18N.Translate("Mode");
            CreateProcessModeToolStripMenuItem.Text = Utils.i18N.Translate("Create Process Mode");
            SubscribeToolStripDropDownButton.Text = Utils.i18N.Translate("Subscribe");
            ManageSubscribeLinksToolStripMenuItem.Text = Utils.i18N.Translate("Manage Subscribe Links");
            UpdateServersFromSubscribeLinksToolStripMenuItem.Text = Utils.i18N.Translate("Update Servers From Subscribe Links");
            OptionsToolStripDropDownButton.Text = Utils.i18N.Translate("Options");
            RestartServiceToolStripMenuItem.Text = Utils.i18N.Translate("Restart Service");
            UninstallServiceToolStripMenuItem.Text = Utils.i18N.Translate("Uninstall Service");
            ReloadModesToolStripMenuItem.Text = Utils.i18N.Translate("Reload Modes");
            AboutToolStripDropDownButton.Text = Utils.i18N.Translate("About");
            TelegarmGroupToolStripMenuItem.Text = Utils.i18N.Translate("Telegram Group");
            TelegramChannelToolStripMenuItem.Text = Utils.i18N.Translate("Telegram Channel");
            ConfigurationGroupBox.Text = Utils.i18N.Translate("Configuration");
            ServerLabel.Text = Utils.i18N.Translate("Server");
            ModeLabel.Text = Utils.i18N.Translate("Mode");
            ControlButton.Text = Utils.i18N.Translate("Start");
            StatusLabel.Text = $"{Utils.i18N.Translate("Status")}{Utils.i18N.Translate(": ")}{Utils.i18N.Translate("Waiting for command")}";

            // 自动检测延迟
            Task.Run(() =>
            {
                while (true)
                {
                    if (State == Objects.State.Waiting || State == Objects.State.Stopped)
                    {
                        if (!Global.ModeWatch.EnableRaisingEvents)
                        {
                            Global.ModeWatch.EnableRaisingEvents = true;
                        }
                        TestServer();
                        Thread.Sleep(10000);
                    }
                    else
                    {
                        if (Global.ModeWatch.EnableRaisingEvents)
                        {
                            Global.ModeWatch.EnableRaisingEvents = false;
                        }
                        Thread.Sleep(200);
                    }
                }
            });
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                if (State != Objects.State.Waiting && State != Objects.State.Stopped)
                {
                    MessageBox.Show(Utils.i18N.Translate("Please press Stop button first"), Utils.i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);

                    e.Cancel = true;
                }
                Global.Settings["ServerComboBoxSelectedIndex"] = ServerComboBox.SelectedIndex;
                Global.Settings["ModeComboBoxSelectedIndex"] = ModeComboBox.SelectedIndex;
                Utils.Configuration.Save();
            }
        }

        private void ImportServersFromClipboardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var texts = Clipboard.GetText();
            if (!String.IsNullOrWhiteSpace(texts))
            {
                using (var sr = new StringReader(texts))
                {
                    string text;

                    while ((text = sr.ReadLine()) != null)
                    {
                        var result = Utils.ShareLink.Parse(text);

                        if (result != null)
                        {
                            Global.Server.AddRange(result);
                        }
                    }
                }

                InitServer();
            }
        }

        private void AddSocks5ServerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            (new Server.Socks5()).Show();
            Hide();
        }

        private void AddShadowsocksServerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            (new Server.Shadowsocks()).Show();
            Hide();
        }

        private void AddShadowsocksRServerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            (new Server.ShadowsocksR()).Show();
            Hide();
        }

        private void AddVMessServerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(Utils.i18N.Translate("VMess is currently not supported. For more information, please see our Github releases\n\nPress OK will redirect"), Utils.i18N.Translate("Information"), MessageBoxButtons.OKCancel, MessageBoxIcon.Information) == DialogResult.OK)
            {
                Process.Start("https://github.com/NetchX/Netch/releases");
            }
        }

        private void CreateProcessModeToolStripButton_Click(object sender, EventArgs e)
        {
            (new Mode.Process()).Show();
            Hide();
        }

        private void ManageSubscribeLinksToolStripMenuItem_Click(object sender, EventArgs e)
        {
            (new SubscribeForm()).Show();
            Hide();
        }

        private void UpdateServersFromSubscribeLinksToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Global.SubscribeLink.Count > 0)
            {
                DeletePictureBox.Enabled = false;
                Task.Run(() =>
                {
                    foreach (var item in Global.SubscribeLink)
                    {
                        using (var client = new Override.WebClient())
                        {
                            try
                            {
                                var response = client.DownloadString(item.Link);

                                try
                                {
                                    response = Utils.ShareLink.URLSafeBase64Decode(response);
                                }
                                catch (Exception)
                                {
                                    // 跳过
                                }

                                var list = new List<Objects.Server>();
                                foreach (var server in Global.Server)
                                {
                                    if (server.Group != item.Remark)
                                    {
                                        list.Add(server);
                                    }
                                }
                                Global.Server = list;

                                using (var sr = new StringReader(response))
                                {
                                    string text;

                                    while ((text = sr.ReadLine()) != null)
                                    {
                                        var result = Utils.ShareLink.Parse(text);

                                        if (result != null)
                                        {
                                            if (item.Link.Contains("n3ro"))
                                            {
                                                foreach (var x in result)
                                                {
                                                    x.Remark = x.Remark.Split('#')[0].Trim();
                                                }
                                            }

                                            foreach (var x in result)
                                            {
                                                x.Group = item.Remark;
                                            }

                                            Global.Server.AddRange(result);
                                        }
                                    }
                                }
                            }
                            catch (Exception)
                            {
                                continue;
                            }
                        }
                    }

                    InitServer();
                    DeletePictureBox.Enabled = true;
                    MessageBox.Show(Utils.i18N.Translate("Update completed"), Utils.i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                });

                MessageBox.Show(Utils.i18N.Translate("Updating in the background"), Utils.i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show(Utils.i18N.Translate("No subscription link"), Utils.i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void RestartServiceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Enabled = false;
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
                    nfapinet.NFAPI.nf_registerDriver("netfilter2");
                }

                MessageBox.Show(Utils.i18N.Translate("Service has been restarted"), Utils.i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                Enabled = true;
            });
        }

        private void UninstallServiceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Enabled = false;
            Task.Run(() =>
            {
                var driver = $"{Environment.SystemDirectory}\\drivers\\netfilter2.sys";
                if (File.Exists(driver))
                {
                    try
                    {
                        var service = new ServiceController("netfilter2");
                        if (service.Status == ServiceControllerStatus.Running)
                        {
                            service.Stop();
                            service.WaitForStatus(ServiceControllerStatus.Stopped);
                        }
                    }
                    catch (Exception)
                    {
                        // 跳过
                    }

                    try
                    {
                        nfapinet.NFAPI.nf_unRegisterDriver("netfilter2");

                        File.Delete(driver);

                        MessageBox.Show(Utils.i18N.Translate("Service has been uninstalled"), Utils.i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(Utils.i18N.Translate("Error") + Utils.i18N.Translate(": ") + ex.ToString(), Utils.i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                else
                {
                    MessageBox.Show(Utils.i18N.Translate("Service has been uninstalled"), Utils.i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                Enabled = true;
            });
        }

        private void ReloadModesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Enabled = false;
            Task.Run(() =>
            {
                ScanMode();

                MessageBox.Show(Utils.i18N.Translate("Modes have been reload"), Utils.i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                Enabled = true;
            });
        }

        private void TelegarmGroupToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("https://t.me/NetchX");
        }

        private void TelegramChannelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("https://t.me/NetchXChannel");
        }

        private void VersionLabel_Click(object sender, EventArgs e)
        {
            Process.Start("https://github.com/netchx/Netch");
        }

        private void EditPictureBox_Click(object sender, EventArgs e)
        {
            // 当前ServerComboBox中至少有一项
            if (ServerComboBox.SelectedIndex != -1)
            {
                switch (Global.Server[ServerComboBox.SelectedIndex].Type)
                {
                    case "Socks5":
                        (new Server.Socks5(ServerComboBox.SelectedIndex)).Show();
                        break;
                    case "Shadowsocks":
                        (new Server.Shadowsocks(ServerComboBox.SelectedIndex)).Show();
                        break;
                    case "ShadowsocksR":
                        (new Server.ShadowsocksR(ServerComboBox.SelectedIndex)).Show();
                        break;
                    case "VMess":
                        return;
                    default:
                        return;
                }

                Hide();
            }
            else
            {
                MessageBox.Show(Utils.i18N.Translate("Please select a server first"), Utils.i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void DeletePictureBox_Click(object sender, EventArgs e)
        {
            // 当前ServerComboBox中至少有一项
            if (ServerComboBox.SelectedIndex != -1)
            {
                var index = ServerComboBox.SelectedIndex;

                Global.Server.Remove(ServerComboBox.SelectedItem as Objects.Server);
                ServerComboBox.Items.RemoveAt(index);

                if (ServerComboBox.Items.Count > 0)
                {
                    ServerComboBox.SelectedIndex = (index != 0) ? index - 1 : index;
                }
            }
            else
            {
                MessageBox.Show(Utils.i18N.Translate("Please select a server first"), Utils.i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void SpeedPictureBox_Click(object sender, EventArgs e)
        {
            Task.Run(() =>
            {
                foreach (var server in Global.Server)
                {
                    Task.Run(server.Test);
                }

                Thread.Sleep(2000);
                Refresh();
            });
        }

        private void ControlButton_Click(object sender, EventArgs e)
        {
            if (State == Objects.State.Waiting || State == Objects.State.Stopped)
            {
                // 当前ServerComboBox中至少有一项
                if (ServerComboBox.SelectedIndex == -1)
                {
                    MessageBox.Show(Utils.i18N.Translate("Please select a server first"), Utils.i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // 当前ModeComboBox中至少有一项
                if (ModeComboBox.SelectedIndex == -1)
                {
                    MessageBox.Show(Utils.i18N.Translate("Please select an mode first"), Utils.i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                ToolStrip.Enabled = ConfigurationGroupBox.Enabled = ControlButton.Enabled = false;
                ControlButton.Text = "...";
                StatusLabel.Text = $"{Utils.i18N.Translate("Status")}{Utils.i18N.Translate(": ")}{Utils.i18N.Translate("Starting")}";
                State = Objects.State.Starting;

                Task.Run(() =>
                {
                    MainController = new Controllers.MainController();
                    if (MainController.Start(ServerComboBox.SelectedItem as Objects.Server, ModeComboBox.SelectedItem as Objects.Mode))
                    {
                        ControlButton.Enabled = true;
                        ControlButton.Text = Utils.i18N.Translate("Stop");
                        StatusLabel.Text = $"{Utils.i18N.Translate("Status")}{Utils.i18N.Translate(": ")}{Utils.i18N.Translate("Started")}";
                        State = Objects.State.Started;
                    }
                    else
                    {
                        ToolStrip.Enabled = ConfigurationGroupBox.Enabled = ControlButton.Enabled = true;
                        ControlButton.Text = Utils.i18N.Translate("Start");
                        StatusLabel.Text = $"{Utils.i18N.Translate("Status")}{Utils.i18N.Translate(": ")}{Utils.i18N.Translate("Start failed")}";
                        State = Objects.State.Stopped;
                    }
                });
            }
            else
            {
                ControlButton.Enabled = false;
                ControlButton.Text = "...";
                StatusLabel.Text = $"{Utils.i18N.Translate("Status")}{Utils.i18N.Translate(": ")}{Utils.i18N.Translate("Stopping")}";
                State = Objects.State.Stopping;

                Task.Run(() =>
                {
                    MainController.Stop();

                    ToolStrip.Enabled = ConfigurationGroupBox.Enabled = ControlButton.Enabled = true;
                    ControlButton.Text = Utils.i18N.Translate("Start");
                    StatusLabel.Text = $"{Utils.i18N.Translate("Status")}{Utils.i18N.Translate(": ")}{Utils.i18N.Translate("Stopped")}";
                    State = Objects.State.Stopped;

                    TestServer();
                });
            }
        }
    }
}
