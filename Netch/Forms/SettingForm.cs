using System.Net;
using Netch.Properties;
using Netch.Utils;

namespace Netch.Forms;

[Fody.ConfigureAwait(true)]
public partial class SettingForm : BindingForm
{
    public SettingForm()
    {
        InitializeComponent();
        Icon = Resources.icon;
        i18N.TranslateForm(this);

        #region General

        BindTextBox<ushort>(Socks5PortTextBox, p => true, p => Global.Settings.Socks5LocalPort = p, Global.Settings.Socks5LocalPort);

        BindCheckBox(AllowDevicesCheckBox,
            c => Global.Settings.LocalAddress = AllowDevicesCheckBox.Checked ? "0.0.0.0" : "127.0.0.1",
            Global.Settings.LocalAddress switch { "127.0.0.1" => false, "0.0.0.0" => true, _ => false });

        BindRadioBox(ICMPingRadioBtn, _ => { }, !Global.Settings.ServerTCPing);

        BindRadioBox(TCPingRadioBtn, c => Global.Settings.ServerTCPing = c, Global.Settings.ServerTCPing);

        BindTextBox<int>(ProfileCountTextBox, i => i > -1, i => Global.Settings.ProfileCount = i, Global.Settings.ProfileCount);
        BindTextBox<int>(DetectionTickTextBox,
            i => DelayTestHelper.Range.InRange(i),
            i => Global.Settings.DetectionTick = i,
            Global.Settings.DetectionTick);

        BindTextBox<int>(StartedPingIntervalTextBox, _ => true, i => Global.Settings.StartedPingInterval = i, Global.Settings.StartedPingInterval);

        object[]? stuns;
        try
        {
            stuns = File.ReadLines(Constants.STUNServersFile).Cast<object>().ToArray();
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
                Global.Settings.STUN_Server = split[0];

                var port = split.ElementAtOrDefault(1);
                Global.Settings.STUN_Server_Port = port != null ? ushort.Parse(port) : 3478;
            },
            Global.Settings.STUN_Server + ":" + Global.Settings.STUN_Server_Port,
            stuns);

        BindListComboBox(LanguageComboBox, o => Global.Settings.Language = o.ToString(), i18N.GetTranslateList(), Global.Settings.Language);

        #endregion

        #region Process Mode

        BindCheckBox(FilterTCPCheckBox, b => Global.Settings.Redirector.FilterTCP = b, Global.Settings.Redirector.FilterTCP);

        BindCheckBox(FilterUDPCheckBox, b => Global.Settings.Redirector.FilterUDP = b, Global.Settings.Redirector.FilterUDP);

        BindCheckBox(FilterICMPCheckBox, b => Global.Settings.Redirector.FilterICMP = b, Global.Settings.Redirector.FilterICMP);

        BindTextBox<int>(ICMPDelayTextBox, s => true, s => Global.Settings.Redirector.ICMPDelay = s, Global.Settings.Redirector.ICMPDelay);

        BindCheckBox(FilterDNSCheckBox, b => Global.Settings.Redirector.FilterDNS = b, Global.Settings.Redirector.FilterDNS);

        // TODO validate Redirector AioDNS DNS
        BindTextBox(DNSHijackHostTextBox, s => true, s => Global.Settings.Redirector.DNSHost = s, Global.Settings.Redirector.DNSHost);

        BindCheckBox(ChildProcessHandleCheckBox, s => Global.Settings.Redirector.FilterParent = s, Global.Settings.Redirector.FilterParent);

        BindCheckBox(DNSProxyCheckBox, b => Global.Settings.Redirector.DNSProxy = b, Global.Settings.Redirector.DNSProxy);

        BindCheckBox(HandleProcDNSCheckBox, b => Global.Settings.Redirector.HandleOnlyDNS = b, Global.Settings.Redirector.HandleOnlyDNS);

        #endregion

        #region TUN/TAP

        BindTextBox(TUNTAPAddressTextBox, s => IPAddress.TryParse(s, out _), s => Global.Settings.TUNTAP.Address = s, Global.Settings.TUNTAP.Address);

        BindTextBox(TUNTAPNetmaskTextBox, s => IPAddress.TryParse(s, out _), s => Global.Settings.TUNTAP.Netmask = s, Global.Settings.TUNTAP.Netmask);

        BindTextBox(TUNTAPGatewayTextBox, s => IPAddress.TryParse(s, out _), s => Global.Settings.TUNTAP.Gateway = s, Global.Settings.TUNTAP.Gateway);

        BindCheckBox(UseCustomDNSCheckBox, b => { Global.Settings.TUNTAP.UseCustomDNS = b; }, Global.Settings.TUNTAP.UseCustomDNS);

        BindTextBox(TUNTAPDNSTextBox,
            s => UseCustomDNSCheckBox.Checked ? IPAddress.TryParse(s, out _) : true,
            s =>
            {
                if (UseCustomDNSCheckBox.Checked)
                    Global.Settings.TUNTAP.DNS = s;
            },
            Global.Settings.TUNTAP.DNS);

        BindCheckBox(ProxyDNSCheckBox, b => Global.Settings.TUNTAP.ProxyDNS = b, Global.Settings.TUNTAP.ProxyDNS);

        #endregion

        #region V2Ray
        BindCheckBox(XrayConeCheckBox, b => Global.Settings.V2RayConfig.XrayCone = b, Global.Settings.V2RayConfig.XrayCone);

        BindCheckBox(TLSAllowInsecureCheckBox, b => Global.Settings.V2RayConfig.AllowInsecure = b, Global.Settings.V2RayConfig.AllowInsecure);
        BindCheckBox(UseMuxCheckBox, b => Global.Settings.V2RayConfig.UseMux = b, Global.Settings.V2RayConfig.UseMux);
        BindCheckBox(TCPFastOpenBox, b => Global.Settings.V2RayConfig.TCPFastOpen = b, Global.Settings.V2RayConfig.TCPFastOpen);

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

        BindCheckBox(congestionCheckBox, b => Global.Settings.V2RayConfig.KcpConfig.congestion = b, Global.Settings.V2RayConfig.KcpConfig.congestion);

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

        BindCheckBox(NoSupportDialogCheckBox, b => Global.Settings.NoSupportDialog = b, Global.Settings.NoSupportDialog);

        #endregion

        #region AioDNS

        BindTextBox(ChinaDNSTextBox, _ => true, s => Global.Settings.AioDNS.ChinaDNS = s, Global.Settings.AioDNS.ChinaDNS);

        BindTextBox(OtherDNSTextBox, _ => true, s => Global.Settings.AioDNS.OtherDNS = s, Global.Settings.AioDNS.OtherDNS);

        BindTextBox(AioDNSListenPortTextBox,
            s => ushort.TryParse(s, out _),
            s => Global.Settings.AioDNS.ListenPort = ushort.Parse(s),
            Global.Settings.AioDNS.ListenPort);

        #endregion
    }

    private void SettingForm_Load(object sender, EventArgs e)
    {
        TUNTAPUseCustomDNSCheckBox_CheckedChanged(null, null);
    }

    protected override void BindTextBox<T>(TextBoxBase control, Func<T, bool> check, Action<T> save, object value)
    {
        base.BindTextBox(control, check, save, value);
        control.TextChanged += (_, _) =>
        {
            if (Validate(control))
            {
                errorProvider.SetError(control, null);
            }
            else
            {
                errorProvider.SetError(control, i18N.Translate("Invalid value"));
            }
        };
    }

    protected new void BindComboBox(ComboBox control, Func<string, bool> check, Action<string> save, string value, object[]? values = null)
    {
        base.BindComboBox(control, check, save, value, values);

        control.TextChanged += (_, _) =>
        {
            if (Validate(control))
            {
                errorProvider.SetError(control, null);
            }
            else
            {
                errorProvider.SetError(control, i18N.Translate("Invalid value"));
            }
        };
    }

    private void TUNTAPUseCustomDNSCheckBox_CheckedChanged(object? sender, EventArgs? e)
    {
        if (UseCustomDNSCheckBox.Checked)
            TUNTAPDNSTextBox.Text = Global.Settings.TUNTAP.DNS;
        else
            TUNTAPDNSTextBox.Text = "AioDNS";
    }

    private void GlobalBypassIPsButton_Click(object sender, EventArgs e)
    {
        Hide();
        new GlobalBypassIPForm().ShowDialog();
        Show();
    }

    private async void ControlButton_Click(object sender, EventArgs e)
    {
        Utils.Utils.ComponentIterator(this, component => Utils.Utils.ChangeControlForeColor(component, Color.Black));

        #region Check

        var checkNotPassControl = GetInvalidateValueControls();

        if (checkNotPassControl.Any())
        {
            var failControl = checkNotPassControl.First();

            // switch to fail control's tab page
            var p = failControl.Parent;
            while (p != null)
            {
                if (p is TabPage tabPage)
                {
                    TabControl.SelectedTab = tabPage;
                    break;
                }

                p = p.Parent;
            }

            return;
        }

        #endregion

        #region Save

        SaveBinds();

        #endregion

        Utils.Utils.RegisterNetchStartupItem();

        await Configuration.SaveAsync();
        MessageBoxX.Show(i18N.Translate("Saved"));
        Close();
    }
}