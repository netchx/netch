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
            this.TUNTAPProxyDNSCheckBox = new System.Windows.Forms.CheckBox();
            this.TUNTAPUseCustomDNSCheckBox = new System.Windows.Forms.CheckBox();
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
            this.BootShadowsocksFromDLLCheckBox = new System.Windows.Forms.CheckBox();
            this.AclAddr = new System.Windows.Forms.TextBox();
            this.AclLabel = new System.Windows.Forms.Label();
            this.DetectionInterval_Label = new System.Windows.Forms.Label();
            this.DetectionInterval_TextBox = new System.Windows.Forms.TextBox();
            this.EnableStartedTcping_CheckBox = new System.Windows.Forms.CheckBox();
            this.DelayTestAfterStartup_Label = new System.Windows.Forms.Label();
            this.BypassModeCheckBox = new System.Windows.Forms.CheckBox();
            this.Redirector2checkBox = new System.Windows.Forms.CheckBox();
            this.ExperimentalFunction_Label = new System.Windows.Forms.Label();
            this.STUN_ServerPortTextBox = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.RunAtStartup = new System.Windows.Forms.CheckBox();
            this.STUN_ServerTextBox = new System.Windows.Forms.TextBox();
            this.MinimizeWhenStartedCheckBox = new System.Windows.Forms.CheckBox();
            this.ProfileCount_Label = new System.Windows.Forms.Label();
            this.ProfileCount_TextBox = new System.Windows.Forms.TextBox();
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
            this.TUNTAPGroupBox.Controls.Add(this.TUNTAPProxyDNSCheckBox);
            this.TUNTAPGroupBox.Controls.Add(this.TUNTAPUseCustomDNSCheckBox);
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
            this.UseFakeDNSCheckBox.Size = new System.Drawing.Size(316, 21);
            this.UseFakeDNSCheckBox.TabIndex = 11;
            this.UseFakeDNSCheckBox.Text = "Use Fake DNS (Suggest open if NTT is Udpblock)";
            this.UseFakeDNSCheckBox.UseVisualStyleBackColor = true;
            // 
            // TUNTAPProxyDNSCheckBox
            // 
            this.TUNTAPProxyDNSCheckBox.AutoSize = true;
            this.TUNTAPProxyDNSCheckBox.Location = new System.Drawing.Point(261, 139);
            this.TUNTAPProxyDNSCheckBox.Name = "TUNTAPProxyDNSCheckBox";
            this.TUNTAPProxyDNSCheckBox.Size = new System.Drawing.Size(153, 21);
            this.TUNTAPProxyDNSCheckBox.TabIndex = 10;
            this.TUNTAPProxyDNSCheckBox.Text = "Proxy DNS in Mode 2";
            this.TUNTAPProxyDNSCheckBox.UseVisualStyleBackColor = true;
            // 
            // TUNTAPUseCustomDNSCheckBox
            // 
            this.TUNTAPUseCustomDNSCheckBox.AutoSize = true;
            this.TUNTAPUseCustomDNSCheckBox.Location = new System.Drawing.Point(10, 139);
            this.TUNTAPUseCustomDNSCheckBox.Name = "TUNTAPUseCustomDNSCheckBox";
            this.TUNTAPUseCustomDNSCheckBox.Size = new System.Drawing.Size(127, 21);
            this.TUNTAPUseCustomDNSCheckBox.TabIndex = 9;
            this.TUNTAPUseCustomDNSCheckBox.Text = "Use Custom DNS";
            this.TUNTAPUseCustomDNSCheckBox.UseVisualStyleBackColor = true;
            this.TUNTAPUseCustomDNSCheckBox.CheckedChanged += new System.EventHandler(this.TUNTAPUseCustomDNSCheckBox_CheckedChanged);
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
            this.ControlButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
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
            this.GlobalBypassIPsButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
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
            this.BehaviorGroupBox.Controls.Add(this.BootShadowsocksFromDLLCheckBox);
            this.BehaviorGroupBox.Controls.Add(this.AclAddr);
            this.BehaviorGroupBox.Controls.Add(this.AclLabel);
            this.BehaviorGroupBox.Controls.Add(this.DetectionInterval_Label);
            this.BehaviorGroupBox.Controls.Add(this.DetectionInterval_TextBox);
            this.BehaviorGroupBox.Controls.Add(this.EnableStartedTcping_CheckBox);
            this.BehaviorGroupBox.Controls.Add(this.DelayTestAfterStartup_Label);
            this.BehaviorGroupBox.Controls.Add(this.BypassModeCheckBox);
            this.BehaviorGroupBox.Controls.Add(this.Redirector2checkBox);
            this.BehaviorGroupBox.Controls.Add(this.ExperimentalFunction_Label);
            this.BehaviorGroupBox.Controls.Add(this.STUN_ServerPortTextBox);
            this.BehaviorGroupBox.Controls.Add(this.label2);
            this.BehaviorGroupBox.Controls.Add(this.label1);
            this.BehaviorGroupBox.Controls.Add(this.RunAtStartup);
            this.BehaviorGroupBox.Controls.Add(this.STUN_ServerTextBox);
            this.BehaviorGroupBox.Controls.Add(this.MinimizeWhenStartedCheckBox);
            this.BehaviorGroupBox.Controls.Add(this.ProfileCount_Label);
            this.BehaviorGroupBox.Controls.Add(this.ProfileCount_TextBox);
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
            // BootShadowsocksFromDLLCheckBox
            // 
            this.BootShadowsocksFromDLLCheckBox.AutoSize = true;
            this.BootShadowsocksFromDLLCheckBox.Location = new System.Drawing.Point(12, 128);
            this.BootShadowsocksFromDLLCheckBox.Name = "BootShadowsocksFromDLLCheckBox";
            this.BootShadowsocksFromDLLCheckBox.Size = new System.Drawing.Size(297, 21);
            this.BootShadowsocksFromDLLCheckBox.TabIndex = 21;
            this.BootShadowsocksFromDLLCheckBox.Text = "Boot Shadowsocks from DLL(No support ACL)";
            this.BootShadowsocksFromDLLCheckBox.UseVisualStyleBackColor = true;
            // 
            // AclAddr
            // 
            this.AclAddr.Location = new System.Drawing.Point(117, 274);
            this.AclAddr.Name = "AclAddr";
            this.AclAddr.Size = new System.Drawing.Size(315, 23);
            this.AclAddr.TabIndex = 19;
            this.AclAddr.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // AclLabel
            // 
            this.AclLabel.AutoSize = true;
            this.AclLabel.Location = new System.Drawing.Point(9, 277);
            this.AclLabel.Name = "AclLabel";
            this.AclLabel.Size = new System.Drawing.Size(78, 17);
            this.AclLabel.TabIndex = 20;
            this.AclLabel.Text = "Custom ACL";
            // 
            // DetectionInterval_Label
            // 
            this.DetectionInterval_Label.AutoSize = true;
            this.DetectionInterval_Label.Location = new System.Drawing.Point(230, 193);
            this.DetectionInterval_Label.Name = "DetectionInterval_Label";
            this.DetectionInterval_Label.Size = new System.Drawing.Size(128, 17);
            this.DetectionInterval_Label.TabIndex = 18;
            this.DetectionInterval_Label.Text = "Detection interval(/s)";
            // 
            // DetectionInterval_TextBox
            // 
            this.DetectionInterval_TextBox.Location = new System.Drawing.Point(364, 190);
            this.DetectionInterval_TextBox.Name = "DetectionInterval_TextBox";
            this.DetectionInterval_TextBox.Size = new System.Drawing.Size(68, 23);
            this.DetectionInterval_TextBox.TabIndex = 17;
            this.DetectionInterval_TextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // EnableStartedTcping_CheckBox
            // 
            this.EnableStartedTcping_CheckBox.AutoSize = true;
            this.EnableStartedTcping_CheckBox.Location = new System.Drawing.Point(152, 192);
            this.EnableStartedTcping_CheckBox.Name = "EnableStartedTcping_CheckBox";
            this.EnableStartedTcping_CheckBox.Size = new System.Drawing.Size(66, 21);
            this.EnableStartedTcping_CheckBox.TabIndex = 15;
            this.EnableStartedTcping_CheckBox.Text = "Enable";
            this.EnableStartedTcping_CheckBox.UseVisualStyleBackColor = true;
            // 
            // DelayTestAfterStartup_Label
            // 
            this.DelayTestAfterStartup_Label.AutoSize = true;
            this.DelayTestAfterStartup_Label.Location = new System.Drawing.Point(9, 193);
            this.DelayTestAfterStartup_Label.Name = "DelayTestAfterStartup_Label";
            this.DelayTestAfterStartup_Label.Size = new System.Drawing.Size(141, 17);
            this.DelayTestAfterStartup_Label.TabIndex = 16;
            this.DelayTestAfterStartup_Label.Text = "Delay test after startup";
            // 
            // BypassModeCheckBox
            // 
            this.BypassModeCheckBox.AutoSize = true;
            this.BypassModeCheckBox.Location = new System.Drawing.Point(12, 102);
            this.BypassModeCheckBox.Name = "BypassModeCheckBox";
            this.BypassModeCheckBox.Size = new System.Drawing.Size(160, 21);
            this.BypassModeCheckBox.TabIndex = 14;
            this.BypassModeCheckBox.Text = "Process whitelist mode";
            this.BypassModeCheckBox.UseVisualStyleBackColor = true;
            this.BypassModeCheckBox.CheckedChanged += new System.EventHandler(this.BypassModeCheckBox_CheckedChanged);
            // 
            // Redirector2checkBox
            // 
            this.Redirector2checkBox.AutoSize = true;
            this.Redirector2checkBox.Location = new System.Drawing.Point(148, 310);
            this.Redirector2checkBox.Name = "Redirector2checkBox";
            this.Redirector2checkBox.Size = new System.Drawing.Size(118, 21);
            this.Redirector2checkBox.TabIndex = 11;
            this.Redirector2checkBox.Text = "是否启用2号核心";
            this.Redirector2checkBox.UseVisualStyleBackColor = true;
            // 
            // ExperimentalFunction_Label
            // 
            this.ExperimentalFunction_Label.AutoSize = true;
            this.ExperimentalFunction_Label.Location = new System.Drawing.Point(9, 310);
            this.ExperimentalFunction_Label.Name = "ExperimentalFunction_Label";
            this.ExperimentalFunction_Label.Size = new System.Drawing.Size(133, 17);
            this.ExperimentalFunction_Label.TabIndex = 13;
            this.ExperimentalFunction_Label.Text = "Experimental function";
            // 
            // STUN_ServerPortTextBox
            // 
            this.STUN_ServerPortTextBox.Location = new System.Drawing.Point(117, 245);
            this.STUN_ServerPortTextBox.Name = "STUN_ServerPortTextBox";
            this.STUN_ServerPortTextBox.Size = new System.Drawing.Size(315, 23);
            this.STUN_ServerPortTextBox.TabIndex = 8;
            this.STUN_ServerPortTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(9, 248);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(110, 17);
            this.label2.TabIndex = 12;
            this.label2.Text = "STUN Server Port";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(9, 222);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(82, 17);
            this.label1.TabIndex = 10;
            this.label1.Text = "STUN Server";
            // 
            // RunAtStartup
            // 
            this.RunAtStartup.AutoSize = true;
            this.RunAtStartup.Location = new System.Drawing.Point(12, 75);
            this.RunAtStartup.Name = "RunAtStartup";
            this.RunAtStartup.Size = new System.Drawing.Size(109, 21);
            this.RunAtStartup.TabIndex = 11;
            this.RunAtStartup.Text = "Run at startup";
            this.RunAtStartup.UseVisualStyleBackColor = true;
            // 
            // STUN_ServerTextBox
            // 
            this.STUN_ServerTextBox.Location = new System.Drawing.Point(117, 216);
            this.STUN_ServerTextBox.Name = "STUN_ServerTextBox";
            this.STUN_ServerTextBox.Size = new System.Drawing.Size(315, 23);
            this.STUN_ServerTextBox.TabIndex = 11;
            this.STUN_ServerTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
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
            // ProfileCount_Label
            // 
            this.ProfileCount_Label.AutoSize = true;
            this.ProfileCount_Label.Location = new System.Drawing.Point(9, 168);
            this.ProfileCount_Label.Name = "ProfileCount_Label";
            this.ProfileCount_Label.Size = new System.Drawing.Size(79, 17);
            this.ProfileCount_Label.TabIndex = 8;
            this.ProfileCount_Label.Text = "ProfileCount";
            // 
            // ProfileCount_TextBox
            // 
            this.ProfileCount_TextBox.Location = new System.Drawing.Point(206, 165);
            this.ProfileCount_TextBox.Name = "ProfileCount_TextBox";
            this.ProfileCount_TextBox.Size = new System.Drawing.Size(226, 23);
            this.ProfileCount_TextBox.TabIndex = 9;
            this.ProfileCount_TextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
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
            this.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.MaximizeBox = false;
            this.Name = "SettingForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Settings";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.SettingForm_FormClosing);
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
        private System.Windows.Forms.CheckBox TUNTAPUseCustomDNSCheckBox;
        private System.Windows.Forms.CheckBox AllowDevicesCheckBox;
        private System.Windows.Forms.GroupBox BehaviorGroupBox;
        private System.Windows.Forms.CheckBox ExitWhenClosedCheckBox;
        private System.Windows.Forms.CheckBox StopWhenExitedCheckBox;
        private System.Windows.Forms.CheckBox StartWhenOpenedCheckBox;
        private System.Windows.Forms.CheckBox CheckUpdateWhenOpenedCheckBox;
        private System.Windows.Forms.Label ProfileCount_Label;
        private System.Windows.Forms.TextBox ProfileCount_TextBox;
        private System.Windows.Forms.CheckBox MinimizeWhenStartedCheckBox;
        private System.Windows.Forms.CheckBox RunAtStartup;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox STUN_ServerTextBox;
        private System.Windows.Forms.TextBox STUN_ServerPortTextBox;
        private System.Windows.Forms.CheckBox TUNTAPProxyDNSCheckBox;
        private System.Windows.Forms.CheckBox Redirector2checkBox;
        private System.Windows.Forms.Label ExperimentalFunction_Label;
        private System.Windows.Forms.CheckBox BypassModeCheckBox;
        private System.Windows.Forms.TextBox DetectionInterval_TextBox;
        private System.Windows.Forms.CheckBox EnableStartedTcping_CheckBox;
        private System.Windows.Forms.Label DelayTestAfterStartup_Label;
        private System.Windows.Forms.Label DetectionInterval_Label;
        private System.Windows.Forms.Label RedirectorLabel;
        private System.Windows.Forms.TextBox RedirectorTextBox;
        private System.Windows.Forms.TextBox AclAddr;
        private System.Windows.Forms.Label AclLabel;
        private System.Windows.Forms.CheckBox UseFakeDNSCheckBox;
        private System.Windows.Forms.CheckBox BootShadowsocksFromDLLCheckBox;
    }
}