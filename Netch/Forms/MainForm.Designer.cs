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
            this.MenuStrip = new System.Windows.Forms.MenuStrip();
            this.ServerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ImportServersFromClipboardToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ModeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.CreateProcessModeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ReloadModesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.SubscribeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ManageSubscribeLinksToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.UpdateServersFromSubscribeLinksToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.UpdateServersFromSubscribeLinksWithProxyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.OptionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.OpenDirectoryToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.CleanDNSCacheToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.UpdateACLToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.updateACLWithProxyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.UninstallServiceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.UninstallTapDriverToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.HelpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.CheckForUpdatesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.fAQToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.AboutToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.NewVersionLabel = new System.Windows.Forms.ToolStripLabel();
            this.VersionLabel = new System.Windows.Forms.ToolStripLabel();
            this.ConfigurationGroupBox = new System.Windows.Forms.GroupBox();
            this.configLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.ProfileLabel = new System.Windows.Forms.Label();
            this.ModeLabel = new System.Windows.Forms.Label();
            this.ServerLabel = new System.Windows.Forms.Label();
            this.ProfileNameText = new System.Windows.Forms.TextBox();
            this.ModeComboBox = new System.Windows.Forms.SearchComboBox();
            this.ServerComboBox = new System.Windows.Forms.ComboBox();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.EditServerPictureBox = new System.Windows.Forms.PictureBox();
            this.CopyLinkPictureBox = new System.Windows.Forms.PictureBox();
            this.DeleteServerPictureBox = new System.Windows.Forms.PictureBox();
            this.SpeedPictureBox = new System.Windows.Forms.PictureBox();
            this.tableLayoutPanel3 = new System.Windows.Forms.TableLayoutPanel();
            this.EditModePictureBox = new System.Windows.Forms.PictureBox();
            this.DeleteModePictureBox = new System.Windows.Forms.PictureBox();
            this.StatusStrip = new System.Windows.Forms.StatusStrip();
            this.StatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.UsedBandwidthLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.DownloadSpeedLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.UploadSpeedLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.blankToolStripStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.NatTypeStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.NatTypeStatusLightLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.ControlButton = new System.Windows.Forms.Button();
            this.NotifyIcon = new System.Windows.Forms.NotifyIcon(this.components);
            this.NotifyMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.ShowMainFormToolStripButton = new System.Windows.Forms.ToolStripMenuItem();
            this.ExitToolStripButton = new System.Windows.Forms.ToolStripMenuItem();
            this.SettingsButton = new System.Windows.Forms.Button();
            this.ProfileGroupBox = new System.Windows.Forms.GroupBox();
            this.ProfileTable = new System.Windows.Forms.TableLayoutPanel();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.ButtomControlContainerControl = new System.Windows.Forms.ContainerControl();
            this.updatePACToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuStrip.SuspendLayout();
            this.ConfigurationGroupBox.SuspendLayout();
            this.configLayoutPanel.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.EditServerPictureBox)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.CopyLinkPictureBox)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.DeleteServerPictureBox)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.SpeedPictureBox)).BeginInit();
            this.tableLayoutPanel3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.EditModePictureBox)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.DeleteModePictureBox)).BeginInit();
            this.StatusStrip.SuspendLayout();
            this.NotifyMenu.SuspendLayout();
            this.ProfileGroupBox.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
            this.ButtomControlContainerControl.SuspendLayout();
            this.SuspendLayout();
            // 
            // MenuStrip
            // 
            this.MenuStrip.BackColor = System.Drawing.SystemColors.Control;
            this.MenuStrip.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.MenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ServerToolStripMenuItem,
            this.ModeToolStripMenuItem,
            this.SubscribeToolStripMenuItem,
            this.OptionsToolStripMenuItem,
            this.HelpToolStripMenuItem,
            this.exitToolStripMenuItem,
            this.AboutToolStripButton,
            this.NewVersionLabel,
            this.VersionLabel});
            this.MenuStrip.Location = new System.Drawing.Point(0, 0);
            this.MenuStrip.Name = "MenuStrip";
            this.MenuStrip.RenderMode = System.Windows.Forms.ToolStripRenderMode.Professional;
            this.MenuStrip.Size = new System.Drawing.Size(740, 26);
            this.MenuStrip.TabIndex = 0;
            // 
            // ServerToolStripMenuItem
            // 
            this.ServerToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ImportServersFromClipboardToolStripMenuItem});
            this.ServerToolStripMenuItem.Margin = new System.Windows.Forms.Padding(3, 0, 0, 1);
            this.ServerToolStripMenuItem.Name = "ServerToolStripMenuItem";
            this.ServerToolStripMenuItem.Size = new System.Drawing.Size(57, 21);
            this.ServerToolStripMenuItem.Text = "Server";
            // 
            // ImportServersFromClipboardToolStripMenuItem
            // 
            this.ImportServersFromClipboardToolStripMenuItem.Name = "ImportServersFromClipboardToolStripMenuItem";
            this.ImportServersFromClipboardToolStripMenuItem.Size = new System.Drawing.Size(259, 22);
            this.ImportServersFromClipboardToolStripMenuItem.Text = "Import Servers From Clipboard";
            this.ImportServersFromClipboardToolStripMenuItem.Click += new System.EventHandler(this.ImportServersFromClipboardToolStripMenuItem_Click);
            // 
            // ModeToolStripMenuItem
            // 
            this.ModeToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.CreateProcessModeToolStripMenuItem,
            this.ReloadModesToolStripMenuItem});
            this.ModeToolStripMenuItem.Margin = new System.Windows.Forms.Padding(0, 0, 0, 1);
            this.ModeToolStripMenuItem.Name = "ModeToolStripMenuItem";
            this.ModeToolStripMenuItem.Size = new System.Drawing.Size(55, 21);
            this.ModeToolStripMenuItem.Text = "Mode";
            // 
            // CreateProcessModeToolStripMenuItem
            // 
            this.CreateProcessModeToolStripMenuItem.Name = "CreateProcessModeToolStripMenuItem";
            this.CreateProcessModeToolStripMenuItem.Size = new System.Drawing.Size(202, 22);
            this.CreateProcessModeToolStripMenuItem.Text = "Create Process Mode";
            this.CreateProcessModeToolStripMenuItem.Click += new System.EventHandler(this.CreateProcessModeToolStripButton_Click);
            // 
            // ReloadModesToolStripMenuItem
            // 
            this.ReloadModesToolStripMenuItem.Name = "ReloadModesToolStripMenuItem";
            this.ReloadModesToolStripMenuItem.Size = new System.Drawing.Size(202, 22);
            this.ReloadModesToolStripMenuItem.Text = "Reload Modes";
            this.ReloadModesToolStripMenuItem.Click += new System.EventHandler(this.ReloadModesToolStripMenuItem_Click);
            // 
            // SubscribeToolStripMenuItem
            // 
            this.SubscribeToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ManageSubscribeLinksToolStripMenuItem,
            this.UpdateServersFromSubscribeLinksToolStripMenuItem,
            this.UpdateServersFromSubscribeLinksWithProxyToolStripMenuItem});
            this.SubscribeToolStripMenuItem.Margin = new System.Windows.Forms.Padding(0, 0, 0, 1);
            this.SubscribeToolStripMenuItem.Name = "SubscribeToolStripMenuItem";
            this.SubscribeToolStripMenuItem.Size = new System.Drawing.Size(77, 21);
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
            // UpdateServersFromSubscribeLinksWithProxyToolStripMenuItem
            // 
            this.UpdateServersFromSubscribeLinksWithProxyToolStripMenuItem.Name = "UpdateServersFromSubscribeLinksWithProxyToolStripMenuItem";
            this.UpdateServersFromSubscribeLinksWithProxyToolStripMenuItem.Size = new System.Drawing.Size(294, 22);
            this.UpdateServersFromSubscribeLinksWithProxyToolStripMenuItem.Text = "Update Servers From Subscribe Links With Proxy";
            this.UpdateServersFromSubscribeLinksWithProxyToolStripMenuItem.Click += new System.EventHandler(this.UpdateServersFromSubscribeLinksWithProxyToolStripMenuItem_Click);
            // 
            // OptionsToolStripMenuItem
            // 
            this.OptionsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.OpenDirectoryToolStripMenuItem,
            this.CleanDNSCacheToolStripMenuItem,
            this.UpdateACLToolStripMenuItem,
            this.updateACLWithProxyToolStripMenuItem,
            this.updatePACToolStripMenuItem,
            this.UninstallServiceToolStripMenuItem,
            this.UninstallTapDriverToolStripMenuItem});
            this.OptionsToolStripMenuItem.Margin = new System.Windows.Forms.Padding(0, 0, 0, 1);
            this.OptionsToolStripMenuItem.Name = "OptionsToolStripMenuItem";
            this.OptionsToolStripMenuItem.Size = new System.Drawing.Size(66, 21);
            this.OptionsToolStripMenuItem.Text = "Options";
            // 
            // OpenDirectoryToolStripMenuItem
            // 
            this.OpenDirectoryToolStripMenuItem.Name = "OpenDirectoryToolStripMenuItem";
            this.OpenDirectoryToolStripMenuItem.Size = new System.Drawing.Size(220, 22);
            this.OpenDirectoryToolStripMenuItem.Text = "Open Directory";
            this.OpenDirectoryToolStripMenuItem.Click += new System.EventHandler(this.OpenDirectoryToolStripMenuItem_Click);
            // 
            // CleanDNSCacheToolStripMenuItem
            // 
            this.CleanDNSCacheToolStripMenuItem.Name = "CleanDNSCacheToolStripMenuItem";
            this.CleanDNSCacheToolStripMenuItem.Size = new System.Drawing.Size(220, 22);
            this.CleanDNSCacheToolStripMenuItem.Text = "Clean DNS Cache";
            this.CleanDNSCacheToolStripMenuItem.Click += new System.EventHandler(this.CleanDNSCacheToolStripMenuItem_Click);
            // 
            // UpdateACLToolStripMenuItem
            // 
            this.UpdateACLToolStripMenuItem.Name = "UpdateACLToolStripMenuItem";
            this.UpdateACLToolStripMenuItem.Size = new System.Drawing.Size(220, 22);
            this.UpdateACLToolStripMenuItem.Text = "Update ACL";
            this.UpdateACLToolStripMenuItem.Click += new System.EventHandler(this.updateACLToolStripMenuItem_Click);
            // 
            // updateACLWithProxyToolStripMenuItem
            // 
            this.updateACLWithProxyToolStripMenuItem.Name = "updateACLWithProxyToolStripMenuItem";
            this.updateACLWithProxyToolStripMenuItem.Size = new System.Drawing.Size(220, 22);
            this.updateACLWithProxyToolStripMenuItem.Text = "Update ACL with proxy";
            this.updateACLWithProxyToolStripMenuItem.Click += new System.EventHandler(this.updateACLWithProxyToolStripMenuItem_Click);
            // 
            // UninstallServiceToolStripMenuItem
            // 
            this.UninstallServiceToolStripMenuItem.Name = "UninstallServiceToolStripMenuItem";
            this.UninstallServiceToolStripMenuItem.Size = new System.Drawing.Size(220, 22);
            this.UninstallServiceToolStripMenuItem.Text = "Uninstall NF Service";
            this.UninstallServiceToolStripMenuItem.Click += new System.EventHandler(this.UninstallServiceToolStripMenuItem_Click);
            // 
            // UninstallTapDriverToolStripMenuItem
            // 
            this.UninstallTapDriverToolStripMenuItem.Name = "UninstallTapDriverToolStripMenuItem";
            this.UninstallTapDriverToolStripMenuItem.Size = new System.Drawing.Size(220, 22);
            this.UninstallTapDriverToolStripMenuItem.Text = "Uninstall TUN/TAP driver";
            this.UninstallTapDriverToolStripMenuItem.Click += new System.EventHandler(this.UninstallTapDriverToolStripMenuItem_Click);
            // 
            // HelpToolStripMenuItem
            // 
            this.HelpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.CheckForUpdatesToolStripMenuItem,
            this.fAQToolStripMenuItem});
            this.HelpToolStripMenuItem.Margin = new System.Windows.Forms.Padding(0, 0, 0, 1);
            this.HelpToolStripMenuItem.Name = "HelpToolStripMenuItem";
            this.HelpToolStripMenuItem.Size = new System.Drawing.Size(47, 21);
            this.HelpToolStripMenuItem.Text = "Help";
            // 
            // CheckForUpdatesToolStripMenuItem
            // 
            this.CheckForUpdatesToolStripMenuItem.Name = "CheckForUpdatesToolStripMenuItem";
            this.CheckForUpdatesToolStripMenuItem.Size = new System.Drawing.Size(183, 22);
            this.CheckForUpdatesToolStripMenuItem.Text = "Check for updates";
            this.CheckForUpdatesToolStripMenuItem.Click += new System.EventHandler(this.CheckForUpdatesToolStripMenuItem_Click);
            // 
            // fAQToolStripMenuItem
            // 
            this.fAQToolStripMenuItem.Name = "fAQToolStripMenuItem";
            this.fAQToolStripMenuItem.Size = new System.Drawing.Size(183, 22);
            this.fAQToolStripMenuItem.Text = "FAQ";
            this.fAQToolStripMenuItem.Click += new System.EventHandler(this.fAQToolStripMenuItem_Click);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(40, 22);
            this.exitToolStripMenuItem.Text = "Exit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // AboutToolStripButton
            // 
            this.AboutToolStripButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.AboutToolStripButton.AutoToolTip = false;
            this.AboutToolStripButton.Margin = new System.Windows.Forms.Padding(0, 0, 3, 1);
            this.AboutToolStripButton.Name = "AboutToolStripButton";
            this.AboutToolStripButton.Size = new System.Drawing.Size(47, 21);
            this.AboutToolStripButton.Text = "About";
            this.AboutToolStripButton.Click += new System.EventHandler(this.AboutToolStripButton_Click);
            // 
            // NewVersionLabel
            // 
            this.NewVersionLabel.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.NewVersionLabel.BackColor = System.Drawing.Color.Transparent;
            this.NewVersionLabel.ForeColor = System.Drawing.Color.Red;
            this.NewVersionLabel.IsLink = true;
            this.NewVersionLabel.LinkBehavior = System.Windows.Forms.LinkBehavior.NeverUnderline;
            this.NewVersionLabel.LinkColor = System.Drawing.Color.Red;
            this.NewVersionLabel.Name = "NewVersionLabel";
            this.NewVersionLabel.Size = new System.Drawing.Size(135, 19);
            this.NewVersionLabel.Text = "New version available";
            this.NewVersionLabel.Visible = false;
            this.NewVersionLabel.Click += new System.EventHandler(this.NewVersionLabel_Click);
            // 
            // VersionLabel
            // 
            this.VersionLabel.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.VersionLabel.BackColor = System.Drawing.Color.Transparent;
            this.VersionLabel.ForeColor = System.Drawing.Color.Red;
            this.VersionLabel.IsLink = true;
            this.VersionLabel.LinkBehavior = System.Windows.Forms.LinkBehavior.NeverUnderline;
            this.VersionLabel.Name = "VersionLabel";
            this.VersionLabel.Size = new System.Drawing.Size(26, 19);
            this.VersionLabel.Text = "xxx";
            this.VersionLabel.Click += new System.EventHandler(this.VersionLabel_Click);
            // 
            // ConfigurationGroupBox
            // 
            this.ConfigurationGroupBox.Controls.Add(this.configLayoutPanel);
            this.ConfigurationGroupBox.Location = new System.Drawing.Point(3, 3);
            this.ConfigurationGroupBox.Name = "ConfigurationGroupBox";
            this.ConfigurationGroupBox.Size = new System.Drawing.Size(709, 115);
            this.ConfigurationGroupBox.TabIndex = 1;
            this.ConfigurationGroupBox.TabStop = false;
            this.ConfigurationGroupBox.Text = "Configuration";
            // 
            // configLayoutPanel
            // 
            this.configLayoutPanel.AutoSize = true;
            this.configLayoutPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.configLayoutPanel.ColumnCount = 3;
            this.configLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.configLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.configLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.configLayoutPanel.Controls.Add(this.ProfileLabel, 0, 2);
            this.configLayoutPanel.Controls.Add(this.ModeLabel, 0, 1);
            this.configLayoutPanel.Controls.Add(this.ServerLabel, 0, 0);
            this.configLayoutPanel.Controls.Add(this.ProfileNameText, 1, 2);
            this.configLayoutPanel.Controls.Add(this.ModeComboBox, 1, 1);
            this.configLayoutPanel.Controls.Add(this.ServerComboBox, 1, 0);
            this.configLayoutPanel.Controls.Add(this.tableLayoutPanel2, 2, 0);
            this.configLayoutPanel.Controls.Add(this.tableLayoutPanel3, 2, 1);
            this.configLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.configLayoutPanel.Location = new System.Drawing.Point(3, 19);
            this.configLayoutPanel.Name = "configLayoutPanel";
            this.configLayoutPanel.RowCount = 3;
            this.configLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.configLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.configLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.configLayoutPanel.Size = new System.Drawing.Size(703, 93);
            this.configLayoutPanel.TabIndex = 15;
            // 
            // ProfileLabel
            // 
            this.ProfileLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.ProfileLabel.AutoSize = true;
            this.ProfileLabel.Location = new System.Drawing.Point(3, 68);
            this.ProfileLabel.Name = "ProfileLabel";
            this.ProfileLabel.Size = new System.Drawing.Size(45, 17);
            this.ProfileLabel.TabIndex = 10;
            this.ProfileLabel.Text = "Profile";
            // 
            // ModeLabel
            // 
            this.ModeLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.ModeLabel.AutoSize = true;
            this.ModeLabel.Location = new System.Drawing.Point(3, 36);
            this.ModeLabel.Name = "ModeLabel";
            this.ModeLabel.Size = new System.Drawing.Size(43, 17);
            this.ModeLabel.TabIndex = 3;
            this.ModeLabel.Text = "Mode";
            // 
            // ServerLabel
            // 
            this.ServerLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.ServerLabel.AutoSize = true;
            this.ServerLabel.Location = new System.Drawing.Point(3, 6);
            this.ServerLabel.Name = "ServerLabel";
            this.ServerLabel.Size = new System.Drawing.Size(45, 17);
            this.ServerLabel.TabIndex = 0;
            this.ServerLabel.Text = "Server";
            // 
            // ProfileNameText
            // 
            this.ProfileNameText.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ProfileNameText.Location = new System.Drawing.Point(54, 63);
            this.ProfileNameText.Name = "ProfileNameText";
            this.ProfileNameText.Size = new System.Drawing.Size(546, 23);
            this.ProfileNameText.TabIndex = 11;
            // 
            // ModeComboBox
            // 
            this.ModeComboBox.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.ModeComboBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ModeComboBox.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.ModeComboBox.FormattingEnabled = true;
            this.ModeComboBox.IntegralHeight = false;
            this.ModeComboBox.Location = new System.Drawing.Point(54, 33);
            this.ModeComboBox.Name = "ModeComboBox";
            this.ModeComboBox.Size = new System.Drawing.Size(546, 24);
            this.ModeComboBox.TabIndex = 2;
            this.ModeComboBox.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.ComboBox_DrawItem);
            this.ModeComboBox.SelectedIndexChanged += new System.EventHandler(this.ModeComboBox_SelectedIndexChanged);
            // 
            // ServerComboBox
            // 
            this.ServerComboBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ServerComboBox.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.ServerComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ServerComboBox.FormattingEnabled = true;
            this.ServerComboBox.IntegralHeight = false;
            this.ServerComboBox.Location = new System.Drawing.Point(54, 3);
            this.ServerComboBox.MaxDropDownItems = 16;
            this.ServerComboBox.Name = "ServerComboBox";
            this.ServerComboBox.Size = new System.Drawing.Size(546, 24);
            this.ServerComboBox.TabIndex = 1;
            this.ServerComboBox.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.ComboBox_DrawItem);
            this.ServerComboBox.SelectedIndexChanged += new System.EventHandler(this.ServerComboBox_SelectedIndexChanged);
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.ColumnCount = 4;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel2.Controls.Add(this.EditServerPictureBox, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.CopyLinkPictureBox, 3, 0);
            this.tableLayoutPanel2.Controls.Add(this.DeleteServerPictureBox, 1, 0);
            this.tableLayoutPanel2.Controls.Add(this.SpeedPictureBox, 2, 0);
            this.tableLayoutPanel2.Location = new System.Drawing.Point(606, 3);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 1;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(94, 24);
            this.tableLayoutPanel2.TabIndex = 12;
            // 
            // EditServerPictureBox
            // 
            this.EditServerPictureBox.BackColor = System.Drawing.SystemColors.Control;
            this.EditServerPictureBox.Cursor = System.Windows.Forms.Cursors.Hand;
            this.EditServerPictureBox.Image = global::Netch.Properties.Resources.edit;
            this.EditServerPictureBox.Location = new System.Drawing.Point(3, 3);
            this.EditServerPictureBox.Name = "EditServerPictureBox";
            this.EditServerPictureBox.Size = new System.Drawing.Size(16, 16);
            this.EditServerPictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.EditServerPictureBox.TabIndex = 7;
            this.EditServerPictureBox.TabStop = false;
            this.EditServerPictureBox.Click += new System.EventHandler(this.EditServerPictureBox_Click);
            // 
            // CopyLinkPictureBox
            // 
            this.CopyLinkPictureBox.Cursor = System.Windows.Forms.Cursors.Hand;
            this.CopyLinkPictureBox.Image = global::Netch.Properties.Resources.CopyLink;
            this.CopyLinkPictureBox.Location = new System.Drawing.Point(72, 3);
            this.CopyLinkPictureBox.Name = "CopyLinkPictureBox";
            this.CopyLinkPictureBox.Size = new System.Drawing.Size(18, 18);
            this.CopyLinkPictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.CopyLinkPictureBox.TabIndex = 14;
            this.CopyLinkPictureBox.TabStop = false;
            this.CopyLinkPictureBox.Click += new System.EventHandler(this.CopyLinkPictureBox_Click);
            // 
            // DeleteServerPictureBox
            // 
            this.DeleteServerPictureBox.Cursor = System.Windows.Forms.Cursors.Hand;
            this.DeleteServerPictureBox.Image = global::Netch.Properties.Resources.delete;
            this.DeleteServerPictureBox.Location = new System.Drawing.Point(26, 3);
            this.DeleteServerPictureBox.Name = "DeleteServerPictureBox";
            this.DeleteServerPictureBox.Size = new System.Drawing.Size(16, 16);
            this.DeleteServerPictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.DeleteServerPictureBox.TabIndex = 8;
            this.DeleteServerPictureBox.TabStop = false;
            this.DeleteServerPictureBox.Click += new System.EventHandler(this.DeleteServerPictureBox_Click);
            // 
            // SpeedPictureBox
            // 
            this.SpeedPictureBox.Cursor = System.Windows.Forms.Cursors.Hand;
            this.SpeedPictureBox.Image = global::Netch.Properties.Resources.speed;
            this.SpeedPictureBox.Location = new System.Drawing.Point(49, 3);
            this.SpeedPictureBox.Name = "SpeedPictureBox";
            this.SpeedPictureBox.Size = new System.Drawing.Size(16, 16);
            this.SpeedPictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.SpeedPictureBox.TabIndex = 9;
            this.SpeedPictureBox.TabStop = false;
            this.SpeedPictureBox.Click += new System.EventHandler(this.SpeedPictureBox_Click);
            // 
            // tableLayoutPanel3
            // 
            this.tableLayoutPanel3.ColumnCount = 4;
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel3.Controls.Add(this.EditModePictureBox, 0, 0);
            this.tableLayoutPanel3.Controls.Add(this.DeleteModePictureBox, 1, 0);
            this.tableLayoutPanel3.Location = new System.Drawing.Point(606, 33);
            this.tableLayoutPanel3.Name = "tableLayoutPanel3";
            this.tableLayoutPanel3.RowCount = 1;
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel3.Size = new System.Drawing.Size(94, 24);
            this.tableLayoutPanel3.TabIndex = 13;
            // 
            // EditModePictureBox
            // 
            this.EditModePictureBox.Cursor = System.Windows.Forms.Cursors.Hand;
            this.EditModePictureBox.Image = global::Netch.Properties.Resources.edit;
            this.EditModePictureBox.Location = new System.Drawing.Point(3, 3);
            this.EditModePictureBox.Name = "EditModePictureBox";
            this.EditModePictureBox.Size = new System.Drawing.Size(16, 16);
            this.EditModePictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.EditModePictureBox.TabIndex = 12;
            this.EditModePictureBox.TabStop = false;
            this.EditModePictureBox.Click += new System.EventHandler(this.EditModePictureBox_Click);
            // 
            // DeleteModePictureBox
            // 
            this.DeleteModePictureBox.Cursor = System.Windows.Forms.Cursors.Hand;
            this.DeleteModePictureBox.Image = global::Netch.Properties.Resources.delete;
            this.DeleteModePictureBox.Location = new System.Drawing.Point(26, 3);
            this.DeleteModePictureBox.Name = "DeleteModePictureBox";
            this.DeleteModePictureBox.Size = new System.Drawing.Size(16, 16);
            this.DeleteModePictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.DeleteModePictureBox.TabIndex = 13;
            this.DeleteModePictureBox.TabStop = false;
            this.DeleteModePictureBox.Click += new System.EventHandler(this.DeleteModePictureBox_Click);
            // 
            // StatusStrip
            // 
            this.StatusStrip.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.StatusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.StatusLabel,
            this.UsedBandwidthLabel,
            this.DownloadSpeedLabel,
            this.UploadSpeedLabel,
            this.blankToolStripStatusLabel,
            this.NatTypeStatusLabel,
            this.NatTypeStatusLightLabel});
            this.StatusStrip.Location = new System.Drawing.Point(0, 272);
            this.StatusStrip.Name = "StatusStrip";
            this.StatusStrip.Size = new System.Drawing.Size(740, 22);
            this.StatusStrip.SizingGrip = false;
            this.StatusStrip.TabIndex = 2;
            // 
            // StatusLabel
            // 
            this.StatusLabel.BackColor = System.Drawing.Color.Transparent;
            this.StatusLabel.Name = "StatusLabel";
            this.StatusLabel.Size = new System.Drawing.Size(177, 17);
            this.StatusLabel.Text = "Status: Waiting for command";
            // 
            // UsedBandwidthLabel
            // 
            this.UsedBandwidthLabel.Name = "UsedBandwidthLabel";
            this.UsedBandwidthLabel.Size = new System.Drawing.Size(72, 17);
            this.UsedBandwidthLabel.Text = "Used: 0 KB";
            this.UsedBandwidthLabel.Visible = false;
            // 
            // DownloadSpeedLabel
            // 
            this.DownloadSpeedLabel.Name = "DownloadSpeedLabel";
            this.DownloadSpeedLabel.Size = new System.Drawing.Size(59, 17);
            this.DownloadSpeedLabel.Text = "↓: 0 KB/s";
            this.DownloadSpeedLabel.Visible = false;
            // 
            // UploadSpeedLabel
            // 
            this.UploadSpeedLabel.Name = "UploadSpeedLabel";
            this.UploadSpeedLabel.Size = new System.Drawing.Size(59, 17);
            this.UploadSpeedLabel.Text = "↑: 0 KB/s";
            this.UploadSpeedLabel.Visible = false;
            // 
            // blankToolStripStatusLabel
            // 
            this.blankToolStripStatusLabel.Name = "blankToolStripStatusLabel";
            this.blankToolStripStatusLabel.Size = new System.Drawing.Size(494, 17);
            this.blankToolStripStatusLabel.Spring = true;
            // 
            // NatTypeStatusLabel
            // 
            this.NatTypeStatusLabel.Name = "NatTypeStatusLabel";
            this.NatTypeStatusLabel.Size = new System.Drawing.Size(36, 17);
            this.NatTypeStatusLabel.Text = "NAT:";
            this.NatTypeStatusLabel.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
            this.NatTypeStatusLabel.Click += new System.EventHandler(this.NatTypeStatusLabel_Click);
            // 
            // NatTypeStatusLightLabel
            // 
            this.NatTypeStatusLightLabel.ActiveLinkColor = System.Drawing.Color.Red;
            this.NatTypeStatusLightLabel.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.NatTypeStatusLightLabel.ForeColor = System.Drawing.Color.Red;
            this.NatTypeStatusLightLabel.Margin = new System.Windows.Forms.Padding(0, 0, 0, 1);
            this.NatTypeStatusLightLabel.Name = "NatTypeStatusLightLabel";
            this.NatTypeStatusLightLabel.Size = new System.Drawing.Size(18, 21);
            this.NatTypeStatusLightLabel.Text = "⬤";
            this.NatTypeStatusLightLabel.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.NatTypeStatusLightLabel.Click += new System.EventHandler(this.NatTypeStatusLabel_Click);
            // 
            // ControlButton
            // 
            this.ControlButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.ControlButton.Location = new System.Drawing.Point(631, 3);
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
            this.SettingsButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.SettingsButton.Location = new System.Drawing.Point(1, 3);
            this.SettingsButton.Name = "SettingsButton";
            this.SettingsButton.Size = new System.Drawing.Size(72, 27);
            this.SettingsButton.TabIndex = 4;
            this.SettingsButton.Text = "Settings";
            this.SettingsButton.UseVisualStyleBackColor = true;
            this.SettingsButton.Click += new System.EventHandler(this.SettingsButton_Click);
            // 
            // ProfileGroupBox
            // 
            this.ProfileGroupBox.Controls.Add(this.ProfileTable);
            this.ProfileGroupBox.Location = new System.Drawing.Point(3, 124);
            this.ProfileGroupBox.Name = "ProfileGroupBox";
            this.ProfileGroupBox.Size = new System.Drawing.Size(709, 65);
            this.ProfileGroupBox.TabIndex = 13;
            this.ProfileGroupBox.TabStop = false;
            this.ProfileGroupBox.Text = "Profiles";
            // 
            // ProfileTable
            // 
            this.ProfileTable.AutoSize = true;
            this.ProfileTable.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ProfileTable.ColumnCount = 2;
            this.ProfileTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.ProfileTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.ProfileTable.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ProfileTable.Location = new System.Drawing.Point(3, 19);
            this.ProfileTable.Name = "ProfileTable";
            this.ProfileTable.RowCount = 1;
            this.ProfileTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.ProfileTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.ProfileTable.Size = new System.Drawing.Size(703, 43);
            this.ProfileTable.TabIndex = 0;
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.AutoSize = true;
            this.flowLayoutPanel1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flowLayoutPanel1.Controls.Add(this.ConfigurationGroupBox);
            this.flowLayoutPanel1.Controls.Add(this.ProfileGroupBox);
            this.flowLayoutPanel1.Controls.Add(this.ButtomControlContainerControl);
            this.flowLayoutPanel1.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(12, 29);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(715, 256);
            this.flowLayoutPanel1.TabIndex = 14;
            // 
            // ButtomControlContainerControl
            // 
            this.ButtomControlContainerControl.Controls.Add(this.ControlButton);
            this.ButtomControlContainerControl.Controls.Add(this.SettingsButton);
            this.ButtomControlContainerControl.Location = new System.Drawing.Point(3, 195);
            this.ButtomControlContainerControl.Name = "ButtomControlContainerControl";
            this.ButtomControlContainerControl.Size = new System.Drawing.Size(706, 58);
            this.ButtomControlContainerControl.TabIndex = 14;
            this.ButtomControlContainerControl.TabStop = false;
            this.ButtomControlContainerControl.Text = "groupBox1";
            // 
            // updatePACToolStripMenuItem
            // 
            this.updatePACToolStripMenuItem.Name = "updatePACToolStripMenuItem";
            this.updatePACToolStripMenuItem.Size = new System.Drawing.Size(220, 22);
            this.updatePACToolStripMenuItem.Text = "Update PAC";
            this.updatePACToolStripMenuItem.Click += new System.EventHandler(this.updatePACToolStripMenuItem_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ClientSize = new System.Drawing.Size(740, 294);
            this.Controls.Add(this.MenuStrip);
            this.Controls.Add(this.StatusStrip);
            this.Controls.Add(this.flowLayoutPanel1);
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
            this.MenuStrip.ResumeLayout(false);
            this.MenuStrip.PerformLayout();
            this.ConfigurationGroupBox.ResumeLayout(false);
            this.ConfigurationGroupBox.PerformLayout();
            this.configLayoutPanel.ResumeLayout(false);
            this.configLayoutPanel.PerformLayout();
            this.tableLayoutPanel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.EditServerPictureBox)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.CopyLinkPictureBox)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.DeleteServerPictureBox)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.SpeedPictureBox)).EndInit();
            this.tableLayoutPanel3.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.EditModePictureBox)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.DeleteModePictureBox)).EndInit();
            this.StatusStrip.ResumeLayout(false);
            this.StatusStrip.PerformLayout();
            this.NotifyMenu.ResumeLayout(false);
            this.ProfileGroupBox.ResumeLayout(false);
            this.ProfileGroupBox.PerformLayout();
            this.flowLayoutPanel1.ResumeLayout(false);
            this.ButtomControlContainerControl.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private System.Windows.Forms.ToolStripButton AboutToolStripButton;
        private System.Windows.Forms.ToolStripMenuItem CleanDNSCacheToolStripMenuItem;
        private System.Windows.Forms.TableLayoutPanel configLayoutPanel;
        private System.Windows.Forms.GroupBox ConfigurationGroupBox;
        private System.Windows.Forms.Button ControlButton;
        private System.Windows.Forms.PictureBox CopyLinkPictureBox;
        private System.Windows.Forms.ToolStripMenuItem CreateProcessModeToolStripMenuItem;
        private System.Windows.Forms.PictureBox DeleteModePictureBox;
        private System.Windows.Forms.PictureBox DeleteServerPictureBox;
        private System.Windows.Forms.ToolStripStatusLabel DownloadSpeedLabel;
        private System.Windows.Forms.PictureBox EditModePictureBox;
        private System.Windows.Forms.PictureBox EditServerPictureBox;
        private System.Windows.Forms.ToolStripMenuItem ExitToolStripButton;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem ImportServersFromClipboardToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem ManageSubscribeLinksToolStripMenuItem;
        private System.Windows.Forms.MenuStrip MenuStrip;
        private System.Windows.Forms.SearchComboBox ModeComboBox;
        private System.Windows.Forms.Label ModeLabel;
        private System.Windows.Forms.ToolStripMenuItem ModeToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem HelpToolStripMenuItem;
        private System.Windows.Forms.ToolStripStatusLabel NatTypeStatusLabel;
        private System.Windows.Forms.NotifyIcon NotifyIcon;
        private System.Windows.Forms.ContextMenuStrip NotifyMenu;
        private System.Windows.Forms.ToolStripMenuItem OpenDirectoryToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem OptionsToolStripMenuItem;
        private System.Windows.Forms.GroupBox ProfileGroupBox;
        private System.Windows.Forms.Label ProfileLabel;
        private System.Windows.Forms.TextBox ProfileNameText;
        private System.Windows.Forms.TableLayoutPanel ProfileTable;
        private System.Windows.Forms.ToolStripMenuItem UninstallTapDriverToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem CheckForUpdatesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem ReloadModesToolStripMenuItem;
        private System.Windows.Forms.ComboBox ServerComboBox;
        private System.Windows.Forms.Label ServerLabel;
        private System.Windows.Forms.ToolStripMenuItem ServerToolStripMenuItem;
        private System.Windows.Forms.Button SettingsButton;
        private System.Windows.Forms.ToolStripMenuItem ShowMainFormToolStripButton;
        private System.Windows.Forms.PictureBox SpeedPictureBox;
        private System.Windows.Forms.ToolStripStatusLabel StatusLabel;
        private System.Windows.Forms.StatusStrip StatusStrip;
        private System.Windows.Forms.ToolStripMenuItem SubscribeToolStripMenuItem;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel3;
        private System.Windows.Forms.ToolStripMenuItem UninstallServiceToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem UpdateACLToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem updateACLWithProxyToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem UpdateServersFromSubscribeLinksToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem UpdateServersFromSubscribeLinksWithProxyToolStripMenuItem;
        private System.Windows.Forms.ToolStripStatusLabel UploadSpeedLabel;
        private System.Windows.Forms.ToolStripStatusLabel UsedBandwidthLabel;
        private System.Windows.Forms.ToolStripLabel NewVersionLabel;
        private System.Windows.Forms.ToolStripLabel VersionLabel;
        private System.Windows.Forms.ToolStripStatusLabel NatTypeStatusLightLabel;
        private System.Windows.Forms.ToolStripStatusLabel blankToolStripStatusLabel;
        private System.Windows.Forms.ToolStripMenuItem fAQToolStripMenuItem;

        #endregion

        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.ContainerControl ButtomControlContainerControl;
        private System.Windows.Forms.ToolStripMenuItem updatePACToolStripMenuItem;
    }
}