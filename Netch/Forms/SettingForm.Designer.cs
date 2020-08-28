namespace Netch.Forms
{
    partial class SettingForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

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
            this.PortGroupBox = new System.Windows.Forms.GroupBox();
            this.RedirectorLabel = new System.Windows.Forms.Label();
            this.RedirectorTextBox = new System.Windows.Forms.TextBox();
            this.AllowDevicesCheckBox = new System.Windows.Forms.CheckBox();
            this.HTTPPortLabel = new System.Windows.Forms.Label();
            this.HTTPPortTextBox = new System.Windows.Forms.TextBox();
            this.Socks5PortLabel = new System.Windows.Forms.Label();
            this.Socks5PortTextBox = new System.Windows.Forms.TextBox();
            this.TUNTAPGroupBox = new System.Windows.Forms.GroupBox();
            this.UseFakeDNSCheckBox = new System.Windows.Forms.CheckBox();
            this.ProxyDNSCheckBox = new System.Windows.Forms.CheckBox();
            this.UseCustomDNSCheckBox = new System.Windows.Forms.CheckBox();
            this.TUNTAPDNSLabel = new System.Windows.Forms.Label();
            this.TUNTAPDNSTextBox = new System.Windows.Forms.TextBox();
            this.TUNTAPGatewayLabel = new System.Windows.Forms.Label();
            this.TUNTAPGatewayTextBox = new System.Windows.Forms.TextBox();
            this.TUNTAPNetmaskLabel = new System.Windows.Forms.Label();
            this.TUNTAPNetmaskTextBox = new System.Windows.Forms.TextBox();
            this.TUNTAPAddressLabel = new System.Windows.Forms.Label();
            this.TUNTAPAddressTextBox = new System.Windows.Forms.TextBox();
            this.ControlButton = new System.Windows.Forms.Button();
            this.GlobalBypassIPsButton = new System.Windows.Forms.Button();
            this.BehaviorGroupBox = new System.Windows.Forms.GroupBox();
            this.LanguageLabel = new System.Windows.Forms.Label();
            this.LanguageComboBox = new System.Windows.Forms.ComboBox();
            this.ModifySystemDNSCheckBox = new System.Windows.Forms.CheckBox();
            this.BootShadowsocksFromDLLCheckBox = new System.Windows.Forms.CheckBox();
            this.AclAddrTextBox = new System.Windows.Forms.TextBox();
            this.AclLabel = new System.Windows.Forms.Label();
            this.DetectionIntervalLabel = new System.Windows.Forms.Label();
            this.DetectionIntervalTextBox = new System.Windows.Forms.TextBox();
            this.TcpingAtStartedCheckBox = new System.Windows.Forms.CheckBox();
            this.STUNServerLabel = new System.Windows.Forms.Label();
            this.RunAtStartupCheckBox = new System.Windows.Forms.CheckBox();
            this.STUN_ServerComboBox = new System.Windows.Forms.ComboBox();
            this.MinimizeWhenStartedCheckBox = new System.Windows.Forms.CheckBox();
            this.ProfileCountLabel = new System.Windows.Forms.Label();
            this.ProfileCountTextBox = new System.Windows.Forms.TextBox();
            this.CheckBetaUpdateCheckBox = new System.Windows.Forms.CheckBox();
            this.CheckUpdateWhenOpenedCheckBox = new System.Windows.Forms.CheckBox();
            this.StartWhenOpenedCheckBox = new System.Windows.Forms.CheckBox();
            this.StopWhenExitedCheckBox = new System.Windows.Forms.CheckBox();
            this.ExitWhenClosedCheckBox = new System.Windows.Forms.CheckBox();
            this.PortGroupBox.SuspendLayout();
            this.TUNTAPGroupBox.SuspendLayout();
            this.BehaviorGroupBox.SuspendLayout();
            this.SuspendLayout();
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
            this.PortGroupBox.Location = new System.Drawing.Point(12, 12);
            this.PortGroupBox.Name = "PortGroupBox";
            this.PortGroupBox.Size = new System.Drawing.Size(420, 140);
            this.PortGroupBox.TabIndex = 0;
            this.PortGroupBox.TabStop = false;
            this.PortGroupBox.Text = "Local Port";
            // 
            // RedirectorLabel
            // 
            this.RedirectorLabel.AutoSize = true;
            this.RedirectorLabel.Location = new System.Drawing.Point(9, 83);
            this.RedirectorLabel.Name = "RedirectorLabel";
            this.RedirectorLabel.Size = new System.Drawing.Size(95, 17);
            this.RedirectorLabel.TabIndex = 6;
            this.RedirectorLabel.Text = "Redirector TCP";
            // 
            // RedirectorTextBox
            // 
            this.RedirectorTextBox.Location = new System.Drawing.Point(120, 80);
            this.RedirectorTextBox.Name = "RedirectorTextBox";
            this.RedirectorTextBox.Size = new System.Drawing.Size(294, 23);
            this.RedirectorTextBox.TabIndex = 7;
            this.RedirectorTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // AllowDevicesCheckBox
            // 
            this.AllowDevicesCheckBox.AutoSize = true;
            this.AllowDevicesCheckBox.Location = new System.Drawing.Point(120, 109);
            this.AllowDevicesCheckBox.Name = "AllowDevicesCheckBox";
            this.AllowDevicesCheckBox.Size = new System.Drawing.Size(206, 21);
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
            this.HTTPPortLabel.Size = new System.Drawing.Size(38, 17);
            this.HTTPPortLabel.TabIndex = 3;
            this.HTTPPortLabel.Text = "HTTP";
            // 
            // HTTPPortTextBox
            // 
            this.HTTPPortTextBox.Location = new System.Drawing.Point(120, 51);
            this.HTTPPortTextBox.Name = "HTTPPortTextBox";
            this.HTTPPortTextBox.Size = new System.Drawing.Size(294, 23);
            this.HTTPPortTextBox.TabIndex = 4;
            this.HTTPPortTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
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
            this.Socks5PortTextBox.Size = new System.Drawing.Size(294, 23);
            this.Socks5PortTextBox.TabIndex = 1;
            this.Socks5PortTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // TUNTAPGroupBox
            // 
            this.TUNTAPGroupBox.Controls.Add(this.UseFakeDNSCheckBox);
            this.TUNTAPGroupBox.Controls.Add(this.ProxyDNSCheckBox);
            this.TUNTAPGroupBox.Controls.Add(this.UseCustomDNSCheckBox);
            this.TUNTAPGroupBox.Controls.Add(this.TUNTAPDNSLabel);
            this.TUNTAPGroupBox.Controls.Add(this.TUNTAPDNSTextBox);
            this.TUNTAPGroupBox.Controls.Add(this.TUNTAPGatewayLabel);
            this.TUNTAPGroupBox.Controls.Add(this.TUNTAPGatewayTextBox);
            this.TUNTAPGroupBox.Controls.Add(this.TUNTAPNetmaskLabel);
            this.TUNTAPGroupBox.Controls.Add(this.TUNTAPNetmaskTextBox);
            this.TUNTAPGroupBox.Controls.Add(this.TUNTAPAddressLabel);
            this.TUNTAPGroupBox.Controls.Add(this.TUNTAPAddressTextBox);
            this.TUNTAPGroupBox.Location = new System.Drawing.Point(12, 158);
            this.TUNTAPGroupBox.Name = "TUNTAPGroupBox";
            this.TUNTAPGroupBox.Size = new System.Drawing.Size(420, 187);
            this.TUNTAPGroupBox.TabIndex = 3;
            this.TUNTAPGroupBox.TabStop = false;
            this.TUNTAPGroupBox.Text = "TUN/TAP";
            // 
            // UseFakeDNSCheckBox
            // 
            this.UseFakeDNSCheckBox.AutoSize = true;
            this.UseFakeDNSCheckBox.Location = new System.Drawing.Point(10, 160);
            this.UseFakeDNSCheckBox.Name = "UseFakeDNSCheckBox";
            this.UseFakeDNSCheckBox.Size = new System.Drawing.Size(110, 21);
            this.UseFakeDNSCheckBox.TabIndex = 11;
            this.UseFakeDNSCheckBox.Text = "Use Fake DNS";
            this.UseFakeDNSCheckBox.UseVisualStyleBackColor = true;
            // 
            // ProxyDNSCheckBox
            // 
            this.ProxyDNSCheckBox.AutoSize = true;
            this.ProxyDNSCheckBox.Location = new System.Drawing.Point(261, 139);
            this.ProxyDNSCheckBox.Name = "ProxyDNSCheckBox";
            this.ProxyDNSCheckBox.Size = new System.Drawing.Size(153, 21);
            this.ProxyDNSCheckBox.TabIndex = 10;
            this.ProxyDNSCheckBox.Text = "Proxy DNS in Mode 2";
            this.ProxyDNSCheckBox.UseVisualStyleBackColor = true;
            // 
            // UseCustomDNSCheckBox
            // 
            this.UseCustomDNSCheckBox.AutoSize = true;
            this.UseCustomDNSCheckBox.Location = new System.Drawing.Point(10, 139);
            this.UseCustomDNSCheckBox.Name = "UseCustomDNSCheckBox";
            this.UseCustomDNSCheckBox.Size = new System.Drawing.Size(127, 21);
            this.UseCustomDNSCheckBox.TabIndex = 9;
            this.UseCustomDNSCheckBox.Text = "Use Custom DNS";
            this.UseCustomDNSCheckBox.UseVisualStyleBackColor = true;
            this.UseCustomDNSCheckBox.CheckedChanged += new System.EventHandler(this.TUNTAPUseCustomDNSCheckBox_CheckedChanged);
            // 
            // TUNTAPDNSLabel
            // 
            this.TUNTAPDNSLabel.AutoSize = true;
            this.TUNTAPDNSLabel.Location = new System.Drawing.Point(9, 112);
            this.TUNTAPDNSLabel.Name = "TUNTAPDNSLabel";
            this.TUNTAPDNSLabel.Size = new System.Drawing.Size(34, 17);
            this.TUNTAPDNSLabel.TabIndex = 7;
            this.TUNTAPDNSLabel.Text = "DNS";
            // 
            // TUNTAPDNSTextBox
            // 
            this.TUNTAPDNSTextBox.Location = new System.Drawing.Point(120, 110);
            this.TUNTAPDNSTextBox.Name = "TUNTAPDNSTextBox";
            this.TUNTAPDNSTextBox.Size = new System.Drawing.Size(294, 23);
            this.TUNTAPDNSTextBox.TabIndex = 8;
            this.TUNTAPDNSTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // TUNTAPGatewayLabel
            // 
            this.TUNTAPGatewayLabel.AutoSize = true;
            this.TUNTAPGatewayLabel.Location = new System.Drawing.Point(9, 83);
            this.TUNTAPGatewayLabel.Name = "TUNTAPGatewayLabel";
            this.TUNTAPGatewayLabel.Size = new System.Drawing.Size(57, 17);
            this.TUNTAPGatewayLabel.TabIndex = 5;
            this.TUNTAPGatewayLabel.Text = "Gateway";
            // 
            // TUNTAPGatewayTextBox
            // 
            this.TUNTAPGatewayTextBox.Location = new System.Drawing.Point(120, 80);
            this.TUNTAPGatewayTextBox.Name = "TUNTAPGatewayTextBox";
            this.TUNTAPGatewayTextBox.Size = new System.Drawing.Size(294, 23);
            this.TUNTAPGatewayTextBox.TabIndex = 6;
            this.TUNTAPGatewayTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // TUNTAPNetmaskLabel
            // 
            this.TUNTAPNetmaskLabel.AutoSize = true;
            this.TUNTAPNetmaskLabel.Location = new System.Drawing.Point(9, 54);
            this.TUNTAPNetmaskLabel.Name = "TUNTAPNetmaskLabel";
            this.TUNTAPNetmaskLabel.Size = new System.Drawing.Size(60, 17);
            this.TUNTAPNetmaskLabel.TabIndex = 3;
            this.TUNTAPNetmaskLabel.Text = "Netmask";
            // 
            // TUNTAPNetmaskTextBox
            // 
            this.TUNTAPNetmaskTextBox.Location = new System.Drawing.Point(120, 51);
            this.TUNTAPNetmaskTextBox.Name = "TUNTAPNetmaskTextBox";
            this.TUNTAPNetmaskTextBox.Size = new System.Drawing.Size(294, 23);
            this.TUNTAPNetmaskTextBox.TabIndex = 4;
            this.TUNTAPNetmaskTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // TUNTAPAddressLabel
            // 
            this.TUNTAPAddressLabel.AutoSize = true;
            this.TUNTAPAddressLabel.Location = new System.Drawing.Point(9, 25);
            this.TUNTAPAddressLabel.Name = "TUNTAPAddressLabel";
            this.TUNTAPAddressLabel.Size = new System.Drawing.Size(56, 17);
            this.TUNTAPAddressLabel.TabIndex = 1;
            this.TUNTAPAddressLabel.Text = "Address";
            // 
            // TUNTAPAddressTextBox
            // 
            this.TUNTAPAddressTextBox.Location = new System.Drawing.Point(120, 22);
            this.TUNTAPAddressTextBox.Name = "TUNTAPAddressTextBox";
            this.TUNTAPAddressTextBox.Size = new System.Drawing.Size(294, 23);
            this.TUNTAPAddressTextBox.TabIndex = 2;
            this.TUNTAPAddressTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // ControlButton
            // 
            this.ControlButton.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.ControlButton.Location = new System.Drawing.Point(804, 356);
            this.ControlButton.Name = "ControlButton";
            this.ControlButton.Size = new System.Drawing.Size(75, 23);
            this.ControlButton.TabIndex = 11;
            this.ControlButton.Text = "Save";
            this.ControlButton.UseVisualStyleBackColor = true;
            this.ControlButton.Click += new System.EventHandler(this.ControlButton_Click);
            // 
            // GlobalBypassIPsButton
            // 
            this.GlobalBypassIPsButton.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.GlobalBypassIPsButton.Location = new System.Drawing.Point(12, 356);
            this.GlobalBypassIPsButton.Name = "GlobalBypassIPsButton";
            this.GlobalBypassIPsButton.Size = new System.Drawing.Size(128, 23);
            this.GlobalBypassIPsButton.TabIndex = 10;
            this.GlobalBypassIPsButton.Text = "Global Bypass IPs";
            this.GlobalBypassIPsButton.UseVisualStyleBackColor = true;
            this.GlobalBypassIPsButton.Click += new System.EventHandler(this.GlobalBypassIPsButton_Click);
            // 
            // BehaviorGroupBox
            // 
            this.BehaviorGroupBox.Controls.Add(this.LanguageLabel);
            this.BehaviorGroupBox.Controls.Add(this.LanguageComboBox);
            this.BehaviorGroupBox.Controls.Add(this.ModifySystemDNSCheckBox);
            this.BehaviorGroupBox.Controls.Add(this.BootShadowsocksFromDLLCheckBox);
            this.BehaviorGroupBox.Controls.Add(this.AclAddrTextBox);
            this.BehaviorGroupBox.Controls.Add(this.AclLabel);
            this.BehaviorGroupBox.Controls.Add(this.DetectionIntervalLabel);
            this.BehaviorGroupBox.Controls.Add(this.DetectionIntervalTextBox);
            this.BehaviorGroupBox.Controls.Add(this.TcpingAtStartedCheckBox);
            this.BehaviorGroupBox.Controls.Add(this.STUNServerLabel);
            this.BehaviorGroupBox.Controls.Add(this.RunAtStartupCheckBox);
            this.BehaviorGroupBox.Controls.Add(this.STUN_ServerComboBox);
            this.BehaviorGroupBox.Controls.Add(this.MinimizeWhenStartedCheckBox);
            this.BehaviorGroupBox.Controls.Add(this.ProfileCountLabel);
            this.BehaviorGroupBox.Controls.Add(this.ProfileCountTextBox);
            this.BehaviorGroupBox.Controls.Add(this.CheckBetaUpdateCheckBox);
            this.BehaviorGroupBox.Controls.Add(this.CheckUpdateWhenOpenedCheckBox);
            this.BehaviorGroupBox.Controls.Add(this.StartWhenOpenedCheckBox);
            this.BehaviorGroupBox.Controls.Add(this.StopWhenExitedCheckBox);
            this.BehaviorGroupBox.Controls.Add(this.ExitWhenClosedCheckBox);
            this.BehaviorGroupBox.Location = new System.Drawing.Point(438, 12);
            this.BehaviorGroupBox.Name = "BehaviorGroupBox";
            this.BehaviorGroupBox.Size = new System.Drawing.Size(441, 333);
            this.BehaviorGroupBox.TabIndex = 8;
            this.BehaviorGroupBox.TabStop = false;
            this.BehaviorGroupBox.Text = "Behavior";
            // 
            // LanguageLabel
            // 
            this.LanguageLabel.AutoSize = true;
            this.LanguageLabel.Location = new System.Drawing.Point(12, 305);
            this.LanguageLabel.Name = "LanguageLabel";
            this.LanguageLabel.Size = new System.Drawing.Size(65, 17);
            this.LanguageLabel.TabIndex = 23;
            this.LanguageLabel.Text = "Language";
            // 
            // LanguageComboBox
            // 
            this.LanguageComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.LanguageComboBox.FormattingEnabled = true;
            this.LanguageComboBox.Location = new System.Drawing.Point(120, 302);
            this.LanguageComboBox.Name = "LanguageComboBox";
            this.LanguageComboBox.Size = new System.Drawing.Size(121, 25);
            this.LanguageComboBox.TabIndex = 22;
            // 
            // ModifySystemDNSCheckBox
            // 
            this.ModifySystemDNSCheckBox.AutoSize = true;
            this.ModifySystemDNSCheckBox.Location = new System.Drawing.Point(12, 129);
            this.ModifySystemDNSCheckBox.Name = "ModifySystemDNSCheckBox";
            this.ModifySystemDNSCheckBox.Size = new System.Drawing.Size(143, 21);
            this.ModifySystemDNSCheckBox.TabIndex = 21;
            this.ModifySystemDNSCheckBox.Text = "Modify System DNS";
            this.ModifySystemDNSCheckBox.UseVisualStyleBackColor = true;
            // 
            // BootShadowsocksFromDLLCheckBox
            // 
            this.BootShadowsocksFromDLLCheckBox.AutoSize = true;
            this.BootShadowsocksFromDLLCheckBox.Location = new System.Drawing.Point(12, 102);
            this.BootShadowsocksFromDLLCheckBox.Name = "BootShadowsocksFromDLLCheckBox";
            this.BootShadowsocksFromDLLCheckBox.Size = new System.Drawing.Size(168, 21);
            this.BootShadowsocksFromDLLCheckBox.TabIndex = 21;
            this.BootShadowsocksFromDLLCheckBox.Text = "SS DLL(No ACL support)";
            this.BootShadowsocksFromDLLCheckBox.UseVisualStyleBackColor = true;
            // 
            // AclAddrTextBox
            // 
            this.AclAddrTextBox.Location = new System.Drawing.Point(120, 273);
            this.AclAddrTextBox.Name = "AclAddrTextBox";
            this.AclAddrTextBox.Size = new System.Drawing.Size(315, 23);
            this.AclAddrTextBox.TabIndex = 19;
            this.AclAddrTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // AclLabel
            // 
            this.AclLabel.AutoSize = true;
            this.AclLabel.Location = new System.Drawing.Point(12, 276);
            this.AclLabel.Name = "AclLabel";
            this.AclLabel.Size = new System.Drawing.Size(78, 17);
            this.AclLabel.TabIndex = 20;
            this.AclLabel.Text = "Custom ACL";
            // 
            // DetectionIntervalLabel
            // 
            this.DetectionIntervalLabel.AutoSize = true;
            this.DetectionIntervalLabel.Location = new System.Drawing.Point(228, 215);
            this.DetectionIntervalLabel.Name = "DetectionIntervalLabel";
            this.DetectionIntervalLabel.Size = new System.Drawing.Size(136, 17);
            this.DetectionIntervalLabel.TabIndex = 18;
            this.DetectionIntervalLabel.Text = "Detection interval(sec)";
            // 
            // DetectionIntervalTextBox
            // 
            this.DetectionIntervalTextBox.Location = new System.Drawing.Point(366, 212);
            this.DetectionIntervalTextBox.Name = "DetectionIntervalTextBox";
            this.DetectionIntervalTextBox.Size = new System.Drawing.Size(68, 23);
            this.DetectionIntervalTextBox.TabIndex = 17;
            this.DetectionIntervalTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // TcpingAtStartedCheckBox
            // 
            this.TcpingAtStartedCheckBox.AutoSize = true;
            this.TcpingAtStartedCheckBox.Location = new System.Drawing.Point(15, 214);
            this.TcpingAtStartedCheckBox.Name = "TcpingAtStartedCheckBox";
            this.TcpingAtStartedCheckBox.Size = new System.Drawing.Size(145, 21);
            this.TcpingAtStartedCheckBox.TabIndex = 15;
            this.TcpingAtStartedCheckBox.TabStop = false;
            this.TcpingAtStartedCheckBox.Text = "Delay test after start";
            this.TcpingAtStartedCheckBox.UseVisualStyleBackColor = true;
            // 
            // STUNServerLabel
            // 
            this.STUNServerLabel.AutoSize = true;
            this.STUNServerLabel.Location = new System.Drawing.Point(12, 244);
            this.STUNServerLabel.Name = "STUNServerLabel";
            this.STUNServerLabel.Size = new System.Drawing.Size(82, 17);
            this.STUNServerLabel.TabIndex = 10;
            this.STUNServerLabel.Text = "STUN Server";
            // 
            // RunAtStartupCheckBox
            // 
            this.RunAtStartupCheckBox.AutoSize = true;
            this.RunAtStartupCheckBox.Location = new System.Drawing.Point(12, 75);
            this.RunAtStartupCheckBox.Name = "RunAtStartupCheckBox";
            this.RunAtStartupCheckBox.Size = new System.Drawing.Size(109, 21);
            this.RunAtStartupCheckBox.TabIndex = 11;
            this.RunAtStartupCheckBox.Text = "Run at startup";
            this.RunAtStartupCheckBox.UseVisualStyleBackColor = true;
            // 
            // STUN_ServerComboBox
            // 
            this.STUN_ServerComboBox.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.STUN_ServerComboBox.Location = new System.Drawing.Point(120, 241);
            this.STUN_ServerComboBox.Name = "STUN_ServerComboBox";
            this.STUN_ServerComboBox.Size = new System.Drawing.Size(314, 25);
            this.STUN_ServerComboBox.TabIndex = 11;
            // 
            // MinimizeWhenStartedCheckBox
            // 
            this.MinimizeWhenStartedCheckBox.AutoSize = true;
            this.MinimizeWhenStartedCheckBox.Location = new System.Drawing.Point(206, 48);
            this.MinimizeWhenStartedCheckBox.Name = "MinimizeWhenStartedCheckBox";
            this.MinimizeWhenStartedCheckBox.Size = new System.Drawing.Size(158, 21);
            this.MinimizeWhenStartedCheckBox.TabIndex = 10;
            this.MinimizeWhenStartedCheckBox.Text = "Minimize when started";
            this.MinimizeWhenStartedCheckBox.UseVisualStyleBackColor = true;
            // 
            // ProfileCountLabel
            // 
            this.ProfileCountLabel.AutoSize = true;
            this.ProfileCountLabel.Location = new System.Drawing.Point(12, 188);
            this.ProfileCountLabel.Name = "ProfileCountLabel";
            this.ProfileCountLabel.Size = new System.Drawing.Size(79, 17);
            this.ProfileCountLabel.TabIndex = 8;
            this.ProfileCountLabel.Text = "ProfileCount";
            // 
            // ProfileCountTextBox
            // 
            this.ProfileCountTextBox.Location = new System.Drawing.Point(120, 185);
            this.ProfileCountTextBox.Name = "ProfileCountTextBox";
            this.ProfileCountTextBox.Size = new System.Drawing.Size(90, 23);
            this.ProfileCountTextBox.TabIndex = 9;
            this.ProfileCountTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // CheckBetaUpdateCheckBox
            // 
            this.CheckBetaUpdateCheckBox.AutoSize = true;
            this.CheckBetaUpdateCheckBox.Location = new System.Drawing.Point(206, 102);
            this.CheckBetaUpdateCheckBox.Name = "CheckBetaUpdateCheckBox";
            this.CheckBetaUpdateCheckBox.Size = new System.Drawing.Size(137, 21);
            this.CheckBetaUpdateCheckBox.TabIndex = 8;
            this.CheckBetaUpdateCheckBox.Text = "Check Beta update";
            this.CheckBetaUpdateCheckBox.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.CheckBetaUpdateCheckBox.UseVisualStyleBackColor = true;
            // 
            // CheckUpdateWhenOpenedCheckBox
            // 
            this.CheckUpdateWhenOpenedCheckBox.AutoSize = true;
            this.CheckUpdateWhenOpenedCheckBox.Location = new System.Drawing.Point(206, 75);
            this.CheckUpdateWhenOpenedCheckBox.Name = "CheckUpdateWhenOpenedCheckBox";
            this.CheckUpdateWhenOpenedCheckBox.Size = new System.Drawing.Size(190, 21);
            this.CheckUpdateWhenOpenedCheckBox.TabIndex = 8;
            this.CheckUpdateWhenOpenedCheckBox.Text = "Check update when opened";
            this.CheckUpdateWhenOpenedCheckBox.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.CheckUpdateWhenOpenedCheckBox.UseVisualStyleBackColor = true;
            // 
            // StartWhenOpenedCheckBox
            // 
            this.StartWhenOpenedCheckBox.AutoSize = true;
            this.StartWhenOpenedCheckBox.Location = new System.Drawing.Point(12, 48);
            this.StartWhenOpenedCheckBox.Name = "StartWhenOpenedCheckBox";
            this.StartWhenOpenedCheckBox.Size = new System.Drawing.Size(137, 21);
            this.StartWhenOpenedCheckBox.TabIndex = 7;
            this.StartWhenOpenedCheckBox.Text = "Start when opened";
            this.StartWhenOpenedCheckBox.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.StartWhenOpenedCheckBox.UseVisualStyleBackColor = true;
            // 
            // StopWhenExitedCheckBox
            // 
            this.StopWhenExitedCheckBox.AutoSize = true;
            this.StopWhenExitedCheckBox.Location = new System.Drawing.Point(206, 22);
            this.StopWhenExitedCheckBox.Name = "StopWhenExitedCheckBox";
            this.StopWhenExitedCheckBox.Size = new System.Drawing.Size(127, 21);
            this.StopWhenExitedCheckBox.TabIndex = 6;
            this.StopWhenExitedCheckBox.Text = "Stop when exited";
            this.StopWhenExitedCheckBox.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.StopWhenExitedCheckBox.UseVisualStyleBackColor = true;
            // 
            // ExitWhenClosedCheckBox
            // 
            this.ExitWhenClosedCheckBox.AutoSize = true;
            this.ExitWhenClosedCheckBox.Location = new System.Drawing.Point(12, 21);
            this.ExitWhenClosedCheckBox.Name = "ExitWhenClosedCheckBox";
            this.ExitWhenClosedCheckBox.Size = new System.Drawing.Size(123, 21);
            this.ExitWhenClosedCheckBox.TabIndex = 5;
            this.ExitWhenClosedCheckBox.Text = "Exit when closed";
            this.ExitWhenClosedCheckBox.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.ExitWhenClosedCheckBox.UseVisualStyleBackColor = true;
            // 
            // SettingForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(891, 390);
            this.Controls.Add(this.BehaviorGroupBox);
            this.Controls.Add(this.PortGroupBox);
            this.Controls.Add(this.GlobalBypassIPsButton);
            this.Controls.Add(this.ControlButton);
            this.Controls.Add(this.TUNTAPGroupBox);
            this.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte) (134)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon) (resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.MaximizeBox = false;
            this.Name = "SettingForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Settings";
            this.Load += new System.EventHandler(this.SettingForm_Load);
            this.PortGroupBox.ResumeLayout(false);
            this.PortGroupBox.PerformLayout();
            this.TUNTAPGroupBox.ResumeLayout(false);
            this.TUNTAPGroupBox.PerformLayout();
            this.BehaviorGroupBox.ResumeLayout(false);
            this.BehaviorGroupBox.PerformLayout();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.GroupBox PortGroupBox;
        private System.Windows.Forms.Label HTTPPortLabel;
        private System.Windows.Forms.TextBox HTTPPortTextBox;
        private System.Windows.Forms.Label Socks5PortLabel;
        private System.Windows.Forms.TextBox Socks5PortTextBox;
        private System.Windows.Forms.GroupBox TUNTAPGroupBox;
        private System.Windows.Forms.TextBox TUNTAPAddressTextBox;
        private System.Windows.Forms.Label TUNTAPAddressLabel;
        private System.Windows.Forms.TextBox TUNTAPNetmaskTextBox;
        private System.Windows.Forms.Label TUNTAPNetmaskLabel;
        private System.Windows.Forms.TextBox TUNTAPGatewayTextBox;
        private System.Windows.Forms.Label TUNTAPGatewayLabel;
        private System.Windows.Forms.Label TUNTAPDNSLabel;
        private System.Windows.Forms.TextBox TUNTAPDNSTextBox;
        private System.Windows.Forms.Button ControlButton;
        private System.Windows.Forms.Button GlobalBypassIPsButton;
        private System.Windows.Forms.CheckBox UseCustomDNSCheckBox;
        private System.Windows.Forms.CheckBox AllowDevicesCheckBox;
        private System.Windows.Forms.GroupBox BehaviorGroupBox;
        private System.Windows.Forms.CheckBox ExitWhenClosedCheckBox;
        private System.Windows.Forms.CheckBox StopWhenExitedCheckBox;
        private System.Windows.Forms.CheckBox StartWhenOpenedCheckBox;
        private System.Windows.Forms.CheckBox CheckUpdateWhenOpenedCheckBox;
        private System.Windows.Forms.Label ProfileCountLabel;
        private System.Windows.Forms.TextBox ProfileCountTextBox;
        private System.Windows.Forms.CheckBox MinimizeWhenStartedCheckBox;
        private System.Windows.Forms.CheckBox RunAtStartupCheckBox;
        private System.Windows.Forms.Label STUNServerLabel;
        private System.Windows.Forms.ComboBox STUN_ServerComboBox;
        private System.Windows.Forms.CheckBox ProxyDNSCheckBox;
        private System.Windows.Forms.TextBox DetectionIntervalTextBox;
        private System.Windows.Forms.CheckBox TcpingAtStartedCheckBox;
        private System.Windows.Forms.Label DetectionIntervalLabel;
        private System.Windows.Forms.Label RedirectorLabel;
        private System.Windows.Forms.TextBox RedirectorTextBox;
        private System.Windows.Forms.TextBox AclAddrTextBox;
        private System.Windows.Forms.Label AclLabel;
        private System.Windows.Forms.CheckBox UseFakeDNSCheckBox;
        private System.Windows.Forms.CheckBox BootShadowsocksFromDLLCheckBox;
        private System.Windows.Forms.Label LanguageLabel;
        private System.Windows.Forms.ComboBox LanguageComboBox;
        private System.Windows.Forms.CheckBox ModifySystemDNSCheckBox;
        private System.Windows.Forms.CheckBox CheckBetaUpdateCheckBox;
    }
}