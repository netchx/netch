namespace Netch.Forms.Server
{
    partial class VMess
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(VMess));
            this.ConfigurationGroupBox = new System.Windows.Forms.GroupBox();
            this.PortTextBox = new System.Windows.Forms.TextBox();
            this.AddressTextBox = new System.Windows.Forms.TextBox();
            this.AddressLabel = new System.Windows.Forms.Label();
            this.RemarkTextBox = new System.Windows.Forms.TextBox();
            this.RemarkLabel = new System.Windows.Forms.Label();
            this.PortLabel = new System.Windows.Forms.Label();
            this.UserIDTextBox = new System.Windows.Forms.TextBox();
            this.UserIDLabel = new System.Windows.Forms.Label();
            this.AlterIDTextBox = new System.Windows.Forms.TextBox();
            this.AlterIDLabel = new System.Windows.Forms.Label();
            this.EncryptMethodComboBox = new System.Windows.Forms.ComboBox();
            this.EncryptMethodLabel = new System.Windows.Forms.Label();
            this.TransferProtocolComboBox = new System.Windows.Forms.ComboBox();
            this.TransferProtocolLabel = new System.Windows.Forms.Label();
            this.FakeTypeComboBox = new System.Windows.Forms.ComboBox();
            this.FakeTypeLabel = new System.Windows.Forms.Label();
            this.HostTextBox = new System.Windows.Forms.TextBox();
            this.HostLabel = new System.Windows.Forms.Label();
            this.PathTextBox = new System.Windows.Forms.TextBox();
            this.PathLabel = new System.Windows.Forms.Label();
            this.QUICSecurityComboBox = new System.Windows.Forms.ComboBox();
            this.QUICSecretTextBox = new System.Windows.Forms.TextBox();
            this.QUICSecurityLabel = new System.Windows.Forms.Label();
            this.QUICSecretLabel = new System.Windows.Forms.Label();
            this.TLSSecureCheckBox = new System.Windows.Forms.CheckBox();
            this.ControlButton = new System.Windows.Forms.Button();
            this.ConfigurationGroupBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // ConfigurationGroupBox
            // 
            this.ConfigurationGroupBox.Controls.Add(this.TLSSecureCheckBox);
            this.ConfigurationGroupBox.Controls.Add(this.QUICSecretLabel);
            this.ConfigurationGroupBox.Controls.Add(this.QUICSecurityLabel);
            this.ConfigurationGroupBox.Controls.Add(this.QUICSecretTextBox);
            this.ConfigurationGroupBox.Controls.Add(this.QUICSecurityComboBox);
            this.ConfigurationGroupBox.Controls.Add(this.PathLabel);
            this.ConfigurationGroupBox.Controls.Add(this.PathTextBox);
            this.ConfigurationGroupBox.Controls.Add(this.HostLabel);
            this.ConfigurationGroupBox.Controls.Add(this.HostTextBox);
            this.ConfigurationGroupBox.Controls.Add(this.FakeTypeLabel);
            this.ConfigurationGroupBox.Controls.Add(this.FakeTypeComboBox);
            this.ConfigurationGroupBox.Controls.Add(this.TransferProtocolLabel);
            this.ConfigurationGroupBox.Controls.Add(this.TransferProtocolComboBox);
            this.ConfigurationGroupBox.Controls.Add(this.EncryptMethodLabel);
            this.ConfigurationGroupBox.Controls.Add(this.EncryptMethodComboBox);
            this.ConfigurationGroupBox.Controls.Add(this.AlterIDLabel);
            this.ConfigurationGroupBox.Controls.Add(this.AlterIDTextBox);
            this.ConfigurationGroupBox.Controls.Add(this.UserIDLabel);
            this.ConfigurationGroupBox.Controls.Add(this.UserIDTextBox);
            this.ConfigurationGroupBox.Controls.Add(this.PortTextBox);
            this.ConfigurationGroupBox.Controls.Add(this.AddressTextBox);
            this.ConfigurationGroupBox.Controls.Add(this.AddressLabel);
            this.ConfigurationGroupBox.Controls.Add(this.RemarkTextBox);
            this.ConfigurationGroupBox.Controls.Add(this.RemarkLabel);
            this.ConfigurationGroupBox.Controls.Add(this.PortLabel);
            this.ConfigurationGroupBox.Location = new System.Drawing.Point(12, 12);
            this.ConfigurationGroupBox.Name = "ConfigurationGroupBox";
            this.ConfigurationGroupBox.Size = new System.Drawing.Size(420, 373);
            this.ConfigurationGroupBox.TabIndex = 1;
            this.ConfigurationGroupBox.TabStop = false;
            this.ConfigurationGroupBox.Text = "Configuration";
            // 
            // PortTextBox
            // 
            this.PortTextBox.Location = new System.Drawing.Point(360, 48);
            this.PortTextBox.Name = "PortTextBox";
            this.PortTextBox.Size = new System.Drawing.Size(54, 23);
            this.PortTextBox.TabIndex = 5;
            this.PortTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // AddressTextBox
            // 
            this.AddressTextBox.Location = new System.Drawing.Point(120, 48);
            this.AddressTextBox.Name = "AddressTextBox";
            this.AddressTextBox.Size = new System.Drawing.Size(234, 23);
            this.AddressTextBox.TabIndex = 3;
            this.AddressTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // AddressLabel
            // 
            this.AddressLabel.AutoSize = true;
            this.AddressLabel.Location = new System.Drawing.Point(10, 51);
            this.AddressLabel.Name = "AddressLabel";
            this.AddressLabel.Size = new System.Drawing.Size(56, 17);
            this.AddressLabel.TabIndex = 2;
            this.AddressLabel.Text = "Address";
            // 
            // RemarkTextBox
            // 
            this.RemarkTextBox.Location = new System.Drawing.Point(120, 19);
            this.RemarkTextBox.Name = "RemarkTextBox";
            this.RemarkTextBox.Size = new System.Drawing.Size(294, 23);
            this.RemarkTextBox.TabIndex = 1;
            this.RemarkTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // RemarkLabel
            // 
            this.RemarkLabel.AutoSize = true;
            this.RemarkLabel.Location = new System.Drawing.Point(10, 22);
            this.RemarkLabel.Name = "RemarkLabel";
            this.RemarkLabel.Size = new System.Drawing.Size(53, 17);
            this.RemarkLabel.TabIndex = 0;
            this.RemarkLabel.Text = "Remark";
            // 
            // PortLabel
            // 
            this.PortLabel.AutoSize = true;
            this.PortLabel.Location = new System.Drawing.Point(353, 51);
            this.PortLabel.Name = "PortLabel";
            this.PortLabel.Size = new System.Drawing.Size(11, 17);
            this.PortLabel.TabIndex = 4;
            this.PortLabel.Text = ":";
            // 
            // UserIDTextBox
            // 
            this.UserIDTextBox.Location = new System.Drawing.Point(120, 77);
            this.UserIDTextBox.Name = "UserIDTextBox";
            this.UserIDTextBox.Size = new System.Drawing.Size(294, 23);
            this.UserIDTextBox.TabIndex = 6;
            this.UserIDTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // UserIDLabel
            // 
            this.UserIDLabel.AutoSize = true;
            this.UserIDLabel.Location = new System.Drawing.Point(10, 80);
            this.UserIDLabel.Name = "UserIDLabel";
            this.UserIDLabel.Size = new System.Drawing.Size(52, 17);
            this.UserIDLabel.TabIndex = 7;
            this.UserIDLabel.Text = "User ID";
            // 
            // AlterIDTextBox
            // 
            this.AlterIDTextBox.Location = new System.Drawing.Point(120, 106);
            this.AlterIDTextBox.Name = "AlterIDTextBox";
            this.AlterIDTextBox.Size = new System.Drawing.Size(54, 23);
            this.AlterIDTextBox.TabIndex = 8;
            this.AlterIDTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // AlterIDLabel
            // 
            this.AlterIDLabel.AutoSize = true;
            this.AlterIDLabel.Location = new System.Drawing.Point(10, 109);
            this.AlterIDLabel.Name = "AlterIDLabel";
            this.AlterIDLabel.Size = new System.Drawing.Size(52, 17);
            this.AlterIDLabel.TabIndex = 9;
            this.AlterIDLabel.Text = "Alter ID";
            // 
            // EncryptMethodComboBox
            // 
            this.EncryptMethodComboBox.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.EncryptMethodComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.EncryptMethodComboBox.FormattingEnabled = true;
            this.EncryptMethodComboBox.Location = new System.Drawing.Point(120, 135);
            this.EncryptMethodComboBox.Name = "EncryptMethodComboBox";
            this.EncryptMethodComboBox.Size = new System.Drawing.Size(294, 24);
            this.EncryptMethodComboBox.TabIndex = 10;
            this.EncryptMethodComboBox.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.ComboBox_DrawItem);
            // 
            // EncryptMethodLabel
            // 
            this.EncryptMethodLabel.AutoSize = true;
            this.EncryptMethodLabel.Location = new System.Drawing.Point(10, 139);
            this.EncryptMethodLabel.Name = "EncryptMethodLabel";
            this.EncryptMethodLabel.Size = new System.Drawing.Size(101, 17);
            this.EncryptMethodLabel.TabIndex = 11;
            this.EncryptMethodLabel.Text = "Encrypt Method";
            // 
            // TransferProtocolComboBox
            // 
            this.TransferProtocolComboBox.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.TransferProtocolComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.TransferProtocolComboBox.FormattingEnabled = true;
            this.TransferProtocolComboBox.Location = new System.Drawing.Point(120, 166);
            this.TransferProtocolComboBox.Name = "TransferProtocolComboBox";
            this.TransferProtocolComboBox.Size = new System.Drawing.Size(294, 24);
            this.TransferProtocolComboBox.TabIndex = 12;
            this.TransferProtocolComboBox.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.ComboBox_DrawItem);
            // 
            // TransferProtocolLabel
            // 
            this.TransferProtocolLabel.AutoSize = true;
            this.TransferProtocolLabel.Location = new System.Drawing.Point(10, 170);
            this.TransferProtocolLabel.Name = "TransferProtocolLabel";
            this.TransferProtocolLabel.Size = new System.Drawing.Size(109, 17);
            this.TransferProtocolLabel.TabIndex = 13;
            this.TransferProtocolLabel.Text = "Transfer Protocol";
            // 
            // FakeTypeComboBox
            // 
            this.FakeTypeComboBox.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.FakeTypeComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.FakeTypeComboBox.FormattingEnabled = true;
            this.FakeTypeComboBox.Location = new System.Drawing.Point(120, 197);
            this.FakeTypeComboBox.Name = "FakeTypeComboBox";
            this.FakeTypeComboBox.Size = new System.Drawing.Size(294, 24);
            this.FakeTypeComboBox.TabIndex = 14;
            this.FakeTypeComboBox.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.ComboBox_DrawItem);
            // 
            // FakeTypeLabel
            // 
            this.FakeTypeLabel.AutoSize = true;
            this.FakeTypeLabel.Location = new System.Drawing.Point(10, 201);
            this.FakeTypeLabel.Name = "FakeTypeLabel";
            this.FakeTypeLabel.Size = new System.Drawing.Size(67, 17);
            this.FakeTypeLabel.TabIndex = 15;
            this.FakeTypeLabel.Text = "Fake Type";
            // 
            // HostTextBox
            // 
            this.HostTextBox.Location = new System.Drawing.Point(120, 228);
            this.HostTextBox.Name = "HostTextBox";
            this.HostTextBox.Size = new System.Drawing.Size(294, 23);
            this.HostTextBox.TabIndex = 16;
            this.HostTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // HostLabel
            // 
            this.HostLabel.AutoSize = true;
            this.HostLabel.Location = new System.Drawing.Point(10, 231);
            this.HostLabel.Name = "HostLabel";
            this.HostLabel.Size = new System.Drawing.Size(35, 17);
            this.HostLabel.TabIndex = 17;
            this.HostLabel.Text = "Host";
            // 
            // PathTextBox
            // 
            this.PathTextBox.Location = new System.Drawing.Point(120, 257);
            this.PathTextBox.Name = "PathTextBox";
            this.PathTextBox.Size = new System.Drawing.Size(294, 23);
            this.PathTextBox.TabIndex = 18;
            this.PathTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // PathLabel
            // 
            this.PathLabel.AutoSize = true;
            this.PathLabel.Location = new System.Drawing.Point(10, 260);
            this.PathLabel.Name = "PathLabel";
            this.PathLabel.Size = new System.Drawing.Size(33, 17);
            this.PathLabel.TabIndex = 19;
            this.PathLabel.Text = "Path";
            // 
            // QUICSecurityComboBox
            // 
            this.QUICSecurityComboBox.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.QUICSecurityComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.QUICSecurityComboBox.FormattingEnabled = true;
            this.QUICSecurityComboBox.Location = new System.Drawing.Point(120, 286);
            this.QUICSecurityComboBox.Name = "QUICSecurityComboBox";
            this.QUICSecurityComboBox.Size = new System.Drawing.Size(294, 24);
            this.QUICSecurityComboBox.TabIndex = 20;
            this.QUICSecurityComboBox.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.ComboBox_DrawItem);
            // 
            // QUICSecretTextBox
            // 
            this.QUICSecretTextBox.Location = new System.Drawing.Point(120, 317);
            this.QUICSecretTextBox.Name = "QUICSecretTextBox";
            this.QUICSecretTextBox.Size = new System.Drawing.Size(294, 23);
            this.QUICSecretTextBox.TabIndex = 21;
            this.QUICSecretTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // QUICSecurityLabel
            // 
            this.QUICSecurityLabel.AutoSize = true;
            this.QUICSecurityLabel.Location = new System.Drawing.Point(10, 290);
            this.QUICSecurityLabel.Name = "QUICSecurityLabel";
            this.QUICSecurityLabel.Size = new System.Drawing.Size(88, 17);
            this.QUICSecurityLabel.TabIndex = 22;
            this.QUICSecurityLabel.Text = "QUIC Security";
            // 
            // QUICSecretLabel
            // 
            this.QUICSecretLabel.AutoSize = true;
            this.QUICSecretLabel.Location = new System.Drawing.Point(10, 320);
            this.QUICSecretLabel.Name = "QUICSecretLabel";
            this.QUICSecretLabel.Size = new System.Drawing.Size(79, 17);
            this.QUICSecretLabel.TabIndex = 23;
            this.QUICSecretLabel.Text = "QUIC Secret";
            // 
            // TLSSecureCheckBox
            // 
            this.TLSSecureCheckBox.AutoSize = true;
            this.TLSSecureCheckBox.Location = new System.Drawing.Point(302, 346);
            this.TLSSecureCheckBox.Name = "TLSSecureCheckBox";
            this.TLSSecureCheckBox.Size = new System.Drawing.Size(90, 21);
            this.TLSSecureCheckBox.TabIndex = 24;
            this.TLSSecureCheckBox.Text = "TLS Secure";
            this.TLSSecureCheckBox.UseVisualStyleBackColor = true;
            // 
            // ControlButton
            // 
            this.ControlButton.Location = new System.Drawing.Point(357, 391);
            this.ControlButton.Name = "ControlButton";
            this.ControlButton.Size = new System.Drawing.Size(75, 23);
            this.ControlButton.TabIndex = 2;
            this.ControlButton.Text = "Save";
            this.ControlButton.UseVisualStyleBackColor = true;
            this.ControlButton.Click += new System.EventHandler(this.ControlButton_Click);
            // 
            // VMess
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(444, 426);
            this.Controls.Add(this.ControlButton);
            this.Controls.Add(this.ConfigurationGroupBox);
            this.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.MaximizeBox = false;
            this.Name = "VMess";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "VMess";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.VMess_FormClosing);
            this.Load += new System.EventHandler(this.VMess_Load);
            this.ConfigurationGroupBox.ResumeLayout(false);
            this.ConfigurationGroupBox.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox ConfigurationGroupBox;
        private System.Windows.Forms.TextBox PortTextBox;
        private System.Windows.Forms.TextBox AddressTextBox;
        private System.Windows.Forms.Label AddressLabel;
        private System.Windows.Forms.TextBox RemarkTextBox;
        private System.Windows.Forms.Label RemarkLabel;
        private System.Windows.Forms.Label PortLabel;
        private System.Windows.Forms.Label UserIDLabel;
        private System.Windows.Forms.TextBox UserIDTextBox;
        private System.Windows.Forms.Label AlterIDLabel;
        private System.Windows.Forms.TextBox AlterIDTextBox;
        private System.Windows.Forms.Label EncryptMethodLabel;
        private System.Windows.Forms.ComboBox EncryptMethodComboBox;
        private System.Windows.Forms.Label TransferProtocolLabel;
        private System.Windows.Forms.ComboBox TransferProtocolComboBox;
        private System.Windows.Forms.Label FakeTypeLabel;
        private System.Windows.Forms.ComboBox FakeTypeComboBox;
        private System.Windows.Forms.Label HostLabel;
        private System.Windows.Forms.TextBox HostTextBox;
        private System.Windows.Forms.Label PathLabel;
        private System.Windows.Forms.TextBox PathTextBox;
        private System.Windows.Forms.Label QUICSecurityLabel;
        private System.Windows.Forms.TextBox QUICSecretTextBox;
        private System.Windows.Forms.ComboBox QUICSecurityComboBox;
        private System.Windows.Forms.Label QUICSecretLabel;
        private System.Windows.Forms.CheckBox TLSSecureCheckBox;
        private System.Windows.Forms.Button ControlButton;
    }
}