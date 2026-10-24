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
            components = new Container();
            TabControl = new TabControl();
            GeneralTabPage = new TabPage();
            OutboundDNSTextBox = new TextBox();
            PortGroupBox = new GroupBox();
            Socks5PortLabel = new Label();
            Socks5PortTextBox = new TextBox();
            AllowDevicesCheckBox = new CheckBox();
            OutboundDNSLabel = new Label();
            ServerPingTypeLabel = new Label();
            ICMPingRadioBtn = new RadioButton();
            TCPingRadioBtn = new RadioButton();
            ProfileCountLabel = new Label();
            ProfileCountTextBox = new TextBox();
            DetectionTickLabel = new Label();
            DetectionTickTextBox = new TextBox();
            StartedPingLabel = new Label();
            StartedPingIntervalTextBox = new TextBox();
            STUNServerLabel = new Label();
            STUN_ServerComboBox = new ComboBox();
            LanguageLabel = new Label();
            LanguageComboBox = new ComboBox();
            NFTabPage = new TabPage();
            FilterTCPCheckBox = new CheckBox();
            FilterUDPCheckBox = new CheckBox();
            FilterICMPCheckBox = new CheckBox();
            DNSHijackLabel = new Label();
            ICMPDelayLabel = new Label();
            ICMPDelayTextBox = new TextBox();
            FilterDNSCheckBox = new CheckBox();
            DNSHijackHostTextBox = new TextBox();
            HandleProcDNSCheckBox = new CheckBox();
            DNSProxyCheckBox = new CheckBox();
            ChildProcessHandleCheckBox = new CheckBox();
            WinTUNTabPage = new TabPage();
            WinTUNGroupBox = new GroupBox();
            TUNTAPAddressLabel = new Label();
            TUNTAPAddressTextBox = new TextBox();
            TUNTAPNetmaskLabel = new Label();
            TUNTAPNetmaskTextBox = new TextBox();
            TUNTAPGatewayLabel = new Label();
            TUNTAPGatewayTextBox = new TextBox();
            EnableIPv6CheckBox = new CheckBox();
            TUNTAPIPv6AddressLabel = new Label();
            TUNTAPIPv6AddressTextBox = new TextBox();
            TUNTAPIPv6GatewayLabel = new Label();
            TUNTAPIPv6GatewayTextBox = new TextBox();
            TUNTAPIPv6PrefixLabel = new Label();
            TUNTAPIPv6PrefixTextBox = new TextBox();
            TUNTAPDNSLabel = new Label();
            TUNTAPDNSTextBox = new TextBox();
            UseCustomDNSCheckBox = new CheckBox();
            ProxyDNSCheckBox = new CheckBox();
            GlobalBypassIPsButton = new Button();
            v2rayTabPage = new TabPage();
            DefFingerprintComboBox = new ComboBox();
            DefFingerprintLabel = new Label();
            EnableFragmentBox = new CheckBox();
            TLSAllowInsecureCheckBox = new CheckBox();
            UseMuxCheckBox = new CheckBox();
            KCPGroupBox = new GroupBox();
            mtuLabel = new Label();
            mtuTextBox = new TextBox();
            ttiLabel = new Label();
            ttiTextBox = new TextBox();
            uplinkCapacityLabel = new Label();
            uplinkCapacityTextBox = new TextBox();
            downlinkCapacityLabel = new Label();
            downlinkCapacityTextBox = new TextBox();
            readBufferSizeLabel = new Label();
            readBufferSizeTextBox = new TextBox();
            writeBufferSizeLabel = new Label();
            writeBufferSizeTextBox = new TextBox();
            congestionCheckBox = new CheckBox();
            HysteriaTabPage = new TabPage();
            HysteriaBandwidthGroupBox = new GroupBox();
            HysteriaDownMbpsTextBox = new TextBox();
            HysteriaUpMbpsTextBox = new TextBox();
            OtherTabPage = new TabPage();
            ExitWhenClosedCheckBox = new CheckBox();
            StopWhenExitedCheckBox = new CheckBox();
            StartWhenOpenedCheckBox = new CheckBox();
            MinimizeWhenStartedCheckBox = new CheckBox();
            RunAtStartupCheckBox = new CheckBox();
            CheckUpdateWhenOpenedCheckBox = new CheckBox();
            NoSupportDialogCheckBox = new CheckBox();
            CheckBetaUpdateCheckBox = new CheckBox();
            UpdateServersWhenOpenedCheckBox = new CheckBox();
            AioDNSTabPage = new TabPage();
            ChinaDNSLabel = new Label();
            ChinaDNSTextBox = new TextBox();
            OtherDNSLabel = new Label();
            OtherDNSTextBox = new TextBox();
            ControlButton = new Button();
            errorProvider = new ErrorProvider(components);
            tableLayoutPanel1 = new TableLayoutPanel();
            TabControl.SuspendLayout();
            GeneralTabPage.SuspendLayout();
            PortGroupBox.SuspendLayout();
            NFTabPage.SuspendLayout();
            WinTUNTabPage.SuspendLayout();
            WinTUNGroupBox.SuspendLayout();
            v2rayTabPage.SuspendLayout();
            KCPGroupBox.SuspendLayout();
            HysteriaTabPage.SuspendLayout();
            HysteriaBandwidthGroupBox.SuspendLayout();
            OtherTabPage.SuspendLayout();
            AioDNSTabPage.SuspendLayout();
            ((ISupportInitialize)errorProvider).BeginInit();
            tableLayoutPanel1.SuspendLayout();
            SuspendLayout();
            // 
            // TabControl
            // 
            TabControl.Appearance = TabAppearance.FlatButtons;
            TabControl.Controls.Add(GeneralTabPage);
            TabControl.Controls.Add(NFTabPage);
            TabControl.Controls.Add(WinTUNTabPage);
            TabControl.Controls.Add(v2rayTabPage);
            TabControl.Controls.Add(HysteriaTabPage);
            TabControl.Controls.Add(OtherTabPage);
            TabControl.Controls.Add(AioDNSTabPage);
            TabControl.Dock = DockStyle.Top;
            TabControl.Location = new Point(3, 3);
            TabControl.Name = "TabControl";
            TabControl.SelectedIndex = 0;
            TabControl.Size = new Size(503, 323);
            TabControl.TabIndex = 0;
            // 
            // GeneralTabPage
            // 
            GeneralTabPage.BackColor = SystemColors.ButtonFace;
            GeneralTabPage.Controls.Add(OutboundDNSTextBox);
            GeneralTabPage.Controls.Add(PortGroupBox);
            GeneralTabPage.Controls.Add(OutboundDNSLabel);
            GeneralTabPage.Controls.Add(ServerPingTypeLabel);
            GeneralTabPage.Controls.Add(ICMPingRadioBtn);
            GeneralTabPage.Controls.Add(TCPingRadioBtn);
            GeneralTabPage.Controls.Add(ProfileCountLabel);
            GeneralTabPage.Controls.Add(ProfileCountTextBox);
            GeneralTabPage.Controls.Add(DetectionTickLabel);
            GeneralTabPage.Controls.Add(DetectionTickTextBox);
            GeneralTabPage.Controls.Add(StartedPingLabel);
            GeneralTabPage.Controls.Add(StartedPingIntervalTextBox);
            GeneralTabPage.Controls.Add(STUNServerLabel);
            GeneralTabPage.Controls.Add(STUN_ServerComboBox);
            GeneralTabPage.Controls.Add(LanguageLabel);
            GeneralTabPage.Controls.Add(LanguageComboBox);
            GeneralTabPage.Location = new Point(4, 29);
            GeneralTabPage.Name = "GeneralTabPage";
            GeneralTabPage.Padding = new Padding(3);
            GeneralTabPage.Size = new Size(495, 290);
            GeneralTabPage.TabIndex = 0;
            GeneralTabPage.Text = "General";
            // 
            // OutboundDNSTextBox
            // 
            OutboundDNSTextBox.Location = new Point(267, 98);
            OutboundDNSTextBox.Name = "OutboundDNSTextBox";
            OutboundDNSTextBox.Size = new Size(164, 23);
            OutboundDNSTextBox.TabIndex = 15;
            // 
            // PortGroupBox
            // 
            PortGroupBox.Controls.Add(Socks5PortLabel);
            PortGroupBox.Controls.Add(Socks5PortTextBox);
            PortGroupBox.Controls.Add(AllowDevicesCheckBox);
            PortGroupBox.Location = new Point(8, 6);
            PortGroupBox.Name = "PortGroupBox";
            PortGroupBox.Size = new Size(241, 115);
            PortGroupBox.TabIndex = 0;
            PortGroupBox.TabStop = false;
            PortGroupBox.Text = "Local Port";
            // 
            // Socks5PortLabel
            // 
            Socks5PortLabel.AutoSize = true;
            Socks5PortLabel.Location = new Point(9, 25);
            Socks5PortLabel.Name = "Socks5PortLabel";
            Socks5PortLabel.Size = new Size(44, 17);
            Socks5PortLabel.TabIndex = 0;
            Socks5PortLabel.Text = "Mixed";
            // 
            // Socks5PortTextBox
            // 
            Socks5PortTextBox.Location = new Point(120, 22);
            Socks5PortTextBox.Name = "Socks5PortTextBox";
            Socks5PortTextBox.Size = new Size(90, 23);
            Socks5PortTextBox.TabIndex = 1;
            Socks5PortTextBox.TextAlign = HorizontalAlignment.Center;
            // 
            // AllowDevicesCheckBox
            // 
            AllowDevicesCheckBox.AutoSize = true;
            AllowDevicesCheckBox.Location = new Point(6, 84);
            AllowDevicesCheckBox.Name = "AllowDevicesCheckBox";
            AllowDevicesCheckBox.Size = new Size(206, 21);
            AllowDevicesCheckBox.TabIndex = 4;
            AllowDevicesCheckBox.Text = "Allow other Devices to connect";
            AllowDevicesCheckBox.TextAlign = ContentAlignment.MiddleRight;
            AllowDevicesCheckBox.UseVisualStyleBackColor = true;
            // 
            // OutboundDNSLabel
            // 
            OutboundDNSLabel.AutoSize = true;
            OutboundDNSLabel.Location = new Point(268, 75);
            OutboundDNSLabel.Name = "OutboundDNSLabel";
            OutboundDNSLabel.Size = new Size(97, 17);
            OutboundDNSLabel.TabIndex = 2;
            OutboundDNSLabel.Text = "Outbound DNS";
            // 
            // ServerPingTypeLabel
            // 
            ServerPingTypeLabel.AutoSize = true;
            ServerPingTypeLabel.Location = new Point(267, 15);
            ServerPingTypeLabel.Name = "ServerPingTypeLabel";
            ServerPingTypeLabel.Size = new Size(86, 17);
            ServerPingTypeLabel.TabIndex = 2;
            ServerPingTypeLabel.Text = "Ping Protocol";
            // 
            // ICMPingRadioBtn
            // 
            ICMPingRadioBtn.AutoSize = true;
            ICMPingRadioBtn.Location = new Point(268, 34);
            ICMPingRadioBtn.Name = "ICMPingRadioBtn";
            ICMPingRadioBtn.Size = new Size(75, 21);
            ICMPingRadioBtn.TabIndex = 3;
            ICMPingRadioBtn.TabStop = true;
            ICMPingRadioBtn.Text = "ICMPing";
            ICMPingRadioBtn.UseVisualStyleBackColor = true;
            // 
            // TCPingRadioBtn
            // 
            TCPingRadioBtn.AutoSize = true;
            TCPingRadioBtn.Location = new Point(366, 35);
            TCPingRadioBtn.Name = "TCPingRadioBtn";
            TCPingRadioBtn.Size = new Size(66, 21);
            TCPingRadioBtn.TabIndex = 4;
            TCPingRadioBtn.TabStop = true;
            TCPingRadioBtn.Text = "TCPing";
            TCPingRadioBtn.UseVisualStyleBackColor = true;
            // 
            // ProfileCountLabel
            // 
            ProfileCountLabel.AutoSize = true;
            ProfileCountLabel.Location = new Point(15, 140);
            ProfileCountLabel.Name = "ProfileCountLabel";
            ProfileCountLabel.Size = new Size(83, 17);
            ProfileCountLabel.TabIndex = 5;
            ProfileCountLabel.Text = "Profile Count";
            // 
            // ProfileCountTextBox
            // 
            ProfileCountTextBox.Location = new Point(182, 137);
            ProfileCountTextBox.Name = "ProfileCountTextBox";
            ProfileCountTextBox.Size = new Size(70, 23);
            ProfileCountTextBox.TabIndex = 6;
            ProfileCountTextBox.TextAlign = HorizontalAlignment.Center;
            // 
            // DetectionTickLabel
            // 
            DetectionTickLabel.AutoSize = true;
            DetectionTickLabel.Location = new Point(15, 170);
            DetectionTickLabel.Name = "DetectionTickLabel";
            DetectionTickLabel.Size = new Size(117, 17);
            DetectionTickLabel.TabIndex = 7;
            DetectionTickLabel.Text = "Detection Tick(sec)";
            // 
            // DetectionTickTextBox
            // 
            DetectionTickTextBox.Location = new Point(182, 167);
            DetectionTickTextBox.Name = "DetectionTickTextBox";
            DetectionTickTextBox.Size = new Size(70, 23);
            DetectionTickTextBox.TabIndex = 8;
            DetectionTickTextBox.TextAlign = HorizontalAlignment.Center;
            // 
            // StartedPingLabel
            // 
            StartedPingLabel.AutoSize = true;
            StartedPingLabel.Location = new Point(15, 200);
            StartedPingLabel.Name = "StartedPingLabel";
            StartedPingLabel.Size = new Size(153, 17);
            StartedPingLabel.TabIndex = 9;
            StartedPingLabel.Text = "Delay test after start(sec)";
            // 
            // StartedPingIntervalTextBox
            // 
            StartedPingIntervalTextBox.Location = new Point(182, 197);
            StartedPingIntervalTextBox.Name = "StartedPingIntervalTextBox";
            StartedPingIntervalTextBox.Size = new Size(70, 23);
            StartedPingIntervalTextBox.TabIndex = 10;
            StartedPingIntervalTextBox.TextAlign = HorizontalAlignment.Center;
            // 
            // STUNServerLabel
            // 
            STUNServerLabel.AutoSize = true;
            STUNServerLabel.Location = new Point(15, 230);
            STUNServerLabel.Name = "STUNServerLabel";
            STUNServerLabel.Size = new Size(82, 17);
            STUNServerLabel.TabIndex = 11;
            STUNServerLabel.Text = "STUN Server";
            // 
            // STUN_ServerComboBox
            // 
            STUN_ServerComboBox.AutoCompleteMode = AutoCompleteMode.Suggest;
            STUN_ServerComboBox.Location = new Point(182, 227);
            STUN_ServerComboBox.Name = "STUN_ServerComboBox";
            STUN_ServerComboBox.Size = new Size(264, 25);
            STUN_ServerComboBox.TabIndex = 12;
            // 
            // LanguageLabel
            // 
            LanguageLabel.AutoSize = true;
            LanguageLabel.Location = new Point(15, 260);
            LanguageLabel.Name = "LanguageLabel";
            LanguageLabel.Size = new Size(65, 17);
            LanguageLabel.TabIndex = 13;
            LanguageLabel.Text = "Language";
            // 
            // LanguageComboBox
            // 
            LanguageComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            LanguageComboBox.FormattingEnabled = true;
            LanguageComboBox.Location = new Point(182, 257);
            LanguageComboBox.Name = "LanguageComboBox";
            LanguageComboBox.Size = new Size(110, 25);
            LanguageComboBox.TabIndex = 14;
            // 
            // NFTabPage
            // 
            NFTabPage.BackColor = SystemColors.ButtonFace;
            NFTabPage.Controls.Add(FilterTCPCheckBox);
            NFTabPage.Controls.Add(FilterUDPCheckBox);
            NFTabPage.Controls.Add(FilterICMPCheckBox);
            NFTabPage.Controls.Add(DNSHijackLabel);
            NFTabPage.Controls.Add(ICMPDelayLabel);
            NFTabPage.Controls.Add(ICMPDelayTextBox);
            NFTabPage.Controls.Add(FilterDNSCheckBox);
            NFTabPage.Controls.Add(DNSHijackHostTextBox);
            NFTabPage.Controls.Add(HandleProcDNSCheckBox);
            NFTabPage.Controls.Add(DNSProxyCheckBox);
            NFTabPage.Controls.Add(ChildProcessHandleCheckBox);
            NFTabPage.Location = new Point(4, 29);
            NFTabPage.Name = "NFTabPage";
            NFTabPage.Padding = new Padding(3);
            NFTabPage.Size = new Size(495, 290);
            NFTabPage.TabIndex = 1;
            NFTabPage.Text = "Process Mode";
            // 
            // FilterTCPCheckBox
            // 
            FilterTCPCheckBox.AutoSize = true;
            FilterTCPCheckBox.Location = new Point(16, 16);
            FilterTCPCheckBox.Name = "FilterTCPCheckBox";
            FilterTCPCheckBox.Size = new Size(94, 21);
            FilterTCPCheckBox.TabIndex = 1;
            FilterTCPCheckBox.Text = "Handle TCP";
            FilterTCPCheckBox.UseVisualStyleBackColor = true;
            // 
            // FilterUDPCheckBox
            // 
            FilterUDPCheckBox.AutoSize = true;
            FilterUDPCheckBox.Location = new Point(216, 16);
            FilterUDPCheckBox.Name = "FilterUDPCheckBox";
            FilterUDPCheckBox.Size = new Size(97, 21);
            FilterUDPCheckBox.TabIndex = 2;
            FilterUDPCheckBox.Text = "Handle UDP";
            FilterUDPCheckBox.UseVisualStyleBackColor = true;
            // 
            // FilterICMPCheckBox
            // 
            FilterICMPCheckBox.AutoSize = true;
            FilterICMPCheckBox.Location = new Point(16, 48);
            FilterICMPCheckBox.Name = "FilterICMPCheckBox";
            FilterICMPCheckBox.Size = new Size(103, 21);
            FilterICMPCheckBox.TabIndex = 3;
            FilterICMPCheckBox.Text = "Handle ICMP";
            FilterICMPCheckBox.UseVisualStyleBackColor = true;
            // 
            // DNSHijackLabel
            // 
            DNSHijackLabel.AutoSize = true;
            DNSHijackLabel.Location = new Point(48, 144);
            DNSHijackLabel.Name = "DNSHijackLabel";
            DNSHijackLabel.Size = new Size(34, 17);
            DNSHijackLabel.TabIndex = 3;
            DNSHijackLabel.Text = "DNS";
            // 
            // ICMPDelayLabel
            // 
            ICMPDelayLabel.AutoSize = true;
            ICMPDelayLabel.Location = new Point(48, 80);
            ICMPDelayLabel.Name = "ICMPDelayLabel";
            ICMPDelayLabel.Size = new Size(99, 17);
            ICMPDelayLabel.TabIndex = 3;
            ICMPDelayLabel.Text = "ICMP delay(ms)";
            // 
            // ICMPDelayTextBox
            // 
            ICMPDelayTextBox.DataBindings.Add(new Binding("Enabled", FilterICMPCheckBox, "Checked", true));
            ICMPDelayTextBox.Location = new Point(216, 80);
            ICMPDelayTextBox.Name = "ICMPDelayTextBox";
            ICMPDelayTextBox.Size = new Size(98, 23);
            ICMPDelayTextBox.TabIndex = 4;
            ICMPDelayTextBox.TextAlign = HorizontalAlignment.Center;
            // 
            // FilterDNSCheckBox
            // 
            FilterDNSCheckBox.AutoSize = true;
            FilterDNSCheckBox.Location = new Point(16, 112);
            FilterDNSCheckBox.Name = "FilterDNSCheckBox";
            FilterDNSCheckBox.Size = new Size(191, 21);
            FilterDNSCheckBox.TabIndex = 5;
            FilterDNSCheckBox.Text = "Handle DNS (DNS hijacking)";
            FilterDNSCheckBox.UseVisualStyleBackColor = true;
            // 
            // DNSHijackHostTextBox
            // 
            DNSHijackHostTextBox.DataBindings.Add(new Binding("Enabled", FilterDNSCheckBox, "Checked", true));
            DNSHijackHostTextBox.Location = new Point(216, 144);
            DNSHijackHostTextBox.Name = "DNSHijackHostTextBox";
            DNSHijackHostTextBox.Size = new Size(191, 23);
            DNSHijackHostTextBox.TabIndex = 6;
            DNSHijackHostTextBox.TextAlign = HorizontalAlignment.Center;
            // 
            // HandleProcDNSCheckBox
            // 
            HandleProcDNSCheckBox.AutoSize = true;
            HandleProcDNSCheckBox.DataBindings.Add(new Binding("Enabled", FilterDNSCheckBox, "Checked", true));
            HandleProcDNSCheckBox.Location = new Point(16, 176);
            HandleProcDNSCheckBox.Name = "HandleProcDNSCheckBox";
            HandleProcDNSCheckBox.Size = new Size(208, 21);
            HandleProcDNSCheckBox.TabIndex = 7;
            HandleProcDNSCheckBox.Text = "Handle handled process's DNS";
            HandleProcDNSCheckBox.UseVisualStyleBackColor = true;
            // 
            // DNSProxyCheckBox
            // 
            DNSProxyCheckBox.AutoSize = true;
            DNSProxyCheckBox.DataBindings.Add(new Binding("Enabled", FilterDNSCheckBox, "Checked", true));
            DNSProxyCheckBox.Location = new Point(16, 208);
            DNSProxyCheckBox.Name = "DNSProxyCheckBox";
            DNSProxyCheckBox.Size = new Size(185, 21);
            DNSProxyCheckBox.TabIndex = 8;
            DNSProxyCheckBox.Text = "Handle DNS through proxy";
            DNSProxyCheckBox.UseVisualStyleBackColor = true;
            // 
            // ChildProcessHandleCheckBox
            // 
            ChildProcessHandleCheckBox.AutoSize = true;
            ChildProcessHandleCheckBox.Location = new Point(16, 240);
            ChildProcessHandleCheckBox.Name = "ChildProcessHandleCheckBox";
            ChildProcessHandleCheckBox.Size = new Size(149, 21);
            ChildProcessHandleCheckBox.TabIndex = 9;
            ChildProcessHandleCheckBox.Text = "Handle child process";
            ChildProcessHandleCheckBox.UseVisualStyleBackColor = true;
            // 
            // WinTUNTabPage
            // 
            WinTUNTabPage.BackColor = SystemColors.ButtonFace;
            WinTUNTabPage.Controls.Add(WinTUNGroupBox);
            WinTUNTabPage.Controls.Add(GlobalBypassIPsButton);
            WinTUNTabPage.Location = new Point(4, 29);
            WinTUNTabPage.Name = "WinTUNTabPage";
            WinTUNTabPage.Padding = new Padding(3);
            WinTUNTabPage.Size = new Size(495, 290);
            WinTUNTabPage.TabIndex = 2;
            WinTUNTabPage.Text = "WinTUN";
            // 
            // WinTUNGroupBox
            // 
            WinTUNGroupBox.Controls.Add(TUNTAPAddressLabel);
            WinTUNGroupBox.Controls.Add(TUNTAPAddressTextBox);
            WinTUNGroupBox.Controls.Add(TUNTAPNetmaskLabel);
            WinTUNGroupBox.Controls.Add(TUNTAPNetmaskTextBox);
            WinTUNGroupBox.Controls.Add(TUNTAPGatewayLabel);
            WinTUNGroupBox.Controls.Add(TUNTAPGatewayTextBox);
            WinTUNGroupBox.Controls.Add(EnableIPv6CheckBox);
            WinTUNGroupBox.Controls.Add(TUNTAPIPv6AddressLabel);
            WinTUNGroupBox.Controls.Add(TUNTAPIPv6AddressTextBox);
            WinTUNGroupBox.Controls.Add(TUNTAPIPv6GatewayLabel);
            WinTUNGroupBox.Controls.Add(TUNTAPIPv6GatewayTextBox);
            WinTUNGroupBox.Controls.Add(TUNTAPIPv6PrefixLabel);
            WinTUNGroupBox.Controls.Add(TUNTAPIPv6PrefixTextBox);
            WinTUNGroupBox.Controls.Add(TUNTAPDNSLabel);
            WinTUNGroupBox.Controls.Add(TUNTAPDNSTextBox);
            WinTUNGroupBox.Controls.Add(UseCustomDNSCheckBox);
            WinTUNGroupBox.Controls.Add(ProxyDNSCheckBox);
            WinTUNGroupBox.Location = new Point(6, 6);
            WinTUNGroupBox.Name = "WinTUNGroupBox";
            WinTUNGroupBox.Size = new Size(450, 250);
            WinTUNGroupBox.TabIndex = 0;
            WinTUNGroupBox.TabStop = false;
            // 
            // TUNTAPAddressLabel
            // 
            TUNTAPAddressLabel.AutoSize = true;
            TUNTAPAddressLabel.Location = new Point(9, 19);
            TUNTAPAddressLabel.Name = "TUNTAPAddressLabel";
            TUNTAPAddressLabel.Size = new Size(56, 17);
            TUNTAPAddressLabel.TabIndex = 0;
            TUNTAPAddressLabel.Text = "Address";
            // 
            // TUNTAPAddressTextBox
            // 
            TUNTAPAddressTextBox.Location = new Point(120, 16);
            TUNTAPAddressTextBox.Name = "TUNTAPAddressTextBox";
            TUNTAPAddressTextBox.Size = new Size(294, 23);
            TUNTAPAddressTextBox.TabIndex = 1;
            TUNTAPAddressTextBox.TextAlign = HorizontalAlignment.Center;
            // 
            // TUNTAPNetmaskLabel
            // 
            TUNTAPNetmaskLabel.AutoSize = true;
            TUNTAPNetmaskLabel.Location = new Point(9, 45);
            TUNTAPNetmaskLabel.Name = "TUNTAPNetmaskLabel";
            TUNTAPNetmaskLabel.Size = new Size(60, 17);
            TUNTAPNetmaskLabel.TabIndex = 2;
            TUNTAPNetmaskLabel.Text = "Netmask";
            // 
            // TUNTAPNetmaskTextBox
            // 
            TUNTAPNetmaskTextBox.Location = new Point(120, 42);
            TUNTAPNetmaskTextBox.Name = "TUNTAPNetmaskTextBox";
            TUNTAPNetmaskTextBox.Size = new Size(294, 23);
            TUNTAPNetmaskTextBox.TabIndex = 3;
            TUNTAPNetmaskTextBox.TextAlign = HorizontalAlignment.Center;
            // 
            // TUNTAPGatewayLabel
            // 
            TUNTAPGatewayLabel.AutoSize = true;
            TUNTAPGatewayLabel.Location = new Point(9, 71);
            TUNTAPGatewayLabel.Name = "TUNTAPGatewayLabel";
            TUNTAPGatewayLabel.Size = new Size(57, 17);
            TUNTAPGatewayLabel.TabIndex = 4;
            TUNTAPGatewayLabel.Text = "Gateway";
            // 
            // TUNTAPGatewayTextBox
            // 
            TUNTAPGatewayTextBox.Location = new Point(120, 68);
            TUNTAPGatewayTextBox.Name = "TUNTAPGatewayTextBox";
            TUNTAPGatewayTextBox.Size = new Size(294, 23);
            TUNTAPGatewayTextBox.TabIndex = 5;
            TUNTAPGatewayTextBox.TextAlign = HorizontalAlignment.Center;
            // 
            // TUNTAPDNSLabel
            // 
            TUNTAPDNSLabel.AutoSize = true;
            EnableIPv6CheckBox.AutoSize = true;
            EnableIPv6CheckBox.Location = new Point(10, 94);
            EnableIPv6CheckBox.Name = "EnableIPv6CheckBox";
            EnableIPv6CheckBox.Size = new Size(103, 21);
            EnableIPv6CheckBox.TabIndex = 6;
            EnableIPv6CheckBox.Text = "Enable IPv6";
            EnableIPv6CheckBox.UseVisualStyleBackColor = true;
            // 
            // TUNTAPIPv6AddressLabel
            // 
            TUNTAPIPv6AddressLabel.AutoSize = true;
            TUNTAPIPv6AddressLabel.Location = new Point(9, 122);
            TUNTAPIPv6AddressLabel.Name = "TUNTAPIPv6AddressLabel";
            TUNTAPIPv6AddressLabel.Size = new Size(80, 17);
            TUNTAPIPv6AddressLabel.TabIndex = 7;
            TUNTAPIPv6AddressLabel.Text = "IPv6 Address";
            // 
            // TUNTAPIPv6AddressTextBox
            // 
            TUNTAPIPv6AddressTextBox.Location = new Point(120, 119);
            TUNTAPIPv6AddressTextBox.Name = "TUNTAPIPv6AddressTextBox";
            TUNTAPIPv6AddressTextBox.Size = new Size(294, 23);
            TUNTAPIPv6AddressTextBox.TabIndex = 8;
            TUNTAPIPv6AddressTextBox.TextAlign = HorizontalAlignment.Center;
            // 
            // TUNTAPIPv6GatewayLabel
            // 
            TUNTAPIPv6GatewayLabel.AutoSize = true;
            TUNTAPIPv6GatewayLabel.Location = new Point(9, 148);
            TUNTAPIPv6GatewayLabel.Name = "TUNTAPIPv6GatewayLabel";
            TUNTAPIPv6GatewayLabel.Size = new Size(81, 17);
            TUNTAPIPv6GatewayLabel.TabIndex = 9;
            TUNTAPIPv6GatewayLabel.Text = "IPv6 Gateway";
            // 
            // TUNTAPIPv6GatewayTextBox
            // 
            TUNTAPIPv6GatewayTextBox.Location = new Point(120, 145);
            TUNTAPIPv6GatewayTextBox.Name = "TUNTAPIPv6GatewayTextBox";
            TUNTAPIPv6GatewayTextBox.Size = new Size(294, 23);
            TUNTAPIPv6GatewayTextBox.TabIndex = 10;
            TUNTAPIPv6GatewayTextBox.TextAlign = HorizontalAlignment.Center;
            // 
            // TUNTAPIPv6PrefixLabel
            // 
            TUNTAPIPv6PrefixLabel.AutoSize = true;
            TUNTAPIPv6PrefixLabel.Location = new Point(9, 174);
            TUNTAPIPv6PrefixLabel.Name = "TUNTAPIPv6PrefixLabel";
            TUNTAPIPv6PrefixLabel.Size = new Size(84, 17);
            TUNTAPIPv6PrefixLabel.TabIndex = 11;
            TUNTAPIPv6PrefixLabel.Text = "IPv6 Prefix";
            // 
            // TUNTAPIPv6PrefixTextBox
            // 
            TUNTAPIPv6PrefixTextBox.Location = new Point(120, 171);
            TUNTAPIPv6PrefixTextBox.Name = "TUNTAPIPv6PrefixTextBox";
            TUNTAPIPv6PrefixTextBox.Size = new Size(294, 23);
            TUNTAPIPv6PrefixTextBox.TabIndex = 12;
            TUNTAPIPv6PrefixTextBox.TextAlign = HorizontalAlignment.Center;
            // 
            // TUNTAPDNSLabel
            // 
            TUNTAPDNSLabel.Location = new Point(9, 200);
            TUNTAPDNSLabel.Name = "TUNTAPDNSLabel";
            TUNTAPDNSLabel.Size = new Size(34, 17);
            TUNTAPDNSLabel.TabIndex = 6;
            TUNTAPDNSLabel.Text = "DNS";
            // 
            // TUNTAPDNSTextBox
            // 
            TUNTAPDNSTextBox.DataBindings.Add(new Binding("Enabled", UseCustomDNSCheckBox, "Checked", true));
            TUNTAPDNSTextBox.Location = new Point(120, 197);
            TUNTAPDNSTextBox.Name = "TUNTAPDNSTextBox";
            TUNTAPDNSTextBox.Size = new Size(294, 23);
            TUNTAPDNSTextBox.TabIndex = 7;
            TUNTAPDNSTextBox.TextAlign = HorizontalAlignment.Center;
            // 
            // UseCustomDNSCheckBox
            // 
            UseCustomDNSCheckBox.AutoSize = true;
            UseCustomDNSCheckBox.Location = new Point(10, 224);
            UseCustomDNSCheckBox.Name = "UseCustomDNSCheckBox";
            UseCustomDNSCheckBox.Size = new Size(125, 21);
            UseCustomDNSCheckBox.TabIndex = 8;
            UseCustomDNSCheckBox.Text = "Use custom DNS";
            UseCustomDNSCheckBox.UseVisualStyleBackColor = true;
            UseCustomDNSCheckBox.Click += TUNTAPUseCustomDNSCheckBox_CheckedChanged;
            // 
            // ProxyDNSCheckBox
            // 
            ProxyDNSCheckBox.AutoSize = true;
            ProxyDNSCheckBox.DataBindings.Add(new Binding("Visible", UseCustomDNSCheckBox, "Checked", true));
            ProxyDNSCheckBox.Location = new Point(175, 224);
            ProxyDNSCheckBox.Name = "ProxyDNSCheckBox";
            ProxyDNSCheckBox.Size = new Size(89, 21);
            ProxyDNSCheckBox.TabIndex = 9;
            ProxyDNSCheckBox.Text = "Proxy DNS";
            ProxyDNSCheckBox.UseVisualStyleBackColor = true;
            // 
            // GlobalBypassIPsButton
            // 
            GlobalBypassIPsButton.Location = new Point(6, 264);
            GlobalBypassIPsButton.Name = "GlobalBypassIPsButton";
            GlobalBypassIPsButton.Size = new Size(128, 23);
            GlobalBypassIPsButton.TabIndex = 1;
            GlobalBypassIPsButton.Text = "Global Bypass IPs";
            GlobalBypassIPsButton.UseVisualStyleBackColor = true;
            GlobalBypassIPsButton.Click += GlobalBypassIPsButton_Click;
            // 
            // v2rayTabPage
            // 
            v2rayTabPage.BackColor = SystemColors.ButtonFace;
            v2rayTabPage.Controls.Add(DefFingerprintComboBox);
            v2rayTabPage.Controls.Add(DefFingerprintLabel);
            v2rayTabPage.Controls.Add(EnableFragmentBox);
            v2rayTabPage.Controls.Add(TLSAllowInsecureCheckBox);
            v2rayTabPage.Controls.Add(UseMuxCheckBox);
            v2rayTabPage.Controls.Add(KCPGroupBox);
            v2rayTabPage.Location = new Point(4, 29);
            v2rayTabPage.Name = "v2rayTabPage";
            v2rayTabPage.Padding = new Padding(3);
            v2rayTabPage.Size = new Size(495, 290);
            v2rayTabPage.TabIndex = 3;
            v2rayTabPage.Text = "V2Ray";
            // 
            // DefFingerprintComboBox
            // 
            DefFingerprintComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            DefFingerprintComboBox.FormattingEnabled = true;
            DefFingerprintComboBox.Location = new Point(150, 43);
            DefFingerprintComboBox.Name = "DefFingerprintComboBox";
            DefFingerprintComboBox.Size = new Size(140, 25);
            DefFingerprintComboBox.TabIndex = 15;
            // 
            // DefFingerprintLabel
            // 
            DefFingerprintLabel.AutoSize = true;
            DefFingerprintLabel.Location = new Point(15, 46);
            DefFingerprintLabel.Name = "DefFingerprintLabel";
            DefFingerprintLabel.Size = new Size(116, 17);
            DefFingerprintLabel.TabIndex = 0;
            DefFingerprintLabel.Text = "Default Fingerprint";
            // 
            // EnableFragmentBox
            // 
            EnableFragmentBox.AutoSize = true;
            EnableFragmentBox.Location = new Point(17, 16);
            EnableFragmentBox.Name = "EnableFragmentBox";
            EnableFragmentBox.Size = new Size(125, 21);
            EnableFragmentBox.TabIndex = 0;
            EnableFragmentBox.Text = "Enable Fragment";
            EnableFragmentBox.UseVisualStyleBackColor = true;
            // 
            // TLSAllowInsecureCheckBox
            // 
            TLSAllowInsecureCheckBox.AutoSize = true;
            TLSAllowInsecureCheckBox.Location = new Point(159, 16);
            TLSAllowInsecureCheckBox.Name = "TLSAllowInsecureCheckBox";
            TLSAllowInsecureCheckBox.Size = new Size(131, 21);
            TLSAllowInsecureCheckBox.TabIndex = 1;
            TLSAllowInsecureCheckBox.Text = "TLS AllowInsecure";
            TLSAllowInsecureCheckBox.UseVisualStyleBackColor = true;
            // 
            // UseMuxCheckBox
            // 
            UseMuxCheckBox.AutoSize = true;
            UseMuxCheckBox.Location = new Point(311, 16);
            UseMuxCheckBox.Name = "UseMuxCheckBox";
            UseMuxCheckBox.Size = new Size(78, 21);
            UseMuxCheckBox.TabIndex = 2;
            UseMuxCheckBox.Text = "Use Mux";
            UseMuxCheckBox.UseVisualStyleBackColor = true;
            // 
            // KCPGroupBox
            // 
            KCPGroupBox.Controls.Add(mtuLabel);
            KCPGroupBox.Controls.Add(mtuTextBox);
            KCPGroupBox.Controls.Add(ttiLabel);
            KCPGroupBox.Controls.Add(ttiTextBox);
            KCPGroupBox.Controls.Add(uplinkCapacityLabel);
            KCPGroupBox.Controls.Add(uplinkCapacityTextBox);
            KCPGroupBox.Controls.Add(downlinkCapacityLabel);
            KCPGroupBox.Controls.Add(downlinkCapacityTextBox);
            KCPGroupBox.Controls.Add(readBufferSizeLabel);
            KCPGroupBox.Controls.Add(readBufferSizeTextBox);
            KCPGroupBox.Controls.Add(writeBufferSizeLabel);
            KCPGroupBox.Controls.Add(writeBufferSizeTextBox);
            KCPGroupBox.Controls.Add(congestionCheckBox);
            KCPGroupBox.Location = new Point(9, 75);
            KCPGroupBox.Name = "KCPGroupBox";
            KCPGroupBox.Size = new Size(447, 204);
            KCPGroupBox.TabIndex = 3;
            KCPGroupBox.TabStop = false;
            KCPGroupBox.Text = "KCP";
            // 
            // mtuLabel
            // 
            mtuLabel.AutoSize = true;
            mtuLabel.Location = new Point(6, 26);
            mtuLabel.Name = "mtuLabel";
            mtuLabel.Size = new Size(36, 17);
            mtuLabel.TabIndex = 0;
            mtuLabel.Text = "MTU";
            // 
            // mtuTextBox
            // 
            mtuTextBox.Location = new Point(103, 17);
            mtuTextBox.Name = "mtuTextBox";
            mtuTextBox.Size = new Size(90, 23);
            mtuTextBox.TabIndex = 1;
            mtuTextBox.TextAlign = HorizontalAlignment.Center;
            // 
            // ttiLabel
            // 
            ttiLabel.AutoSize = true;
            ttiLabel.Location = new Point(216, 26);
            ttiLabel.Name = "ttiLabel";
            ttiLabel.Size = new Size(26, 17);
            ttiLabel.TabIndex = 2;
            ttiLabel.Text = "TTI";
            // 
            // ttiTextBox
            // 
            ttiTextBox.Location = new Point(331, 17);
            ttiTextBox.Name = "ttiTextBox";
            ttiTextBox.Size = new Size(90, 23);
            ttiTextBox.TabIndex = 3;
            ttiTextBox.TextAlign = HorizontalAlignment.Center;
            // 
            // uplinkCapacityLabel
            // 
            uplinkCapacityLabel.AutoSize = true;
            uplinkCapacityLabel.Location = new Point(6, 68);
            uplinkCapacityLabel.Name = "uplinkCapacityLabel";
            uplinkCapacityLabel.Size = new Size(94, 17);
            uplinkCapacityLabel.TabIndex = 4;
            uplinkCapacityLabel.Text = "UplinkCapacity";
            // 
            // uplinkCapacityTextBox
            // 
            uplinkCapacityTextBox.Location = new Point(103, 65);
            uplinkCapacityTextBox.Name = "uplinkCapacityTextBox";
            uplinkCapacityTextBox.Size = new Size(90, 23);
            uplinkCapacityTextBox.TabIndex = 5;
            uplinkCapacityTextBox.TextAlign = HorizontalAlignment.Center;
            // 
            // downlinkCapacityLabel
            // 
            downlinkCapacityLabel.AutoSize = true;
            downlinkCapacityLabel.Location = new Point(216, 68);
            downlinkCapacityLabel.Name = "downlinkCapacityLabel";
            downlinkCapacityLabel.Size = new Size(110, 17);
            downlinkCapacityLabel.TabIndex = 6;
            downlinkCapacityLabel.Text = "DownlinkCapacity";
            // 
            // downlinkCapacityTextBox
            // 
            downlinkCapacityTextBox.Location = new Point(331, 65);
            downlinkCapacityTextBox.Name = "downlinkCapacityTextBox";
            downlinkCapacityTextBox.Size = new Size(90, 23);
            downlinkCapacityTextBox.TabIndex = 7;
            downlinkCapacityTextBox.TextAlign = HorizontalAlignment.Center;
            // 
            // readBufferSizeLabel
            // 
            readBufferSizeLabel.AutoSize = true;
            readBufferSizeLabel.Location = new Point(6, 109);
            readBufferSizeLabel.Name = "readBufferSizeLabel";
            readBufferSizeLabel.Size = new Size(96, 17);
            readBufferSizeLabel.TabIndex = 8;
            readBufferSizeLabel.Text = "ReadBufferSize";
            // 
            // readBufferSizeTextBox
            // 
            readBufferSizeTextBox.Location = new Point(103, 106);
            readBufferSizeTextBox.Name = "readBufferSizeTextBox";
            readBufferSizeTextBox.Size = new Size(90, 23);
            readBufferSizeTextBox.TabIndex = 9;
            readBufferSizeTextBox.TextAlign = HorizontalAlignment.Center;
            // 
            // writeBufferSizeLabel
            // 
            writeBufferSizeLabel.AutoSize = true;
            writeBufferSizeLabel.Location = new Point(216, 109);
            writeBufferSizeLabel.Name = "writeBufferSizeLabel";
            writeBufferSizeLabel.Size = new Size(97, 17);
            writeBufferSizeLabel.TabIndex = 10;
            writeBufferSizeLabel.Text = "WriteBufferSize";
            // 
            // writeBufferSizeTextBox
            // 
            writeBufferSizeTextBox.Location = new Point(331, 106);
            writeBufferSizeTextBox.Name = "writeBufferSizeTextBox";
            writeBufferSizeTextBox.Size = new Size(90, 23);
            writeBufferSizeTextBox.TabIndex = 11;
            writeBufferSizeTextBox.TextAlign = HorizontalAlignment.Center;
            // 
            // congestionCheckBox
            // 
            congestionCheckBox.AutoSize = true;
            congestionCheckBox.Location = new Point(8, 139);
            congestionCheckBox.Name = "congestionCheckBox";
            congestionCheckBox.Size = new Size(93, 21);
            congestionCheckBox.TabIndex = 12;
            congestionCheckBox.Text = "Congestion";
            congestionCheckBox.UseVisualStyleBackColor = true;
            // 
            // HysteriaTabPage
            // 
            HysteriaTabPage.Controls.Add(HysteriaBandwidthGroupBox);
            HysteriaTabPage.Location = new Point(4, 29);
            HysteriaTabPage.Name = "HysteriaTabPage";
            HysteriaTabPage.Padding = new Padding(3);
            HysteriaTabPage.Size = new Size(495, 290);
            HysteriaTabPage.TabIndex = 6;
            HysteriaTabPage.Text = "Hysteria";
            HysteriaTabPage.UseVisualStyleBackColor = true;
            // 
            // HysteriaBandwidthGroupBox
            // 
            HysteriaBandwidthGroupBox.Controls.Add(HysteriaDownMbpsTextBox);
            HysteriaBandwidthGroupBox.Controls.Add(HysteriaUpMbpsTextBox);
            HysteriaBandwidthGroupBox.Location = new Point(6, 6);
            HysteriaBandwidthGroupBox.Name = "HysteriaBandwidthGroupBox";
            HysteriaBandwidthGroupBox.Size = new Size(260, 69);
            HysteriaBandwidthGroupBox.TabIndex = 1;
            HysteriaBandwidthGroupBox.TabStop = false;
            HysteriaBandwidthGroupBox.Text = "Bande Passante Maximale (Up/Down)";
            // 
            // HysteriaDownMbpsTextBox
            // 
            HysteriaDownMbpsTextBox.Location = new Point(118, 22);
            HysteriaDownMbpsTextBox.Name = "HysteriaDownMbpsTextBox";
            HysteriaDownMbpsTextBox.Size = new Size(100, 23);
            HysteriaDownMbpsTextBox.TabIndex = 1;
            // 
            // HysteriaUpMbpsTextBox
            // 
            HysteriaUpMbpsTextBox.Location = new Point(12, 22);
            HysteriaUpMbpsTextBox.Name = "HysteriaUpMbpsTextBox";
            HysteriaUpMbpsTextBox.Size = new Size(100, 23);
            HysteriaUpMbpsTextBox.TabIndex = 0;
            // 
            // OtherTabPage
            // 
            OtherTabPage.BackColor = SystemColors.ButtonFace;
            OtherTabPage.Controls.Add(ExitWhenClosedCheckBox);
            OtherTabPage.Controls.Add(StopWhenExitedCheckBox);
            OtherTabPage.Controls.Add(StartWhenOpenedCheckBox);
            OtherTabPage.Controls.Add(MinimizeWhenStartedCheckBox);
            OtherTabPage.Controls.Add(RunAtStartupCheckBox);
            OtherTabPage.Controls.Add(CheckUpdateWhenOpenedCheckBox);
            OtherTabPage.Controls.Add(NoSupportDialogCheckBox);
            OtherTabPage.Controls.Add(CheckBetaUpdateCheckBox);
            OtherTabPage.Controls.Add(UpdateServersWhenOpenedCheckBox);
            OtherTabPage.Location = new Point(4, 29);
            OtherTabPage.Name = "OtherTabPage";
            OtherTabPage.Padding = new Padding(3);
            OtherTabPage.Size = new Size(495, 290);
            OtherTabPage.TabIndex = 4;
            OtherTabPage.Text = "Others";
            // 
            // ExitWhenClosedCheckBox
            // 
            ExitWhenClosedCheckBox.AutoSize = true;
            ExitWhenClosedCheckBox.Location = new Point(16, 16);
            ExitWhenClosedCheckBox.Name = "ExitWhenClosedCheckBox";
            ExitWhenClosedCheckBox.Size = new Size(123, 21);
            ExitWhenClosedCheckBox.TabIndex = 0;
            ExitWhenClosedCheckBox.Text = "Exit when closed";
            ExitWhenClosedCheckBox.TextAlign = ContentAlignment.MiddleRight;
            ExitWhenClosedCheckBox.UseVisualStyleBackColor = true;
            // 
            // StopWhenExitedCheckBox
            // 
            StopWhenExitedCheckBox.AutoSize = true;
            StopWhenExitedCheckBox.Location = new Point(224, 18);
            StopWhenExitedCheckBox.Name = "StopWhenExitedCheckBox";
            StopWhenExitedCheckBox.Size = new Size(127, 21);
            StopWhenExitedCheckBox.TabIndex = 1;
            StopWhenExitedCheckBox.Text = "Stop when exited";
            StopWhenExitedCheckBox.TextAlign = ContentAlignment.MiddleRight;
            StopWhenExitedCheckBox.UseVisualStyleBackColor = true;
            // 
            // StartWhenOpenedCheckBox
            // 
            StartWhenOpenedCheckBox.AutoSize = true;
            StartWhenOpenedCheckBox.Location = new Point(16, 48);
            StartWhenOpenedCheckBox.Name = "StartWhenOpenedCheckBox";
            StartWhenOpenedCheckBox.Size = new Size(137, 21);
            StartWhenOpenedCheckBox.TabIndex = 2;
            StartWhenOpenedCheckBox.Text = "Start when opened";
            StartWhenOpenedCheckBox.TextAlign = ContentAlignment.MiddleRight;
            StartWhenOpenedCheckBox.UseVisualStyleBackColor = true;
            // 
            // MinimizeWhenStartedCheckBox
            // 
            MinimizeWhenStartedCheckBox.AutoSize = true;
            MinimizeWhenStartedCheckBox.Location = new Point(224, 48);
            MinimizeWhenStartedCheckBox.Name = "MinimizeWhenStartedCheckBox";
            MinimizeWhenStartedCheckBox.Size = new Size(158, 21);
            MinimizeWhenStartedCheckBox.TabIndex = 3;
            MinimizeWhenStartedCheckBox.Text = "Minimize when started";
            MinimizeWhenStartedCheckBox.UseVisualStyleBackColor = true;
            // 
            // RunAtStartupCheckBox
            // 
            RunAtStartupCheckBox.AutoSize = true;
            RunAtStartupCheckBox.Location = new Point(16, 80);
            RunAtStartupCheckBox.Name = "RunAtStartupCheckBox";
            RunAtStartupCheckBox.Size = new Size(109, 21);
            RunAtStartupCheckBox.TabIndex = 4;
            RunAtStartupCheckBox.Text = "Run at startup";
            RunAtStartupCheckBox.UseVisualStyleBackColor = true;
            // 
            // CheckUpdateWhenOpenedCheckBox
            // 
            CheckUpdateWhenOpenedCheckBox.AutoSize = true;
            CheckUpdateWhenOpenedCheckBox.Location = new Point(224, 80);
            CheckUpdateWhenOpenedCheckBox.Name = "CheckUpdateWhenOpenedCheckBox";
            CheckUpdateWhenOpenedCheckBox.Size = new Size(190, 21);
            CheckUpdateWhenOpenedCheckBox.TabIndex = 5;
            CheckUpdateWhenOpenedCheckBox.Text = "Check update when opened";
            CheckUpdateWhenOpenedCheckBox.TextAlign = ContentAlignment.MiddleRight;
            CheckUpdateWhenOpenedCheckBox.UseVisualStyleBackColor = true;
            // 
            // NoSupportDialogCheckBox
            // 
            NoSupportDialogCheckBox.AutoSize = true;
            NoSupportDialogCheckBox.Location = new Point(16, 112);
            NoSupportDialogCheckBox.Name = "NoSupportDialogCheckBox";
            NoSupportDialogCheckBox.Size = new Size(174, 21);
            NoSupportDialogCheckBox.TabIndex = 6;
            NoSupportDialogCheckBox.Text = "Disable Support Warning";
            NoSupportDialogCheckBox.UseVisualStyleBackColor = true;
            // 
            // CheckBetaUpdateCheckBox
            // 
            CheckBetaUpdateCheckBox.AutoSize = true;
            CheckBetaUpdateCheckBox.Location = new Point(224, 112);
            CheckBetaUpdateCheckBox.Name = "CheckBetaUpdateCheckBox";
            CheckBetaUpdateCheckBox.Size = new Size(137, 21);
            CheckBetaUpdateCheckBox.TabIndex = 7;
            CheckBetaUpdateCheckBox.Text = "Check Beta update";
            CheckBetaUpdateCheckBox.TextAlign = ContentAlignment.MiddleRight;
            CheckBetaUpdateCheckBox.UseVisualStyleBackColor = true;
            // 
            // UpdateServersWhenOpenedCheckBox
            // 
            UpdateServersWhenOpenedCheckBox.AutoSize = true;
            UpdateServersWhenOpenedCheckBox.Location = new Point(224, 144);
            UpdateServersWhenOpenedCheckBox.Name = "UpdateServersWhenOpenedCheckBox";
            UpdateServersWhenOpenedCheckBox.Size = new Size(200, 21);
            UpdateServersWhenOpenedCheckBox.TabIndex = 8;
            UpdateServersWhenOpenedCheckBox.Text = "Update Servers when opened";
            UpdateServersWhenOpenedCheckBox.TextAlign = ContentAlignment.MiddleRight;
            UpdateServersWhenOpenedCheckBox.UseVisualStyleBackColor = true;
            // 
            // AioDNSTabPage
            // 
            AioDNSTabPage.Controls.Add(ChinaDNSLabel);
            AioDNSTabPage.Controls.Add(ChinaDNSTextBox);
            AioDNSTabPage.Controls.Add(OtherDNSLabel);
            AioDNSTabPage.Controls.Add(OtherDNSTextBox);
            AioDNSTabPage.Location = new Point(4, 29);
            AioDNSTabPage.Name = "AioDNSTabPage";
            AioDNSTabPage.Padding = new Padding(3);
            AioDNSTabPage.Size = new Size(495, 290);
            AioDNSTabPage.TabIndex = 5;
            AioDNSTabPage.Text = "AioDNS";
            AioDNSTabPage.UseVisualStyleBackColor = true;
            // 
            // ChinaDNSLabel
            // 
            ChinaDNSLabel.AutoSize = true;
            ChinaDNSLabel.Location = new Point(15, 23);
            ChinaDNSLabel.Name = "ChinaDNSLabel";
            ChinaDNSLabel.Size = new Size(70, 17);
            ChinaDNSLabel.TabIndex = 0;
            ChinaDNSLabel.Text = "China DNS";
            // 
            // ChinaDNSTextBox
            // 
            ChinaDNSTextBox.Location = new Point(114, 20);
            ChinaDNSTextBox.Name = "ChinaDNSTextBox";
            ChinaDNSTextBox.Size = new Size(312, 23);
            ChinaDNSTextBox.TabIndex = 1;
            ChinaDNSTextBox.TextAlign = HorizontalAlignment.Center;
            // 
            // OtherDNSLabel
            // 
            OtherDNSLabel.AutoSize = true;
            OtherDNSLabel.Location = new Point(15, 63);
            OtherDNSLabel.Name = "OtherDNSLabel";
            OtherDNSLabel.Size = new Size(71, 17);
            OtherDNSLabel.TabIndex = 2;
            OtherDNSLabel.Text = "Other DNS";
            // 
            // OtherDNSTextBox
            // 
            OtherDNSTextBox.Location = new Point(114, 60);
            OtherDNSTextBox.Name = "OtherDNSTextBox";
            OtherDNSTextBox.Size = new Size(312, 23);
            OtherDNSTextBox.TabIndex = 3;
            OtherDNSTextBox.TextAlign = HorizontalAlignment.Center;
            // 
            // ControlButton
            // 
            ControlButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            ControlButton.Location = new Point(431, 332);
            ControlButton.Name = "ControlButton";
            ControlButton.Size = new Size(75, 23);
            ControlButton.TabIndex = 1;
            ControlButton.Text = "Save";
            ControlButton.UseVisualStyleBackColor = true;
            ControlButton.Click += ControlButton_Click;
            // 
            // errorProvider
            // 
            errorProvider.BlinkStyle = ErrorBlinkStyle.NeverBlink;
            errorProvider.ContainerControl = this;
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 1;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.Controls.Add(TabControl);
            tableLayoutPanel1.Controls.Add(ControlButton);
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.Location = new Point(0, 0);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 2;
            tableLayoutPanel1.RowStyles.Add(new RowStyle());
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 23F));
            tableLayoutPanel1.Size = new Size(509, 358);
            tableLayoutPanel1.TabIndex = 10;
            tableLayoutPanel1.Paint += tableLayoutPanel1_Paint;
            // 
            // SettingForm
            // 
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            ClientSize = new Size(509, 358);
            Controls.Add(tableLayoutPanel1);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Name = "SettingForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Settings";
            Load += SettingForm_Load;
            TabControl.ResumeLayout(false);
            GeneralTabPage.ResumeLayout(false);
            GeneralTabPage.PerformLayout();
            PortGroupBox.ResumeLayout(false);
            PortGroupBox.PerformLayout();
            NFTabPage.ResumeLayout(false);
            NFTabPage.PerformLayout();
            WinTUNTabPage.ResumeLayout(false);
            WinTUNGroupBox.ResumeLayout(false);
            WinTUNGroupBox.PerformLayout();
            v2rayTabPage.ResumeLayout(false);
            v2rayTabPage.PerformLayout();
            KCPGroupBox.ResumeLayout(false);
            KCPGroupBox.PerformLayout();
            HysteriaTabPage.ResumeLayout(false);
            HysteriaBandwidthGroupBox.ResumeLayout(false);
            HysteriaBandwidthGroupBox.PerformLayout();
            OtherTabPage.ResumeLayout(false);
            OtherTabPage.PerformLayout();
            AioDNSTabPage.ResumeLayout(false);
            AioDNSTabPage.PerformLayout();
            ((ISupportInitialize)errorProvider).EndInit();
            tableLayoutPanel1.ResumeLayout(false);
            ResumeLayout(false);

        }
        private System.Windows.Forms.CheckBox EnableFragmentBox;
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
        private System.Windows.Forms.CheckBox EnableIPv6CheckBox;
        private System.Windows.Forms.Label TUNTAPIPv6AddressLabel;
        private System.Windows.Forms.TextBox TUNTAPIPv6AddressTextBox;
        private System.Windows.Forms.Label TUNTAPIPv6GatewayLabel;
        private System.Windows.Forms.TextBox TUNTAPIPv6GatewayTextBox;
        private System.Windows.Forms.Label TUNTAPIPv6PrefixLabel;
        private System.Windows.Forms.TextBox TUNTAPIPv6PrefixTextBox;
        private System.Windows.Forms.Label TUNTAPNetmaskLabel;
        private System.Windows.Forms.TextBox TUNTAPNetmaskTextBox;
        private System.Windows.Forms.Label TUNTAPAddressLabel;
        private System.Windows.Forms.TextBox TUNTAPAddressTextBox;
        private System.Windows.Forms.Button GlobalBypassIPsButton;
        private System.Windows.Forms.CheckBox FilterDNSCheckBox;
        private System.Windows.Forms.Button ControlButton;
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
        private TextBox OutboundDNSTextBox;
        private Label OutboundDNSLabel;
        private Label DefFingerprintLabel;
        private ComboBox DefFingerprintComboBox;
        private TabPage HysteriaTabPage;
        private TableLayoutPanel tableLayoutPanel1;
        private GroupBox HysteriaBandwidthGroupBox;
        private TextBox HysteriaDownMbpsTextBox;
        private TextBox HysteriaUpMbpsTextBox;
    }
}
