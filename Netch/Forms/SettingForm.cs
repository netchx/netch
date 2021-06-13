using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows.Forms;
using Netch.Interfaces;
using Netch.Models;
using Netch.Properties;
using Netch.Services;
using Netch.Utils;
using Serilog;

namespace Netch.Forms
{
    public partial class SettingForm : Form
    {
        private readonly Setting _setting;
        private readonly IConfigService _configService;
        private readonly Dictionary<Control, Func<string, bool>> _checkActions = new();

        private readonly Dictionary<Control, Action<Control>> _saveActions = new();

        public SettingForm(Setting setting, IConfigService configService)
        {
            _setting = setting;
            _configService = configService;

            InitializeComponent();
            Icon = Resources.icon;
            i18N.TranslateForm(this);

            #region General

            BindTextBox<ushort>(Socks5PortTextBox,
                p => p.ToString() != HTTPPortTextBox.Text,
                p => _setting.Socks5LocalPort = p,
                _setting.Socks5LocalPort);

            BindTextBox<ushort>(HTTPPortTextBox,
                p => p.ToString() != Socks5PortTextBox.Text,
                p => _setting.HTTPLocalPort = p,
                _setting.HTTPLocalPort);

            BindCheckBox(AllowDevicesCheckBox,
                c => _setting.LocalAddress = AllowDevicesCheckBox.Checked ? "0.0.0.0" : "127.0.0.1",
                _setting.LocalAddress switch { "127.0.0.1" => false, "0.0.0.0" => true, _ => false });

            BindCheckBox(ResolveServerHostnameCheckBox, c => _setting.ResolveServerHostname = c, _setting.ResolveServerHostname);

            BindRadioBox(ICMPingRadioBtn, _ => { }, !_setting.ServerTCPing);

            BindRadioBox(TCPingRadioBtn, c => _setting.ServerTCPing = c, _setting.ServerTCPing);

            BindTextBox<int>(ProfileCountTextBox, i => i > -1, i => _setting.ProfileCount = i, _setting.ProfileCount);

            BindTextBox<int>(DetectionTickTextBox,
                i => ServerService.DelayTestHelper.Range.InRange(i),
                i => _setting.DetectionTick = i,
                _setting.DetectionTick);

            BindTextBox<int>(StartedPingIntervalTextBox, _ => true, i => _setting.StartedPingInterval = i, _setting.StartedPingInterval);

            object[]? stuns;
            try
            {
                stuns = File.ReadLines("bin\\stun.txt").Cast<object>().ToArray();
            }
            catch (Exception e)
            {
                Log.Warning(e, "Load stun.txt failed");
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
                    _setting.STUN_Server = split[0];

                    var port = split.ElementAtOrDefault(1);
                    _setting.STUN_Server_Port = port != null ? ushort.Parse(port) : 3478;
                },
                _setting.STUN_Server + ":" + _setting.STUN_Server_Port,
                stuns);

            BindListComboBox(LanguageComboBox, o => _setting.Language = o.ToString(), i18N.GetTranslateList(), _setting.Language);

            #endregion

            #region Process Mode

            BindListComboBox(ProcessFilterProtocolComboBox,
                s => _setting.Redirector.FilterProtocol = (PortType)Enum.Parse(typeof(PortType), s.ToString(), false),
                Enum.GetNames(typeof(PortType)),
                _setting.Redirector.FilterProtocol.ToString());

            BindCheckBox(DNSHijackCheckBox, b => _setting.Redirector.DNSHijack = b, _setting.Redirector.DNSHijack);

            BindTextBox(DNSHijackHostTextBox, s => true, s => _setting.Redirector.DNSHijackHost = s, _setting.Redirector.DNSHijackHost);

            BindCheckBox(FilterICMPCheckBox, b => _setting.Redirector.FilterICMP = b, _setting.Redirector.FilterICMP);

            BindTextBox(ICMPDelayTextBox, s => int.TryParse(s, out _), s => { }, _setting.Redirector.ICMPDelay);

            BindCheckBox(RedirectorSSCheckBox, s => _setting.Redirector.RedirectorSS = s, _setting.Redirector.RedirectorSS);

            BindCheckBox(ChildProcessHandleCheckBox, s => _setting.Redirector.ChildProcessHandle = s, _setting.Redirector.ChildProcessHandle);

            #endregion

            #region TUN/TAP

            BindTextBox(TUNTAPAddressTextBox, s => IPAddress.TryParse(s, out _), s => _setting.TUNTAP.Address = s, _setting.TUNTAP.Address);

            BindTextBox(TUNTAPNetmaskTextBox, s => IPAddress.TryParse(s, out _), s => _setting.TUNTAP.Netmask = s, _setting.TUNTAP.Netmask);

            BindTextBox(TUNTAPGatewayTextBox, s => IPAddress.TryParse(s, out _), s => _setting.TUNTAP.Gateway = s, _setting.TUNTAP.Gateway);

            BindCheckBox(UseCustomDNSCheckBox, b => { _setting.TUNTAP.UseCustomDNS = b; }, _setting.TUNTAP.UseCustomDNS);

            BindTextBox(TUNTAPDNSTextBox,
                _ => true,
                s =>
                {
                    if (UseCustomDNSCheckBox.Checked)
                        _setting.TUNTAP.HijackDNS = s;
                },
                _setting.TUNTAP.HijackDNS);

            BindCheckBox(ProxyDNSCheckBox, b => _setting.TUNTAP.ProxyDNS = b, _setting.TUNTAP.ProxyDNS);

            #endregion

            #region V2Ray

            BindCheckBox(XrayConeCheckBox, b => _setting.V2RayConfig.XrayCone = b, _setting.V2RayConfig.XrayCone);

            BindCheckBox(TLSAllowInsecureCheckBox, b => _setting.V2RayConfig.AllowInsecure = b, _setting.V2RayConfig.AllowInsecure);
            BindCheckBox(UseMuxCheckBox, b => _setting.V2RayConfig.UseMux = b, _setting.V2RayConfig.UseMux);

            BindTextBox<int>(mtuTextBox, i => true, i => _setting.V2RayConfig.KcpConfig.mtu = i, _setting.V2RayConfig.KcpConfig.mtu);
            BindTextBox<int>(ttiTextBox, i => true, i => _setting.V2RayConfig.KcpConfig.tti = i, _setting.V2RayConfig.KcpConfig.tti);
            BindTextBox<int>(uplinkCapacityTextBox,
                i => true,
                i => _setting.V2RayConfig.KcpConfig.uplinkCapacity = i,
                _setting.V2RayConfig.KcpConfig.uplinkCapacity);

            BindTextBox<int>(downlinkCapacityTextBox,
                i => true,
                i => _setting.V2RayConfig.KcpConfig.downlinkCapacity = i,
                _setting.V2RayConfig.KcpConfig.downlinkCapacity);

            BindTextBox<int>(readBufferSizeTextBox,
                i => true,
                i => _setting.V2RayConfig.KcpConfig.readBufferSize = i,
                _setting.V2RayConfig.KcpConfig.readBufferSize);

            BindTextBox<int>(writeBufferSizeTextBox,
                i => true,
                i => _setting.V2RayConfig.KcpConfig.writeBufferSize = i,
                _setting.V2RayConfig.KcpConfig.writeBufferSize);

            BindCheckBox(congestionCheckBox, b => _setting.V2RayConfig.KcpConfig.congestion = b, _setting.V2RayConfig.KcpConfig.congestion);

            #endregion

            #region Others

            BindCheckBox(ExitWhenClosedCheckBox, b => _setting.ExitWhenClosed = b, _setting.ExitWhenClosed);

            BindCheckBox(StopWhenExitedCheckBox, b => _setting.StopWhenExited = b, _setting.StopWhenExited);

            BindCheckBox(StartWhenOpenedCheckBox, b => _setting.StartWhenOpened = b, _setting.StartWhenOpened);

            BindCheckBox(MinimizeWhenStartedCheckBox, b => _setting.MinimizeWhenStarted = b, _setting.MinimizeWhenStarted);

            BindCheckBox(RunAtStartupCheckBox, b => _setting.RunAtStartup = b, _setting.RunAtStartup);

            BindCheckBox(CheckUpdateWhenOpenedCheckBox, b => _setting.CheckUpdateWhenOpened = b, _setting.CheckUpdateWhenOpened);

            BindCheckBox(CheckBetaUpdateCheckBox, b => _setting.CheckBetaUpdate = b, _setting.CheckBetaUpdate);

            BindCheckBox(UpdateServersWhenOpenedCheckBox, b => _setting.UpdateServersWhenOpened = b, _setting.UpdateServersWhenOpened);

            #endregion

            #region AioDNS

            BindTextBox(ChinaDNSTextBox, _ => true, s => _setting.AioDNS.ChinaDNS = s, _setting.AioDNS.ChinaDNS);

            BindTextBox(OtherDNSTextBox, _ => true, s => _setting.AioDNS.OtherDNS = s, _setting.AioDNS.OtherDNS);

            BindTextBox(AioDNSListenPortTextBox,
                s => ushort.TryParse(s, out _),
                s => _setting.AioDNS.ListenPort = ushort.Parse(s),
                _setting.AioDNS.ListenPort);

            #endregion
        }

        private void SettingForm_Load(object sender, EventArgs e)
        {
            TUNTAPUseCustomDNSCheckBox_CheckedChanged(null, null);
        }

        private void TUNTAPUseCustomDNSCheckBox_CheckedChanged(object? sender, EventArgs? e)
        {
            if (UseCustomDNSCheckBox.Checked)
                TUNTAPDNSTextBox.Text = _setting.TUNTAP.HijackDNS;
            else
                TUNTAPDNSTextBox.Text = "AioDNS";
        }

        private void GlobalBypassIPsButton_Click(object sender, EventArgs e)
        {
            Hide();
            DI.GetRequiredService<GlobalBypassIPForm>().ShowDialog();
            Show();
        }

        private async void ControlButton_Click(object sender, EventArgs e)
        {
            Misc.ComponentIterator(this, component => Misc.ChangeControlForeColor(component, Color.Black));

            #region Check

            var checkNotPassControl = _checkActions.Where(pair => !pair.Value.Invoke(pair.Key.Text)).Select(pair => pair.Key).ToList();
            foreach (Control control in checkNotPassControl)
                Misc.ChangeControlForeColor(control, Color.Red);

            if (checkNotPassControl.Any())
                return;

            #endregion

            #region Save

            foreach (var pair in _saveActions)
                pair.Value.Invoke(pair.Key);

            #endregion

            Misc.RegisterNetchStartupItem();

            await _configService.SaveAsync();
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