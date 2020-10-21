using Netch.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;
using TaskScheduler;

namespace Netch.Forms
{
    public partial class SettingForm : Form
    {
        public SettingForm()
        {
            InitializeComponent();
            i18N.TranslateForm(this);
            InitValue();
        }


        private void SettingForm2_Load(object sender, EventArgs e)
        {
            Task.Run(() => BeginInvoke(new Action(() => UseFakeDNSCheckBox.Visible = Global.Flags.SupportFakeDns)));
        }

        private void InitValue()
        {
            #region General

            BindTextBox<ushort>(Socks5PortTextBox,
                p => CheckPort("Socks5", p, Global.Settings.Socks5LocalPort),
                p => Global.Settings.Socks5LocalPort = p,
                Global.Settings.Socks5LocalPort);
            BindTextBox<ushort>(HTTPPortTextBox,
                p => CheckPort("HTTP", p, Global.Settings.HTTPLocalPort),
                p => Global.Settings.HTTPLocalPort = p,
                Global.Settings.HTTPLocalPort);
            BindTextBox<ushort>(RedirectorTextBox,
                s => CheckPort("RedirectorTCP", s, Global.Settings.RedirectorTCPPort),
                s => Global.Settings.RedirectorTCPPort = s,
                Global.Settings.HTTPLocalPort);
            BindCheckBox(AllowDevicesCheckBox,
                c => Global.Settings.LocalAddress = AllowDevicesCheckBox.Checked ? "0.0.0.0" : "127.0.0.1",
                Global.Settings.LocalAddress switch
                {
                    "127.0.0.1" => false,
                    "0.0.0.0" => true,
                    _ => false
                });


            BindCheckBox(BootShadowsocksFromDLLCheckBox,
                c => Global.Settings.BootShadowsocksFromDLL = c,
                Global.Settings.BootShadowsocksFromDLL);
            BindCheckBox(ResolveServerHostnameCheckBox,
                c => Global.Settings.ResolveServerHostname = c,
                Global.Settings.ResolveServerHostname);

            BindTextBox<int>(ProfileCountTextBox,
                i => i > -1,
                i => Global.Settings.ProfileCount = i,
                Global.Settings.ProfileCount);
            BindCheckBox(TcpingAtStartedCheckBox,
                b => Global.Settings.StartedTcping = b,
                Global.Settings.StartedTcping);
            BindTextBox<int>(DetectionIntervalTextBox,
                i => i >= 0,
                i => Global.Settings.StartedTcping_Interval = i,
                Global.Settings.StartedTcping_Interval);

            InitSTUN();

            BindTextBox<string>(AclAddrTextBox,
                s => true,
                s => Global.Settings.ACL = s,
                Global.Settings.ACL);
            AclAddrTextBox.Text = Global.Settings.ACL;

            LanguageComboBox.Items.AddRange(i18N.GetTranslateList().ToArray());
            LanguageComboBox.SelectedItem = Global.Settings.Language;

            #endregion


            #region Process Mode

            BindCheckBox(ModifySystemDNSCheckBox,
                b => Global.Settings.ModifySystemDNS = b,
                Global.Settings.ModifySystemDNS);

            #endregion

            #region TUN/TAP

            BindTextBox(TUNTAPAddressTextBox,
                s => IPAddress.TryParse(s, out _),
                s => Global.Settings.TUNTAP.Address = s,
                Global.Settings.TUNTAP.Address);
            BindTextBox(TUNTAPNetmaskTextBox,
                s => IPAddress.TryParse(s, out _),
                s => Global.Settings.TUNTAP.Netmask = s,
                Global.Settings.TUNTAP.Netmask);
            BindTextBox(TUNTAPGatewayTextBox,
                s => IPAddress.TryParse(s, out _),
                s => Global.Settings.TUNTAP.Gateway = s,
                Global.Settings.TUNTAP.Gateway);
            BindCheckBox(UseCustomDNSCheckBox,
                b => { Global.Settings.TUNTAP.UseCustomDNS = b; },
                Global.Settings.TUNTAP.UseCustomDNS);
            TUNTAPUseCustomDNSCheckBox_CheckedChanged(null, null);

            BindCheckBox(ProxyDNSCheckBox,
                b => Global.Settings.TUNTAP.ProxyDNS = b,
                Global.Settings.TUNTAP.ProxyDNS);
            BindCheckBox(UseFakeDNSCheckBox,
                b => Global.Settings.TUNTAP.UseFakeDNS = b,
                Global.Settings.TUNTAP.UseFakeDNS);

            try
            {
                var icsHelperEnabled = ICSHelper.Enabled;
                if (icsHelperEnabled != null)
                {
                    ICSCheckBox.Enabled = true;
                    ICSCheckBox.Checked = (bool) icsHelperEnabled;
                }
            }
            catch
            {
                // ignored
            }

            #endregion

            #region V2Ray

            BindCheckBox(TLSAllowInsecureCheckBox,
                b => Global.Settings.V2RayConfig.AllowInsecure = b,
                Global.Settings.V2RayConfig.AllowInsecure);
            BindCheckBox(UseMuxCheckBox,
                b => Global.Settings.V2RayConfig.UseMux = b,
                Global.Settings.V2RayConfig.UseMux);

            BindTextBox<int>(mtuTextBox,
                i => true,
                i => Global.Settings.V2RayConfig.KcpConfig.mtu = i,
                Global.Settings.V2RayConfig.KcpConfig.mtu);
            BindTextBox<int>(ttiTextBox,
                i => true,
                i => Global.Settings.V2RayConfig.KcpConfig.tti = i,
                Global.Settings.V2RayConfig.KcpConfig.tti);
            BindTextBox<int>(uplinkCapacityTextBox,
                i => true,
                i => Global.Settings.V2RayConfig.KcpConfig.uplinkCapacity = i,
                Global.Settings.V2RayConfig.KcpConfig.uplinkCapacity);
            BindTextBox<int>(downlinkCapacityTextBox,
                i => true,
                i => Global.Settings.V2RayConfig.KcpConfig.downlinkCapacity = i,
                Global.Settings.V2RayConfig.KcpConfig.downlinkCapacity);
            BindTextBox<int>(readBufferSizeTextBox,
                i => true,
                i => Global.Settings.V2RayConfig.KcpConfig.readBufferSize = i,
                Global.Settings.V2RayConfig.KcpConfig.readBufferSize);
            BindTextBox<int>(writeBufferSizeTextBox,
                i => true,
                i => Global.Settings.V2RayConfig.KcpConfig.writeBufferSize = i,
                Global.Settings.V2RayConfig.KcpConfig.writeBufferSize);
            BindCheckBox(congestionCheckBox,
                b => Global.Settings.V2RayConfig.KcpConfig.congestion = b,
                Global.Settings.V2RayConfig.KcpConfig.congestion);

            #endregion

            #region Others

            BindCheckBox(ExitWhenClosedCheckBox,
                b => Global.Settings.ExitWhenClosed = b,
                Global.Settings.ExitWhenClosed);

            BindCheckBox(StopWhenExitedCheckBox,
                b => Global.Settings.StopWhenExited = b,
                Global.Settings.StopWhenExited);

            BindCheckBox(StartWhenOpenedCheckBox,
                b => Global.Settings.StartWhenOpened = b,
                Global.Settings.StartWhenOpened);

            BindCheckBox(MinimizeWhenStartedCheckBox,
                b => Global.Settings.MinimizeWhenStarted = b,
                Global.Settings.MinimizeWhenStarted);

            BindCheckBox(RunAtStartupCheckBox,
                b => Global.Settings.RunAtStartup = b,
                Global.Settings.RunAtStartup);

            BindCheckBox(CheckUpdateWhenOpenedCheckBox,
                b => Global.Settings.CheckUpdateWhenOpened = b,
                Global.Settings.CheckUpdateWhenOpened);

            BindCheckBox(CheckBetaUpdateCheckBox,
                b => Global.Settings.CheckBetaUpdate = b,
                Global.Settings.CheckBetaUpdate);

            BindCheckBox(UpdateSubscribeatWhenOpenedCheckBox,
                b => Global.Settings.UpdateSubscribeatWhenOpened = b,
                Global.Settings.UpdateSubscribeatWhenOpened);

            #endregion
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


        private void InitSTUN()
        {
            try
            {
                var stuns = File.ReadLines("bin\\stun.txt");
                STUN_ServerComboBox.Items.AddRange(stuns.ToArray());
            }
            catch
            {
                // ignored
            }

            STUN_ServerComboBox.Text = $"{Global.Settings.STUN_Server}:{Global.Settings.STUN_Server_Port}";
        }

        private void GlobalBypassIPsButton_Click(object sender, EventArgs e)
        {
            Hide();
            new GlobalBypassIPForm().ShowDialog();
            Show();
        }

        private void ControlButton_Click(object sender, EventArgs e)
        {
            if (!_checkActions.All(pair => pair.Value.Invoke(pair.Key.Text)))
                return;

            #region Check

            #region TUNTAP

            var dns = new string[0];
            try
            {
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

                if (UseCustomDNSCheckBox.Checked)
                {
                    TUNTAPDNSTextBox.Text = Global.Settings.TUNTAP.DNS.Aggregate((current, ip) => $"{current},{ip}");
                }

                return;
            }

            #endregion

            #region Behavior

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
                MessageBoxX.Show(i18N.Translate("STUN_ServerPort value illegal. Try again."));

                return;
            }

            #endregion

            #endregion

            #region Save

            foreach (var pair in _saveActions)
            {
                pair.Value.Invoke(pair.Key);
            }

            Global.Settings.TUNTAP.DNS = dns.ToList();
            Global.Settings.STUN_Server = stunServer;
            Global.Settings.STUN_Server_Port = stunServerPort;

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

        private bool CheckPort(string portName, ushort port, ushort originPort, PortType portType = PortType.Both)
        {
            if (port == originPort) return true;
            if (!PortHelper.PortInUse(port, portType)) return true;

            MessageBoxX.Show(i18N.TranslateFormat("The {0} port is in use.", portName));
            return false;
        }

        private async void ICSCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                ICSCheckBox.Enabled = false;
                await Task.Run(() =>
                {
                    if (ICSCheckBox.Checked)
                    {
                        if (!(ICSHelper.Enabled ?? true))
                            ICSCheckBox.Checked = ICSHelper.Enable();
                    }
                    else
                    {
                        ICSHelper.Disable();
                    }
                });
            }
            catch (Exception exception)
            {
                ICSCheckBox.Checked = false;
                Logging.Error(exception.ToString());
            }
            finally
            {
                ICSCheckBox.Enabled = true;
            }
        }

        private void BindTextBox(TextBox control, Func<string, bool> check, Action<string> save, object value)
        {
            BindTextBox<string>(control, check, save, value);
        }

        private void BindTextBox<T>(TextBox control, Func<T, bool> check, Action<T> save, object value)
        {
            control.Text = value.ToString();
            _checkActions.Add(control, s =>
            {
                try
                {
                    return check.Invoke((T) Convert.ChangeType(s, typeof(T)));
                }
                catch
                {
                    return false;
                }
            });
            _saveActions.Add(control, c => save.Invoke((T) Convert.ChangeType(((TextBox) c).Text, typeof(T))));
        }

        private void BindCheckBox(CheckBox control, Action<bool> save, bool value)
        {
            control.Checked = value;
            _checkActions.Add(control, s => true);
            _saveActions.Add(control, c => save.Invoke(((CheckBox) c).Checked));
        }

        private readonly Dictionary<Control, Func<string, bool>> _checkActions = new Dictionary<Control, Func<string, bool>>();

        private readonly Dictionary<Control, Action<Control>> _saveActions = new Dictionary<Control, Action<Control>>();
    }
}