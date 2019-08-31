namespace Netch.Forms
{
    partial class MainForm
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.ToolStrip = new System.Windows.Forms.ToolStrip();
            this.ServerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ImportServersFromClipboardToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.AddSocks5ServerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.AddShadowsocksServerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.AddShadowsocksRServerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.AddVMessServerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ModeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.CreateProcessModeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.SubscribeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ManageSubscribeLinksToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.UpdateServersFromSubscribeLinksToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.OptionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ReloadModesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.RestartServiceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.UninstallServiceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.AboutToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.VersionLabel = new System.Windows.Forms.ToolStripLabel();
            this.ConfigurationGroupBox = new System.Windows.Forms.GroupBox();
            this.SpeedPictureBox = new System.Windows.Forms.PictureBox();
            this.DeletePictureBox = new System.Windows.Forms.PictureBox();
            this.EditPictureBox = new System.Windows.Forms.PictureBox();
            this.ModeLabel = new System.Windows.Forms.Label();
            this.ModeComboBox = new System.Windows.Forms.ComboBox();
            this.ServerComboBox = new System.Windows.Forms.ComboBox();
            this.ServerLabel = new System.Windows.Forms.Label();
            this.StatusStrip = new System.Windows.Forms.StatusStrip();
            this.UsedBandwidthLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.UploadSpeedLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.DownloadSpeedLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.StatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.ControlButton = new System.Windows.Forms.Button();
            this.NotifyIcon = new System.Windows.Forms.NotifyIcon(this.components);
            this.NotifyMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.ShowMainFormToolStripButton = new System.Windows.Forms.ToolStripMenuItem();
            this.ExitToolStripButton = new System.Windows.Forms.ToolStripMenuItem();
            this.SettingsButton = new System.Windows.Forms.Button();
            this.ToolStrip.SuspendLayout();
            this.ConfigurationGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.SpeedPictureBox)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.DeletePictureBox)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.EditPictureBox)).BeginInit();
            this.StatusStrip.SuspendLayout();
            this.NotifyMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // ToolStrip
            // 
            this.ToolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.ToolStrip.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.ToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ServerToolStripMenuItem,
            this.ModeToolStripMenuItem,
            this.SubscribeToolStripMenuItem,
            this.OptionsToolStripMenuItem,
            this.AboutToolStripButton,
            this.VersionLabel});
            this.ToolStrip.Location = new System.Drawing.Point(0, 0);
            this.ToolStrip.Name = "ToolStrip";
            this.ToolStrip.RenderMode = System.Windows.Forms.ToolStripRenderMode.Professional;
            this.ToolStrip.Size = new System.Drawing.Size(608, 25);
            this.ToolStrip.TabIndex = 0;
            // 
            // ServerToolStripMenuItem
            // 
            this.ServerToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ImportServersFromClipboardToolStripMenuItem,
            this.AddSocks5ServerToolStripMenuItem,
            this.AddShadowsocksServerToolStripMenuItem,
            this.AddShadowsocksRServerToolStripMenuItem,
            this.AddVMessServerToolStripMenuItem});
            this.ServerToolStripMenuItem.Margin = new System.Windows.Forms.Padding(3, 0, 0, 1);
            this.ServerToolStripMenuItem.Name = "ServerToolStripMenuItem";
            this.ServerToolStripMenuItem.Size = new System.Drawing.Size(57, 24);
            this.ServerToolStripMenuItem.Text = "Server";
            // 
            // ImportServersFromClipboardToolStripMenuItem
            // 
            this.ImportServersFromClipboardToolStripMenuItem.Name = "ImportServersFromClipboardToolStripMenuItem";
            this.ImportServersFromClipboardToolStripMenuItem.Size = new System.Drawing.Size(259, 22);
            this.ImportServersFromClipboardToolStripMenuItem.Text = "Import Servers From Clipboard";
            this.ImportServersFromClipboardToolStripMenuItem.Click += new System.EventHandler(this.ImportServersFromClipboardToolStripMenuItem_Click);
            // 
            // AddSocks5ServerToolStripMenuItem
            // 
            this.AddSocks5ServerToolStripMenuItem.Name = "AddSocks5ServerToolStripMenuItem";
            this.AddSocks5ServerToolStripMenuItem.Size = new System.Drawing.Size(259, 22);
            this.AddSocks5ServerToolStripMenuItem.Text = "Add [Socks5] Server";
            this.AddSocks5ServerToolStripMenuItem.Click += new System.EventHandler(this.AddSocks5ServerToolStripMenuItem_Click);
            // 
            // AddShadowsocksServerToolStripMenuItem
            // 
            this.AddShadowsocksServerToolStripMenuItem.Name = "AddShadowsocksServerToolStripMenuItem";
            this.AddShadowsocksServerToolStripMenuItem.Size = new System.Drawing.Size(259, 22);
            this.AddShadowsocksServerToolStripMenuItem.Text = "Add [Shadowsocks] Server";
            this.AddShadowsocksServerToolStripMenuItem.Click += new System.EventHandler(this.AddShadowsocksServerToolStripMenuItem_Click);
            // 
            // AddShadowsocksRServerToolStripMenuItem
            // 
            this.AddShadowsocksRServerToolStripMenuItem.Name = "AddShadowsocksRServerToolStripMenuItem";
            this.AddShadowsocksRServerToolStripMenuItem.Size = new System.Drawing.Size(259, 22);
            this.AddShadowsocksRServerToolStripMenuItem.Text = "Add [ShadowsocksR] Server";
            this.AddShadowsocksRServerToolStripMenuItem.Click += new System.EventHandler(this.AddShadowsocksRServerToolStripMenuItem_Click);
            // 
            // AddVMessServerToolStripMenuItem
            // 
            this.AddVMessServerToolStripMenuItem.Name = "AddVMessServerToolStripMenuItem";
            this.AddVMessServerToolStripMenuItem.Size = new System.Drawing.Size(259, 22);
            this.AddVMessServerToolStripMenuItem.Text = "Add [VMess] Server";
            this.AddVMessServerToolStripMenuItem.Click += new System.EventHandler(this.AddVMessServerToolStripMenuItem_Click);
            // 
            // ModeToolStripMenuItem
            // 
            this.ModeToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.CreateProcessModeToolStripMenuItem});
            this.ModeToolStripMenuItem.Margin = new System.Windows.Forms.Padding(0, 0, 0, 1);
            this.ModeToolStripMenuItem.Name = "ModeToolStripMenuItem";
            this.ModeToolStripMenuItem.Size = new System.Drawing.Size(55, 24);
            this.ModeToolStripMenuItem.Text = "Mode";
            // 
            // CreateProcessModeToolStripMenuItem
            // 
            this.CreateProcessModeToolStripMenuItem.Name = "CreateProcessModeToolStripMenuItem";
            this.CreateProcessModeToolStripMenuItem.Size = new System.Drawing.Size(202, 22);
            this.CreateProcessModeToolStripMenuItem.Text = "Create Process Mode";
            this.CreateProcessModeToolStripMenuItem.Click += new System.EventHandler(this.CreateProcessModeToolStripButton_Click);
            // 
            // SubscribeToolStripMenuItem
            // 
            this.SubscribeToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ManageSubscribeLinksToolStripMenuItem,
            this.UpdateServersFromSubscribeLinksToolStripMenuItem});
            this.SubscribeToolStripMenuItem.Margin = new System.Windows.Forms.Padding(0, 0, 0, 1);
            this.SubscribeToolStripMenuItem.Name = "SubscribeToolStripMenuItem";
            this.SubscribeToolStripMenuItem.Size = new System.Drawing.Size(77, 24);
            this.SubscribeToolStripMenuItem.Text = "Subscribe";
            // 
            // ManageSubscribeLinksToolStripMenuItem
            // 
            this.ManageSubscribeLinksToolStripMenuItem.Name = "ManageSubscribeLinksToolStripMenuItem";
            this.ManageSubscribeLinksToolStripMenuItem.Size = new System.Drawing.Size(294, 22);
            this.ManageSubscribeLinksToolStripMenuItem.Text = "Manage Subscribe Links";
            this.ManageSubscribeLinksToolStripMenuItem.Click += new System.EventHandler(this.ManageSubscribeLinksToolStripMenuItem_Click);
            // 
            // UpdateServersFromSubscribeLinksToolStripMenuItem
            // 
            this.UpdateServersFromSubscribeLinksToolStripMenuItem.Name = "UpdateServersFromSubscribeLinksToolStripMenuItem";
            this.UpdateServersFromSubscribeLinksToolStripMenuItem.Size = new System.Drawing.Size(294, 22);
            this.UpdateServersFromSubscribeLinksToolStripMenuItem.Text = "Update Servers From Subscribe Links";
            this.UpdateServersFromSubscribeLinksToolStripMenuItem.Click += new System.EventHandler(this.UpdateServersFromSubscribeLinksToolStripMenuItem_Click);
            // 
            // OptionsToolStripMenuItem
            // 
            this.OptionsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ReloadModesToolStripMenuItem,
            this.RestartServiceToolStripMenuItem,
            this.UninstallServiceToolStripMenuItem});
            this.OptionsToolStripMenuItem.Margin = new System.Windows.Forms.Padding(0, 0, 0, 1);
            this.OptionsToolStripMenuItem.Name = "OptionsToolStripMenuItem";
            this.OptionsToolStripMenuItem.Size = new System.Drawing.Size(66, 24);
            this.OptionsToolStripMenuItem.Text = "Options";
            // 
            // ReloadModesToolStripMenuItem
            // 
            this.ReloadModesToolStripMenuItem.Name = "ReloadModesToolStripMenuItem";
            this.ReloadModesToolStripMenuItem.Size = new System.Drawing.Size(170, 22);
            this.ReloadModesToolStripMenuItem.Text = "Reload Modes";
            this.ReloadModesToolStripMenuItem.Click += new System.EventHandler(this.ReloadModesToolStripMenuItem_Click);
            // 
            // RestartServiceToolStripMenuItem
            // 
            this.RestartServiceToolStripMenuItem.Name = "RestartServiceToolStripMenuItem";
            this.RestartServiceToolStripMenuItem.Size = new System.Drawing.Size(170, 22);
            this.RestartServiceToolStripMenuItem.Text = "Restart Service";
            this.RestartServiceToolStripMenuItem.Click += new System.EventHandler(this.RestartServiceToolStripMenuItem_Click);
            // 
            // UninstallServiceToolStripMenuItem
            // 
            this.UninstallServiceToolStripMenuItem.Name = "UninstallServiceToolStripMenuItem";
            this.UninstallServiceToolStripMenuItem.Size = new System.Drawing.Size(170, 22);
            this.UninstallServiceToolStripMenuItem.Text = "Uninstall Service";
            this.UninstallServiceToolStripMenuItem.Click += new System.EventHandler(this.UninstallServiceToolStripMenuItem_Click);
            // 
            // AboutToolStripButton
            // 
            this.AboutToolStripButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.AboutToolStripButton.AutoToolTip = false;
            this.AboutToolStripButton.Margin = new System.Windows.Forms.Padding(0, 0, 3, 1);
            this.AboutToolStripButton.Name = "AboutToolStripButton";
            this.AboutToolStripButton.Size = new System.Drawing.Size(47, 24);
            this.AboutToolStripButton.Text = "About";
            this.AboutToolStripButton.Click += new System.EventHandler(this.AboutToolStripButton_Click);
            // 
            // VersionLabel
            // 
            this.VersionLabel.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.VersionLabel.BackColor = System.Drawing.Color.Transparent;
            this.VersionLabel.ForeColor = System.Drawing.Color.Red;
            this.VersionLabel.IsLink = true;
            this.VersionLabel.LinkBehavior = System.Windows.Forms.LinkBehavior.NeverUnderline;
            this.VersionLabel.Name = "VersionLabel";
            this.VersionLabel.Size = new System.Drawing.Size(83, 22);
            this.VersionLabel.Text = "1.2.7-STABLE";
            this.VersionLabel.Click += new System.EventHandler(this.VersionLabel_Click);
            // 
            // ConfigurationGroupBox
            // 
            this.ConfigurationGroupBox.Controls.Add(this.SpeedPictureBox);
            this.ConfigurationGroupBox.Controls.Add(this.DeletePictureBox);
            this.ConfigurationGroupBox.Controls.Add(this.EditPictureBox);
            this.ConfigurationGroupBox.Controls.Add(this.ModeLabel);
            this.ConfigurationGroupBox.Controls.Add(this.ModeComboBox);
            this.ConfigurationGroupBox.Controls.Add(this.ServerComboBox);
            this.ConfigurationGroupBox.Controls.Add(this.ServerLabel);
            this.ConfigurationGroupBox.Location = new System.Drawing.Point(12, 28);
            this.ConfigurationGroupBox.Name = "ConfigurationGroupBox";
            this.ConfigurationGroupBox.Size = new System.Drawing.Size(584, 86);
            this.ConfigurationGroupBox.TabIndex = 1;
            this.ConfigurationGroupBox.TabStop = false;
            this.ConfigurationGroupBox.Text = "Configuration";
            // 
            // SpeedPictureBox
            // 
            this.SpeedPictureBox.Cursor = System.Windows.Forms.Cursors.Hand;
            this.SpeedPictureBox.Image = ((System.Drawing.Image)(resources.GetObject("SpeedPictureBox.Image")));
            this.SpeedPictureBox.Location = new System.Drawing.Point(562, 26);
            this.SpeedPictureBox.Name = "SpeedPictureBox";
            this.SpeedPictureBox.Size = new System.Drawing.Size(16, 16);
            this.SpeedPictureBox.TabIndex = 9;
            this.SpeedPictureBox.TabStop = false;
            this.SpeedPictureBox.Click += new System.EventHandler(this.SpeedPictureBox_Click);
            // 
            // DeletePictureBox
            // 
            this.DeletePictureBox.Cursor = System.Windows.Forms.Cursors.Hand;
            this.DeletePictureBox.Image = ((System.Drawing.Image)(resources.GetObject("DeletePictureBox.Image")));
            this.DeletePictureBox.Location = new System.Drawing.Point(540, 26);
            this.DeletePictureBox.Name = "DeletePictureBox";
            this.DeletePictureBox.Size = new System.Drawing.Size(16, 16);
            this.DeletePictureBox.TabIndex = 8;
            this.DeletePictureBox.TabStop = false;
            this.DeletePictureBox.Click += new System.EventHandler(this.DeletePictureBox_Click);
            // 
            // EditPictureBox
            // 
            this.EditPictureBox.Cursor = System.Windows.Forms.Cursors.Hand;
            this.EditPictureBox.Image = ((System.Drawing.Image)(resources.GetObject("EditPictureBox.Image")));
            this.EditPictureBox.Location = new System.Drawing.Point(518, 26);
            this.EditPictureBox.Name = "EditPictureBox";
            this.EditPictureBox.Size = new System.Drawing.Size(16, 16);
            this.EditPictureBox.TabIndex = 7;
            this.EditPictureBox.TabStop = false;
            this.EditPictureBox.Click += new System.EventHandler(this.EditPictureBox_Click);
            // 
            // ModeLabel
            // 
            this.ModeLabel.AutoSize = true;
            this.ModeLabel.Location = new System.Drawing.Point(6, 56);
            this.ModeLabel.Name = "ModeLabel";
            this.ModeLabel.Size = new System.Drawing.Size(43, 17);
            this.ModeLabel.TabIndex = 3;
            this.ModeLabel.Text = "Mode";
            // 
            // ModeComboBox
            // 
            this.ModeComboBox.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.ModeComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ModeComboBox.FormattingEnabled = true;
            this.ModeComboBox.IntegralHeight = false;
            this.ModeComboBox.Location = new System.Drawing.Point(57, 53);
            this.ModeComboBox.Name = "ModeComboBox";
            this.ModeComboBox.Size = new System.Drawing.Size(455, 24);
            this.ModeComboBox.TabIndex = 2;
            this.ModeComboBox.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.ComboBox_DrawItem);
            // 
            // ServerComboBox
            // 
            this.ServerComboBox.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.ServerComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ServerComboBox.FormattingEnabled = true;
            this.ServerComboBox.IntegralHeight = false;
            this.ServerComboBox.Location = new System.Drawing.Point(57, 22);
            this.ServerComboBox.MaxDropDownItems = 16;
            this.ServerComboBox.Name = "ServerComboBox";
            this.ServerComboBox.Size = new System.Drawing.Size(455, 24);
            this.ServerComboBox.TabIndex = 1;
            this.ServerComboBox.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.ComboBox_DrawItem);
            // 
            // ServerLabel
            // 
            this.ServerLabel.AutoSize = true;
            this.ServerLabel.Location = new System.Drawing.Point(6, 26);
            this.ServerLabel.Name = "ServerLabel";
            this.ServerLabel.Size = new System.Drawing.Size(45, 17);
            this.ServerLabel.TabIndex = 0;
            this.ServerLabel.Text = "Server";
            // 
            // StatusStrip
            // 
            this.StatusStrip.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.StatusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.UsedBandwidthLabel,
            this.UploadSpeedLabel,
            this.DownloadSpeedLabel,
            this.StatusLabel});
            this.StatusStrip.Location = new System.Drawing.Point(0, 154);
            this.StatusStrip.Name = "StatusStrip";
            this.StatusStrip.Size = new System.Drawing.Size(608, 22);
            this.StatusStrip.SizingGrip = false;
            this.StatusStrip.TabIndex = 2;
            // 
            // UsedBandwidthLabel
            // 
            this.UsedBandwidthLabel.Name = "UsedBandwidthLabel";
            this.UsedBandwidthLabel.Size = new System.Drawing.Size(72, 17);
            this.UsedBandwidthLabel.Text = "Used: 0 KB";
            this.UsedBandwidthLabel.Visible = false;
            // 
            // UploadSpeedLabel
            // 
            this.UploadSpeedLabel.Name = "UploadSpeedLabel";
            this.UploadSpeedLabel.Size = new System.Drawing.Size(59, 17);
            this.UploadSpeedLabel.Text = "↑: 0 KB/s";
            this.UploadSpeedLabel.Visible = false;
            // 
            // DownloadSpeedLabel
            // 
            this.DownloadSpeedLabel.Name = "DownloadSpeedLabel";
            this.DownloadSpeedLabel.Size = new System.Drawing.Size(59, 17);
            this.DownloadSpeedLabel.Text = "↓: 0 KB/s";
            this.DownloadSpeedLabel.Visible = false;
            // 
            // StatusLabel
            // 
            this.StatusLabel.BackColor = System.Drawing.Color.Transparent;
            this.StatusLabel.Name = "StatusLabel";
            this.StatusLabel.Size = new System.Drawing.Size(177, 17);
            this.StatusLabel.Text = "Status: Waiting for command";
            // 
            // ControlButton
            // 
            this.ControlButton.Location = new System.Drawing.Point(521, 120);
            this.ControlButton.Name = "ControlButton";
            this.ControlButton.Size = new System.Drawing.Size(75, 27);
            this.ControlButton.TabIndex = 3;
            this.ControlButton.Text = "Start";
            this.ControlButton.UseVisualStyleBackColor = true;
            this.ControlButton.Click += new System.EventHandler(this.ControlButton_Click);
            // 
            // NotifyIcon
            // 
            this.NotifyIcon.ContextMenuStrip = this.NotifyMenu;
            this.NotifyIcon.Icon = ((System.Drawing.Icon)(resources.GetObject("NotifyIcon.Icon")));
            this.NotifyIcon.Text = "Netch";
            this.NotifyIcon.Visible = true;
            this.NotifyIcon.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.NotifyIcon_MouseDoubleClick);
            // 
            // NotifyMenu
            // 
            this.NotifyMenu.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.NotifyMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ShowMainFormToolStripButton,
            this.ExitToolStripButton});
            this.NotifyMenu.Name = "NotifyMenu";
            this.NotifyMenu.ShowItemToolTips = false;
            this.NotifyMenu.Size = new System.Drawing.Size(108, 48);
            // 
            // ShowMainFormToolStripButton
            // 
            this.ShowMainFormToolStripButton.Name = "ShowMainFormToolStripButton";
            this.ShowMainFormToolStripButton.Size = new System.Drawing.Size(107, 22);
            this.ShowMainFormToolStripButton.Text = "Show";
            this.ShowMainFormToolStripButton.Click += new System.EventHandler(this.ShowMainFormToolStripButton_Click);
            // 
            // ExitToolStripButton
            // 
            this.ExitToolStripButton.Name = "ExitToolStripButton";
            this.ExitToolStripButton.Size = new System.Drawing.Size(107, 22);
            this.ExitToolStripButton.Text = "Exit";
            this.ExitToolStripButton.Click += new System.EventHandler(this.ExitToolStripButton_Click);
            // 
            // SettingsButton
            // 
            this.SettingsButton.Location = new System.Drawing.Point(12, 120);
            this.SettingsButton.Name = "SettingsButton";
            this.SettingsButton.Size = new System.Drawing.Size(72, 24);
            this.SettingsButton.TabIndex = 4;
            this.SettingsButton.Text = "Settings";
            this.SettingsButton.UseVisualStyleBackColor = true;
            this.SettingsButton.Click += new System.EventHandler(this.SettingsButton_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(608, 176);
            this.Controls.Add(this.SettingsButton);
            this.Controls.Add(this.ControlButton);
            this.Controls.Add(this.StatusStrip);
            this.Controls.Add(this.ConfigurationGroupBox);
            this.Controls.Add(this.ToolStrip);
            this.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Netch";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.ToolStrip.ResumeLayout(false);
            this.ToolStrip.PerformLayout();
            this.ConfigurationGroupBox.ResumeLayout(false);
            this.ConfigurationGroupBox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.SpeedPictureBox)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.DeletePictureBox)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.EditPictureBox)).EndInit();
            this.StatusStrip.ResumeLayout(false);
            this.StatusStrip.PerformLayout();
            this.NotifyMenu.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ToolStrip ToolStrip;
        private System.Windows.Forms.ToolStripMenuItem ServerToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem SubscribeToolStripMenuItem;
        private System.Windows.Forms.ToolStripLabel VersionLabel;
        private System.Windows.Forms.GroupBox ConfigurationGroupBox;
        private System.Windows.Forms.Label ServerLabel;
        private System.Windows.Forms.Label ModeLabel;
        private System.Windows.Forms.ComboBox ModeComboBox;
        private System.Windows.Forms.ComboBox ServerComboBox;
        private System.Windows.Forms.StatusStrip StatusStrip;
        private System.Windows.Forms.ToolStripStatusLabel StatusLabel;
        private System.Windows.Forms.Button ControlButton;
        private System.Windows.Forms.ToolStripMenuItem AddSocks5ServerToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem AddShadowsocksServerToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem AddShadowsocksRServerToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem AddVMessServerToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem ImportServersFromClipboardToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem ManageSubscribeLinksToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem UpdateServersFromSubscribeLinksToolStripMenuItem;
        private System.Windows.Forms.PictureBox SpeedPictureBox;
        private System.Windows.Forms.PictureBox DeletePictureBox;
        private System.Windows.Forms.PictureBox EditPictureBox;
        private System.Windows.Forms.ToolStripMenuItem RestartServiceToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem UninstallServiceToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem OptionsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem ModeToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem CreateProcessModeToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem ReloadModesToolStripMenuItem;
        private System.Windows.Forms.NotifyIcon NotifyIcon;
        private System.Windows.Forms.ContextMenuStrip NotifyMenu;
        private System.Windows.Forms.ToolStripMenuItem ShowMainFormToolStripButton;
        private System.Windows.Forms.ToolStripMenuItem ExitToolStripButton;
        private System.Windows.Forms.Button SettingsButton;
        private System.Windows.Forms.ToolStripButton AboutToolStripButton;
        private System.Windows.Forms.ToolStripStatusLabel UsedBandwidthLabel;
        private System.Windows.Forms.ToolStripStatusLabel UploadSpeedLabel;
        private System.Windows.Forms.ToolStripStatusLabel DownloadSpeedLabel;
    }
}