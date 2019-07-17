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
            this.TUNTAPGroupBox = new System.Windows.Forms.GroupBox();
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
            this.TUNTAPGroupBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // TUNTAPGroupBox
            // 
            this.TUNTAPGroupBox.Controls.Add(this.TUNTAPUseCustomDNSCheckBox);
            this.TUNTAPGroupBox.Controls.Add(this.TUNTAPDNSLabel);
            this.TUNTAPGroupBox.Controls.Add(this.TUNTAPDNSTextBox);
            this.TUNTAPGroupBox.Controls.Add(this.TUNTAPGatewayLabel);
            this.TUNTAPGroupBox.Controls.Add(this.TUNTAPGatewayTextBox);
            this.TUNTAPGroupBox.Controls.Add(this.TUNTAPNetmaskLabel);
            this.TUNTAPGroupBox.Controls.Add(this.TUNTAPNetmaskTextBox);
            this.TUNTAPGroupBox.Controls.Add(this.TUNTAPAddressLabel);
            this.TUNTAPGroupBox.Controls.Add(this.TUNTAPAddressTextBox);
            this.TUNTAPGroupBox.Location = new System.Drawing.Point(12, 12);
            this.TUNTAPGroupBox.Name = "TUNTAPGroupBox";
            this.TUNTAPGroupBox.Size = new System.Drawing.Size(420, 162);
            this.TUNTAPGroupBox.TabIndex = 0;
            this.TUNTAPGroupBox.TabStop = false;
            this.TUNTAPGroupBox.Text = "TUN/TAP";
            // 
            // TUNTAPUseCustomDNSCheckBox
            // 
            this.TUNTAPUseCustomDNSCheckBox.AutoSize = true;
            this.TUNTAPUseCustomDNSCheckBox.Location = new System.Drawing.Point(287, 138);
            this.TUNTAPUseCustomDNSCheckBox.Name = "TUNTAPUseCustomDNSCheckBox";
            this.TUNTAPUseCustomDNSCheckBox.Size = new System.Drawing.Size(127, 21);
            this.TUNTAPUseCustomDNSCheckBox.TabIndex = 8;
            this.TUNTAPUseCustomDNSCheckBox.Text = "Use Custom DNS";
            this.TUNTAPUseCustomDNSCheckBox.UseVisualStyleBackColor = true;
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
            this.TUNTAPDNSTextBox.Location = new System.Drawing.Point(120, 109);
            this.TUNTAPDNSTextBox.Name = "TUNTAPDNSTextBox";
            this.TUNTAPDNSTextBox.Size = new System.Drawing.Size(294, 23);
            this.TUNTAPDNSTextBox.TabIndex = 6;
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
            this.TUNTAPGatewayTextBox.TabIndex = 4;
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
            this.TUNTAPNetmaskTextBox.TabIndex = 2;
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
            this.TUNTAPAddressTextBox.TabIndex = 0;
            this.TUNTAPAddressTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // ControlButton
            // 
            this.ControlButton.Location = new System.Drawing.Point(357, 180);
            this.ControlButton.Name = "ControlButton";
            this.ControlButton.Size = new System.Drawing.Size(75, 23);
            this.ControlButton.TabIndex = 1;
            this.ControlButton.Text = "Save";
            this.ControlButton.UseVisualStyleBackColor = true;
            this.ControlButton.Click += new System.EventHandler(this.ControlButton_Click);
            // 
            // GlobalBypassIPsButton
            // 
            this.GlobalBypassIPsButton.Location = new System.Drawing.Point(12, 180);
            this.GlobalBypassIPsButton.Name = "GlobalBypassIPsButton";
            this.GlobalBypassIPsButton.Size = new System.Drawing.Size(128, 23);
            this.GlobalBypassIPsButton.TabIndex = 2;
            this.GlobalBypassIPsButton.Text = "Global Bypass IPs";
            this.GlobalBypassIPsButton.UseVisualStyleBackColor = true;
            this.GlobalBypassIPsButton.Click += new System.EventHandler(this.GlobalBypassIPsButton_Click);
            // 
            // SettingForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(444, 215);
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
            this.TUNTAPGroupBox.ResumeLayout(false);
            this.TUNTAPGroupBox.PerformLayout();
            this.ResumeLayout(false);

		}

		#endregion

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
	}
}