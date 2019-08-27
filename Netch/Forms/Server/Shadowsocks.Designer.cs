namespace Netch.Forms.Server
{
    partial class Shadowsocks
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Shadowsocks));
            this.ConfigurationGroupBox = new System.Windows.Forms.GroupBox();
            this.EncryptMethodLabel = new System.Windows.Forms.Label();
            this.EncryptMethodComboBox = new System.Windows.Forms.ComboBox();
            this.PasswordTextBox = new System.Windows.Forms.TextBox();
            this.PasswordLabel = new System.Windows.Forms.Label();
            this.AddressLabel = new System.Windows.Forms.Label();
            this.PortTextBox = new System.Windows.Forms.TextBox();
            this.AddressTextBox = new System.Windows.Forms.TextBox();
            this.RemarkTextBox = new System.Windows.Forms.TextBox();
            this.RemarkLabel = new System.Windows.Forms.Label();
            this.PortLabel = new System.Windows.Forms.Label();
            this.ControlButton = new System.Windows.Forms.Button();
            this.PluginLabel = new System.Windows.Forms.Label();
            this.PluginTextBox = new System.Windows.Forms.TextBox();
            this.PluginOptionsTextBox = new System.Windows.Forms.TextBox();
            this.PluginOptionsLabel = new System.Windows.Forms.Label();
            this.ConfigurationGroupBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // ConfigurationGroupBox
            // 
            this.ConfigurationGroupBox.Controls.Add(this.PluginOptionsTextBox);
            this.ConfigurationGroupBox.Controls.Add(this.PluginOptionsLabel);
            this.ConfigurationGroupBox.Controls.Add(this.PluginTextBox);
            this.ConfigurationGroupBox.Controls.Add(this.PluginLabel);
            this.ConfigurationGroupBox.Controls.Add(this.EncryptMethodLabel);
            this.ConfigurationGroupBox.Controls.Add(this.EncryptMethodComboBox);
            this.ConfigurationGroupBox.Controls.Add(this.PasswordTextBox);
            this.ConfigurationGroupBox.Controls.Add(this.PasswordLabel);
            this.ConfigurationGroupBox.Controls.Add(this.AddressLabel);
            this.ConfigurationGroupBox.Controls.Add(this.PortTextBox);
            this.ConfigurationGroupBox.Controls.Add(this.AddressTextBox);
            this.ConfigurationGroupBox.Controls.Add(this.RemarkTextBox);
            this.ConfigurationGroupBox.Controls.Add(this.RemarkLabel);
            this.ConfigurationGroupBox.Controls.Add(this.PortLabel);
            this.ConfigurationGroupBox.Location = new System.Drawing.Point(12, 12);
            this.ConfigurationGroupBox.Name = "ConfigurationGroupBox";
            this.ConfigurationGroupBox.Size = new System.Drawing.Size(420, 197);
            this.ConfigurationGroupBox.TabIndex = 0;
            this.ConfigurationGroupBox.TabStop = false;
            this.ConfigurationGroupBox.Text = "Configuration";
            // 
            // EncryptMethodLabel
            // 
            this.EncryptMethodLabel.AutoSize = true;
            this.EncryptMethodLabel.Location = new System.Drawing.Point(10, 110);
            this.EncryptMethodLabel.Name = "EncryptMethodLabel";
            this.EncryptMethodLabel.Size = new System.Drawing.Size(101, 17);
            this.EncryptMethodLabel.TabIndex = 13;
            this.EncryptMethodLabel.Text = "Encrypt Method";
            // 
            // EncryptMethodComboBox
            // 
            this.EncryptMethodComboBox.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.EncryptMethodComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.EncryptMethodComboBox.FormattingEnabled = true;
            this.EncryptMethodComboBox.Location = new System.Drawing.Point(120, 106);
            this.EncryptMethodComboBox.Name = "EncryptMethodComboBox";
            this.EncryptMethodComboBox.Size = new System.Drawing.Size(294, 24);
            this.EncryptMethodComboBox.TabIndex = 12;
            this.EncryptMethodComboBox.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.ComboBox_DrawItem);
            // 
            // PasswordTextBox
            // 
            this.PasswordTextBox.Location = new System.Drawing.Point(120, 77);
            this.PasswordTextBox.Name = "PasswordTextBox";
            this.PasswordTextBox.Size = new System.Drawing.Size(294, 23);
            this.PasswordTextBox.TabIndex = 10;
            this.PasswordTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // PasswordLabel
            // 
            this.PasswordLabel.AutoSize = true;
            this.PasswordLabel.Location = new System.Drawing.Point(10, 80);
            this.PasswordLabel.Name = "PasswordLabel";
            this.PasswordLabel.Size = new System.Drawing.Size(64, 17);
            this.PasswordLabel.TabIndex = 11;
            this.PasswordLabel.Text = "Password";
            // 
            // AddressLabel
            // 
            this.AddressLabel.AutoSize = true;
            this.AddressLabel.Location = new System.Drawing.Point(10, 51);
            this.AddressLabel.Name = "AddressLabel";
            this.AddressLabel.Size = new System.Drawing.Size(56, 17);
            this.AddressLabel.TabIndex = 5;
            this.AddressLabel.Text = "Address";
            // 
            // PortTextBox
            // 
            this.PortTextBox.Location = new System.Drawing.Point(358, 48);
            this.PortTextBox.Name = "PortTextBox";
            this.PortTextBox.Size = new System.Drawing.Size(56, 23);
            this.PortTextBox.TabIndex = 4;
            this.PortTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // AddressTextBox
            // 
            this.AddressTextBox.Location = new System.Drawing.Point(120, 48);
            this.AddressTextBox.Name = "AddressTextBox";
            this.AddressTextBox.Size = new System.Drawing.Size(232, 23);
            this.AddressTextBox.TabIndex = 2;
            this.AddressTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
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
            this.PortLabel.Location = new System.Drawing.Point(351, 51);
            this.PortLabel.Name = "PortLabel";
            this.PortLabel.Size = new System.Drawing.Size(11, 17);
            this.PortLabel.TabIndex = 3;
            this.PortLabel.Text = ":";
            // 
            // ControlButton
            // 
            this.ControlButton.Location = new System.Drawing.Point(357, 215);
            this.ControlButton.Name = "ControlButton";
            this.ControlButton.Size = new System.Drawing.Size(75, 23);
            this.ControlButton.TabIndex = 1;
            this.ControlButton.Text = "Save";
            this.ControlButton.UseVisualStyleBackColor = true;
            this.ControlButton.Click += new System.EventHandler(this.ControlButton_Click);
            // 
            // PluginLabel
            // 
            this.PluginLabel.AutoSize = true;
            this.PluginLabel.Location = new System.Drawing.Point(10, 140);
            this.PluginLabel.Name = "PluginLabel";
            this.PluginLabel.Size = new System.Drawing.Size(43, 17);
            this.PluginLabel.TabIndex = 14;
            this.PluginLabel.Text = "Plugin";
            // 
            // PluginTextBox
            // 
            this.PluginTextBox.Location = new System.Drawing.Point(120, 137);
            this.PluginTextBox.Name = "PluginTextBox";
            this.PluginTextBox.Size = new System.Drawing.Size(294, 23);
            this.PluginTextBox.TabIndex = 15;
            this.PluginTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // PluginOptionsTextBox
            // 
            this.PluginOptionsTextBox.Location = new System.Drawing.Point(120, 166);
            this.PluginOptionsTextBox.Name = "PluginOptionsTextBox";
            this.PluginOptionsTextBox.Size = new System.Drawing.Size(294, 23);
            this.PluginOptionsTextBox.TabIndex = 17;
            this.PluginOptionsTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // PluginOptionsLabel
            // 
            this.PluginOptionsLabel.AutoSize = true;
            this.PluginOptionsLabel.Location = new System.Drawing.Point(10, 169);
            this.PluginOptionsLabel.Name = "PluginOptionsLabel";
            this.PluginOptionsLabel.Size = new System.Drawing.Size(93, 17);
            this.PluginOptionsLabel.TabIndex = 16;
            this.PluginOptionsLabel.Text = "Plugin Options";
            // 
            // Shadowsocks
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(444, 248);
            this.Controls.Add(this.ControlButton);
            this.Controls.Add(this.ConfigurationGroupBox);
            this.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.MaximizeBox = false;
            this.Name = "Shadowsocks";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Shadowsocks";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Shadowsocks_FormClosing);
            this.Load += new System.EventHandler(this.Shadowsocks_Load);
            this.ConfigurationGroupBox.ResumeLayout(false);
            this.ConfigurationGroupBox.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox ConfigurationGroupBox;
        private System.Windows.Forms.Button ControlButton;
        private System.Windows.Forms.Label RemarkLabel;
        private System.Windows.Forms.TextBox RemarkTextBox;
        private System.Windows.Forms.Label PortLabel;
        private System.Windows.Forms.TextBox AddressTextBox;
        private System.Windows.Forms.TextBox PortTextBox;
        private System.Windows.Forms.Label AddressLabel;
        private System.Windows.Forms.Label EncryptMethodLabel;
        private System.Windows.Forms.ComboBox EncryptMethodComboBox;
        private System.Windows.Forms.TextBox PasswordTextBox;
        private System.Windows.Forms.Label PasswordLabel;
        private System.Windows.Forms.TextBox PluginOptionsTextBox;
        private System.Windows.Forms.Label PluginOptionsLabel;
        private System.Windows.Forms.TextBox PluginTextBox;
        private System.Windows.Forms.Label PluginLabel;
    }
}