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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SettingForm));
            this.TabControl = new System.Windows.Forms.TabControl();
            this.GeneralTabPage = new System.Windows.Forms.TabPage();
            this.LanguageLabel = new System.Windows.Forms.Label();
            this.LanguageComboBox = new System.Windows.Forms.ComboBox();
            this.AclAddrTextBox = new System.Windows.Forms.TextBox();
            this.AclLabel = new System.Windows.Forms.Label();
            this.DetectionIntervalLabel = new System.Windows.Forms.Label();
            this.DetectionIntervalTextBox = new System.Windows.Forms.TextBox();
            this.TcpingAtStartedCheckBox = new System.Windows.Forms.CheckBox();
            this.STUNServerLabel = new System.Windows.Forms.Label();
            this.STUN_ServerComboBox = new System.Windows.Forms.ComboBox();
            this.ProfileCountLabel = new System.Windows.Forms.Label();
            this.ProfileCountTextBox = new System.Windows.Forms.TextBox();
            this.PortGroupBox = new System.Windows.Forms.GroupBox();
            this.RedirectorLabel = new System.Windows.Forms.Label();
            this.RedirectorTextBox = new System.Windows.Forms.TextBox();
            this.AllowDevicesCheckBox = new System.Windows.Forms.CheckBox();
            this.HTTPPortLabel = new System.Windows.Forms.Label();
            this.HTTPPortTextBox = new System.Windows.Forms.TextBox();
            this.Socks5PortLabel = new System.Windows.Forms.Label();
            this.Socks5PortTextBox = new System.Windows.Forms.TextBox();
            this.ResolveServerHostnameCheckBox = new System.Windows.Forms.CheckBox();
            this.BootShadowsocksFromDLLCheckBox = new System.Windows.Forms.CheckBox();
            this.NFTabPage = new System.Windows.Forms.TabPage();
            this.ModifySystemDNSCheckBox = new System.Windows.Forms.CheckBox();
            this.TAPTabPage = new System.Windows.Forms.TabPage();
            this.GlobalBypassIPsButton = new System.Windows.Forms.Button();
            this.TUNTAPGroupBox = new System.Windows.Forms.GroupBox();
            this.UseFakeDNSCheckBox = new System.Windows.Forms.CheckBox();
            this.ProxyDNSCheckBox = new System.Windows.Forms.CheckBox();
            this.ICSCheckBox = new System.Windows.Forms.CheckBox();
            this.UseCustomDNSCheckBox = new System.Windows.Forms.CheckBox();
            this.TUNTAPDNSLabel = new System.Windows.Forms.Label();
            this.TUNTAPDNSTextBox = new System.Windows.Forms.TextBox();
            this.TUNTAPGatewayLabel = new System.Windows.Forms.Label();
            this.TUNTAPGatewayTextBox = new System.Windows.Forms.TextBox();
            this.TUNTAPNetmaskLabel = new System.Windows.Forms.Label();
            this.TUNTAPNetmaskTextBox = new System.Windows.Forms.TextBox();
            this.TUNTAPAddressLabel = new System.Windows.Forms.Label();
            this.TUNTAPAddressTextBox = new System.Windows.Forms.TextBox();
            this.v2rayTabPage = new System.Windows.Forms.TabPage();
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
            this.TLSAllowInsecureCheckBox = new System.Windows.Forms.CheckBox();
            this.OtherTabPage = new System.Windows.Forms.TabPage();
            this.UpdateSubscribeatWhenOpenedCheckBox = new System.Windows.Forms.CheckBox();
            this.RunAtStartupCheckBox = new System.Windows.Forms.CheckBox();
            this.MinimizeWhenStartedCheckBox = new System.Windows.Forms.CheckBox();
            this.CheckBetaUpdateCheckBox = new System.Windows.Forms.CheckBox();
            this.CheckUpdateWhenOpenedCheckBox = new System.Windows.Forms.CheckBox();
            this.StartWhenOpenedCheckBox = new System.Windows.Forms.CheckBox();
            this.StopWhenExitedCheckBox = new System.Windows.Forms.CheckBox();
            this.ExitWhenClosedCheckBox = new System.Windows.Forms.CheckBox();
            this.ControlButton = new System.Windows.Forms.Button();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.UseMuxCheckBox = new System.Windows.Forms.CheckBox();
            this.TabControl.SuspendLayout();
            this.GeneralTabPage.SuspendLayout();
            this.PortGroupBox.SuspendLayout();
            this.NFTabPage.SuspendLayout();
            this.TAPTabPage.SuspendLayout();
            this.TUNTAPGroupBox.SuspendLayout();
            this.v2rayTabPage.SuspendLayout();
            this.KCPGroupBox.SuspendLayout();
            this.OtherTabPage.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // TabControl
            // 
            this.TabControl.Appearance = System.Windows.Forms.TabAppearance.FlatButtons;
            this.TabControl.Controls.Add(this.GeneralTabPage);
            this.TabControl.Controls.Add(this.NFTabPage);
            this.TabControl.Controls.Add(this.TAPTabPage);
            this.TabControl.Controls.Add(this.v2rayTabPage);
            this.TabControl.Controls.Add(this.OtherTabPage);
            this.TabControl.Location = new System.Drawing.Point(3, 3);
            this.TabControl.Name = "TabControl";
            this.TabControl.SelectedIndex = 0;
            this.TabControl.Size = new System.Drawing.Size(469, 354);
            this.TabControl.TabIndex = 0;
            // 
            // GeneralTabPage
            // 
            this.GeneralTabPage.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.GeneralTabPage.Controls.Add(this.LanguageLabel);
            this.GeneralTabPage.Controls.Add(this.LanguageComboBox);
            this.GeneralTabPage.Controls.Add(this.AclAddrTextBox);
            this.GeneralTabPage.Controls.Add(this.AclLabel);
            this.GeneralTabPage.Controls.Add(this.DetectionIntervalLabel);
            this.GeneralTabPage.Controls.Add(this.DetectionIntervalTextBox);
            this.GeneralTabPage.Controls.Add(this.TcpingAtStartedCheckBox);
            this.GeneralTabPage.Controls.Add(this.STUNServerLabel);
            this.GeneralTabPage.Controls.Add(this.STUN_ServerComboBox);
            this.GeneralTabPage.Controls.Add(this.ProfileCountLabel);
            this.GeneralTabPage.Controls.Add(this.ProfileCountTextBox);
            this.GeneralTabPage.Controls.Add(this.PortGroupBox);
            this.GeneralTabPage.Controls.Add(this.ResolveServerHostnameCheckBox);
            this.GeneralTabPage.Controls.Add(this.BootShadowsocksFromDLLCheckBox);
            this.GeneralTabPage.Location = new System.Drawing.Point(4, 25);
            this.GeneralTabPage.Name = "GeneralTabPage";
            this.GeneralTabPage.Padding = new System.Windows.Forms.Padding(3);
            this.GeneralTabPage.Size = new System.Drawing.Size(461, 325);
            this.GeneralTabPage.TabIndex = 0;
            this.GeneralTabPage.Text = "General";
            // 
            // LanguageLabel
            // 
            this.LanguageLabel.AutoSize = true;
            this.LanguageLabel.Location = new System.Drawing.Point(12, 277);
            this.LanguageLabel.Name = "LanguageLabel";
            this.LanguageLabel.Size = new System.Drawing.Size(53, 12);
            this.LanguageLabel.TabIndex = 34;
            this.LanguageLabel.Text = "Language";
            // 
            // LanguageComboBox
            // 
            this.LanguageComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.LanguageComboBox.FormattingEnabled = true;
            this.LanguageComboBox.Location = new System.Drawing.Point(120, 274);
            this.LanguageComboBox.Name = "LanguageComboBox";
            this.LanguageComboBox.Size = new System.Drawing.Size(121, 20);
            this.LanguageComboBox.TabIndex = 33;
            // 
            // AclAddrTextBox
            // 
            this.AclAddrTextBox.Location = new System.Drawing.Point(120, 245);
            this.AclAddrTextBox.Name = "AclAddrTextBox";
            this.AclAddrTextBox.Size = new System.Drawing.Size(315, 21);
            this.AclAddrTextBox.TabIndex = 31;
            this.AclAddrTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // AclLabel
            // 
            this.AclLabel.AutoSize = true;
            this.AclLabel.Location = new System.Drawing.Point(12, 248);
            this.AclLabel.Name = "AclLabel";
            this.AclLabel.Size = new System.Drawing.Size(65, 12);
            this.AclLabel.TabIndex = 32;
            this.AclLabel.Text = "Custom ACL";
            // 
            // DetectionIntervalLabel
            // 
            this.DetectionIntervalLabel.AutoSize = true;
            this.DetectionIntervalLabel.Location = new System.Drawing.Point(228, 187);
            this.DetectionIntervalLabel.Name = "DetectionIntervalLabel";
            this.DetectionIntervalLabel.Size = new System.Drawing.Size(143, 12);
            this.DetectionIntervalLabel.TabIndex = 30;
            this.DetectionIntervalLabel.Text = "Detection interval(sec)";
            // 
            // DetectionIntervalTextBox
            // 
            this.DetectionIntervalTextBox.Location = new System.Drawing.Point(366, 184);
            this.DetectionIntervalTextBox.Name = "DetectionIntervalTextBox";
            this.DetectionIntervalTextBox.Size = new System.Drawing.Size(68, 21);
            this.DetectionIntervalTextBox.TabIndex = 29;
            this.DetectionIntervalTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // TcpingAtStartedCheckBox
            // 
            this.TcpingAtStartedCheckBox.AutoSize = true;
            this.TcpingAtStartedCheckBox.Location = new System.Drawing.Point(15, 186);
            this.TcpingAtStartedCheckBox.Name = "TcpingAtStartedCheckBox";
            this.TcpingAtStartedCheckBox.Size = new System.Drawing.Size(156, 16);
            this.TcpingAtStartedCheckBox.TabIndex = 28;
            this.TcpingAtStartedCheckBox.TabStop = false;
            this.TcpingAtStartedCheckBox.Text = "Delay test after start";
            this.TcpingAtStartedCheckBox.UseVisualStyleBackColor = true;
            // 
            // STUNServerLabel
            // 
            this.STUNServerLabel.AutoSize = true;
            this.STUNServerLabel.Location = new System.Drawing.Point(12, 216);
            this.STUNServerLabel.Name = "STUNServerLabel";
            this.STUNServerLabel.Size = new System.Drawing.Size(71, 12);
            this.STUNServerLabel.TabIndex = 26;
            this.STUNServerLabel.Text = "STUN Server";
            // 
            // STUN_ServerComboBox
            // 
            this.STUN_ServerComboBox.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.STUN_ServerComboBox.Location = new System.Drawing.Point(120, 213);
            this.STUN_ServerComboBox.Name = "STUN_ServerComboBox";
            this.STUN_ServerComboBox.Size = new System.Drawing.Size(314, 20);
            this.STUN_ServerComboBox.TabIndex = 27;
            // 
            // ProfileCountLabel
            // 
            this.ProfileCountLabel.AutoSize = true;
            this.ProfileCountLabel.Location = new System.Drawing.Point(12, 160);
            this.ProfileCountLabel.Name = "ProfileCountLabel";
            this.ProfileCountLabel.Size = new System.Drawing.Size(77, 12);
            this.ProfileCountLabel.TabIndex = 24;
            this.ProfileCountLabel.Text = "ProfileCount";
            // 
            // ProfileCountTextBox
            // 
            this.ProfileCountTextBox.Location = new System.Drawing.Point(120, 157);
            this.ProfileCountTextBox.Name = "ProfileCountTextBox";
            this.ProfileCountTextBox.Size = new System.Drawing.Size(90, 21);
            this.ProfileCountTextBox.TabIndex = 25;
            this.ProfileCountTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // PortGroupBox
            // 
            this.PortGroupBox.Controls.Add(this.RedirectorLabel);
            this.PortGroupBox.Controls.Add(this.RedirectorTextBox);
            this.PortGroupBox.Controls.Add(this.AllowDevicesCheckBox);
            this.PortGroupBox.Controls.Add(this.HTTPPortLabel);
            this.PortGroupBox.Controls.Add(this.HTTPPortTextBox);
            this.PortGroupBox.Controls.Add(this.Socks5PortLabel);
            this.PortGroupBox.Controls.Add(this.Socks5PortTextBox);
            this.PortGroupBox.Location = new System.Drawing.Point(8, 6);
            this.PortGroupBox.Name = "PortGroupBox";
            this.PortGroupBox.Size = new System.Drawing.Size(241, 140);
            this.PortGroupBox.TabIndex = 10;
            this.PortGroupBox.TabStop = false;
            this.PortGroupBox.Text = "Local Port";
            // 
            // RedirectorLabel
            // 
            this.RedirectorLabel.AutoSize = true;
            this.RedirectorLabel.Location = new System.Drawing.Point(9, 83);
            this.RedirectorLabel.Name = "RedirectorLabel";
            this.RedirectorLabel.Size = new System.Drawing.Size(89, 12);
            this.RedirectorLabel.TabIndex = 6;
            this.RedirectorLabel.Text = "Redirector TCP";
            // 
            // RedirectorTextBox
            // 
            this.RedirectorTextBox.Location = new System.Drawing.Point(120, 80);
            this.RedirectorTextBox.Name = "RedirectorTextBox";
            this.RedirectorTextBox.Size = new System.Drawing.Size(90, 21);
            this.RedirectorTextBox.TabIndex = 7;
            this.RedirectorTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // AllowDevicesCheckBox
            // 
            this.AllowDevicesCheckBox.AutoSize = true;
            this.AllowDevicesCheckBox.Location = new System.Drawing.Point(6, 107);
            this.AllowDevicesCheckBox.Name = "AllowDevicesCheckBox";
            this.AllowDevicesCheckBox.Size = new System.Drawing.Size(204, 16);
            this.AllowDevicesCheckBox.TabIndex = 5;
            this.AllowDevicesCheckBox.Text = "Allow other Devices to connect";
            this.AllowDevicesCheckBox.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.AllowDevicesCheckBox.UseVisualStyleBackColor = true;
            // 
            // HTTPPortLabel
            // 
            this.HTTPPortLabel.AutoSize = true;
            this.HTTPPortLabel.Location = new System.Drawing.Point(9, 54);
            this.HTTPPortLabel.Name = "HTTPPortLabel";
            this.HTTPPortLabel.Size = new System.Drawing.Size(29, 12);
            this.HTTPPortLabel.TabIndex = 3;
            this.HTTPPortLabel.Text = "HTTP";
            // 
            // HTTPPortTextBox
            // 
            this.HTTPPortTextBox.Location = new System.Drawing.Point(120, 51);
            this.HTTPPortTextBox.Name = "HTTPPortTextBox";
            this.HTTPPortTextBox.Size = new System.Drawing.Size(90, 21);
            this.HTTPPortTextBox.TabIndex = 4;
            this.HTTPPortTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // Socks5PortLabel
            // 
            this.Socks5PortLabel.AutoSize = true;
            this.Socks5PortLabel.Location = new System.Drawing.Point(9, 25);
            this.Socks5PortLabel.Name = "Socks5PortLabel";
            this.Socks5PortLabel.Size = new System.Drawing.Size(41, 12);
            this.Socks5PortLabel.TabIndex = 0;
            this.Socks5PortLabel.Text = "Socks5";
            // 
            // Socks5PortTextBox
            // 
            this.Socks5PortTextBox.Location = new System.Drawing.Point(120, 22);
            this.Socks5PortTextBox.Name = "Socks5PortTextBox";
            this.Socks5PortTextBox.Size = new System.Drawing.Size(90, 21);
            this.Socks5PortTextBox.TabIndex = 1;
            this.Socks5PortTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // ResolveServerHostnameCheckBox
            // 
            this.ResolveServerHostnameCheckBox.AutoSize = true;
            this.ResolveServerHostnameCheckBox.Location = new System.Drawing.Point(266, 42);
            this.ResolveServerHostnameCheckBox.Name = "ResolveServerHostnameCheckBox";
            this.ResolveServerHostnameCheckBox.Size = new System.Drawing.Size(162, 16);
            this.ResolveServerHostnameCheckBox.TabIndex = 21;
            this.ResolveServerHostnameCheckBox.Text = "Resolve Server Hostname";
            this.ResolveServerHostnameCheckBox.UseVisualStyleBackColor = true;
            // 
            // BootShadowsocksFromDLLCheckBox
            // 
            this.BootShadowsocksFromDLLCheckBox.AutoSize = true;
            this.BootShadowsocksFromDLLCheckBox.Location = new System.Drawing.Point(267, 15);
            this.BootShadowsocksFromDLLCheckBox.Name = "BootShadowsocksFromDLLCheckBox";
            this.BootShadowsocksFromDLLCheckBox.Size = new System.Drawing.Size(60, 16);
            this.BootShadowsocksFromDLLCheckBox.TabIndex = 21;
            this.BootShadowsocksFromDLLCheckBox.Text = "SS DLL";
            this.BootShadowsocksFromDLLCheckBox.UseVisualStyleBackColor = true;
            // 
            // NFTabPage
            // 
            this.NFTabPage.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.NFTabPage.Controls.Add(this.ModifySystemDNSCheckBox);
            this.NFTabPage.Location = new System.Drawing.Point(4, 25);
            this.NFTabPage.Name = "NFTabPage";
            this.NFTabPage.Padding = new System.Windows.Forms.Padding(3);
            this.NFTabPage.Size = new System.Drawing.Size(461, 325);
            this.NFTabPage.TabIndex = 1;
            this.NFTabPage.Text = "Process Mode";
            // 
            // ModifySystemDNSCheckBox
            // 
            this.ModifySystemDNSCheckBox.AutoSize = true;
            this.ModifySystemDNSCheckBox.Location = new System.Drawing.Point(8, 16);
            this.ModifySystemDNSCheckBox.Name = "ModifySystemDNSCheckBox";
            this.ModifySystemDNSCheckBox.Size = new System.Drawing.Size(126, 16);
            this.ModifySystemDNSCheckBox.TabIndex = 22;
            this.ModifySystemDNSCheckBox.Text = "Modify System DNS";
            this.ModifySystemDNSCheckBox.UseVisualStyleBackColor = true;
            // 
            // TAPTabPage
            // 
            this.TAPTabPage.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.TAPTabPage.Controls.Add(this.GlobalBypassIPsButton);
            this.TAPTabPage.Controls.Add(this.TUNTAPGroupBox);
            this.TAPTabPage.Location = new System.Drawing.Point(4, 25);
            this.TAPTabPage.Name = "TAPTabPage";
            this.TAPTabPage.Padding = new System.Windows.Forms.Padding(3);
            this.TAPTabPage.Size = new System.Drawing.Size(461, 325);
            this.TAPTabPage.TabIndex = 2;
            this.TAPTabPage.Text = "TUN/TAP";
            // 
            // GlobalBypassIPsButton
            // 
            this.GlobalBypassIPsButton.Location = new System.Drawing.Point(6, 199);
            this.GlobalBypassIPsButton.Name = "GlobalBypassIPsButton";
            this.GlobalBypassIPsButton.Size = new System.Drawing.Size(128, 23);
            this.GlobalBypassIPsButton.TabIndex = 11;
            this.GlobalBypassIPsButton.Text = "Global Bypass IPs";
            this.GlobalBypassIPsButton.UseVisualStyleBackColor = true;
            this.GlobalBypassIPsButton.Click += new System.EventHandler(this.GlobalBypassIPsButton_Click);
            // 
            // TUNTAPGroupBox
            // 
            this.TUNTAPGroupBox.Controls.Add(this.UseFakeDNSCheckBox);
            this.TUNTAPGroupBox.Controls.Add(this.ProxyDNSCheckBox);
            this.TUNTAPGroupBox.Controls.Add(this.ICSCheckBox);
            this.TUNTAPGroupBox.Controls.Add(this.UseCustomDNSCheckBox);
            this.TUNTAPGroupBox.Controls.Add(this.TUNTAPDNSLabel);
            this.TUNTAPGroupBox.Controls.Add(this.TUNTAPDNSTextBox);
            this.TUNTAPGroupBox.Controls.Add(this.TUNTAPGatewayLabel);
            this.TUNTAPGroupBox.Controls.Add(this.TUNTAPGatewayTextBox);
            this.TUNTAPGroupBox.Controls.Add(this.TUNTAPNetmaskLabel);
            this.TUNTAPGroupBox.Controls.Add(this.TUNTAPNetmaskTextBox);
            this.TUNTAPGroupBox.Controls.Add(this.TUNTAPAddressLabel);
            this.TUNTAPGroupBox.Controls.Add(this.TUNTAPAddressTextBox);
            this.TUNTAPGroupBox.Location = new System.Drawing.Point(6, 6);
            this.TUNTAPGroupBox.Name = "TUNTAPGroupBox";
            this.TUNTAPGroupBox.Size = new System.Drawing.Size(420, 187);
            this.TUNTAPGroupBox.TabIndex = 4;
            this.TUNTAPGroupBox.TabStop = false;
            this.TUNTAPGroupBox.Text = "TUN/TAP";
            // 
            // UseFakeDNSCheckBox
            // 
            this.UseFakeDNSCheckBox.AutoSize = true;
            this.UseFakeDNSCheckBox.Location = new System.Drawing.Point(10, 160);
            this.UseFakeDNSCheckBox.Name = "UseFakeDNSCheckBox";
            this.UseFakeDNSCheckBox.Size = new System.Drawing.Size(96, 16);
            this.UseFakeDNSCheckBox.TabIndex = 11;
            this.UseFakeDNSCheckBox.Text = "Use Fake DNS";
            this.UseFakeDNSCheckBox.UseVisualStyleBackColor = true;
            this.UseFakeDNSCheckBox.Visible = false;
            // 
            // ProxyDNSCheckBox
            // 
            this.ProxyDNSCheckBox.AutoSize = true;
            this.ProxyDNSCheckBox.Location = new System.Drawing.Point(261, 139);
            this.ProxyDNSCheckBox.Name = "ProxyDNSCheckBox";
            this.ProxyDNSCheckBox.Size = new System.Drawing.Size(138, 16);
            this.ProxyDNSCheckBox.TabIndex = 10;
            this.ProxyDNSCheckBox.Text = "Proxy DNS in Mode 2";
            this.ProxyDNSCheckBox.UseVisualStyleBackColor = true;
            // 
            // ICSCheckBox
            // 
            this.ICSCheckBox.AutoSize = true;
            this.ICSCheckBox.Enabled = false;
            this.ICSCheckBox.Location = new System.Drawing.Point(261, 160);
            this.ICSCheckBox.Name = "ICSCheckBox";
            this.ICSCheckBox.Size = new System.Drawing.Size(138, 16);
            this.ICSCheckBox.TabIndex = 5;
            this.ICSCheckBox.Text = "Tap Network Sharing";
            this.ICSCheckBox.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.ICSCheckBox.UseVisualStyleBackColor = true;
            this.ICSCheckBox.Click += new System.EventHandler(this.ICSCheckBox_CheckedChanged);
            // 
            // UseCustomDNSCheckBox
            // 
            this.UseCustomDNSCheckBox.AutoSize = true;
            this.UseCustomDNSCheckBox.Location = new System.Drawing.Point(10, 139);
            this.UseCustomDNSCheckBox.Name = "UseCustomDNSCheckBox";
            this.UseCustomDNSCheckBox.Size = new System.Drawing.Size(108, 16);
            this.UseCustomDNSCheckBox.TabIndex = 9;
            this.UseCustomDNSCheckBox.Text = "Use Custom DNS";
            this.UseCustomDNSCheckBox.UseVisualStyleBackColor = true;
            // 
            // TUNTAPDNSLabel
            // 
            this.TUNTAPDNSLabel.AutoSize = true;
            this.TUNTAPDNSLabel.Location = new System.Drawing.Point(9, 112);
            this.TUNTAPDNSLabel.Name = "TUNTAPDNSLabel";
            this.TUNTAPDNSLabel.Size = new System.Drawing.Size(23, 12);
            this.TUNTAPDNSLabel.TabIndex = 7;
            this.TUNTAPDNSLabel.Text = "DNS";
            // 
            // TUNTAPDNSTextBox
            // 
            this.TUNTAPDNSTextBox.Location = new System.Drawing.Point(120, 110);
            this.TUNTAPDNSTextBox.Name = "TUNTAPDNSTextBox";
            this.TUNTAPDNSTextBox.Size = new System.Drawing.Size(294, 21);
            this.TUNTAPDNSTextBox.TabIndex = 8;
            this.TUNTAPDNSTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // TUNTAPGatewayLabel
            // 
            this.TUNTAPGatewayLabel.AutoSize = true;
            this.TUNTAPGatewayLabel.Location = new System.Drawing.Point(9, 83);
            this.TUNTAPGatewayLabel.Name = "TUNTAPGatewayLabel";
            this.TUNTAPGatewayLabel.Size = new System.Drawing.Size(47, 12);
            this.TUNTAPGatewayLabel.TabIndex = 5;
            this.TUNTAPGatewayLabel.Text = "Gateway";
            // 
            // TUNTAPGatewayTextBox
            // 
            this.TUNTAPGatewayTextBox.Location = new System.Drawing.Point(120, 80);
            this.TUNTAPGatewayTextBox.Name = "TUNTAPGatewayTextBox";
            this.TUNTAPGatewayTextBox.Size = new System.Drawing.Size(294, 21);
            this.TUNTAPGatewayTextBox.TabIndex = 6;
            this.TUNTAPGatewayTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // TUNTAPNetmaskLabel
            // 
            this.TUNTAPNetmaskLabel.AutoSize = true;
            this.TUNTAPNetmaskLabel.Location = new System.Drawing.Point(9, 54);
            this.TUNTAPNetmaskLabel.Name = "TUNTAPNetmaskLabel";
            this.TUNTAPNetmaskLabel.Size = new System.Drawing.Size(47, 12);
            this.TUNTAPNetmaskLabel.TabIndex = 3;
            this.TUNTAPNetmaskLabel.Text = "Netmask";
            // 
            // TUNTAPNetmaskTextBox
            // 
            this.TUNTAPNetmaskTextBox.Location = new System.Drawing.Point(120, 51);
            this.TUNTAPNetmaskTextBox.Name = "TUNTAPNetmaskTextBox";
            this.TUNTAPNetmaskTextBox.Size = new System.Drawing.Size(294, 21);
            this.TUNTAPNetmaskTextBox.TabIndex = 4;
            this.TUNTAPNetmaskTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // TUNTAPAddressLabel
            // 
            this.TUNTAPAddressLabel.AutoSize = true;
            this.TUNTAPAddressLabel.Location = new System.Drawing.Point(9, 25);
            this.TUNTAPAddressLabel.Name = "TUNTAPAddressLabel";
            this.TUNTAPAddressLabel.Size = new System.Drawing.Size(47, 12);
            this.TUNTAPAddressLabel.TabIndex = 1;
            this.TUNTAPAddressLabel.Text = "Address";
            // 
            // TUNTAPAddressTextBox
            // 
            this.TUNTAPAddressTextBox.Location = new System.Drawing.Point(120, 22);
            this.TUNTAPAddressTextBox.Name = "TUNTAPAddressTextBox";
            this.TUNTAPAddressTextBox.Size = new System.Drawing.Size(294, 21);
            this.TUNTAPAddressTextBox.TabIndex = 2;
            this.TUNTAPAddressTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // v2rayTabPage
            // 
            this.v2rayTabPage.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.v2rayTabPage.Controls.Add(this.KCPGroupBox);
            this.v2rayTabPage.Controls.Add(this.UseMuxCheckBox);
            this.v2rayTabPage.Controls.Add(this.TLSAllowInsecureCheckBox);
            this.v2rayTabPage.Location = new System.Drawing.Point(4, 25);
            this.v2rayTabPage.Name = "v2rayTabPage";
            this.v2rayTabPage.Padding = new System.Windows.Forms.Padding(3);
            this.v2rayTabPage.Size = new System.Drawing.Size(461, 325);
            this.v2rayTabPage.TabIndex = 3;
            this.v2rayTabPage.Text = "V2Ray";
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
            this.KCPGroupBox.Location = new System.Drawing.Point(9, 48);
            this.KCPGroupBox.Name = "KCPGroupBox";
            this.KCPGroupBox.Size = new System.Drawing.Size(427, 204);
            this.KCPGroupBox.TabIndex = 1;
            this.KCPGroupBox.TabStop = false;
            this.KCPGroupBox.Text = "KCP";
            // 
            // mtuLabel
            // 
            this.mtuLabel.AutoSize = true;
            this.mtuLabel.Location = new System.Drawing.Point(6, 26);
            this.mtuLabel.Name = "mtuLabel";
            this.mtuLabel.Size = new System.Drawing.Size(23, 12);
            this.mtuLabel.TabIndex = 2;
            this.mtuLabel.Text = "mtu";
            // 
            // mtuTextBox
            // 
            this.mtuTextBox.Location = new System.Drawing.Point(103, 17);
            this.mtuTextBox.Name = "mtuTextBox";
            this.mtuTextBox.Size = new System.Drawing.Size(90, 21);
            this.mtuTextBox.TabIndex = 3;
            this.mtuTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // ttiLabel
            // 
            this.ttiLabel.AutoSize = true;
            this.ttiLabel.Location = new System.Drawing.Point(224, 26);
            this.ttiLabel.Name = "ttiLabel";
            this.ttiLabel.Size = new System.Drawing.Size(23, 12);
            this.ttiLabel.TabIndex = 4;
            this.ttiLabel.Text = "tti";
            // 
            // ttiTextBox
            // 
            this.ttiTextBox.Location = new System.Drawing.Point(331, 17);
            this.ttiTextBox.Name = "ttiTextBox";
            this.ttiTextBox.Size = new System.Drawing.Size(90, 21);
            this.ttiTextBox.TabIndex = 5;
            this.ttiTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // uplinkCapacityLabel
            // 
            this.uplinkCapacityLabel.AutoSize = true;
            this.uplinkCapacityLabel.Location = new System.Drawing.Point(6, 68);
            this.uplinkCapacityLabel.Name = "uplinkCapacityLabel";
            this.uplinkCapacityLabel.Size = new System.Drawing.Size(89, 12);
            this.uplinkCapacityLabel.TabIndex = 6;
            this.uplinkCapacityLabel.Text = "uplinkCapacity";
            // 
            // uplinkCapacityTextBox
            // 
            this.uplinkCapacityTextBox.Location = new System.Drawing.Point(103, 59);
            this.uplinkCapacityTextBox.Name = "uplinkCapacityTextBox";
            this.uplinkCapacityTextBox.Size = new System.Drawing.Size(90, 21);
            this.uplinkCapacityTextBox.TabIndex = 7;
            this.uplinkCapacityTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // downlinkCapacityLabel
            // 
            this.downlinkCapacityLabel.AutoSize = true;
            this.downlinkCapacityLabel.Location = new System.Drawing.Point(224, 68);
            this.downlinkCapacityLabel.Name = "downlinkCapacityLabel";
            this.downlinkCapacityLabel.Size = new System.Drawing.Size(101, 12);
            this.downlinkCapacityLabel.TabIndex = 8;
            this.downlinkCapacityLabel.Text = "downlinkCapacity";
            // 
            // downlinkCapacityTextBox
            // 
            this.downlinkCapacityTextBox.Location = new System.Drawing.Point(331, 65);
            this.downlinkCapacityTextBox.Name = "downlinkCapacityTextBox";
            this.downlinkCapacityTextBox.Size = new System.Drawing.Size(90, 21);
            this.downlinkCapacityTextBox.TabIndex = 9;
            this.downlinkCapacityTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // readBufferSizeLabel
            // 
            this.readBufferSizeLabel.AutoSize = true;
            this.readBufferSizeLabel.Location = new System.Drawing.Point(6, 109);
            this.readBufferSizeLabel.Name = "readBufferSizeLabel";
            this.readBufferSizeLabel.Size = new System.Drawing.Size(89, 12);
            this.readBufferSizeLabel.TabIndex = 10;
            this.readBufferSizeLabel.Text = "readBufferSize";
            // 
            // readBufferSizeTextBox
            // 
            this.readBufferSizeTextBox.Location = new System.Drawing.Point(103, 100);
            this.readBufferSizeTextBox.Name = "readBufferSizeTextBox";
            this.readBufferSizeTextBox.Size = new System.Drawing.Size(90, 21);
            this.readBufferSizeTextBox.TabIndex = 11;
            this.readBufferSizeTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // writeBufferSizeLabel
            // 
            this.writeBufferSizeLabel.AutoSize = true;
            this.writeBufferSizeLabel.Location = new System.Drawing.Point(224, 109);
            this.writeBufferSizeLabel.Name = "writeBufferSizeLabel";
            this.writeBufferSizeLabel.Size = new System.Drawing.Size(95, 12);
            this.writeBufferSizeLabel.TabIndex = 12;
            this.writeBufferSizeLabel.Text = "writeBufferSize";
            // 
            // writeBufferSizeTextBox
            // 
            this.writeBufferSizeTextBox.Location = new System.Drawing.Point(331, 106);
            this.writeBufferSizeTextBox.Name = "writeBufferSizeTextBox";
            this.writeBufferSizeTextBox.Size = new System.Drawing.Size(90, 21);
            this.writeBufferSizeTextBox.TabIndex = 13;
            this.writeBufferSizeTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // congestionCheckBox
            // 
            this.congestionCheckBox.AutoSize = true;
            this.congestionCheckBox.Location = new System.Drawing.Point(8, 139);
            this.congestionCheckBox.Name = "congestionCheckBox";
            this.congestionCheckBox.Size = new System.Drawing.Size(84, 16);
            this.congestionCheckBox.TabIndex = 0;
            this.congestionCheckBox.Text = "congestion";
            this.congestionCheckBox.UseVisualStyleBackColor = true;
            // 
            // TLSAllowInsecureCheckBox
            // 
            this.TLSAllowInsecureCheckBox.AutoSize = true;
            this.TLSAllowInsecureCheckBox.Location = new System.Drawing.Point(6, 15);
            this.TLSAllowInsecureCheckBox.Name = "TLSAllowInsecureCheckBox";
            this.TLSAllowInsecureCheckBox.Size = new System.Drawing.Size(126, 16);
            this.TLSAllowInsecureCheckBox.TabIndex = 0;
            this.TLSAllowInsecureCheckBox.Text = "TLS AllowInsecure";
            this.TLSAllowInsecureCheckBox.UseVisualStyleBackColor = true;
            // 
            // OtherTabPage
            // 
            this.OtherTabPage.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.OtherTabPage.Controls.Add(this.UpdateSubscribeatWhenOpenedCheckBox);
            this.OtherTabPage.Controls.Add(this.RunAtStartupCheckBox);
            this.OtherTabPage.Controls.Add(this.MinimizeWhenStartedCheckBox);
            this.OtherTabPage.Controls.Add(this.CheckBetaUpdateCheckBox);
            this.OtherTabPage.Controls.Add(this.CheckUpdateWhenOpenedCheckBox);
            this.OtherTabPage.Controls.Add(this.StartWhenOpenedCheckBox);
            this.OtherTabPage.Controls.Add(this.StopWhenExitedCheckBox);
            this.OtherTabPage.Controls.Add(this.ExitWhenClosedCheckBox);
            this.OtherTabPage.Location = new System.Drawing.Point(4, 25);
            this.OtherTabPage.Name = "OtherTabPage";
            this.OtherTabPage.Padding = new System.Windows.Forms.Padding(3);
            this.OtherTabPage.Size = new System.Drawing.Size(461, 325);
            this.OtherTabPage.TabIndex = 4;
            this.OtherTabPage.Text = "Others";
            // 
            // UpdateSubscribeatWhenOpenedCheckBox
            // 
            this.UpdateSubscribeatWhenOpenedCheckBox.AutoSize = true;
            this.UpdateSubscribeatWhenOpenedCheckBox.Location = new System.Drawing.Point(200, 114);
            this.UpdateSubscribeatWhenOpenedCheckBox.Name = "UpdateSubscribeatWhenOpenedCheckBox";
            this.UpdateSubscribeatWhenOpenedCheckBox.Size = new System.Drawing.Size(204, 16);
            this.UpdateSubscribeatWhenOpenedCheckBox.TabIndex = 32;
            this.UpdateSubscribeatWhenOpenedCheckBox.Text = "Update subscribeat when opened";
            this.UpdateSubscribeatWhenOpenedCheckBox.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.UpdateSubscribeatWhenOpenedCheckBox.UseVisualStyleBackColor = true;
            // 
            // RunAtStartupCheckBox
            // 
            this.RunAtStartupCheckBox.AutoSize = true;
            this.RunAtStartupCheckBox.Location = new System.Drawing.Point(6, 60);
            this.RunAtStartupCheckBox.Name = "RunAtStartupCheckBox";
            this.RunAtStartupCheckBox.Size = new System.Drawing.Size(108, 16);
            this.RunAtStartupCheckBox.TabIndex = 31;
            this.RunAtStartupCheckBox.Text = "Run at startup";
            this.RunAtStartupCheckBox.UseVisualStyleBackColor = true;
            // 
            // MinimizeWhenStartedCheckBox
            // 
            this.MinimizeWhenStartedCheckBox.AutoSize = true;
            this.MinimizeWhenStartedCheckBox.Location = new System.Drawing.Point(200, 33);
            this.MinimizeWhenStartedCheckBox.Name = "MinimizeWhenStartedCheckBox";
            this.MinimizeWhenStartedCheckBox.Size = new System.Drawing.Size(150, 16);
            this.MinimizeWhenStartedCheckBox.TabIndex = 30;
            this.MinimizeWhenStartedCheckBox.Text = "Minimize when started";
            this.MinimizeWhenStartedCheckBox.UseVisualStyleBackColor = true;
            // 
            // CheckBetaUpdateCheckBox
            // 
            this.CheckBetaUpdateCheckBox.AutoSize = true;
            this.CheckBetaUpdateCheckBox.Location = new System.Drawing.Point(200, 87);
            this.CheckBetaUpdateCheckBox.Name = "CheckBetaUpdateCheckBox";
            this.CheckBetaUpdateCheckBox.Size = new System.Drawing.Size(126, 16);
            this.CheckBetaUpdateCheckBox.TabIndex = 28;
            this.CheckBetaUpdateCheckBox.Text = "Check Beta update";
            this.CheckBetaUpdateCheckBox.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.CheckBetaUpdateCheckBox.UseVisualStyleBackColor = true;
            // 
            // CheckUpdateWhenOpenedCheckBox
            // 
            this.CheckUpdateWhenOpenedCheckBox.AutoSize = true;
            this.CheckUpdateWhenOpenedCheckBox.Location = new System.Drawing.Point(200, 60);
            this.CheckUpdateWhenOpenedCheckBox.Name = "CheckUpdateWhenOpenedCheckBox";
            this.CheckUpdateWhenOpenedCheckBox.Size = new System.Drawing.Size(168, 16);
            this.CheckUpdateWhenOpenedCheckBox.TabIndex = 29;
            this.CheckUpdateWhenOpenedCheckBox.Text = "Check update when opened";
            this.CheckUpdateWhenOpenedCheckBox.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.CheckUpdateWhenOpenedCheckBox.UseVisualStyleBackColor = true;
            // 
            // StartWhenOpenedCheckBox
            // 
            this.StartWhenOpenedCheckBox.AutoSize = true;
            this.StartWhenOpenedCheckBox.Location = new System.Drawing.Point(6, 33);
            this.StartWhenOpenedCheckBox.Name = "StartWhenOpenedCheckBox";
            this.StartWhenOpenedCheckBox.Size = new System.Drawing.Size(126, 16);
            this.StartWhenOpenedCheckBox.TabIndex = 27;
            this.StartWhenOpenedCheckBox.Text = "Start when opened";
            this.StartWhenOpenedCheckBox.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.StartWhenOpenedCheckBox.UseVisualStyleBackColor = true;
            // 
            // StopWhenExitedCheckBox
            // 
            this.StopWhenExitedCheckBox.AutoSize = true;
            this.StopWhenExitedCheckBox.Location = new System.Drawing.Point(200, 7);
            this.StopWhenExitedCheckBox.Name = "StopWhenExitedCheckBox";
            this.StopWhenExitedCheckBox.Size = new System.Drawing.Size(120, 16);
            this.StopWhenExitedCheckBox.TabIndex = 26;
            this.StopWhenExitedCheckBox.Text = "Stop when exited";
            this.StopWhenExitedCheckBox.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.StopWhenExitedCheckBox.UseVisualStyleBackColor = true;
            // 
            // ExitWhenClosedCheckBox
            // 
            this.ExitWhenClosedCheckBox.AutoSize = true;
            this.ExitWhenClosedCheckBox.Location = new System.Drawing.Point(6, 6);
            this.ExitWhenClosedCheckBox.Name = "ExitWhenClosedCheckBox";
            this.ExitWhenClosedCheckBox.Size = new System.Drawing.Size(120, 16);
            this.ExitWhenClosedCheckBox.TabIndex = 25;
            this.ExitWhenClosedCheckBox.Text = "Exit when closed";
            this.ExitWhenClosedCheckBox.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.ExitWhenClosedCheckBox.UseVisualStyleBackColor = true;
            // 
            // ControlButton
            // 
            this.ControlButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.ControlButton.Location = new System.Drawing.Point(397, 363);
            this.ControlButton.Name = "ControlButton";
            this.ControlButton.Size = new System.Drawing.Size(75, 23);
            this.ControlButton.TabIndex = 12;
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
            this.flowLayoutPanel1.TabIndex = 13;
            // 
            // UseMuxCheckBox
            // 
            this.UseMuxCheckBox.AutoSize = true;
            this.UseMuxCheckBox.Location = new System.Drawing.Point(138, 15);
            this.UseMuxCheckBox.Name = "UseMuxCheckBox";
            this.UseMuxCheckBox.Size = new System.Drawing.Size(66, 16);
            this.UseMuxCheckBox.TabIndex = 0;
            this.UseMuxCheckBox.Text = "Use Mux";
            this.UseMuxCheckBox.UseVisualStyleBackColor = true;
            // 
            // SettingForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ClientSize = new System.Drawing.Size(480, 400);
            this.Controls.Add(this.flowLayoutPanel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "SettingForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Settings";
            this.Load += new System.EventHandler(this.SettingForm2_Load);
            this.TabControl.ResumeLayout(false);
            this.GeneralTabPage.ResumeLayout(false);
            this.GeneralTabPage.PerformLayout();
            this.PortGroupBox.ResumeLayout(false);
            this.PortGroupBox.PerformLayout();
            this.NFTabPage.ResumeLayout(false);
            this.NFTabPage.PerformLayout();
            this.TAPTabPage.ResumeLayout(false);
            this.TUNTAPGroupBox.ResumeLayout(false);
            this.TUNTAPGroupBox.PerformLayout();
            this.v2rayTabPage.ResumeLayout(false);
            this.v2rayTabPage.PerformLayout();
            this.KCPGroupBox.ResumeLayout(false);
            this.KCPGroupBox.PerformLayout();
            this.OtherTabPage.ResumeLayout(false);
            this.OtherTabPage.PerformLayout();
            this.flowLayoutPanel1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TabControl TabControl;
        private System.Windows.Forms.TabPage GeneralTabPage;
        private System.Windows.Forms.TabPage NFTabPage;
        private System.Windows.Forms.TabPage TAPTabPage;
        private System.Windows.Forms.TabPage v2rayTabPage;
        private System.Windows.Forms.GroupBox PortGroupBox;
        private System.Windows.Forms.Label RedirectorLabel;
        private System.Windows.Forms.TextBox RedirectorTextBox;
        private System.Windows.Forms.CheckBox AllowDevicesCheckBox;
        private System.Windows.Forms.Label HTTPPortLabel;
        private System.Windows.Forms.TextBox HTTPPortTextBox;
        private System.Windows.Forms.Label Socks5PortLabel;
        private System.Windows.Forms.TextBox Socks5PortTextBox;
        private System.Windows.Forms.CheckBox ResolveServerHostnameCheckBox;
        private System.Windows.Forms.CheckBox BootShadowsocksFromDLLCheckBox;
        private System.Windows.Forms.GroupBox TUNTAPGroupBox;
        private System.Windows.Forms.CheckBox UseFakeDNSCheckBox;
        private System.Windows.Forms.CheckBox ProxyDNSCheckBox;
        private System.Windows.Forms.CheckBox ICSCheckBox;
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
        private System.Windows.Forms.CheckBox ModifySystemDNSCheckBox;
        private System.Windows.Forms.Button ControlButton;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.TabPage OtherTabPage;
        private System.Windows.Forms.CheckBox UpdateSubscribeatWhenOpenedCheckBox;
        private System.Windows.Forms.CheckBox RunAtStartupCheckBox;
        private System.Windows.Forms.CheckBox MinimizeWhenStartedCheckBox;
        private System.Windows.Forms.CheckBox CheckBetaUpdateCheckBox;
        private System.Windows.Forms.CheckBox CheckUpdateWhenOpenedCheckBox;
        private System.Windows.Forms.CheckBox StartWhenOpenedCheckBox;
        private System.Windows.Forms.CheckBox StopWhenExitedCheckBox;
        private System.Windows.Forms.CheckBox ExitWhenClosedCheckBox;
        private System.Windows.Forms.Label LanguageLabel;
        private System.Windows.Forms.ComboBox LanguageComboBox;
        private System.Windows.Forms.TextBox AclAddrTextBox;
        private System.Windows.Forms.Label AclLabel;
        private System.Windows.Forms.Label DetectionIntervalLabel;
        private System.Windows.Forms.TextBox DetectionIntervalTextBox;
        private System.Windows.Forms.CheckBox TcpingAtStartedCheckBox;
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
    }
}