namespace Netch.Forms
{
    partial class ServerForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ServerForm));
            this.ConfigurationGroupBox = new System.Windows.Forms.GroupBox();
            this.AddressLabel = new System.Windows.Forms.Label();
            this.PortTextBox = new System.Windows.Forms.TextBox();
            this.AddressTextBox = new System.Windows.Forms.TextBox();
            this.RemarkTextBox = new System.Windows.Forms.TextBox();
            this.RemarkLabel = new System.Windows.Forms.Label();
            this.PortLabel = new System.Windows.Forms.Label();
            this.ConfigurationGroupBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // ConfigurationGroupBox
            // 
            this.ConfigurationGroupBox.AutoSize = true;
            this.ConfigurationGroupBox.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ConfigurationGroupBox.Controls.Add(this.AddressLabel);
            this.ConfigurationGroupBox.Controls.Add(this.PortTextBox);
            this.ConfigurationGroupBox.Controls.Add(this.AddressTextBox);
            this.ConfigurationGroupBox.Controls.Add(this.RemarkTextBox);
            this.ConfigurationGroupBox.Controls.Add(this.RemarkLabel);
            this.ConfigurationGroupBox.Controls.Add(this.PortLabel);
            this.ConfigurationGroupBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ConfigurationGroupBox.Location = new System.Drawing.Point(5, 5);
            this.ConfigurationGroupBox.Name = "ConfigurationGroupBox";
            this.ConfigurationGroupBox.Size = new System.Drawing.Size(434, 127);
            this.ConfigurationGroupBox.TabIndex = 0;
            this.ConfigurationGroupBox.TabStop = false;
            this.ConfigurationGroupBox.Text = "Configuration";
            // 
            // AddressLabel
            // 
            this.AddressLabel.AutoSize = true;
            this.AddressLabel.Location = new System.Drawing.Point(10, ControlLineHeight*2);
            this.AddressLabel.Name = "AddressLabel";
            this.AddressLabel.Size = new System.Drawing.Size(56, 17);
            this.AddressLabel.TabIndex = 2;
            this.AddressLabel.Text = "Address";
            // 
            // PortTextBox
            // 
            this.PortTextBox.Location = new System.Drawing.Point(358, ControlLineHeight*2);
            this.PortTextBox.Name = "PortTextBox";
            this.PortTextBox.Size = new System.Drawing.Size(56, 23);
            this.PortTextBox.TabIndex = 5;
            this.PortTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // AddressTextBox
            // 
            this.AddressTextBox.Location = new System.Drawing.Point(120, ControlLineHeight*2);
            this.AddressTextBox.Name = "AddressTextBox";
            this.AddressTextBox.Size = new System.Drawing.Size(232, 23);
            this.AddressTextBox.TabIndex = 3;
            this.AddressTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // RemarkTextBox
            // 
            this.RemarkTextBox.Location = new System.Drawing.Point(120, ControlLineHeight);
            this.RemarkTextBox.Name = "RemarkTextBox";
            this.RemarkTextBox.Size = new System.Drawing.Size(294, 23);
            this.RemarkTextBox.TabIndex = 1;
            this.RemarkTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // RemarkLabel
            // 
            this.RemarkLabel.AutoSize = true;
            this.RemarkLabel.Location = new System.Drawing.Point(10, ControlLineHeight);
            this.RemarkLabel.Name = "RemarkLabel";
            this.RemarkLabel.Size = new System.Drawing.Size(53, 17);
            this.RemarkLabel.TabIndex = 0;
            this.RemarkLabel.Text = "Remark";
            // 
            // PortLabel
            // 
            this.PortLabel.AutoSize = true;
            this.PortLabel.Location = new System.Drawing.Point(351, ControlLineHeight*2);
            this.PortLabel.Name = "PortLabel";
            this.PortLabel.Size = new System.Drawing.Size(11, 17);
            this.PortLabel.TabIndex = 4;
            this.PortLabel.Text = ":";
            // 
            // ServerForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ClientSize = new System.Drawing.Size(444, 137);
            this.Controls.Add(this.ConfigurationGroupBox);
            this.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte) (134)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon) (resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.MaximizeBox = false;
            this.Name = "ServerForm";
            this.Padding = new System.Windows.Forms.Padding(11, 5, 11, 4);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Load += new System.EventHandler(this.ServerForm_Load);
            this.ConfigurationGroupBox.ResumeLayout(false);
            this.ConfigurationGroupBox.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.GroupBox ConfigurationGroupBox;
        private System.Windows.Forms.Label RemarkLabel;
        protected System.Windows.Forms.TextBox RemarkTextBox;
        private System.Windows.Forms.Label PortLabel;
        protected System.Windows.Forms.TextBox AddressTextBox;
        private System.Windows.Forms.TextBox PortTextBox;
        private System.Windows.Forms.Label AddressLabel;
    }
}