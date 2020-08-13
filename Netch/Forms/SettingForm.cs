using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows.Forms;
using Netch.Models;
using Netch.Utils;
using TaskScheduler;

namespace Netch.Forms
{
    public partial class SettingForm : Form
    {
        public SettingForm()
        {
            InitializeComponent();
        }

        private void TUNTAPUseCustomDNSCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (!UseCustomDNSCheckBox.Checked)
            {
                TUNTAPDNSTextBox.Enabled = false;
            }
            else
            {
                TUNTAPDNSTextBox.Enabled = true;
            }

            if (Global.Settings.TUNTAP.DNS.Count > 0)
            {
                var dns = "";
                foreach (var ip in Global.Settings.TUNTAP.DNS)
                {
                    dns += ip;
                    dns += ',';
                }

                dns = dns.Trim();
                TUNTAPDNSTextBox.Text = dns.Substring(0, dns.Length - 1);
            }
            // 如果 DNS 为空，设置为默认 DNS 1.1.1.1
            else
            {
                Global.Settings.TUNTAP.DNS.Add("1.1.1.1");
                TUNTAPDNSTextBox.Text = "1.1.1.1";
            }
        }

        private void InitValue()
        {
            // Local Port
            Socks5PortTextBox.Text = Global.Settings.Socks5LocalPort.ToString();
            HTTPPortTextBox.Text = Global.Settings.HTTPLocalPort.ToString();
            RedirectorTextBox.Text = Global.Settings.RedirectorTCPPort.ToString();
            switch (Global.Settings.LocalAddress)
            {
                case "127.0.0.1":
                    AllowDevicesCheckBox.Checked = false;
                    break;
                case "0.0.0.0":
                    AllowDevicesCheckBox.Checked = true;
                    break;
                default:
                    Global.Settings.LocalAddress = "127.0.0.1";
                    AllowDevicesCheckBox.Checked = false;
                    break;
            }

            // TUN/TAP
            TUNTAPAddressTextBox.Text = Global.Settings.TUNTAP.Address;
            TUNTAPNetmaskTextBox.Text = Global.Settings.TUNTAP.Netmask;
            TUNTAPGatewayTextBox.Text = Global.Settings.TUNTAP.Gateway;
            UseCustomDNSCheckBox.Checked = Global.Settings.TUNTAP.UseCustomDNS;
            ProxyDNSCheckBox.Checked = Global.Settings.TUNTAP.ProxyDNS;
            UseFakeDNSCheckBox.Checked = Global.Settings.TUNTAP.UseFakeDNS;
            if (Global.Settings.TUNTAP.DNS.Count > 0)
            {
                var dns = "";
                foreach (var ip in Global.Settings.TUNTAP.DNS)
                {
                    dns += ip;
                    dns += ',';
                }

                dns = dns.Trim();
                TUNTAPDNSTextBox.Text = dns.Substring(0, dns.Length - 1);
            }
            else
            {
                // 如果 DNS 为空，设置为默认 DNS 1.1.1.1
                Global.Settings.TUNTAP.DNS.Add("1.1.1.1");
                TUNTAPDNSTextBox.Text = "1.1.1.1";
            }

            if (!UseCustomDNSCheckBox.Checked)
            {
                TUNTAPDNSTextBox.Enabled = false;
            }

            // Behavior
            ExitWhenClosedCheckBox.Checked = Global.Settings.ExitWhenClosed;
            StopWhenExitedCheckBox.Checked = Global.Settings.StopWhenExited;
            StartWhenOpenedCheckBox.Checked = Global.Settings.StartWhenOpened;
            MinimizeWhenStartedCheckBox.Checked = Global.Settings.MinimizeWhenStarted;
            RunAtStartupCheckBox.Checked = Global.Settings.RunAtStartup;
            CheckUpdateWhenOpenedCheckBox.Checked = Global.Settings.CheckUpdateWhenOpened;
            BootShadowsocksFromDLLCheckBox.Checked = Global.Settings.BootShadowsocksFromDLL;
            ModifySystemDNSCheckBox.Checked = Global.Settings.ModifySystemDNS;
            CheckBetaUpdateCheckBox.Checked = Global.Settings.CheckBetaUpdate;

            ProfileCountTextBox.Text = Global.Settings.ProfileCount.ToString();
            TcpingAtStartedCheckBox.Checked = Global.Settings.StartedTcping;
            DetectionIntervalTextBox.Text = Global.Settings.StartedTcping_Interval.ToString();
            AclAddrTextBox.Text = Global.Settings.ACL;
            LanguageComboBox.Items.AddRange(i18N.GetTranslateList().ToArray());
            LanguageComboBox.SelectedItem = Global.Settings.Language;
            InitSTUN();
        }

        private void InitText()
        {
            Text = i18N.Translate(Text);

            PortGroupBox.Text = i18N.Translate(PortGroupBox.Text);
            AllowDevicesCheckBox.Text = i18N.Translate(AllowDevicesCheckBox.Text);
            TUNTAPAddressLabel.Text = i18N.Translate(TUNTAPAddressLabel.Text);
            TUNTAPNetmaskLabel.Text = i18N.Translate(TUNTAPNetmaskLabel.Text);
            TUNTAPGatewayLabel.Text = i18N.Translate(TUNTAPGatewayLabel.Text);
            UseCustomDNSCheckBox.Text = i18N.Translate(UseCustomDNSCheckBox.Text);
            ProxyDNSCheckBox.Text = i18N.Translate(ProxyDNSCheckBox.Text);
            UseFakeDNSCheckBox.Text = i18N.Translate(UseFakeDNSCheckBox.Text);
            GlobalBypassIPsButton.Text = i18N.Translate(GlobalBypassIPsButton.Text);
            ControlButton.Text = i18N.Translate(ControlButton.Text);
            BootShadowsocksFromDLLCheckBox.Text = i18N.Translate(BootShadowsocksFromDLLCheckBox.Text);
            ModifySystemDNSCheckBox.Text = i18N.Translate(ModifySystemDNSCheckBox.Text);
            CheckBetaUpdateCheckBox.Text = i18N.Translate(CheckBetaUpdateCheckBox.Text);
            BehaviorGroupBox.Text = i18N.Translate(BehaviorGroupBox.Text);
            ExitWhenClosedCheckBox.Text = i18N.Translate(ExitWhenClosedCheckBox.Text);
            StopWhenExitedCheckBox.Text = i18N.Translate(StopWhenExitedCheckBox.Text);
            StartWhenOpenedCheckBox.Text = i18N.Translate(StartWhenOpenedCheckBox.Text);
            MinimizeWhenStartedCheckBox.Text = i18N.Translate(MinimizeWhenStartedCheckBox.Text);
            RunAtStartupCheckBox.Text = i18N.Translate(RunAtStartupCheckBox.Text);
            CheckUpdateWhenOpenedCheckBox.Text = i18N.Translate(CheckUpdateWhenOpenedCheckBox.Text);
            ProfileCountLabel.Text = i18N.Translate(ProfileCountLabel.Text);
            TcpingAtStartedCheckBox.Text = i18N.Translate(TcpingAtStartedCheckBox.Text);
            DetectionIntervalLabel.Text = i18N.Translate(DetectionIntervalLabel.Text);
            STUNServerLabel.Text = i18N.Translate(STUNServerLabel.Text);
            AclLabel.Text = i18N.Translate(AclLabel.Text);
            LanguageLabel.Text = i18N.Translate(LanguageLabel.Text);
        }

        private void InitSTUN()
        {
            try
            {
                var stuns = File.ReadLines("bin\\stun.txt");
                STUN_ServerComboBox.Items.AddRange(stuns.ToArray());
            }
            catch (Exception)
            {
                // ignored
            }

            STUN_ServerComboBox.Text = $"{Global.Settings.STUN_Server}:{Global.Settings.STUN_Server_Port}";
        }

        private void SettingForm_Load(object sender, EventArgs e)
        {
            InitText();
            InitValue();
        }

        private void SettingForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Global.MainForm.Show();
        }

        private void GlobalBypassIPsButton_Click(object sender, EventArgs e)
        {
            new GlobalBypassIPForm().Show();
            Hide();
        }

        private void ControlButton_Click(object sender, EventArgs e)
        {
            Global.Settings.ExitWhenClosed = ExitWhenClosedCheckBox.Checked;
            Global.Settings.StopWhenExited = StopWhenExitedCheckBox.Checked;
            Global.Settings.StartWhenOpened = StartWhenOpenedCheckBox.Checked;
            Global.Settings.CheckUpdateWhenOpened = CheckUpdateWhenOpenedCheckBox.Checked;
            Global.Settings.CheckBetaUpdate = CheckBetaUpdateCheckBox.Checked;
            Global.Settings.MinimizeWhenStarted = MinimizeWhenStartedCheckBox.Checked;
            Global.Settings.RunAtStartup = RunAtStartupCheckBox.Checked;
            Global.Settings.BootShadowsocksFromDLL = BootShadowsocksFromDLLCheckBox.Checked;
            Global.Settings.Language = LanguageComboBox.SelectedItem.ToString();

            // 开机自启判断
            var scheduler = new TaskSchedulerClass();
            scheduler.Connect();
            var folder = scheduler.GetFolder("\\");

            var taskIsExists = false;
            try

            {
                folder.GetTask("Netch Startup");
                taskIsExists = true;
            }
            catch (Exception)
            {
                // ignored
            }

            if (RunAtStartupCheckBox.Checked)
            {
                if (taskIsExists)
                    folder.DeleteTask("Netch Startup", 0);

                var task = scheduler.NewTask(0);
                task.RegistrationInfo.Author = "Netch";
                task.RegistrationInfo.Description = "Netch run at startup.";
                task.Principal.RunLevel = _TASK_RUNLEVEL.TASK_RUNLEVEL_HIGHEST;

                task.Triggers.Create(_TASK_TRIGGER_TYPE2.TASK_TRIGGER_LOGON);
                var action = (IExecAction) task.Actions.Create(_TASK_ACTION_TYPE.TASK_ACTION_EXEC);
                action.Path = Application.ExecutablePath;


                task.Settings.ExecutionTimeLimit = "PT0S";
                task.Settings.DisallowStartIfOnBatteries = false;
                task.Settings.RunOnlyIfIdle = false;

                folder.RegisterTaskDefinition("Netch Startup", task, (int) _TASK_CREATION.TASK_CREATE, null, null,
                    _TASK_LOGON_TYPE.TASK_LOGON_INTERACTIVE_TOKEN, "");
            }

            else
            {
                if (taskIsExists)
                    folder.DeleteTask("Netch Startup", 0);
            }

            // 端口检查
            if (!CheckPortText("Socks5", ref Socks5PortTextBox, ref Global.Settings.Socks5LocalPort))
                return;
            if (!CheckPortText("HTTP", ref HTTPPortTextBox, ref Global.Settings.HTTPLocalPort))
                return;
            if (!CheckPortText("RedirectorTCP", ref RedirectorTextBox, ref Global.Settings.RedirectorTCPPort, PortType.TCP))
                return;
            Global.Settings.LocalAddress = AllowDevicesCheckBox.Checked ? "0.0.0.0" : "127.0.0.1";
            try
            {
                var Address = IPAddress.Parse(TUNTAPAddressTextBox.Text);
                var Netmask = IPAddress.Parse(TUNTAPNetmaskTextBox.Text);
                var Gateway = IPAddress.Parse(TUNTAPGatewayTextBox.Text);

                var DNS = new List<IPAddress>();
                foreach (var ip in TUNTAPDNSTextBox.Text.Split(','))
                {
                    DNS.Add(IPAddress.Parse(ip));
                }
            }
            catch (FormatException)
            {
                MessageBoxX.Show(i18N.Translate("IP address format illegal. Try again."));

                TUNTAPAddressTextBox.Text = Global.Settings.TUNTAP.Address;
                TUNTAPNetmaskTextBox.Text = Global.Settings.TUNTAP.Netmask;
                TUNTAPGatewayTextBox.Text = Global.Settings.TUNTAP.Gateway;

                var DNS = "";
                foreach (var ip in Global.Settings.TUNTAP.DNS)
                {
                    DNS += ip;
                    DNS += ',';
                }

                DNS = DNS.Trim();
                TUNTAPDNSTextBox.Text = DNS.Substring(0, DNS.Length - 1);
                UseCustomDNSCheckBox.Checked = Global.Settings.TUNTAP.UseCustomDNS;

                return;
            }

            try
            {
                var ProfileCount = int.Parse(ProfileCountTextBox.Text);

                if (ProfileCount > -1)
                {
                    Global.Settings.ProfileCount = ProfileCount;
                }
                else
                {
                    throw new FormatException();
                }
            }
            catch (FormatException)
            {
                ProfileCountTextBox.Text = Global.Settings.ProfileCount.ToString();
                MessageBoxX.Show(i18N.Translate("ProfileCount value illegal. Try again."));

                return;
            }

            try
            {
                var stun = STUN_ServerComboBox.Text.Split(':');
                var STUN_Server = stun[0];
                Global.Settings.STUN_Server = STUN_Server;

                var STUN_ServerPort = 3478;
                if (stun.Length > 1)
                    STUN_ServerPort = int.Parse(stun[1]);

                if (STUN_ServerPort > 0)
                {
                    Global.Settings.STUN_Server_Port = STUN_ServerPort;
                }
                else
                {
                    throw new FormatException();
                }
            }
            catch (FormatException)
            {
                ProfileCountTextBox.Text = Global.Settings.ProfileCount.ToString();
                MessageBoxX.Show(i18N.Translate("STUN_ServerPort value illegal. Try again."));

                return;
            }

            try
            {
                Global.Settings.StartedTcping = TcpingAtStartedCheckBox.Checked;

                var DetectionInterval = int.Parse(DetectionIntervalTextBox.Text);

                if (DetectionInterval > 0)
                {
                    Global.Settings.StartedTcping_Interval = DetectionInterval;
                }
                else
                {
                    throw new FormatException();
                }
            }
            catch (FormatException)
            {
                ProfileCountTextBox.Text = Global.Settings.ProfileCount.ToString();
                MessageBoxX.Show(i18N.Translate("Detection interval value illegal. Try again."));

                return;
            }

            Global.Settings.ACL = AclAddrTextBox.Text;
            Global.Settings.TUNTAP.Address = TUNTAPAddressTextBox.Text;
            Global.Settings.TUNTAP.Netmask = TUNTAPNetmaskTextBox.Text;
            Global.Settings.TUNTAP.Gateway = TUNTAPGatewayTextBox.Text;
            Global.Settings.TUNTAP.DNS.Clear();
            foreach (var ip in TUNTAPDNSTextBox.Text.Split(','))
            {
                Global.Settings.TUNTAP.DNS.Add(ip);
            }

            Global.Settings.TUNTAP.UseCustomDNS = UseCustomDNSCheckBox.Checked;
            Global.Settings.TUNTAP.ProxyDNS = ProxyDNSCheckBox.Checked;
            Global.Settings.TUNTAP.UseFakeDNS = UseFakeDNSCheckBox.Checked;
            Global.Settings.ModifySystemDNS = ModifySystemDNSCheckBox.Checked;
            Configuration.Save();
            MessageBoxX.Show(i18N.Translate("Saved"));
            Close();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="portName"></param>
        /// <param name="portTextBox"></param>
        /// <param name="originPort"></param>
        /// <param name="portType"></param>
        /// <returns></returns>
        private bool CheckPortText(string portName, ref TextBox portTextBox, ref int originPort, PortType portType = PortType.Both)
        {
            // 端口检查
            try
            {
                var port = int.Parse(portTextBox.Text);

                if (port <= 0 || port >= 65536)
                {
                    throw new FormatException();
                }

                if (port == originPort)
                {
                    return true;
                }

                if (PortHelper.PortInUse(port, portType))
                {
                    MessageBoxX.Show(i18N.TranslateFormat("The {0} port is in use.", portName));
                    return false;
                }

                originPort = port;
            }
            catch (FormatException)
            {
                MessageBoxX.Show(i18N.Translate("Port value illegal. Try again."));
                return false;
            }

            return true;
        }
    }
}