using System.ComponentModel;

namespace Netch.Forms
{
    partial class SettingForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }

            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.TabControl = new System.Windows.Forms.TabControl();
            this.GeneralTabPage = new System.Windows.Forms.TabPage();
            this.PortGroupBox = new System.Windows.Forms.GroupBox();
            this.Socks5PortLabel = new System.Windows.Forms.Label();
            this.Socks5PortTextBox = new System.Windows.Forms.TextBox();
            this.AllowDevicesCheckBox = new System.Windows.Forms.CheckBox();
            this.ServerPingTypeLabel = new System.Windows.Forms.Label();
            this.ICMPingRadioBtn = new System.Windows.Forms.RadioButton();
            this.TCPingRadioBtn = new System.Windows.Forms.RadioButton();
            this.ProfileCountLabel = new System.Windows.Forms.Label();
            this.ProfileCountTextBox = new System.Windows.Forms.TextBox();
            this.DetectionTickLabel = new System.Windows.Forms.Label();
            this.DetectionTickTextBox = new System.Windows.Forms.TextBox();
            this.StartedPingLabel = new System.Windows.Forms.Label();
            this.StartedPingIntervalTextBox = new System.Windows.Forms.TextBox();
            this.STUNServerLabel = new System.Windows.Forms.Label();
            this.STUN_ServerComboBox = new System.Windows.Forms.ComboBox();
            this.LanguageLabel = new System.Windows.Forms.Label();
            this.LanguageComboBox = new System.Windows.Forms.ComboBox();
            this.NFTabPage = new System.Windows.Forms.TabPage();
            this.FilterTCPCheckBox = new System.Windows.Forms.CheckBox();
            this.FilterUDPCheckBox = new System.Windows.Forms.CheckBox();
            this.FilterICMPCheckBox = new System.Windows.Forms.CheckBox();
            this.DNSHijackLabel = new System.Windows.Forms.Label();
            this.ICMPDelayLabel = new System.Windows.Forms.Label();
            this.ICMPDelayTextBox = new System.Windows.Forms.TextBox();
            this.FilterDNSCheckBox = new System.Windows.Forms.CheckBox();
            this.DNSHijackHostTextBox = new System.Windows.Forms.TextBox();
            this.HandleProcDNSCheckBox = new System.Windows.Forms.CheckBox();
            this.DNSProxyCheckBox = new System.Windows.Forms.CheckBox();
            this.ChildProcessHandleCheckBox = new System.Windows.Forms.CheckBox();
            this.WinTUNTabPage = new System.Windows.Forms.TabPage();
            this.WinTUNGroupBox = new System.Windows.Forms.GroupBox();
            this.TUNTAPAddressLabel = new System.Windows.Forms.Label();
            this.TUNTAPAddressTextBox = new System.Windows.Forms.TextBox();
            this.TUNTAPNetmaskLabel = new System.Windows.Forms.Label();
            this.TUNTAPNetmaskTextBox = new System.Windows.Forms.TextBox();
            this.TUNTAPGatewayLabel = new System.Windows.Forms.Label();
            this.TUNTAPGatewayTextBox = new System.Windows.Forms.TextBox();
            this.TUNTAPDNSLabel = new System.Windows.Forms.Label();
            this.TUNTAPDNSTextBox = new System.Windows.Forms.TextBox();
            this.UseCustomDNSCheckBox = new System.Windows.Forms.CheckBox();
            this.ProxyDNSCheckBox = new System.Windows.Forms.CheckBox();
            this.GlobalBypassIPsButton = new System.Windows.Forms.Button();
            this.v2rayTabPage = new System.Windows.Forms.TabPage();
            this.XrayConeCheckBox = new System.Windows.Forms.CheckBox();
            this.TLSAllowInsecureCheckBox = new System.Windows.Forms.CheckBox();
            this.UseMuxCheckBox = new System.Windows.Forms.CheckBox();
            this.TCPFastOpenBox = new System.Windows.Forms.CheckBox();
            this.KCPGroupBox = new System.Windows.Forms.GroupBox();
            this.mtuLabel = new System.Windows.Forms.Label();
            this.mtuTextBox = new System.Windows.Forms.TextBox();
            this.ttiLabel = new System.Windows.Forms.Label();
            this.ttiTextBox = new System.Windows.Forms.TextBox();
            this.uplinkCapacityLabel = new System.Windows.Forms.Label();
            this.uplinkCapacityTextBox = new System.Windows.Forms.TextBox();
            this.downlinkCapacityLabel = new System.Windows.Forms.Label();
            this.downlinkCapacityTextBox = new System.Windows.Forms.TextBox();
            this.readBufferSizeLabel = new System.Windows.Forms.Label();
            this.readBufferSizeTextBox = new System.Windows.Forms.TextBox();
            this.writeBufferSizeLabel = new System.Windows.Forms.Label();
            this.writeBufferSizeTextBox = new System.Windows.Forms.TextBox();
            this.congestionCheckBox = new System.Windows.Forms.CheckBox();
            this.OtherTabPage = new System.Windows.Forms.TabPage();
            this.ExitWhenClosedCheckBox = new System.Windows.Forms.CheckBox();
            this.StopWhenExitedCheckBox = new System.Windows.Forms.CheckBox();
            this.StartWhenOpenedCheckBox = new System.Windows.Forms.CheckBox();
            this.MinimizeWhenStartedCheckBox = new System.Windows.Forms.CheckBox();
            this.RunAtStartupCheckBox = new System.Windows.Forms.CheckBox();
            this.CheckUpdateWhenOpenedCheckBox = new System.Windows.Forms.CheckBox();
            this.NoSupportDialogCheckBox = new System.Windows.Forms.CheckBox();
            this.CheckBetaUpdateCheckBox = new System.Windows.Forms.CheckBox();
            this.UpdateServersWhenOpenedCheckBox = new System.Windows.Forms.CheckBox();
            this.AioDNSTabPage = new System.Windows.Forms.TabPage();
            this.ChinaDNSLabel = new System.Windows.Forms.Label();
            this.ChinaDNSTextBox = new System.Windows.Forms.TextBox();
            this.OtherDNSLabel = new System.Windows.Forms.Label();
            this.OtherDNSTextBox = new System.Windows.Forms.TextBox();
            this.AioDNSListenPortLabel = new System.Windows.Forms.Label();
            this.AioDNSListenPortTextBox = new System.Windows.Forms.TextBox();
            this.ControlButton = new System.Windows.Forms.Button();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.errorProvider = new System.Windows.Forms.ErrorProvider(this.components);
            this.TabControl.SuspendLayout();
            this.GeneralTabPage.SuspendLayout();
            this.PortGroupBox.SuspendLayout();
            this.NFTabPage.SuspendLayout();
            this.WinTUNTabPage.SuspendLayout();
            this.WinTUNGroupBox.SuspendLayout();
            this.v2rayTabPage.SuspendLayout();
            this.KCPGroupBox.SuspendLayout();
            this.OtherTabPage.SuspendLayout();
            this.AioDNSTabPage.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.errorProvider)).BeginInit();
            this.SuspendLayout();
            // 
            // TabControl
            // 
            this.TabControl.Appearance = System.Windows.Forms.TabAppearance.FlatButtons;
            this.TabControl.Controls.Add(this.GeneralTabPage);
            this.TabControl.Controls.Add(this.NFTabPage);
            this.TabControl.Controls.Add(this.WinTUNTabPage);
            this.TabControl.Controls.Add(this.v2rayTabPage);
            this.TabControl.Controls.Add(this.OtherTabPage);
            this.TabControl.Controls.Add(this.AioDNSTabPage);
            this.TabControl.Location = new System.Drawing.Point(3, 3);
            this.TabControl.Name = "TabControl";
            this.TabControl.SelectedIndex = 0;
            this.TabControl.Size = new System.Drawing.Size(469, 354);
            this.TabControl.TabIndex = 0;
            // 
            // GeneralTabPage
            // 
            this.GeneralTabPage.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.GeneralTabPage.Controls.Add(this.PortGroupBox);
            this.GeneralTabPage.Controls.Add(this.ServerPingTypeLabel);
            this.GeneralTabPage.Controls.Add(this.ICMPingRadioBtn);
            this.GeneralTabPage.Controls.Add(this.TCPingRadioBtn);
            this.GeneralTabPage.Controls.Add(this.ProfileCountLabel);
            this.GeneralTabPage.Controls.Add(this.ProfileCountTextBox);
            this.GeneralTabPage.Controls.Add(this.DetectionTickLabel);
            this.GeneralTabPage.Controls.Add(this.DetectionTickTextBox);
            this.GeneralTabPage.Controls.Add(this.StartedPingLabel);
            this.GeneralTabPage.Controls.Add(this.StartedPingIntervalTextBox);
            this.GeneralTabPage.Controls.Add(this.STUNServerLabel);
            this.GeneralTabPage.Controls.Add(this.STUN_ServerComboBox);
            this.GeneralTabPage.Controls.Add(this.LanguageLabel);
            this.GeneralTabPage.Controls.Add(this.LanguageComboBox);
            this.GeneralTabPage.Location = new System.Drawing.Point(4, 29);
            this.GeneralTabPage.Name = "GeneralTabPage";
            this.GeneralTabPage.Padding = new System.Windows.Forms.Padding(3);
            this.GeneralTabPage.Size = new System.Drawing.Size(461, 321);
            this.GeneralTabPage.TabIndex = 0;
            this.GeneralTabPage.Text = "General";
            // 
            // PortGroupBox
            // 
            this.PortGroupBox.Controls.Add(this.Socks5PortLabel);
            this.PortGroupBox.Controls.Add(this.Socks5PortTextBox);
            this.PortGroupBox.Controls.Add(this.AllowDevicesCheckBox);
            this.PortGroupBox.Location = new System.Drawing.Point(8, 6);
            this.PortGroupBox.Name = "PortGroupBox";
            this.PortGroupBox.Size = new System.Drawing.Size(241, 115);
            this.PortGroupBox.TabIndex = 0;
            this.PortGroupBox.TabStop = false;
            this.PortGroupBox.Text = "Local Port";
            // 
            // Socks5PortLabel
            // 
            this.Socks5PortLabel.AutoSize = true;
            this.Socks5PortLabel.Location = new System.Drawing.Point(9, 25);
            this.Socks5PortLabel.Name = "Socks5PortLabel";
            this.Socks5PortLabel.Size = new System.Drawing.Size(49, 17);
            this.Socks5PortLabel.TabIndex = 0;
            this.Socks5PortLabel.Text = "Socks5";
            // 
            // Socks5PortTextBox
            // 
            this.Socks5PortTextBox.Location = new System.Drawing.Point(120, 22);
            this.Socks5PortTextBox.Name = "Socks5PortTextBox";
            this.Socks5PortTextBox.Size = new System.Drawing.Size(90, 23);
            this.Socks5PortTextBox.TabIndex = 1;
            this.Socks5PortTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // AllowDevicesCheckBox
            // 
            this.AllowDevicesCheckBox.AutoSize = true;
            this.AllowDevicesCheckBox.Location = new System.Drawing.Point(6, 84);
            this.AllowDevicesCheckBox.Name = "AllowDevicesCheckBox";
            this.AllowDevicesCheckBox.Size = new System.Drawing.Size(206, 21);
            this.AllowDevicesCheckBox.TabIndex = 4;
            this.AllowDevicesCheckBox.Text = "Allow other Devices to connect";
            this.AllowDevicesCheckBox.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.AllowDevicesCheckBox.UseVisualStyleBackColor = true;
            // 
            // ServerPingTypeLabel
            // 
            this.ServerPingTypeLabel.AutoSize = true;
            this.ServerPingTypeLabel.Location = new System.Drawing.Point(267, 15);
            this.ServerPingTypeLabel.Name = "ServerPingTypeLabel";
            this.ServerPingTypeLabel.Size = new System.Drawing.Size(86, 17);
            this.ServerPingTypeLabel.TabIndex = 2;
            this.ServerPingTypeLabel.Text = "Ping Protocol";
            // 
            // ICMPingRadioBtn
            // 
            this.ICMPingRadioBtn.AutoSize = true;
            this.ICMPingRadioBtn.Location = new System.Drawing.Point(268, 34);
            this.ICMPingRadioBtn.Name = "ICMPingRadioBtn";
            this.ICMPingRadioBtn.Size = new System.Drawing.Size(75, 21);
            this.ICMPingRadioBtn.TabIndex = 3;
            this.ICMPingRadioBtn.TabStop = true;
            this.ICMPingRadioBtn.Text = "ICMPing";
            this.ICMPingRadioBtn.UseVisualStyleBackColor = true;
            // 
            // TCPingRadioBtn
            // 
            this.TCPingRadioBtn.AutoSize = true;
            this.TCPingRadioBtn.Location = new System.Drawing.Point(366, 35);
            this.TCPingRadioBtn.Name = "TCPingRadioBtn";
            this.TCPingRadioBtn.Size = new System.Drawing.Size(66, 21);
            this.TCPingRadioBtn.TabIndex = 4;
            this.TCPingRadioBtn.TabStop = true;
            this.TCPingRadioBtn.Text = "TCPing";
            this.TCPingRadioBtn.UseVisualStyleBackColor = true;
            // 
            // ProfileCountLabel
            // 
            this.ProfileCountLabel.AutoSize = true;
            this.ProfileCountLabel.Location = new System.Drawing.Point(15, 140);
            this.ProfileCountLabel.Name = "ProfileCountLabel";
            this.ProfileCountLabel.Size = new System.Drawing.Size(83, 17);
            this.ProfileCountLabel.TabIndex = 5;
            this.ProfileCountLabel.Text = "Profile Count";
            // 
            // ProfileCountTextBox
            // 
            this.ProfileCountTextBox.Location = new System.Drawing.Point(182, 137);
            this.ProfileCountTextBox.Name = "ProfileCountTextBox";
            this.ProfileCountTextBox.Size = new System.Drawing.Size(70, 23);
            this.ProfileCountTextBox.TabIndex = 6;
            this.ProfileCountTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // DetectionTickLabel
            // 
            this.DetectionTickLabel.AutoSize = true;
            this.DetectionTickLabel.Location = new System.Drawing.Point(15, 170);
            this.DetectionTickLabel.Name = "DetectionTickLabel";
            this.DetectionTickLabel.Size = new System.Drawing.Size(117, 17);
            this.DetectionTickLabel.TabIndex = 7;
            this.DetectionTickLabel.Text = "Detection Tick(sec)";
            // 
            // DetectionTickTextBox
            // 
            this.DetectionTickTextBox.Location = new System.Drawing.Point(182, 167);
            this.DetectionTickTextBox.Name = "DetectionTickTextBox";
            this.DetectionTickTextBox.Size = new System.Drawing.Size(70, 23);
            this.DetectionTickTextBox.TabIndex = 8;
            this.DetectionTickTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // StartedPingLabel
            // 
            this.StartedPingLabel.AutoSize = true;
            this.StartedPingLabel.Location = new System.Drawing.Point(15, 200);
            this.StartedPingLabel.Name = "StartedPingLabel";
            this.StartedPingLabel.Size = new System.Drawing.Size(153, 17);
            this.StartedPingLabel.TabIndex = 9;
            this.StartedPingLabel.Text = "Delay test after start(sec)";
            // 
            // StartedPingIntervalTextBox
            // 
            this.StartedPingIntervalTextBox.Location = new System.Drawing.Point(182, 197);
            this.StartedPingIntervalTextBox.Name = "StartedPingIntervalTextBox";
            this.StartedPingIntervalTextBox.Size = new System.Drawing.Size(70, 23);
            this.StartedPingIntervalTextBox.TabIndex = 10;
            this.StartedPingIntervalTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // STUNServerLabel
            // 
            this.STUNServerLabel.AutoSize = true;
            this.STUNServerLabel.Location = new System.Drawing.Point(15, 230);
            this.STUNServerLabel.Name = "STUNServerLabel";
            this.STUNServerLabel.Size = new System.Drawing.Size(82, 17);
            this.STUNServerLabel.TabIndex = 11;
            this.STUNServerLabel.Text = "STUN Server";
            // 
            // STUN_ServerComboBox
            // 
            this.STUN_ServerComboBox.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.STUN_ServerComboBox.Location = new System.Drawing.Point(182, 227);
            this.STUN_ServerComboBox.Name = "STUN_ServerComboBox";
            this.STUN_ServerComboBox.Size = new System.Drawing.Size(264, 25);
            this.STUN_ServerComboBox.TabIndex = 12;
            // 
            // LanguageLabel
            // 
            this.LanguageLabel.AutoSize = true;
            this.LanguageLabel.Location = new System.Drawing.Point(15, 260);
            this.LanguageLabel.Name = "LanguageLabel";
            this.LanguageLabel.Size = new System.Drawing.Size(65, 17);
            this.LanguageLabel.TabIndex = 13;
            this.LanguageLabel.Text = "Language";
            // 
            // LanguageComboBox
            // 
            this.LanguageComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.LanguageComboBox.FormattingEnabled = true;
            this.LanguageComboBox.Location = new System.Drawing.Point(182, 257);
            this.LanguageComboBox.Name = "LanguageComboBox";
            this.LanguageComboBox.Size = new System.Drawing.Size(110, 25);
            this.LanguageComboBox.TabIndex = 14;
            // 
            // NFTabPage
            // 
            this.NFTabPage.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.NFTabPage.Controls.Add(this.FilterTCPCheckBox);
            this.NFTabPage.Controls.Add(this.FilterUDPCheckBox);
            this.NFTabPage.Controls.Add(this.FilterICMPCheckBox);
            this.NFTabPage.Controls.Add(this.DNSHijackLabel);
            this.NFTabPage.Controls.Add(this.ICMPDelayLabel);
            this.NFTabPage.Controls.Add(this.ICMPDelayTextBox);
            this.NFTabPage.Controls.Add(this.FilterDNSCheckBox);
            this.NFTabPage.Controls.Add(this.DNSHijackHostTextBox);
            this.NFTabPage.Controls.Add(this.HandleProcDNSCheckBox);
            this.NFTabPage.Controls.Add(this.DNSProxyCheckBox);
            this.NFTabPage.Controls.Add(this.ChildProcessHandleCheckBox);
            this.NFTabPage.Location = new System.Drawing.Point(4, 29);
            this.NFTabPage.Name = "NFTabPage";
            this.NFTabPage.Padding = new System.Windows.Forms.Padding(3);
            this.NFTabPage.Size = new System.Drawing.Size(461, 321);
            this.NFTabPage.TabIndex = 1;
            this.NFTabPage.Text = "Process Mode";
            // 
            // FilterTCPCheckBox
            // 
            this.FilterTCPCheckBox.AutoSize = true;
            this.FilterTCPCheckBox.Location = new System.Drawing.Point(16, 16);
            this.FilterTCPCheckBox.Name = "FilterTCPCheckBox";
            this.FilterTCPCheckBox.Size = new System.Drawing.Size(94, 21);
            this.FilterTCPCheckBox.TabIndex = 1;
            this.FilterTCPCheckBox.Text = "Handle TCP";
            this.FilterTCPCheckBox.UseVisualStyleBackColor = true;
            // 
            // FilterUDPCheckBox
            // 
            this.FilterUDPCheckBox.AutoSize = true;
            this.FilterUDPCheckBox.Location = new System.Drawing.Point(216, 16);
            this.FilterUDPCheckBox.Name = "FilterUDPCheckBox";
            this.FilterUDPCheckBox.Size = new System.Drawing.Size(97, 21);
            this.FilterUDPCheckBox.TabIndex = 2;
            this.FilterUDPCheckBox.Text = "Handle UDP";
            this.FilterUDPCheckBox.UseVisualStyleBackColor = true;
            // 
            // FilterICMPCheckBox
            // 
            this.FilterICMPCheckBox.AutoSize = true;
            this.FilterICMPCheckBox.Location = new System.Drawing.Point(16, 48);
            this.FilterICMPCheckBox.Name = "FilterICMPCheckBox";
            this.FilterICMPCheckBox.Size = new System.Drawing.Size(103, 21);
            this.FilterICMPCheckBox.TabIndex = 3;
            this.FilterICMPCheckBox.Text = "Handle ICMP";
            this.FilterICMPCheckBox.UseVisualStyleBackColor = true;
            // 
            // DNSHijackLabel
            // 
            this.DNSHijackLabel.AutoSize = true;
            this.DNSHijackLabel.Location = new System.Drawing.Point(48, 144);
            this.DNSHijackLabel.Name = "DNSHijackLabel";
            this.DNSHijackLabel.Size = new System.Drawing.Size(34, 17);
            this.DNSHijackLabel.TabIndex = 3;
            this.DNSHijackLabel.Text = "DNS";
            // 
            // ICMPDelayLabel
            // 
            this.ICMPDelayLabel.AutoSize = true;
            this.ICMPDelayLabel.Location = new System.Drawing.Point(48, 80);
            this.ICMPDelayLabel.Name = "ICMPDelayLabel";
            this.ICMPDelayLabel.Size = new System.Drawing.Size(99, 17);
            this.ICMPDelayLabel.TabIndex = 3;
            this.ICMPDelayLabel.Text = "ICMP delay(ms)";
            // 
            // ICMPDelayTextBox
            // 
            this.ICMPDelayTextBox.DataBindings.Add(new System.Windows.Forms.Binding("Enabled", this.FilterICMPCheckBox, "Checked", true));
            this.ICMPDelayTextBox.Location = new System.Drawing.Point(216, 80);
            this.ICMPDelayTextBox.Name = "ICMPDelayTextBox";
            this.ICMPDelayTextBox.Size = new System.Drawing.Size(98, 23);
            this.ICMPDelayTextBox.TabIndex = 4;
            this.ICMPDelayTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // FilterDNSCheckBox
            // 
            this.FilterDNSCheckBox.AutoSize = true;
            this.FilterDNSCheckBox.Location = new System.Drawing.Point(16, 112);
            this.FilterDNSCheckBox.Name = "FilterDNSCheckBox";
            this.FilterDNSCheckBox.Size = new System.Drawing.Size(191, 21);
            this.FilterDNSCheckBox.TabIndex = 5;
            this.FilterDNSCheckBox.Text = "Handle DNS (DNS hijacking)";
            this.FilterDNSCheckBox.UseVisualStyleBackColor = true;
            // 
            // DNSHijackHostTextBox
            // 
            this.DNSHijackHostTextBox.DataBindings.Add(new System.Windows.Forms.Binding("Enabled", this.FilterDNSCheckBox, "Checked", true));
            this.DNSHijackHostTextBox.Location = new System.Drawing.Point(216, 144);
            this.DNSHijackHostTextBox.Name = "DNSHijackHostTextBox";
            this.DNSHijackHostTextBox.Size = new System.Drawing.Size(191, 23);
            this.DNSHijackHostTextBox.TabIndex = 6;
            this.DNSHijackHostTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // HandleProcDNSCheckBox
            // 
            this.HandleProcDNSCheckBox.AutoSize = true;
            this.HandleProcDNSCheckBox.DataBindings.Add(new System.Windows.Forms.Binding("Enabled", this.FilterDNSCheckBox, "Checked", true));
            this.HandleProcDNSCheckBox.Location = new System.Drawing.Point(16, 176);
            this.HandleProcDNSCheckBox.Name = "HandleProcDNSCheckBox";
            this.HandleProcDNSCheckBox.Size = new System.Drawing.Size(208, 21);
            this.HandleProcDNSCheckBox.TabIndex = 7;
            this.HandleProcDNSCheckBox.Text = "Handle handled process\'s DNS";
            this.HandleProcDNSCheckBox.UseVisualStyleBackColor = true;
            // 
            // DNSProxyCheckBox
            // 
            this.DNSProxyCheckBox.AutoSize = true;
            this.DNSProxyCheckBox.DataBindings.Add(new System.Windows.Forms.Binding("Enabled", this.FilterDNSCheckBox, "Checked", true));
            this.DNSProxyCheckBox.Location = new System.Drawing.Point(16, 208);
            this.DNSProxyCheckBox.Name = "DNSProxyCheckBox";
            this.DNSProxyCheckBox.Size = new System.Drawing.Size(185, 21);
            this.DNSProxyCheckBox.TabIndex = 8;
            this.DNSProxyCheckBox.Text = "Handle DNS through proxy";
            this.DNSProxyCheckBox.UseVisualStyleBackColor = true;
            // 
            // ChildProcessHandleCheckBox
            // 
            this.ChildProcessHandleCheckBox.AutoSize = true;
            this.ChildProcessHandleCheckBox.Location = new System.Drawing.Point(16, 240);
            this.ChildProcessHandleCheckBox.Name = "ChildProcessHandleCheckBox";
            this.ChildProcessHandleCheckBox.Size = new System.Drawing.Size(149, 21);
            this.ChildProcessHandleCheckBox.TabIndex = 9;
            this.ChildProcessHandleCheckBox.Text = "Handle child process";
            this.ChildProcessHandleCheckBox.UseVisualStyleBackColor = true;
            // 
            // WinTUNTabPage
            // 
            this.WinTUNTabPage.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.WinTUNTabPage.Controls.Add(this.WinTUNGroupBox);
            this.WinTUNTabPage.Controls.Add(this.GlobalBypassIPsButton);
            this.WinTUNTabPage.Location = new System.Drawing.Point(4, 29);
            this.WinTUNTabPage.Name = "WinTUNTabPage";
            this.WinTUNTabPage.Padding = new System.Windows.Forms.Padding(3);
            this.WinTUNTabPage.Size = new System.Drawing.Size(461, 321);
            this.WinTUNTabPage.TabIndex = 2;
            this.WinTUNTabPage.Text = "WinTUN";
            // 
            // WinTUNGroupBox
            // 
            this.WinTUNGroupBox.Controls.Add(this.TUNTAPAddressLabel);
            this.WinTUNGroupBox.Controls.Add(this.TUNTAPAddressTextBox);
            this.WinTUNGroupBox.Controls.Add(this.TUNTAPNetmaskLabel);
            this.WinTUNGroupBox.Controls.Add(this.TUNTAPNetmaskTextBox);
            this.WinTUNGroupBox.Controls.Add(this.TUNTAPGatewayLabel);
            this.WinTUNGroupBox.Controls.Add(this.TUNTAPGatewayTextBox);
            this.WinTUNGroupBox.Controls.Add(this.TUNTAPDNSLabel);
            this.WinTUNGroupBox.Controls.Add(this.TUNTAPDNSTextBox);
            this.WinTUNGroupBox.Controls.Add(this.UseCustomDNSCheckBox);
            this.WinTUNGroupBox.Controls.Add(this.ProxyDNSCheckBox);
            this.WinTUNGroupBox.Location = new System.Drawing.Point(6, 6);
            this.WinTUNGroupBox.Name = "WinTUNGroupBox";
            this.WinTUNGroupBox.Size = new System.Drawing.Size(450, 175);
            this.WinTUNGroupBox.TabIndex = 0;
            this.WinTUNGroupBox.TabStop = false;
            // 
            // TUNTAPAddressLabel
            // 
            this.TUNTAPAddressLabel.AutoSize = true;
            this.TUNTAPAddressLabel.Location = new System.Drawing.Point(9, 25);
            this.TUNTAPAddressLabel.Name = "TUNTAPAddressLabel";
            this.TUNTAPAddressLabel.Size = new System.Drawing.Size(56, 17);
            this.TUNTAPAddressLabel.TabIndex = 0;
            this.TUNTAPAddressLabel.Text = "Address";
            // 
            // TUNTAPAddressTextBox
            // 
            this.TUNTAPAddressTextBox.Location = new System.Drawing.Point(120, 22);
            this.TUNTAPAddressTextBox.Name = "TUNTAPAddressTextBox";
            this.TUNTAPAddressTextBox.Size = new System.Drawing.Size(294, 23);
            this.TUNTAPAddressTextBox.TabIndex = 1;
            this.TUNTAPAddressTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // TUNTAPNetmaskLabel
            // 
            this.TUNTAPNetmaskLabel.AutoSize = true;
            this.TUNTAPNetmaskLabel.Location = new System.Drawing.Point(9, 54);
            this.TUNTAPNetmaskLabel.Name = "TUNTAPNetmaskLabel";
            this.TUNTAPNetmaskLabel.Size = new System.Drawing.Size(60, 17);
            this.TUNTAPNetmaskLabel.TabIndex = 2;
            this.TUNTAPNetmaskLabel.Text = "Netmask";
            // 
            // TUNTAPNetmaskTextBox
            // 
            this.TUNTAPNetmaskTextBox.Location = new System.Drawing.Point(120, 51);
            this.TUNTAPNetmaskTextBox.Name = "TUNTAPNetmaskTextBox";
            this.TUNTAPNetmaskTextBox.Size = new System.Drawing.Size(294, 23);
            this.TUNTAPNetmaskTextBox.TabIndex = 3;
            this.TUNTAPNetmaskTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // TUNTAPGatewayLabel
            // 
            this.TUNTAPGatewayLabel.AutoSize = true;
            this.TUNTAPGatewayLabel.Location = new System.Drawing.Point(9, 83);
            this.TUNTAPGatewayLabel.Name = "TUNTAPGatewayLabel";
            this.TUNTAPGatewayLabel.Size = new System.Drawing.Size(57, 17);
            this.TUNTAPGatewayLabel.TabIndex = 4;
            this.TUNTAPGatewayLabel.Text = "Gateway";
            // 
            // TUNTAPGatewayTextBox
            // 
            this.TUNTAPGatewayTextBox.Location = new System.Drawing.Point(120, 80);
            this.TUNTAPGatewayTextBox.Name = "TUNTAPGatewayTextBox";
            this.TUNTAPGatewayTextBox.Size = new System.Drawing.Size(294, 23);
            this.TUNTAPGatewayTextBox.TabIndex = 5;
            this.TUNTAPGatewayTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // TUNTAPDNSLabel
            // 
            this.TUNTAPDNSLabel.AutoSize = true;
            this.TUNTAPDNSLabel.Location = new System.Drawing.Point(9, 112);
            this.TUNTAPDNSLabel.Name = "TUNTAPDNSLabel";
            this.TUNTAPDNSLabel.Size = new System.Drawing.Size(34, 17);
            this.TUNTAPDNSLabel.TabIndex = 6;
            this.TUNTAPDNSLabel.Text = "DNS";
            // 
            // TUNTAPDNSTextBox
            // 
            this.TUNTAPDNSTextBox.DataBindings.Add(new System.Windows.Forms.Binding("Enabled", this.UseCustomDNSCheckBox, "Checked", true));
            this.TUNTAPDNSTextBox.Location = new System.Drawing.Point(120, 110);
            this.TUNTAPDNSTextBox.Name = "TUNTAPDNSTextBox";
            this.TUNTAPDNSTextBox.Size = new System.Drawing.Size(294, 23);
            this.TUNTAPDNSTextBox.TabIndex = 7;
            this.TUNTAPDNSTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // UseCustomDNSCheckBox
            // 
            this.UseCustomDNSCheckBox.AutoSize = true;
            this.UseCustomDNSCheckBox.Location = new System.Drawing.Point(10, 139);
            this.UseCustomDNSCheckBox.Name = "UseCustomDNSCheckBox";
            this.UseCustomDNSCheckBox.Size = new System.Drawing.Size(125, 21);
            this.UseCustomDNSCheckBox.TabIndex = 8;
            this.UseCustomDNSCheckBox.Text = "Use custom DNS";
            this.UseCustomDNSCheckBox.UseVisualStyleBackColor = true;
            this.UseCustomDNSCheckBox.Click += new System.EventHandler(this.TUNTAPUseCustomDNSCheckBox_CheckedChanged);
            // 
            // ProxyDNSCheckBox
            // 
            this.ProxyDNSCheckBox.AutoSize = true;
            this.ProxyDNSCheckBox.DataBindings.Add(new System.Windows.Forms.Binding("Visible", this.UseCustomDNSCheckBox, "Checked", true));
            this.ProxyDNSCheckBox.Location = new System.Drawing.Point(175, 139);
            this.ProxyDNSCheckBox.Name = "ProxyDNSCheckBox";
            this.ProxyDNSCheckBox.Size = new System.Drawing.Size(89, 21);
            this.ProxyDNSCheckBox.TabIndex = 9;
            this.ProxyDNSCheckBox.Text = "Proxy DNS";
            this.ProxyDNSCheckBox.UseVisualStyleBackColor = true;
            // 
            // GlobalBypassIPsButton
            // 
            this.GlobalBypassIPsButton.Location = new System.Drawing.Point(6, 199);
            this.GlobalBypassIPsButton.Name = "GlobalBypassIPsButton";
            this.GlobalBypassIPsButton.Size = new System.Drawing.Size(128, 23);
            this.GlobalBypassIPsButton.TabIndex = 1;
            this.GlobalBypassIPsButton.Text = "Global Bypass IPs";
            this.GlobalBypassIPsButton.UseVisualStyleBackColor = true;
            this.GlobalBypassIPsButton.Click += new System.EventHandler(this.GlobalBypassIPsButton_Click);
            // 
            // v2rayTabPage
            // 
            this.v2rayTabPage.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.v2rayTabPage.Controls.Add(this.XrayConeCheckBox);
            this.v2rayTabPage.Controls.Add(this.TLSAllowInsecureCheckBox);
            this.v2rayTabPage.Controls.Add(this.UseMuxCheckBox);
            this.v2rayTabPage.Controls.Add(this.TCPFastOpenBox);
            this.v2rayTabPage.Controls.Add(this.KCPGroupBox);
            this.v2rayTabPage.Location = new System.Drawing.Point(4, 29);
            this.v2rayTabPage.Name = "v2rayTabPage";
            this.v2rayTabPage.Padding = new System.Windows.Forms.Padding(3);
            this.v2rayTabPage.Size = new System.Drawing.Size(461, 321);
            this.v2rayTabPage.TabIndex = 3;
            this.v2rayTabPage.Text = "V2Ray";
            // 
            // XrayConeCheckBox
            // 
            this.XrayConeCheckBox.AutoSize = true;
            this.XrayConeCheckBox.Location = new System.Drawing.Point(6, 15);
            this.XrayConeCheckBox.Name = "XrayConeCheckBox";
            this.XrayConeCheckBox.Size = new System.Drawing.Size(340, 21);
            this.XrayConeCheckBox.TabIndex = 0;
            this.XrayConeCheckBox.Text = "FullCone Support (Required Server Xray-core v1.3.0+)";
            this.XrayConeCheckBox.UseVisualStyleBackColor = true;
            // 
            // TLSAllowInsecureCheckBox
            // 
            this.TLSAllowInsecureCheckBox.AutoSize = true;
            this.TLSAllowInsecureCheckBox.Location = new System.Drawing.Point(6, 42);
            this.TLSAllowInsecureCheckBox.Name = "TLSAllowInsecureCheckBox";
            this.TLSAllowInsecureCheckBox.Size = new System.Drawing.Size(131, 21);
            this.TLSAllowInsecureCheckBox.TabIndex = 1;
            this.TLSAllowInsecureCheckBox.Text = "TLS AllowInsecure";
            this.TLSAllowInsecureCheckBox.UseVisualStyleBackColor = true;
            // 
            // UseMuxCheckBox
            // 
            this.UseMuxCheckBox.AutoSize = true;
            this.UseMuxCheckBox.Location = new System.Drawing.Point(148, 42);
            this.UseMuxCheckBox.Name = "UseMuxCheckBox";
            this.UseMuxCheckBox.Size = new System.Drawing.Size(78, 21);
            this.UseMuxCheckBox.TabIndex = 2;
            this.UseMuxCheckBox.Text = "Use Mux";
            this.UseMuxCheckBox.UseVisualStyleBackColor = true;
            // 
            // TCPFastOpenBox
            // 
            this.TCPFastOpenBox.AutoSize = true;
            this.TCPFastOpenBox.Location = new System.Drawing.Point(300, 42);
            this.TCPFastOpenBox.Name = "TCPFastOpenBox";
            this.TCPFastOpenBox.Size = new System.Drawing.Size(131, 21);
            this.TCPFastOpenBox.TabIndex = 3;
            this.TCPFastOpenBox.Text = "TCP FastOpen";
            this.TCPFastOpenBox.UseVisualStyleBackColor = true;
            // 
            // KCPGroupBox
            // 
            this.KCPGroupBox.Controls.Add(this.mtuLabel);
            this.KCPGroupBox.Controls.Add(this.mtuTextBox);
            this.KCPGroupBox.Controls.Add(this.ttiLabel);
            this.KCPGroupBox.Controls.Add(this.ttiTextBox);
            this.KCPGroupBox.Controls.Add(this.uplinkCapacityLabel);
            this.KCPGroupBox.Controls.Add(this.uplinkCapacityTextBox);
            this.KCPGroupBox.Controls.Add(this.downlinkCapacityLabel);
            this.KCPGroupBox.Controls.Add(this.downlinkCapacityTextBox);
            this.KCPGroupBox.Controls.Add(this.readBufferSizeLabel);
            this.KCPGroupBox.Controls.Add(this.readBufferSizeTextBox);
            this.KCPGroupBox.Controls.Add(this.writeBufferSizeLabel);
            this.KCPGroupBox.Controls.Add(this.writeBufferSizeTextBox);
            this.KCPGroupBox.Controls.Add(this.congestionCheckBox);
            this.KCPGroupBox.Location = new System.Drawing.Point(9, 75);
            this.KCPGroupBox.Name = "KCPGroupBox";
            this.KCPGroupBox.Size = new System.Drawing.Size(447, 204);
            this.KCPGroupBox.TabIndex = 3;
            this.KCPGroupBox.TabStop = false;
            this.KCPGroupBox.Text = "KCP";
            // 
            // mtuLabel
            // 
            this.mtuLabel.AutoSize = true;
            this.mtuLabel.Location = new System.Drawing.Point(6, 26);
            this.mtuLabel.Name = "mtuLabel";
            this.mtuLabel.Size = new System.Drawing.Size(30, 17);
            this.mtuLabel.TabIndex = 0;
            this.mtuLabel.Text = "mtu";
            // 
            // mtuTextBox
            // 
            this.mtuTextBox.Location = new System.Drawing.Point(103, 17);
            this.mtuTextBox.Name = "mtuTextBox";
            this.mtuTextBox.Size = new System.Drawing.Size(90, 23);
            this.mtuTextBox.TabIndex = 1;
            this.mtuTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // ttiLabel
            // 
            this.ttiLabel.AutoSize = true;
            this.ttiLabel.Location = new System.Drawing.Point(216, 26);
            this.ttiLabel.Name = "ttiLabel";
            this.ttiLabel.Size = new System.Drawing.Size(19, 17);
            this.ttiLabel.TabIndex = 2;
            this.ttiLabel.Text = "tti";
            // 
            // ttiTextBox
            // 
            this.ttiTextBox.Location = new System.Drawing.Point(331, 17);
            this.ttiTextBox.Name = "ttiTextBox";
            this.ttiTextBox.Size = new System.Drawing.Size(90, 23);
            this.ttiTextBox.TabIndex = 3;
            this.ttiTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // uplinkCapacityLabel
            // 
            this.uplinkCapacityLabel.AutoSize = true;
            this.uplinkCapacityLabel.Location = new System.Drawing.Point(6, 68);
            this.uplinkCapacityLabel.Name = "uplinkCapacityLabel";
            this.uplinkCapacityLabel.Size = new System.Drawing.Size(92, 17);
            this.uplinkCapacityLabel.TabIndex = 4;
            this.uplinkCapacityLabel.Text = "uplinkCapacity";
            // 
            // uplinkCapacityTextBox
            // 
            this.uplinkCapacityTextBox.Location = new System.Drawing.Point(103, 59);
            this.uplinkCapacityTextBox.Name = "uplinkCapacityTextBox";
            this.uplinkCapacityTextBox.Size = new System.Drawing.Size(90, 23);
            this.uplinkCapacityTextBox.TabIndex = 5;
            this.uplinkCapacityTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // downlinkCapacityLabel
            // 
            this.downlinkCapacityLabel.AutoSize = true;
            this.downlinkCapacityLabel.Location = new System.Drawing.Point(216, 68);
            this.downlinkCapacityLabel.Name = "downlinkCapacityLabel";
            this.downlinkCapacityLabel.Size = new System.Drawing.Size(109, 17);
            this.downlinkCapacityLabel.TabIndex = 6;
            this.downlinkCapacityLabel.Text = "downlinkCapacity";
            // 
            // downlinkCapacityTextBox
            // 
            this.downlinkCapacityTextBox.Location = new System.Drawing.Point(331, 65);
            this.downlinkCapacityTextBox.Name = "downlinkCapacityTextBox";
            this.downlinkCapacityTextBox.Size = new System.Drawing.Size(90, 23);
            this.downlinkCapacityTextBox.TabIndex = 7;
            this.downlinkCapacityTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // readBufferSizeLabel
            // 
            this.readBufferSizeLabel.AutoSize = true;
            this.readBufferSizeLabel.Location = new System.Drawing.Point(6, 109);
            this.readBufferSizeLabel.Name = "readBufferSizeLabel";
            this.readBufferSizeLabel.Size = new System.Drawing.Size(93, 17);
            this.readBufferSizeLabel.TabIndex = 8;
            this.readBufferSizeLabel.Text = "readBufferSize";
            // 
            // readBufferSizeTextBox
            // 
            this.readBufferSizeTextBox.Location = new System.Drawing.Point(103, 100);
            this.readBufferSizeTextBox.Name = "readBufferSizeTextBox";
            this.readBufferSizeTextBox.Size = new System.Drawing.Size(90, 23);
            this.readBufferSizeTextBox.TabIndex = 9;
            this.readBufferSizeTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // writeBufferSizeLabel
            // 
            this.writeBufferSizeLabel.AutoSize = true;
            this.writeBufferSizeLabel.Location = new System.Drawing.Point(216, 109);
            this.writeBufferSizeLabel.Name = "writeBufferSizeLabel";
            this.writeBufferSizeLabel.Size = new System.Drawing.Size(94, 17);
            this.writeBufferSizeLabel.TabIndex = 10;
            this.writeBufferSizeLabel.Text = "writeBufferSize";
            // 
            // writeBufferSizeTextBox
            // 
            this.writeBufferSizeTextBox.Location = new System.Drawing.Point(331, 106);
            this.writeBufferSizeTextBox.Name = "writeBufferSizeTextBox";
            this.writeBufferSizeTextBox.Size = new System.Drawing.Size(90, 23);
            this.writeBufferSizeTextBox.TabIndex = 11;
            this.writeBufferSizeTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // congestionCheckBox
            // 
            this.congestionCheckBox.AutoSize = true;
            this.congestionCheckBox.Location = new System.Drawing.Point(8, 139);
            this.congestionCheckBox.Name = "congestionCheckBox";
            this.congestionCheckBox.Size = new System.Drawing.Size(91, 21);
            this.congestionCheckBox.TabIndex = 12;
            this.congestionCheckBox.Text = "congestion";
            this.congestionCheckBox.UseVisualStyleBackColor = true;
            // 
            // OtherTabPage
            // 
            this.OtherTabPage.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.OtherTabPage.Controls.Add(this.ExitWhenClosedCheckBox);
            this.OtherTabPage.Controls.Add(this.StopWhenExitedCheckBox);
            this.OtherTabPage.Controls.Add(this.StartWhenOpenedCheckBox);
            this.OtherTabPage.Controls.Add(this.MinimizeWhenStartedCheckBox);
            this.OtherTabPage.Controls.Add(this.RunAtStartupCheckBox);
            this.OtherTabPage.Controls.Add(this.CheckUpdateWhenOpenedCheckBox);
            this.OtherTabPage.Controls.Add(this.NoSupportDialogCheckBox);
            this.OtherTabPage.Controls.Add(this.CheckBetaUpdateCheckBox);
            this.OtherTabPage.Controls.Add(this.UpdateServersWhenOpenedCheckBox);
            this.OtherTabPage.Location = new System.Drawing.Point(4, 29);
            this.OtherTabPage.Name = "OtherTabPage";
            this.OtherTabPage.Padding = new System.Windows.Forms.Padding(3);
            this.OtherTabPage.Size = new System.Drawing.Size(461, 321);
            this.OtherTabPage.TabIndex = 4;
            this.OtherTabPage.Text = "Others";
            // 
            // ExitWhenClosedCheckBox
            // 
            this.ExitWhenClosedCheckBox.AutoSize = true;
            this.ExitWhenClosedCheckBox.Location = new System.Drawing.Point(16, 16);
            this.ExitWhenClosedCheckBox.Name = "ExitWhenClosedCheckBox";
            this.ExitWhenClosedCheckBox.Size = new System.Drawing.Size(123, 21);
            this.ExitWhenClosedCheckBox.TabIndex = 0;
            this.ExitWhenClosedCheckBox.Text = "Exit when closed";
            this.ExitWhenClosedCheckBox.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.ExitWhenClosedCheckBox.UseVisualStyleBackColor = true;
            // 
            // StopWhenExitedCheckBox
            // 
            this.StopWhenExitedCheckBox.AutoSize = true;
            this.StopWhenExitedCheckBox.Location = new System.Drawing.Point(224, 18);
            this.StopWhenExitedCheckBox.Name = "StopWhenExitedCheckBox";
            this.StopWhenExitedCheckBox.Size = new System.Drawing.Size(127, 21);
            this.StopWhenExitedCheckBox.TabIndex = 1;
            this.StopWhenExitedCheckBox.Text = "Stop when exited";
            this.StopWhenExitedCheckBox.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.StopWhenExitedCheckBox.UseVisualStyleBackColor = true;
            // 
            // StartWhenOpenedCheckBox
            // 
            this.StartWhenOpenedCheckBox.AutoSize = true;
            this.StartWhenOpenedCheckBox.Location = new System.Drawing.Point(16, 48);
            this.StartWhenOpenedCheckBox.Name = "StartWhenOpenedCheckBox";
            this.StartWhenOpenedCheckBox.Size = new System.Drawing.Size(137, 21);
            this.StartWhenOpenedCheckBox.TabIndex = 2;
            this.StartWhenOpenedCheckBox.Text = "Start when opened";
            this.StartWhenOpenedCheckBox.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.StartWhenOpenedCheckBox.UseVisualStyleBackColor = true;
            // 
            // MinimizeWhenStartedCheckBox
            // 
            this.MinimizeWhenStartedCheckBox.AutoSize = true;
            this.MinimizeWhenStartedCheckBox.Location = new System.Drawing.Point(224, 48);
            this.MinimizeWhenStartedCheckBox.Name = "MinimizeWhenStartedCheckBox";
            this.MinimizeWhenStartedCheckBox.Size = new System.Drawing.Size(158, 21);
            this.MinimizeWhenStartedCheckBox.TabIndex = 3;
            this.MinimizeWhenStartedCheckBox.Text = "Minimize when started";
            this.MinimizeWhenStartedCheckBox.UseVisualStyleBackColor = true;
            // 
            // RunAtStartupCheckBox
            // 
            this.RunAtStartupCheckBox.AutoSize = true;
            this.RunAtStartupCheckBox.Location = new System.Drawing.Point(16, 80);
            this.RunAtStartupCheckBox.Name = "RunAtStartupCheckBox";
            this.RunAtStartupCheckBox.Size = new System.Drawing.Size(109, 21);
            this.RunAtStartupCheckBox.TabIndex = 4;
            this.RunAtStartupCheckBox.Text = "Run at startup";
            this.RunAtStartupCheckBox.UseVisualStyleBackColor = true;
            // 
            // CheckUpdateWhenOpenedCheckBox
            // 
            this.CheckUpdateWhenOpenedCheckBox.AutoSize = true;
            this.CheckUpdateWhenOpenedCheckBox.Location = new System.Drawing.Point(224, 80);
            this.CheckUpdateWhenOpenedCheckBox.Name = "CheckUpdateWhenOpenedCheckBox";
            this.CheckUpdateWhenOpenedCheckBox.Size = new System.Drawing.Size(190, 21);
            this.CheckUpdateWhenOpenedCheckBox.TabIndex = 5;
            this.CheckUpdateWhenOpenedCheckBox.Text = "Check update when opened";
            this.CheckUpdateWhenOpenedCheckBox.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.CheckUpdateWhenOpenedCheckBox.UseVisualStyleBackColor = true;
            // 
            // NoSupportDialogCheckBox
            // 
            this.NoSupportDialogCheckBox.AutoSize = true;
            this.NoSupportDialogCheckBox.Location = new System.Drawing.Point(16, 112);
            this.NoSupportDialogCheckBox.Name = "NoSupportDialogCheckBox";
            this.NoSupportDialogCheckBox.Size = new System.Drawing.Size(174, 21);
            this.NoSupportDialogCheckBox.TabIndex = 6;
            this.NoSupportDialogCheckBox.Text = "Disable Support Warning";
            this.NoSupportDialogCheckBox.UseVisualStyleBackColor = true;
            // 
            // CheckBetaUpdateCheckBox
            // 
            this.CheckBetaUpdateCheckBox.AutoSize = true;
            this.CheckBetaUpdateCheckBox.Location = new System.Drawing.Point(224, 112);
            this.CheckBetaUpdateCheckBox.Name = "CheckBetaUpdateCheckBox";
            this.CheckBetaUpdateCheckBox.Size = new System.Drawing.Size(137, 21);
            this.CheckBetaUpdateCheckBox.TabIndex = 7;
            this.CheckBetaUpdateCheckBox.Text = "Check Beta update";
            this.CheckBetaUpdateCheckBox.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.CheckBetaUpdateCheckBox.UseVisualStyleBackColor = true;
            // 
            // UpdateServersWhenOpenedCheckBox
            // 
            this.UpdateServersWhenOpenedCheckBox.AutoSize = true;
            this.UpdateServersWhenOpenedCheckBox.Location = new System.Drawing.Point(224, 144);
            this.UpdateServersWhenOpenedCheckBox.Name = "UpdateServersWhenOpenedCheckBox";
            this.UpdateServersWhenOpenedCheckBox.Size = new System.Drawing.Size(200, 21);
            this.UpdateServersWhenOpenedCheckBox.TabIndex = 8;
            this.UpdateServersWhenOpenedCheckBox.Text = "Update Servers when opened";
            this.UpdateServersWhenOpenedCheckBox.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.UpdateServersWhenOpenedCheckBox.UseVisualStyleBackColor = true;
            // 
            // AioDNSTabPage
            // 
            this.AioDNSTabPage.Controls.Add(this.ChinaDNSLabel);
            this.AioDNSTabPage.Controls.Add(this.ChinaDNSTextBox);
            this.AioDNSTabPage.Controls.Add(this.OtherDNSLabel);
            this.AioDNSTabPage.Controls.Add(this.OtherDNSTextBox);
            this.AioDNSTabPage.Controls.Add(this.AioDNSListenPortLabel);
            this.AioDNSTabPage.Controls.Add(this.AioDNSListenPortTextBox);
            this.AioDNSTabPage.Location = new System.Drawing.Point(4, 29);
            this.AioDNSTabPage.Name = "AioDNSTabPage";
            this.AioDNSTabPage.Padding = new System.Windows.Forms.Padding(3);
            this.AioDNSTabPage.Size = new System.Drawing.Size(461, 321);
            this.AioDNSTabPage.TabIndex = 5;
            this.AioDNSTabPage.Text = "AioDNS";
            this.AioDNSTabPage.UseVisualStyleBackColor = true;
            // 
            // ChinaDNSLabel
            // 
            this.ChinaDNSLabel.AutoSize = true;
            this.ChinaDNSLabel.Location = new System.Drawing.Point(15, 23);
            this.ChinaDNSLabel.Name = "ChinaDNSLabel";
            this.ChinaDNSLabel.Size = new System.Drawing.Size(70, 17);
            this.ChinaDNSLabel.TabIndex = 0;
            this.ChinaDNSLabel.Text = "China DNS";
            // 
            // ChinaDNSTextBox
            // 
            this.ChinaDNSTextBox.Location = new System.Drawing.Point(150, 20);
            this.ChinaDNSTextBox.Name = "ChinaDNSTextBox";
            this.ChinaDNSTextBox.Size = new System.Drawing.Size(201, 23);
            this.ChinaDNSTextBox.TabIndex = 1;
            this.ChinaDNSTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // OtherDNSLabel
            // 
            this.OtherDNSLabel.AutoSize = true;
            this.OtherDNSLabel.Location = new System.Drawing.Point(15, 63);
            this.OtherDNSLabel.Name = "OtherDNSLabel";
            this.OtherDNSLabel.Size = new System.Drawing.Size(71, 17);
            this.OtherDNSLabel.TabIndex = 2;
            this.OtherDNSLabel.Text = "Other DNS";
            // 
            // OtherDNSTextBox
            // 
            this.OtherDNSTextBox.Location = new System.Drawing.Point(150, 60);
            this.OtherDNSTextBox.Name = "OtherDNSTextBox";
            this.OtherDNSTextBox.Size = new System.Drawing.Size(201, 23);
            this.OtherDNSTextBox.TabIndex = 3;
            this.OtherDNSTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // AioDNSListenPortLabel
            // 
            this.AioDNSListenPortLabel.AutoSize = true;
            this.AioDNSListenPortLabel.Location = new System.Drawing.Point(15, 103);
            this.AioDNSListenPortLabel.Name = "AioDNSListenPortLabel";
            this.AioDNSListenPortLabel.Size = new System.Drawing.Size(69, 17);
            this.AioDNSListenPortLabel.TabIndex = 4;
            this.AioDNSListenPortLabel.Text = "Listen Port";
            this.AioDNSListenPortLabel.Visible = false;
            // 
            // AioDNSListenPortTextBox
            // 
            this.AioDNSListenPortTextBox.Location = new System.Drawing.Point(150, 100);
            this.AioDNSListenPortTextBox.Name = "AioDNSListenPortTextBox";
            this.AioDNSListenPortTextBox.Size = new System.Drawing.Size(80, 23);
            this.AioDNSListenPortTextBox.TabIndex = 5;
            this.AioDNSListenPortTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.AioDNSListenPortTextBox.Visible = false;
            // 
            // ControlButton
            // 
            this.ControlButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.ControlButton.Location = new System.Drawing.Point(397, 363);
            this.ControlButton.Name = "ControlButton";
            this.ControlButton.Size = new System.Drawing.Size(75, 23);
            this.ControlButton.TabIndex = 1;
            this.ControlButton.Text = "Save";
            this.ControlButton.UseVisualStyleBackColor = true;
            this.ControlButton.Click += new System.EventHandler(this.ControlButton_Click);
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.AutoSize = true;
            this.flowLayoutPanel1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flowLayoutPanel1.Controls.Add(this.TabControl);
            this.flowLayoutPanel1.Controls.Add(this.ControlButton);
            this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanel1.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(480, 400);
            this.flowLayoutPanel1.TabIndex = 0;
            // 
            // errorProvider
            // 
            this.errorProvider.BlinkStyle = System.Windows.Forms.ErrorBlinkStyle.NeverBlink;
            this.errorProvider.ContainerControl = this;
            // 
            // SettingForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ClientSize = new System.Drawing.Size(480, 400);
            this.Controls.Add(this.flowLayoutPanel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "SettingForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Settings";
            this.Load += new System.EventHandler(this.SettingForm_Load);
            this.TabControl.ResumeLayout(false);
            this.GeneralTabPage.ResumeLayout(false);
            this.GeneralTabPage.PerformLayout();
            this.PortGroupBox.ResumeLayout(false);
            this.PortGroupBox.PerformLayout();
            this.NFTabPage.ResumeLayout(false);
            this.NFTabPage.PerformLayout();
            this.WinTUNTabPage.ResumeLayout(false);
            this.WinTUNGroupBox.ResumeLayout(false);
            this.WinTUNGroupBox.PerformLayout();
            this.v2rayTabPage.ResumeLayout(false);
            this.v2rayTabPage.PerformLayout();
            this.KCPGroupBox.ResumeLayout(false);
            this.KCPGroupBox.PerformLayout();
            this.OtherTabPage.ResumeLayout(false);
            this.OtherTabPage.PerformLayout();
            this.AioDNSTabPage.ResumeLayout(false);
            this.AioDNSTabPage.PerformLayout();
            this.flowLayoutPanel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.errorProvider)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }
        private System.Windows.Forms.CheckBox XrayConeCheckBox;
        private System.Windows.Forms.TextBox StartedPingIntervalTextBox;

        #endregion

        private System.Windows.Forms.TabControl TabControl;
        private System.Windows.Forms.TabPage GeneralTabPage;
        private System.Windows.Forms.TabPage NFTabPage;
        private System.Windows.Forms.TabPage WinTUNTabPage;
        private System.Windows.Forms.TabPage v2rayTabPage;
        private System.Windows.Forms.GroupBox PortGroupBox;
        private System.Windows.Forms.CheckBox AllowDevicesCheckBox;
        private System.Windows.Forms.Label Socks5PortLabel;
        private System.Windows.Forms.TextBox Socks5PortTextBox;
        private System.Windows.Forms.GroupBox WinTUNGroupBox;
        private System.Windows.Forms.CheckBox ProxyDNSCheckBox;
        private System.Windows.Forms.CheckBox UseCustomDNSCheckBox;
        private System.Windows.Forms.Label TUNTAPDNSLabel;
        private System.Windows.Forms.TextBox TUNTAPDNSTextBox;
        private System.Windows.Forms.Label TUNTAPGatewayLabel;
        private System.Windows.Forms.TextBox TUNTAPGatewayTextBox;
        private System.Windows.Forms.Label TUNTAPNetmaskLabel;
        private System.Windows.Forms.TextBox TUNTAPNetmaskTextBox;
        private System.Windows.Forms.Label TUNTAPAddressLabel;
        private System.Windows.Forms.TextBox TUNTAPAddressTextBox;
        private System.Windows.Forms.Button GlobalBypassIPsButton;
        private System.Windows.Forms.CheckBox FilterDNSCheckBox;
        private System.Windows.Forms.Button ControlButton;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.TabPage OtherTabPage;
        private System.Windows.Forms.CheckBox UpdateServersWhenOpenedCheckBox;
        private System.Windows.Forms.CheckBox RunAtStartupCheckBox;
        private System.Windows.Forms.CheckBox MinimizeWhenStartedCheckBox;
        private System.Windows.Forms.CheckBox CheckBetaUpdateCheckBox;
        private System.Windows.Forms.CheckBox CheckUpdateWhenOpenedCheckBox;
        private System.Windows.Forms.CheckBox StartWhenOpenedCheckBox;
        private System.Windows.Forms.CheckBox StopWhenExitedCheckBox;
        private System.Windows.Forms.CheckBox ExitWhenClosedCheckBox;
        private System.Windows.Forms.Label LanguageLabel;
        private System.Windows.Forms.ComboBox LanguageComboBox;
        private System.Windows.Forms.Label DetectionTickLabel;
        private System.Windows.Forms.TextBox DetectionTickTextBox;
        private System.Windows.Forms.Label StartedPingLabel;
        private System.Windows.Forms.Label STUNServerLabel;
        private System.Windows.Forms.ComboBox STUN_ServerComboBox;
        private System.Windows.Forms.Label ProfileCountLabel;
        private System.Windows.Forms.TextBox ProfileCountTextBox;
        private System.Windows.Forms.GroupBox KCPGroupBox;
        private System.Windows.Forms.CheckBox congestionCheckBox;
        private System.Windows.Forms.CheckBox TLSAllowInsecureCheckBox;
        private System.Windows.Forms.CheckBox TCPFastOpenBox;
        private System.Windows.Forms.Label mtuLabel;
        private System.Windows.Forms.TextBox mtuTextBox;
        private System.Windows.Forms.Label writeBufferSizeLabel;
        private System.Windows.Forms.TextBox writeBufferSizeTextBox;
        private System.Windows.Forms.Label readBufferSizeLabel;
        private System.Windows.Forms.TextBox readBufferSizeTextBox;
        private System.Windows.Forms.Label downlinkCapacityLabel;
        private System.Windows.Forms.TextBox downlinkCapacityTextBox;
        private System.Windows.Forms.Label uplinkCapacityLabel;
        private System.Windows.Forms.TextBox uplinkCapacityTextBox;
        private System.Windows.Forms.Label ttiLabel;
        private System.Windows.Forms.TextBox ttiTextBox;
        private System.Windows.Forms.CheckBox UseMuxCheckBox;
        private System.Windows.Forms.TabPage AioDNSTabPage;
        private System.Windows.Forms.Label AioDNSListenPortLabel;
        private System.Windows.Forms.TextBox AioDNSListenPortTextBox;
        private System.Windows.Forms.Label OtherDNSLabel;
        private System.Windows.Forms.Label ChinaDNSLabel;
        private System.Windows.Forms.TextBox OtherDNSTextBox;
        private System.Windows.Forms.TextBox ChinaDNSTextBox;
        private System.Windows.Forms.TextBox DNSHijackHostTextBox;
        private System.Windows.Forms.Label ServerPingTypeLabel;
        private System.Windows.Forms.RadioButton TCPingRadioBtn;
        private System.Windows.Forms.RadioButton ICMPingRadioBtn;
        private System.Windows.Forms.CheckBox FilterICMPCheckBox;
        private System.Windows.Forms.CheckBox ChildProcessHandleCheckBox;
        private System.Windows.Forms.TextBox ICMPDelayTextBox;
        private System.Windows.Forms.Label ICMPDelayLabel;
        private System.Windows.Forms.CheckBox NoSupportDialogCheckBox;
        private System.Windows.Forms.Label DNSHijackLabel;
        private System.Windows.Forms.CheckBox HandleProcDNSCheckBox;
        private System.Windows.Forms.CheckBox FilterTCPCheckBox;
        private System.Windows.Forms.CheckBox FilterUDPCheckBox;
        private System.Windows.Forms.CheckBox DNSProxyCheckBox;
        private ErrorProvider errorProvider;
    }
}