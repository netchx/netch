using Netch.Models;
using Netch.Properties;
using Netch.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows.Forms;

namespace Netch.Forms
{
    public partial class SettingForm : Form
    {
        private readonly Dictionary<Control, Func<string, bool>> _checkActions = new();

        private readonly Dictionary<Control, Action<Control>> _saveActions = new();

        public SettingForm()
        {
            InitializeComponent();
            Icon = Resources.icon;
            i18N.TranslateForm(this);

            #region General

            BindTextBox<ushort>(Socks5PortTextBox,
                p => p.ToString() != HTTPPortTextBox.Text,
                p => Global.Settings.Socks5LocalPort = p,
                Global.Settings.Socks5LocalPort);

            BindTextBox<ushort>(HTTPPortTextBox,
                p => p.ToString() != Socks5PortTextBox.Text,
                p => Global.Settings.HTTPLocalPort = p,
                Global.Settings.HTTPLocalPort);

            BindCheckBox(AllowDevicesCheckBox,
                c => Global.Settings.LocalAddress = AllowDevicesCheckBox.Checked ? "0.0.0.0" : "127.0.0.1",
                Global.Settings.LocalAddress switch { "127.0.0.1" => false, "0.0.0.0" => true, _ => false });

            BindCheckBox(ResolveServerHostnameCheckBox, c => Global.Settings.ResolveServerHostname = c, Global.Settings.ResolveServerHostname);

            BindRadioBox(ICMPingRadioBtn, _ => { }, !Global.Settings.ServerTCPing);

            BindRadioBox(TCPingRadioBtn, c => Global.Settings.ServerTCPing = c, Global.Settings.ServerTCPing);

            BindTextBox<int>(ProfileCountTextBox, i => i > -1, i => Global.Settings.ProfileCount = i, Global.Settings.ProfileCount);
            BindTextBox<int>(DetectionTickTextBox,
                i => ServerHelper.DelayTestHelper.Range.InRange(i),
                i => Global.Settings.DetectionTick = i,
                Global.Settings.DetectionTick);

            BindTextBox<int>(StartedPingIntervalTextBox,
                _ => true,
                i => Global.Settings.StartedPingInterval = i,
                Global.Settings.StartedPingInterval);

            object[]? stuns;
            try
            {
                stuns = File.ReadLines("bin\\stun.txt").Cast<object>().ToArray();
            }
            catch (Exception e)
            {
                Global.Logger.Warning($"Load stun.txt failed: {e.Message}");
                stuns = null;
            }

            BindComboBox(STUN_ServerComboBox,
                s =>
                {
                    var split = s.SplitRemoveEmptyEntriesAndTrimEntries(':');
                    if (!split.Any())
                        return false;

                    var port = split.ElementAtOrDefault(1);
                    if (port != null)
                        if (!ushort.TryParse(split[1], out _))
                            return false;

                    return true;
                },
                o =>
                {
                    var split = o.ToString().SplitRemoveEmptyEntriesAndTrimEntries(':');
                    Global.Settings.STUN_Server = split[0];

                    var port = split.ElementAtOrDefault(1);
                    Global.Settings.STUN_Server_Port = port != null ? ushort.Parse(port) : 3478;
                },
                Global.Settings.STUN_Server + ":" + Global.Settings.STUN_Server_Port,
                stuns);

            BindListComboBox(LanguageComboBox, o => Global.Settings.Language = o.ToString(), i18N.GetTranslateList(), Global.Settings.Language);

            #endregion

            #region Process Mode

            BindCheckBox(DNSHijackCheckBox, b => Global.Settings.Redirector.DNSHijack = b, Global.Settings.Redirector.DNSHijack);

            BindTextBox(DNSHijackHostTextBox, s => true, s => Global.Settings.Redirector.DNSHijackHost = s, Global.Settings.Redirector.DNSHijackHost);

            BindCheckBox(ICMPHijackCheckBox, b => Global.Settings.Redirector.ICMPHijack = b, Global.Settings.Redirector.ICMPHijack);

            BindTextBox(ICMPHijackHostTextBox,
                s => IPAddress.TryParse(s, out _),
                s => Global.Settings.Redirector.ICMPHost = s,
                Global.Settings.Redirector.ICMPHost);

            BindCheckBox(RedirectorSSCheckBox, s => Global.Settings.Redirector.RedirectorSS = s, Global.Settings.Redirector.RedirectorSS);

            BindCheckBox(ChildProcessHandleCheckBox,
                s => Global.Settings.Redirector.ChildProcessHandle = s,
                Global.Settings.Redirector.ChildProcessHandle);

            BindListComboBox(ProcessProxyProtocolComboBox,
                s => Global.Settings.Redirector.ProxyProtocol = (PortType)Enum.Parse(typeof(PortType), s.ToString(), false),
                Enum.GetNames(typeof(PortType)),
                Global.Settings.Redirector.ProxyProtocol.ToString());

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

            BindCheckBox(UseCustomDNSCheckBox, b => { Global.Settings.TUNTAP.UseCustomDNS = b; }, Global.Settings.TUNTAP.UseCustomDNS);

            BindTextBox(TUNTAPDNSTextBox,
                _ => true,
                s =>
                {
                    if (UseCustomDNSCheckBox.Checked)
                        Global.Settings.TUNTAP.HijackDNS = s;
                },
                Global.Settings.TUNTAP.HijackDNS);

            BindCheckBox(ProxyDNSCheckBox, b => Global.Settings.TUNTAP.ProxyDNS = b, Global.Settings.TUNTAP.ProxyDNS);

            #endregion

            #region V2Ray

            BindCheckBox(XrayConeCheckBox, b => Global.Settings.V2RayConfig.XrayCone = b, Global.Settings.V2RayConfig.XrayCone);

            BindCheckBox(TLSAllowInsecureCheckBox, b => Global.Settings.V2RayConfig.AllowInsecure = b, Global.Settings.V2RayConfig.AllowInsecure);
            BindCheckBox(UseMuxCheckBox, b => Global.Settings.V2RayConfig.UseMux = b, Global.Settings.V2RayConfig.UseMux);

            BindTextBox<int>(mtuTextBox, i => true, i => Global.Settings.V2RayConfig.KcpConfig.mtu = i, Global.Settings.V2RayConfig.KcpConfig.mtu);
            BindTextBox<int>(ttiTextBox, i => true, i => Global.Settings.V2RayConfig.KcpConfig.tti = i, Global.Settings.V2RayConfig.KcpConfig.tti);
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

            BindCheckBox(ExitWhenClosedCheckBox, b => Global.Settings.ExitWhenClosed = b, Global.Settings.ExitWhenClosed);

            BindCheckBox(StopWhenExitedCheckBox, b => Global.Settings.StopWhenExited = b, Global.Settings.StopWhenExited);

            BindCheckBox(StartWhenOpenedCheckBox, b => Global.Settings.StartWhenOpened = b, Global.Settings.StartWhenOpened);

            BindCheckBox(MinimizeWhenStartedCheckBox, b => Global.Settings.MinimizeWhenStarted = b, Global.Settings.MinimizeWhenStarted);

            BindCheckBox(RunAtStartupCheckBox, b => Global.Settings.RunAtStartup = b, Global.Settings.RunAtStartup);

            BindCheckBox(CheckUpdateWhenOpenedCheckBox, b => Global.Settings.CheckUpdateWhenOpened = b, Global.Settings.CheckUpdateWhenOpened);

            BindCheckBox(CheckBetaUpdateCheckBox, b => Global.Settings.CheckBetaUpdate = b, Global.Settings.CheckBetaUpdate);

            BindCheckBox(UpdateServersWhenOpenedCheckBox, b => Global.Settings.UpdateServersWhenOpened = b, Global.Settings.UpdateServersWhenOpened);

            #endregion

            #region AioDNS

            BindTextBox(AioDNSRulePathTextBox, _ => true, s => Global.Settings.AioDNS.RulePath = s, Global.Settings.AioDNS.RulePath);

            BindTextBox(ChinaDNSTextBox, _ => true, s => Global.Settings.AioDNS.ChinaDNS = s, Global.Settings.AioDNS.ChinaDNS);

            BindTextBox(OtherDNSTextBox, _ => true, s => Global.Settings.AioDNS.OtherDNS = s, Global.Settings.AioDNS.OtherDNS);

            #endregion
        }

        private void SettingForm_Load(object sender, EventArgs e)
        {
            TUNTAPUseCustomDNSCheckBox_CheckedChanged(null, null);
        }

        private void TUNTAPUseCustomDNSCheckBox_CheckedChanged(object? sender, EventArgs? e)
        {
            if (UseCustomDNSCheckBox.Checked)
                TUNTAPDNSTextBox.Text = Global.Settings.TUNTAP.HijackDNS;
            else
                TUNTAPDNSTextBox.Text = "AioDNS";
        }

        private void GlobalBypassIPsButton_Click(object sender, EventArgs e)
        {
            Hide();
            new GlobalBypassIPForm().ShowDialog();
            Show();
        }

        private void ControlButton_Click(object sender, EventArgs e)
        {
            Utils.Utils.ComponentIterator(this, component => Utils.Utils.ChangeControlForeColor(component, Color.Black));

            #region Check

            var checkNotPassControl = _checkActions.Where(pair => !pair.Value.Invoke(pair.Key.Text)).Select(pair => pair.Key).ToList();
            foreach (Control control in checkNotPassControl)
                Utils.Utils.ChangeControlForeColor(control, Color.Red);

            if (checkNotPassControl.Any())
                return;

            #endregion

            #region Save

            foreach (var pair in _saveActions)
                pair.Value.Invoke(pair.Key);

            #endregion

            Utils.Utils.RegisterNetchStartupItem();

            Configuration.Save();
            MessageBoxX.Show(i18N.Translate("Saved"));
            Close();
        }

        #region BindUtils

        private void BindTextBox(TextBox control, Func<string, bool> check, Action<string> save, object value)
        {
            BindTextBox<string>(control, check, save, value);
        }

        private void BindTextBox<T>(TextBox control, Func<T, bool> check, Action<T> save, object value)
        {
            control.Text = value.ToString();
            _checkActions.Add(control,
                s =>
                {
                    try
                    {
                        return check.Invoke((T)Convert.ChangeType(s, typeof(T)));
                    }
                    catch
                    {
                        return false;
                    }
                });

            _saveActions.Add(control, c => save.Invoke((T)Convert.ChangeType(((TextBox)c).Text, typeof(T))));
        }

        private void BindCheckBox(CheckBox control, Action<bool> save, bool value)
        {
            control.Checked = value;
            _saveActions.Add(control, c => save.Invoke(((CheckBox)c).Checked));
        }

        private void BindRadioBox(RadioButton control, Action<bool> save, bool value)
        {
            control.Checked = value;
            _saveActions.Add(control, c => save.Invoke(((RadioButton)c).Checked));
        }

        private void BindListComboBox<T>(ComboBox comboBox, Action<T> save, IEnumerable<T> values, T value) where T : notnull
        {
            if (comboBox.DropDownStyle != ComboBoxStyle.DropDownList)
                throw new ArgumentOutOfRangeException();

            var tagItems = values.Select(o => new TagItem<T>(o, o.ToString()!)).ToArray();
            comboBox.Items.AddRange(tagItems.Cast<object>().ToArray());

            comboBox.ValueMember = nameof(TagItem<T>.Value);
            comboBox.DisplayMember = nameof(TagItem<T>.Text);

            _saveActions.Add(comboBox, c => save.Invoke(((TagItem<T>)((ComboBox)c).SelectedItem).Value));
            Load += (_, _) => { comboBox.SelectedItem = tagItems.SingleOrDefault(t => t.Value.Equals(value)); };
        }

        private void BindComboBox(ComboBox control, Func<string, bool> check, Action<string> save, string value, object[]? values = null)
        {
            if (values != null)
                control.Items.AddRange(values);

            _saveActions.Add(control, c => save.Invoke(((ComboBox)c).Text));
            _checkActions.Add(control, check.Invoke);

            Load += (_, _) => { control.Text = value; };
        }

        #endregion
    }
}