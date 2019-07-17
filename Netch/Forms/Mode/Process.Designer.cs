namespace Netch.Forms.Mode
{
    partial class Process
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Process));
            this.ConfigurationGroupBox = new System.Windows.Forms.GroupBox();
            this.ScanButton = new System.Windows.Forms.Button();
            this.ProcessGroupBox = new System.Windows.Forms.GroupBox();
            this.AddButton = new System.Windows.Forms.Button();
            this.ProcessNameTextBox = new System.Windows.Forms.TextBox();
            this.RuleListBox = new System.Windows.Forms.ListBox();
            this.RemarkTextBox = new System.Windows.Forms.TextBox();
            this.RemarkLabel = new System.Windows.Forms.Label();
            this.ControlButton = new System.Windows.Forms.Button();
            this.ConfigurationGroupBox.SuspendLayout();
            this.ProcessGroupBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // ConfigurationGroupBox
            // 
            this.ConfigurationGroupBox.Controls.Add(this.ScanButton);
            this.ConfigurationGroupBox.Controls.Add(this.ProcessGroupBox);
            this.ConfigurationGroupBox.Controls.Add(this.RuleListBox);
            this.ConfigurationGroupBox.Controls.Add(this.RemarkTextBox);
            this.ConfigurationGroupBox.Controls.Add(this.RemarkLabel);
            this.ConfigurationGroupBox.Location = new System.Drawing.Point(12, 12);
            this.ConfigurationGroupBox.Name = "ConfigurationGroupBox";
            this.ConfigurationGroupBox.Size = new System.Drawing.Size(327, 275);
            this.ConfigurationGroupBox.TabIndex = 0;
            this.ConfigurationGroupBox.TabStop = false;
            this.ConfigurationGroupBox.Text = "Configuration";
            // 
            // ScanButton
            // 
            this.ScanButton.Location = new System.Drawing.Point(6, 246);
            this.ScanButton.Name = "ScanButton";
            this.ScanButton.Size = new System.Drawing.Size(75, 23);
            this.ScanButton.TabIndex = 4;
            this.ScanButton.Text = "Scan";
            this.ScanButton.UseVisualStyleBackColor = true;
            this.ScanButton.Click += new System.EventHandler(this.ScanButton_Click);
            // 
            // ProcessGroupBox
            // 
            this.ProcessGroupBox.Controls.Add(this.AddButton);
            this.ProcessGroupBox.Controls.Add(this.ProcessNameTextBox);
            this.ProcessGroupBox.Location = new System.Drawing.Point(6, 194);
            this.ProcessGroupBox.Name = "ProcessGroupBox";
            this.ProcessGroupBox.Size = new System.Drawing.Size(315, 46);
            this.ProcessGroupBox.TabIndex = 3;
            this.ProcessGroupBox.TabStop = false;
            // 
            // AddButton
            // 
            this.AddButton.Location = new System.Drawing.Point(234, 15);
            this.AddButton.Name = "AddButton";
            this.AddButton.Size = new System.Drawing.Size(75, 23);
            this.AddButton.TabIndex = 1;
            this.AddButton.Text = "Add";
            this.AddButton.UseVisualStyleBackColor = true;
            this.AddButton.Click += new System.EventHandler(this.AddButton_Click);
            // 
            // ProcessNameTextBox
            // 
            this.ProcessNameTextBox.Location = new System.Drawing.Point(6, 15);
            this.ProcessNameTextBox.Name = "ProcessNameTextBox";
            this.ProcessNameTextBox.Size = new System.Drawing.Size(222, 23);
            this.ProcessNameTextBox.TabIndex = 0;
            // 
            // RuleListBox
            // 
            this.RuleListBox.FormattingEnabled = true;
            this.RuleListBox.ItemHeight = 17;
            this.RuleListBox.Location = new System.Drawing.Point(6, 48);
            this.RuleListBox.Name = "RuleListBox";
            this.RuleListBox.Size = new System.Drawing.Size(315, 140);
            this.RuleListBox.TabIndex = 2;
            // 
            // RemarkTextBox
            // 
            this.RemarkTextBox.Location = new System.Drawing.Point(71, 19);
            this.RemarkTextBox.Name = "RemarkTextBox";
            this.RemarkTextBox.Size = new System.Drawing.Size(250, 23);
            this.RemarkTextBox.TabIndex = 1;
            // 
            // RemarkLabel
            // 
            this.RemarkLabel.AutoSize = true;
            this.RemarkLabel.Location = new System.Drawing.Point(12, 22);
            this.RemarkLabel.Name = "RemarkLabel";
            this.RemarkLabel.Size = new System.Drawing.Size(53, 17);
            this.RemarkLabel.TabIndex = 0;
            this.RemarkLabel.Text = "Remark";
            // 
            // ControlButton
            // 
            this.ControlButton.Location = new System.Drawing.Point(264, 293);
            this.ControlButton.Name = "ControlButton";
            this.ControlButton.Size = new System.Drawing.Size(75, 23);
            this.ControlButton.TabIndex = 1;
            this.ControlButton.Text = "Save";
            this.ControlButton.UseVisualStyleBackColor = true;
            this.ControlButton.Click += new System.EventHandler(this.ControlButton_Click);
            // 
            // ModeForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(351, 327);
            this.Controls.Add(this.ControlButton);
            this.Controls.Add(this.ConfigurationGroupBox);
            this.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.MaximizeBox = false;
            this.Name = "ModeForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Fast Create Mode";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ModeForm_FormClosing);
            this.Load += new System.EventHandler(this.ModeForm_Load);
            this.ConfigurationGroupBox.ResumeLayout(false);
            this.ConfigurationGroupBox.PerformLayout();
            this.ProcessGroupBox.ResumeLayout(false);
            this.ProcessGroupBox.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox ConfigurationGroupBox;
        private System.Windows.Forms.Label RemarkLabel;
        private System.Windows.Forms.GroupBox ProcessGroupBox;
        private System.Windows.Forms.ListBox RuleListBox;
        private System.Windows.Forms.TextBox RemarkTextBox;
        private System.Windows.Forms.TextBox ProcessNameTextBox;
        private System.Windows.Forms.Button AddButton;
        private System.Windows.Forms.Button ScanButton;
        private System.Windows.Forms.Button ControlButton;
    }
}