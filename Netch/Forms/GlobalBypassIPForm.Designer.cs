namespace Netch.Forms
{
    partial class GlobalBypassIPForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GlobalBypassIPForm));
            this.IPGroupBox = new System.Windows.Forms.GroupBox();
            this.AddButton = new System.Windows.Forms.Button();
            this.DeleteButton = new System.Windows.Forms.Button();
            this.PrefixComboBox = new System.Windows.Forms.ComboBox();
            this.IPTextBox = new System.Windows.Forms.TextBox();
            this.IPListBox = new System.Windows.Forms.ListBox();
            this.ControlButton = new System.Windows.Forms.Button();
            this.IPGroupBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // IPGroupBox
            // 
            this.IPGroupBox.Controls.Add(this.AddButton);
            this.IPGroupBox.Controls.Add(this.DeleteButton);
            this.IPGroupBox.Controls.Add(this.PrefixComboBox);
            this.IPGroupBox.Controls.Add(this.IPTextBox);
            this.IPGroupBox.Controls.Add(this.IPListBox);
            this.IPGroupBox.Location = new System.Drawing.Point(12, 12);
            this.IPGroupBox.Name = "IPGroupBox";
            this.IPGroupBox.Size = new System.Drawing.Size(316, 295);
            this.IPGroupBox.TabIndex = 0;
            this.IPGroupBox.TabStop = false;
            this.IPGroupBox.Text = "IPs";
            // 
            // AddButton
            // 
            this.AddButton.Location = new System.Drawing.Point(198, 266);
            this.AddButton.Name = "AddButton";
            this.AddButton.Size = new System.Drawing.Size(53, 23);
            this.AddButton.TabIndex = 4;
            this.AddButton.Text = "Add";
            this.AddButton.UseVisualStyleBackColor = true;
            this.AddButton.Click += new System.EventHandler(this.AddButton_Click);
            // 
            // DeleteButton
            // 
            this.DeleteButton.Location = new System.Drawing.Point(257, 266);
            this.DeleteButton.Name = "DeleteButton";
            this.DeleteButton.Size = new System.Drawing.Size(53, 23);
            this.DeleteButton.TabIndex = 3;
            this.DeleteButton.Text = "Delete";
            this.DeleteButton.UseVisualStyleBackColor = true;
            this.DeleteButton.Click += new System.EventHandler(this.DeleteButton_Click);
            // 
            // PrefixComboBox
            // 
            this.PrefixComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.PrefixComboBox.FormattingEnabled = true;
            this.PrefixComboBox.IntegralHeight = false;
            this.PrefixComboBox.Location = new System.Drawing.Point(271, 235);
            this.PrefixComboBox.MaxDropDownItems = 4;
            this.PrefixComboBox.Name = "PrefixComboBox";
            this.PrefixComboBox.Size = new System.Drawing.Size(39, 25);
            this.PrefixComboBox.TabIndex = 2;
            // 
            // IPTextBox
            // 
            this.IPTextBox.Location = new System.Drawing.Point(6, 236);
            this.IPTextBox.Name = "IPTextBox";
            this.IPTextBox.Size = new System.Drawing.Size(259, 23);
            this.IPTextBox.TabIndex = 1;
            // 
            // IPListBox
            // 
            this.IPListBox.FormattingEnabled = true;
            this.IPListBox.ItemHeight = 17;
            this.IPListBox.Location = new System.Drawing.Point(6, 22);
            this.IPListBox.Name = "IPListBox";
            this.IPListBox.Size = new System.Drawing.Size(304, 208);
            this.IPListBox.TabIndex = 0;
            // 
            // ControlButton
            // 
            this.ControlButton.Location = new System.Drawing.Point(253, 313);
            this.ControlButton.Name = "ControlButton";
            this.ControlButton.Size = new System.Drawing.Size(75, 23);
            this.ControlButton.TabIndex = 1;
            this.ControlButton.Text = "Save";
            this.ControlButton.UseVisualStyleBackColor = true;
            this.ControlButton.Click += new System.EventHandler(this.ControlButton_Click);
            // 
            // GlobalBypassIPForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(340, 348);
            this.Controls.Add(this.ControlButton);
            this.Controls.Add(this.IPGroupBox);
            this.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.MaximizeBox = false;
            this.Name = "GlobalBypassIPForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Global Bypass IPs";
            this.Load += new System.EventHandler(this.GlobalBypassIPForm_Load);
            this.IPGroupBox.ResumeLayout(false);
            this.IPGroupBox.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox IPGroupBox;
        private System.Windows.Forms.ListBox IPListBox;
        private System.Windows.Forms.ComboBox PrefixComboBox;
        private System.Windows.Forms.TextBox IPTextBox;
        private System.Windows.Forms.Button DeleteButton;
        private System.Windows.Forms.Button AddButton;
        private System.Windows.Forms.Button ControlButton;
    }
}