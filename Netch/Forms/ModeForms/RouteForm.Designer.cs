using System.ComponentModel;
using Netch.Properties;

namespace Netch.Forms.ModeForms
{
    partial class RouteForm
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
            this.HandleTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.HandleLabel = new System.Windows.Forms.Label();
            this.HandleContentTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.HandleRuleRichTextBox = new System.Windows.Forms.RichTextBox();
            this.BypassTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.BypassLabel = new System.Windows.Forms.Label();
            this.BypassContentTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.BypassRuleRichTextBox = new System.Windows.Forms.RichTextBox();
            this.HandleDNSCheckBox = new Netch.Forms.SyncGlobalCheckBox();
            this.DNSLabel = new System.Windows.Forms.Label();
            this.DNSTextBox = new System.Windows.Forms.TextBox();
            this.UseCustomDNSCheckBox = new System.Windows.Forms.CheckBox();
            this.RuleTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.OptionsGroupBox = new System.Windows.Forms.GroupBox();
            this.RemarkLabel = new System.Windows.Forms.Label();
            this.RemarkTextBox = new System.Windows.Forms.TextBox();
            this.FilenameLabel = new System.Windows.Forms.Label();
            this.FilenameTextBox = new System.Windows.Forms.TextBox();
            this.ControlButton = new System.Windows.Forms.Button();
            this.NamePanel = new System.Windows.Forms.Panel();
            this.ConfigurationLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.ConfigurationGroupBox = new System.Windows.Forms.GroupBox();
            this.HandleTableLayoutPanel.SuspendLayout();
            this.HandleContentTableLayoutPanel.SuspendLayout();
            this.BypassTableLayoutPanel.SuspendLayout();
            this.BypassContentTableLayoutPanel.SuspendLayout();
            this.RuleTableLayoutPanel.SuspendLayout();
            this.OptionsGroupBox.SuspendLayout();
            this.NamePanel.SuspendLayout();
            this.ConfigurationLayoutPanel.SuspendLayout();
            this.ConfigurationGroupBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // HandleTableLayoutPanel
            // 
            this.HandleTableLayoutPanel.ColumnCount = 1;
            this.HandleTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.HandleTableLayoutPanel.Controls.Add(this.HandleLabel, 0, 0);
            this.HandleTableLayoutPanel.Controls.Add(this.HandleContentTableLayoutPanel, 0, 1);
            this.HandleTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.HandleTableLayoutPanel.Location = new System.Drawing.Point(3, 3);
            this.HandleTableLayoutPanel.Name = "HandleTableLayoutPanel";
            this.HandleTableLayoutPanel.RowCount = 2;
            this.HandleTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.HandleTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.HandleTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.HandleTableLayoutPanel.Size = new System.Drawing.Size(455, 424);
            this.HandleTableLayoutPanel.TabIndex = 1;
            // 
            // HandleLabel
            // 
            this.HandleLabel.AutoSize = true;
            this.HandleLabel.Location = new System.Drawing.Point(3, 0);
            this.HandleLabel.Name = "HandleLabel";
            this.HandleLabel.Size = new System.Drawing.Size(81, 17);
            this.HandleLabel.TabIndex = 0;
            this.HandleLabel.Text = "Handle rules";
            // 
            // HandleContentTableLayoutPanel
            // 
            this.HandleContentTableLayoutPanel.ColumnCount = 1;
            this.HandleContentTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.HandleContentTableLayoutPanel.Controls.Add(this.HandleRuleRichTextBox, 0, 0);
            this.HandleContentTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.HandleContentTableLayoutPanel.Location = new System.Drawing.Point(3, 20);
            this.HandleContentTableLayoutPanel.Name = "HandleContentTableLayoutPanel";
            this.HandleContentTableLayoutPanel.RowCount = 1;
            this.HandleContentTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.HandleContentTableLayoutPanel.Size = new System.Drawing.Size(449, 401);
            this.HandleContentTableLayoutPanel.TabIndex = 1;
            // 
            // HandleRuleRichTextBox
            // 
            this.HandleRuleRichTextBox.DetectUrls = false;
            this.HandleRuleRichTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.HandleRuleRichTextBox.Location = new System.Drawing.Point(3, 3);
            this.HandleRuleRichTextBox.Name = "HandleRuleRichTextBox";
            this.HandleRuleRichTextBox.Size = new System.Drawing.Size(443, 395);
            this.HandleRuleRichTextBox.TabIndex = 0;
            this.HandleRuleRichTextBox.Text = "";
            this.HandleRuleRichTextBox.WordWrap = false;
            // 
            // BypassTableLayoutPanel
            // 
            this.BypassTableLayoutPanel.ColumnCount = 1;
            this.BypassTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.BypassTableLayoutPanel.Controls.Add(this.BypassLabel, 0, 0);
            this.BypassTableLayoutPanel.Controls.Add(this.BypassContentTableLayoutPanel, 0, 1);
            this.BypassTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.BypassTableLayoutPanel.Location = new System.Drawing.Point(464, 3);
            this.BypassTableLayoutPanel.Name = "BypassTableLayoutPanel";
            this.BypassTableLayoutPanel.RowCount = 2;
            this.BypassTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.BypassTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.BypassTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.BypassTableLayoutPanel.Size = new System.Drawing.Size(455, 424);
            this.BypassTableLayoutPanel.TabIndex = 2;
            // 
            // BypassLabel
            // 
            this.BypassLabel.AutoSize = true;
            this.BypassLabel.Location = new System.Drawing.Point(3, 0);
            this.BypassLabel.Name = "BypassLabel";
            this.BypassLabel.Size = new System.Drawing.Size(81, 17);
            this.BypassLabel.TabIndex = 0;
            this.BypassLabel.Text = "Bypass rules";
            // 
            // BypassContentTableLayoutPanel
            // 
            this.BypassContentTableLayoutPanel.ColumnCount = 1;
            this.BypassContentTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.BypassContentTableLayoutPanel.Controls.Add(this.BypassRuleRichTextBox, 0, 0);
            this.BypassContentTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.BypassContentTableLayoutPanel.Location = new System.Drawing.Point(3, 20);
            this.BypassContentTableLayoutPanel.Name = "BypassContentTableLayoutPanel";
            this.BypassContentTableLayoutPanel.RowCount = 1;
            this.BypassContentTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.BypassContentTableLayoutPanel.Size = new System.Drawing.Size(449, 401);
            this.BypassContentTableLayoutPanel.TabIndex = 1;
            // 
            // BypassRuleRichTextBox
            // 
            this.BypassRuleRichTextBox.DetectUrls = false;
            this.BypassRuleRichTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.BypassRuleRichTextBox.Location = new System.Drawing.Point(3, 3);
            this.BypassRuleRichTextBox.Name = "BypassRuleRichTextBox";
            this.BypassRuleRichTextBox.Size = new System.Drawing.Size(443, 395);
            this.BypassRuleRichTextBox.TabIndex = 0;
            this.BypassRuleRichTextBox.Text = "";
            this.BypassRuleRichTextBox.WordWrap = false;
            // 
            // HandleDNSCheckBox
            // 
            this.HandleDNSCheckBox.AutoCheck = false;
            this.HandleDNSCheckBox.AutoSize = true;
            this.HandleDNSCheckBox.BackColor = System.Drawing.Color.Yellow;
            this.HandleDNSCheckBox.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point);
            this.HandleDNSCheckBox.GlobalValue = false;
            this.HandleDNSCheckBox.Location = new System.Drawing.Point(8, 24);
            this.HandleDNSCheckBox.Name = "HandleDNSCheckBox";
            this.HandleDNSCheckBox.Size = new System.Drawing.Size(203, 21);
            this.HandleDNSCheckBox.SyncGlobal = false;
            this.HandleDNSCheckBox.TabIndex = 2;
            this.HandleDNSCheckBox.Text = "Handle DNS (DNS hijacking)";
            this.HandleDNSCheckBox.ThreeState = true;
            this.HandleDNSCheckBox.UseVisualStyleBackColor = true;
            this.HandleDNSCheckBox.Value = false;
            // 
            // DNSLabel
            // 
            this.DNSLabel.AutoSize = true;
            this.DNSLabel.Location = new System.Drawing.Point(176, 56);
            this.DNSLabel.Name = "DNSLabel";
            this.DNSLabel.Size = new System.Drawing.Size(34, 17);
            this.DNSLabel.TabIndex = 3;
            this.DNSLabel.Text = "DNS";
            // 
            // DNSTextBox
            // 
            this.DNSTextBox.DataBindings.Add(new System.Windows.Forms.Binding("Enabled", this.UseCustomDNSCheckBox, "Checked", true));
            this.DNSTextBox.Location = new System.Drawing.Point(224, 56);
            this.DNSTextBox.Name = "DNSTextBox";
            this.DNSTextBox.Size = new System.Drawing.Size(184, 23);
            this.DNSTextBox.TabIndex = 4;
            this.DNSTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // UseCustomDNSCheckBox
            // 
            this.UseCustomDNSCheckBox.AutoSize = true;
            this.UseCustomDNSCheckBox.DataBindings.Add(new System.Windows.Forms.Binding("Enabled", this.HandleDNSCheckBox, "Checked", true));
            this.UseCustomDNSCheckBox.Location = new System.Drawing.Point(8, 56);
            this.UseCustomDNSCheckBox.Name = "UseCustomDNSCheckBox";
            this.UseCustomDNSCheckBox.Size = new System.Drawing.Size(125, 21);
            this.UseCustomDNSCheckBox.TabIndex = 7;
            this.UseCustomDNSCheckBox.Text = "Use custom DNS";
            this.UseCustomDNSCheckBox.ThreeState = true;
            this.UseCustomDNSCheckBox.UseVisualStyleBackColor = true;
            // 
            // RuleTableLayoutPanel
            // 
            this.RuleTableLayoutPanel.ColumnCount = 2;
            this.RuleTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.RuleTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.RuleTableLayoutPanel.Controls.Add(this.HandleTableLayoutPanel, 0, 0);
            this.RuleTableLayoutPanel.Controls.Add(this.BypassTableLayoutPanel, 1, 0);
            this.RuleTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.RuleTableLayoutPanel.Location = new System.Drawing.Point(3, 206);
            this.RuleTableLayoutPanel.Name = "RuleTableLayoutPanel";
            this.RuleTableLayoutPanel.RowCount = 1;
            this.RuleTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.RuleTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 430F));
            this.RuleTableLayoutPanel.Size = new System.Drawing.Size(922, 430);
            this.RuleTableLayoutPanel.TabIndex = 2;
            // 
            // OptionsGroupBox
            // 
            this.OptionsGroupBox.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.OptionsGroupBox.Controls.Add(this.HandleDNSCheckBox);
            this.OptionsGroupBox.Controls.Add(this.UseCustomDNSCheckBox);
            this.OptionsGroupBox.Controls.Add(this.DNSLabel);
            this.OptionsGroupBox.Controls.Add(this.DNSTextBox);
            this.OptionsGroupBox.Location = new System.Drawing.Point(15, 81);
            this.OptionsGroupBox.Name = "OptionsGroupBox";
            this.OptionsGroupBox.Size = new System.Drawing.Size(898, 119);
            this.OptionsGroupBox.TabIndex = 1;
            this.OptionsGroupBox.TabStop = false;
            this.OptionsGroupBox.Visible = false;
            // 
            // RemarkLabel
            // 
            this.RemarkLabel.AutoSize = true;
            this.RemarkLabel.Location = new System.Drawing.Point(8, 8);
            this.RemarkLabel.Name = "RemarkLabel";
            this.RemarkLabel.Size = new System.Drawing.Size(53, 17);
            this.RemarkLabel.TabIndex = 0;
            this.RemarkLabel.Text = "Remark";
            // 
            // RemarkTextBox
            // 
            this.RemarkTextBox.Location = new System.Drawing.Point(72, 8);
            this.RemarkTextBox.Name = "RemarkTextBox";
            this.RemarkTextBox.Size = new System.Drawing.Size(341, 23);
            this.RemarkTextBox.TabIndex = 1;
            this.RemarkTextBox.TextChanged += new System.EventHandler(this.RemarkTextBox_TextChanged);
            // 
            // FilenameLabel
            // 
            this.FilenameLabel.AutoSize = true;
            this.FilenameLabel.Location = new System.Drawing.Point(8, 40);
            this.FilenameLabel.Name = "FilenameLabel";
            this.FilenameLabel.Size = new System.Drawing.Size(59, 17);
            this.FilenameLabel.TabIndex = 2;
            this.FilenameLabel.Text = "Filename";
            // 
            // FilenameTextBox
            // 
            this.FilenameTextBox.Location = new System.Drawing.Point(72, 40);
            this.FilenameTextBox.Name = "FilenameTextBox";
            this.FilenameTextBox.ReadOnly = true;
            this.FilenameTextBox.Size = new System.Drawing.Size(341, 23);
            this.FilenameTextBox.TabIndex = 3;
            // 
            // ControlButton
            // 
            this.ControlButton.Location = new System.Drawing.Point(424, 40);
            this.ControlButton.Name = "ControlButton";
            this.ControlButton.Size = new System.Drawing.Size(75, 23);
            this.ControlButton.TabIndex = 4;
            this.ControlButton.Text = "Save";
            this.ControlButton.UseVisualStyleBackColor = true;
            this.ControlButton.Click += new System.EventHandler(this.ControlButton_Click);
            // 
            // NamePanel
            // 
            this.NamePanel.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.NamePanel.Controls.Add(this.RemarkLabel);
            this.NamePanel.Controls.Add(this.RemarkTextBox);
            this.NamePanel.Controls.Add(this.FilenameLabel);
            this.NamePanel.Controls.Add(this.FilenameTextBox);
            this.NamePanel.Controls.Add(this.ControlButton);
            this.NamePanel.Location = new System.Drawing.Point(208, 3);
            this.NamePanel.Name = "NamePanel";
            this.NamePanel.Size = new System.Drawing.Size(512, 72);
            this.NamePanel.TabIndex = 0;
            // 
            // ConfigurationLayoutPanel
            // 
            this.ConfigurationLayoutPanel.ColumnCount = 1;
            this.ConfigurationLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.ConfigurationLayoutPanel.Controls.Add(this.NamePanel, 0, 0);
            this.ConfigurationLayoutPanel.Controls.Add(this.OptionsGroupBox, 0, 1);
            this.ConfigurationLayoutPanel.Controls.Add(this.RuleTableLayoutPanel, 0, 2);
            this.ConfigurationLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ConfigurationLayoutPanel.Location = new System.Drawing.Point(3, 19);
            this.ConfigurationLayoutPanel.Name = "ConfigurationLayoutPanel";
            this.ConfigurationLayoutPanel.RowCount = 3;
            this.ConfigurationLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.ConfigurationLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.ConfigurationLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.ConfigurationLayoutPanel.Size = new System.Drawing.Size(928, 639);
            this.ConfigurationLayoutPanel.TabIndex = 0;
            // 
            // ConfigurationGroupBox
            // 
            this.ConfigurationGroupBox.Controls.Add(this.ConfigurationLayoutPanel);
            this.ConfigurationGroupBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ConfigurationGroupBox.Location = new System.Drawing.Point(0, 0);
            this.ConfigurationGroupBox.Name = "ConfigurationGroupBox";
            this.ConfigurationGroupBox.Size = new System.Drawing.Size(934, 661);
            this.ConfigurationGroupBox.TabIndex = 1;
            this.ConfigurationGroupBox.TabStop = false;
            this.ConfigurationGroupBox.Text = "Configuration";
            // 
            // RouteForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(934, 661);
            this.Controls.Add(this.ConfigurationGroupBox);
            this.MinimumSize = new System.Drawing.Size(950, 700);
            this.Name = "RouteForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Create Route Table Rule";
            this.Load += new System.EventHandler(this.Route_Load);
            this.HandleTableLayoutPanel.ResumeLayout(false);
            this.HandleTableLayoutPanel.PerformLayout();
            this.HandleContentTableLayoutPanel.ResumeLayout(false);
            this.BypassTableLayoutPanel.ResumeLayout(false);
            this.BypassTableLayoutPanel.PerformLayout();
            this.BypassContentTableLayoutPanel.ResumeLayout(false);
            this.RuleTableLayoutPanel.ResumeLayout(false);
            this.OptionsGroupBox.ResumeLayout(false);
            this.OptionsGroupBox.PerformLayout();
            this.NamePanel.ResumeLayout(false);
            this.NamePanel.PerformLayout();
            this.ConfigurationLayoutPanel.ResumeLayout(false);
            this.ConfigurationGroupBox.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel HandleTableLayoutPanel;
        private System.Windows.Forms.Label HandleLabel;
        private System.Windows.Forms.TableLayoutPanel HandleContentTableLayoutPanel;
        private System.Windows.Forms.RichTextBox HandleRuleRichTextBox;
        private System.Windows.Forms.TableLayoutPanel BypassTableLayoutPanel;
        private System.Windows.Forms.Label BypassLabel;
        private System.Windows.Forms.TableLayoutPanel BypassContentTableLayoutPanel;
        private System.Windows.Forms.RichTextBox BypassRuleRichTextBox;
        private SyncGlobalCheckBox HandleDNSCheckBox;
        private System.Windows.Forms.Label DNSLabel;
        private System.Windows.Forms.TextBox DNSTextBox;
        private System.Windows.Forms.TableLayoutPanel RuleTableLayoutPanel;
        private System.Windows.Forms.GroupBox OptionsGroupBox;
        private System.Windows.Forms.Label RemarkLabel;
        private System.Windows.Forms.TextBox RemarkTextBox;
        private System.Windows.Forms.Label FilenameLabel;
        private System.Windows.Forms.TextBox FilenameTextBox;
        private System.Windows.Forms.Panel NamePanel;
        private System.Windows.Forms.TableLayoutPanel ConfigurationLayoutPanel;
        private System.Windows.Forms.CheckBox UseCustomDNSCheckBox;
        private System.Windows.Forms.Button ControlButton;
        private System.Windows.Forms.GroupBox ConfigurationGroupBox;
    }
}