using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;
using Netch.Controllers;
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

        private void InitValue()
        {
            // Local Port
            Socks5PortTextBox.Text = Global.Settings.Socks5LocalPort.ToString();
            HTTPPortTextBox.Text = Global.Settings.HTTPLocalPort.ToString();
            RedirectorTextBox.Text = Global.Settings.RedirectorTCPPort.ToString();
            AllowDevicesCheckBox.Checked = Global.Settings.LocalAddress switch
            {
                "127.0.0.1" => false,
                "0.0.0.0" => true,
                _ => false
            };

            // TUN/TAP
            TUNTAPAddressTextBox.Text = Global.Settings.TUNTAP.Address;
            TUNTAPNetmaskTextBox.Text = Global.Settings.TUNTAP.Netmask;
            TUNTAPGatewayTextBox.Text = Global.Settings.TUNTAP.Gateway;
            UseCustomDNSCheckBox.Checked = Global.Settings.TUNTAP.UseCustomDNS;
            TUNTAPUseCustomDNSCheckBox_CheckedChanged(null, null);
            ProxyDNSCheckBox.Checked = Global.Settings.TUNTAP.ProxyDNS;
            UseFakeDNSCheckBox.Checked = Global.Settings.TUNTAP.UseFakeDNS;
            ICSCheckBox.Checked = ICSController.Enabled;

            // Behavior
            ExitWhenClosedCheckBox.Checked = Global.Settings.ExitWhenClosed;
            StopWhenExitedCheckBox.Checked = Global.Settings.StopWhenExited;
            StartWhenOpenedCheckBox.Checked = Global.Settings.StartWhenOpened;
            MinimizeWhenStartedCheckBox.Checked = Global.Settings.MinimizeWhenStarted;
            RunAtStartupCheckBox.Checked = Global.Settings.RunAtStartup;
            CheckUpdateWhenOpenedCheckBox.Checked = Global.Settings.CheckUpdateWhenOpened;
            BootShadowsocksFromDLLCheckBox.Checked = Global.Settings.BootShadowsocksFromDLL;
            CheckBetaUpdateCheckBox.Checked = Global.Settings.CheckBetaUpdate;
            ModifySystemDNSCheckBox.Checked = Global.Settings.ModifySystemDNS;
            UpdateSubscribeatWhenOpenedCheckBox.Checked = Global.Settings.UpdateSubscribeatWhenOpened;

            ProfileCountTextBox.Text = Global.Settings.ProfileCount.ToString();
            TcpingAtStartedCheckBox.Checked = Global.Settings.StartedTcping;
            DetectionIntervalTextBox.Text = Global.Settings.StartedTcping_Interval.ToString();
            InitSTUN();
            AclAddrTextBox.Text = Global.Settings.ACL;
            LanguageComboBox.Items.AddRange(i18N.GetTranslateList().ToArray());
            LanguageComboBox.SelectedItem = Global.Settings.Language;
        }

        private void TUNTAPUseCustomDNSCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            TUNTAPDNSTextBox.Enabled = UseCustomDNSCheckBox.Checked;

            if (UseCustomDNSCheckBox.Checked)
            {
                TUNTAPDNSTextBox.Text = Global.Settings.TUNTAP.DNS.Any()
                    ? Global.Settings.TUNTAP.DNS.Aggregate((current, ip) => $"{current},{ip}")
                    : "1.1.1.1";
            }
            else
            {
                TUNTAPDNSTextBox.Text = "Local DNS";
            }
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
            UpdateSubscribeatWhenOpenedCheckBox.Text = i18N.Translate(UpdateSubscribeatWhenOpenedCheckBox.Text);
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
            UseFakeDNSCheckBox.Visible = Global.SupportFakeDns;
            InitText();
            InitValue();
        }

        private void GlobalBypassIPsButton_Click(object sender, EventArgs e)
        {
            Hide();
            new GlobalBypassIPForm().ShowDialog();
            Show();
        }

        private void ControlButton_Click(object sender, EventArgs e)
        {
            #region Check

            #region Port

            int socks5LocalPort;
            int httpLocalPort;
            int redirectorTCPPort;
            try
            {
                socks5LocalPort = int.Parse(Socks5PortTextBox.Text);
                httpLocalPort = int.Parse(HTTPPortTextBox.Text);
                redirectorTCPPort = int.Parse(RedirectorTextBox.Text);

                static void CheckPort(string portName, int port, int originPort, PortType portType = PortType.Both)
                {
                    if (port <= 0 || port >= 65536)
                        throw new FormatException();

                    if (port == originPort)
                        return;

                    if (PortHelper.PortInUse(port, portType))
                    {
                        MessageBoxX.Show(i18N.TranslateFormat("The {0} port is in use.", portName));
                        throw new PortInUseException();
                    }
                }

                CheckPort("Socks5", socks5LocalPort, Global.Settings.Socks5LocalPort);
                CheckPort("HTTP", httpLocalPort, Global.Settings.HTTPLocalPort);
                CheckPort("RedirectorTCP", redirectorTCPPort, Global.Settings.RedirectorTCPPort);
            }
            catch (Exception exception)
            {
                switch (exception)
                {
                    case FormatException _:
                        MessageBoxX.Show(i18N.Translate("Port value illegal. Try again."));
                        break;
                    case PortInUseException _:
                        break;
                }

                return;
            }

            #endregion

            #region TUNTAP

            var dns = new string[0];
            try
            {
                IPAddress.Parse(TUNTAPAddressTextBox.Text);
                IPAddress.Parse(TUNTAPNetmaskTextBox.Text);
                IPAddress.Parse(TUNTAPGatewayTextBox.Text);

                if (UseCustomDNSCheckBox.Checked)
                {
                    dns = TUNTAPDNSTextBox.Text.Split(',').Where(s => !string.IsNullOrEmpty(s)).Select(s => s.Trim())
                        .ToArray();
                    if (dns.Any())
                    {
                        foreach (var ip in dns)
                            IPAddress.Parse(ip);
                    }
                    else
                    {
                        MessageBoxX.Show("DNS can not be empty");
                        return;
                    }
                }
            }
            catch (Exception exception)
            {
                if (exception is FormatException)
                    MessageBoxX.Show(i18N.Translate("IP address format illegal. Try again."));

                TUNTAPAddressTextBox.Text = Global.Settings.TUNTAP.Address;
                TUNTAPNetmaskTextBox.Text = Global.Settings.TUNTAP.Netmask;
                TUNTAPGatewayTextBox.Text = Global.Settings.TUNTAP.Gateway;
                UseCustomDNSCheckBox.Checked = Global.Settings.TUNTAP.UseCustomDNS;

                if (UseCustomDNSCheckBox.Checked)
                {
                    TUNTAPDNSTextBox.Text = Global.Settings.TUNTAP.DNS.Aggregate((current, ip) => $"{current},{ip}");
                }

                return;
            }

            #endregion

            #region Behavior

            // Profile
            int profileCount;
            try
            {
                profileCount = int.Parse(ProfileCountTextBox.Text);

                if (profileCount <= -1)
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

            // Started TCPing Interval
            int detectionInterval;
            try
            {
                detectionInterval = int.Parse(DetectionIntervalTextBox.Text);

                if (detectionInterval <= 0)
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

            // STUN
            string stunServer;
            int stunServerPort;
            try
            {
                var stun = STUN_ServerComboBox.Text.Split(':');
                stunServer = stun[0];

                stunServerPort = 3478;
                if (stun.Length > 1)
                    stunServerPort = int.Parse(stun[1]);

                if (stunServerPort <= 0)
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

            #endregion

            #endregion

            #region Save

            #region Port

            Global.Settings.Socks5LocalPort = socks5LocalPort;
            Global.Settings.HTTPLocalPort = httpLocalPort;
            Global.Settings.RedirectorTCPPort = redirectorTCPPort;
            Global.Settings.LocalAddress = AllowDevicesCheckBox.Checked ? "0.0.0.0" : "127.0.0.1";

            #endregion

            #region TUNTAP

            Global.Settings.TUNTAP.Address = TUNTAPAddressTextBox.Text;
            Global.Settings.TUNTAP.Netmask = TUNTAPNetmaskTextBox.Text;
            Global.Settings.TUNTAP.Gateway = TUNTAPGatewayTextBox.Text;
            Global.Settings.TUNTAP.UseCustomDNS = UseCustomDNSCheckBox.Checked;
            if (Global.Settings.TUNTAP.UseCustomDNS)
            {
                Global.Settings.TUNTAP.DNS.Clear();
                Global.Settings.TUNTAP.DNS.AddRange(dns);
            }

            Global.Settings.TUNTAP.ProxyDNS = ProxyDNSCheckBox.Checked;
            Global.Settings.TUNTAP.UseFakeDNS = UseFakeDNSCheckBox.Checked;

            #endregion

            #region Behavior

            Global.Settings.ExitWhenClosed = ExitWhenClosedCheckBox.Checked;
            Global.Settings.StopWhenExited = StopWhenExitedCheckBox.Checked;
            Global.Settings.StartWhenOpened = StartWhenOpenedCheckBox.Checked;
            Global.Settings.MinimizeWhenStarted = MinimizeWhenStartedCheckBox.Checked;
            Global.Settings.RunAtStartup = RunAtStartupCheckBox.Checked;
            Global.Settings.CheckUpdateWhenOpened = CheckUpdateWhenOpenedCheckBox.Checked;
            Global.Settings.BootShadowsocksFromDLL = BootShadowsocksFromDLLCheckBox.Checked;
            Global.Settings.CheckBetaUpdate = CheckBetaUpdateCheckBox.Checked;
            Global.Settings.ModifySystemDNS = ModifySystemDNSCheckBox.Checked;
            Global.Settings.UpdateSubscribeatWhenOpened = UpdateSubscribeatWhenOpenedCheckBox.Checked;

            Global.Settings.ProfileCount = profileCount;
            Global.Settings.StartedTcping = TcpingAtStartedCheckBox.Checked;
            Global.Settings.StartedTcping_Interval = detectionInterval;
            Global.Settings.STUN_Server = stunServer;
            Global.Settings.STUN_Server_Port = stunServerPort;
            Global.Settings.ACL = AclAddrTextBox.Text;
            Global.Settings.Language = LanguageComboBox.SelectedItem.ToString();

            #endregion

            #endregion

            #region Register Startup Item

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

            if (Global.Settings.RunAtStartup)
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

            #endregion

            Configuration.Save();
            MessageBoxX.Show(i18N.Translate("Saved"));
            Close();
        }

        private async void ICSCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            ICSCheckBox.Enabled = false;
            await Task.Run(() =>
            {
                if (ICSCheckBox.Checked)
                {
                    if (!ICSController.Enabled)
                        ICSCheckBox.Checked = ICSController.Enable();
                }
                else
                {
                    ICSController.Disable();
                }
            });
            ICSCheckBox.Enabled = true;
        }
    }
}