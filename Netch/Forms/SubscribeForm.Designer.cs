namespace Netch.Forms
{
    partial class SubscribeForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SubscribeForm));
            this.AddSubscriptionBox = new System.Windows.Forms.GroupBox();
            this.UserAgentTextBox = new System.Windows.Forms.TextBox();
            this.AddButton = new System.Windows.Forms.Button();
            this.UserAgentLabel = new System.Windows.Forms.Label();
            this.LinkTextBox = new System.Windows.Forms.TextBox();
            this.LinkLabel = new System.Windows.Forms.Label();
            this.RemarkTextBox = new System.Windows.Forms.TextBox();
            this.RemarkLabel = new System.Windows.Forms.Label();
            this.ControlButton = new System.Windows.Forms.Button();
            this.SubscribeLinkListView = new System.Windows.Forms.ListView();
            this.RemarkColumnHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.LinkColumnHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.UserAgentHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.pContextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.DeleteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.CopyLinkToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.UseSelectedServerCheckBox = new System.Windows.Forms.CheckBox();
            this.AddSubscriptionBox.SuspendLayout();
            this.pContextMenuStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // AddSubscriptionBox
            // 
            this.AddSubscriptionBox.Controls.Add(this.UserAgentTextBox);
            this.AddSubscriptionBox.Controls.Add(this.AddButton);
            this.AddSubscriptionBox.Controls.Add(this.UserAgentLabel);
            this.AddSubscriptionBox.Controls.Add(this.LinkTextBox);
            this.AddSubscriptionBox.Controls.Add(this.LinkLabel);
            this.AddSubscriptionBox.Controls.Add(this.RemarkTextBox);
            this.AddSubscriptionBox.Controls.Add(this.RemarkLabel);
            this.AddSubscriptionBox.Location = new System.Drawing.Point(12, 226);
            this.AddSubscriptionBox.Name = "AddSubscriptionBox";
            this.AddSubscriptionBox.Size = new System.Drawing.Size(660, 135);
            this.AddSubscriptionBox.TabIndex = 1;
            this.AddSubscriptionBox.TabStop = false;
            // 
            // UserAgentTextBox
            // 
            this.UserAgentTextBox.Location = new System.Drawing.Point(109, 74);
            this.UserAgentTextBox.Name = "UserAgentTextBox";
            this.UserAgentTextBox.Size = new System.Drawing.Size(545, 23);
            this.UserAgentTextBox.TabIndex = 6;
            // 
            // AddButton
            // 
            this.AddButton.Location = new System.Drawing.Point(541, 103);
            this.AddButton.Name = "AddButton";
            this.AddButton.Size = new System.Drawing.Size(113, 26);
            this.AddButton.TabIndex = 7;
            this.AddButton.Text = "Add / Modify";
            this.AddButton.UseVisualStyleBackColor = true;
            this.AddButton.Click += new System.EventHandler(this.AddButton_Click);
            // 
            // UserAgentLabel
            // 
            this.UserAgentLabel.AutoSize = true;
            this.UserAgentLabel.Location = new System.Drawing.Point(11, 77);
            this.UserAgentLabel.Name = "UserAgentLabel";
            this.UserAgentLabel.Size = new System.Drawing.Size(74, 17);
            this.UserAgentLabel.TabIndex = 5;
            this.UserAgentLabel.Text = "User-Agent";
            // 
            // LinkTextBox
            // 
            this.LinkTextBox.Location = new System.Drawing.Point(109, 45);
            this.LinkTextBox.Name = "LinkTextBox";
            this.LinkTextBox.Size = new System.Drawing.Size(545, 23);
            this.LinkTextBox.TabIndex = 4;
            // 
            // LinkLabel
            // 
            this.LinkLabel.AutoSize = true;
            this.LinkLabel.Location = new System.Drawing.Point(11, 48);
            this.LinkLabel.Name = "LinkLabel";
            this.LinkLabel.Size = new System.Drawing.Size(31, 17);
            this.LinkLabel.TabIndex = 3;
            this.LinkLabel.Text = "Link";
            // 
            // RemarkTextBox
            // 
            this.RemarkTextBox.Location = new System.Drawing.Point(109, 16);
            this.RemarkTextBox.Name = "RemarkTextBox";
            this.RemarkTextBox.Size = new System.Drawing.Size(545, 23);
            this.RemarkTextBox.TabIndex = 2;
            // 
            // RemarkLabel
            // 
            this.RemarkLabel.AutoSize = true;
            this.RemarkLabel.Location = new System.Drawing.Point(11, 19);
            this.RemarkLabel.Name = "RemarkLabel";
            this.RemarkLabel.Size = new System.Drawing.Size(53, 17);
            this.RemarkLabel.TabIndex = 1;
            this.RemarkLabel.Text = "Remark";
            // 
            // ControlButton
            // 
            this.ControlButton.Location = new System.Drawing.Point(597, 396);
            this.ControlButton.Name = "ControlButton";
            this.ControlButton.Size = new System.Drawing.Size(75, 23);
            this.ControlButton.TabIndex = 8;
            this.ControlButton.Text = "Save";
            this.ControlButton.UseVisualStyleBackColor = true;
            this.ControlButton.Click += new System.EventHandler(this.ControlButton_Click);
            // 
            // SubscribeLinkListView
            // 
            this.SubscribeLinkListView.AllowColumnReorder = true;
            this.SubscribeLinkListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.RemarkColumnHeader,
            this.LinkColumnHeader,
            this.UserAgentHeader});
            this.SubscribeLinkListView.ContextMenuStrip = this.pContextMenuStrip;
            this.SubscribeLinkListView.FullRowSelect = true;
            this.SubscribeLinkListView.HideSelection = false;
            this.SubscribeLinkListView.Location = new System.Drawing.Point(12, 12);
            this.SubscribeLinkListView.Name = "SubscribeLinkListView";
            this.SubscribeLinkListView.Size = new System.Drawing.Size(660, 208);
            this.SubscribeLinkListView.TabIndex = 0;
            this.SubscribeLinkListView.UseCompatibleStateImageBehavior = false;
            this.SubscribeLinkListView.View = System.Windows.Forms.View.Details;
            this.SubscribeLinkListView.SelectedIndexChanged += new System.EventHandler(this.SubscribeLinkListView_SelectedIndexChanged);
            // 
            // RemarkColumnHeader
            // 
            this.RemarkColumnHeader.Text = "Remark";
            this.RemarkColumnHeader.Width = 120;
            // 
            // LinkColumnHeader
            // 
            this.LinkColumnHeader.Text = "Link";
            this.LinkColumnHeader.Width = 400;
            // 
            // UserAgentHeader
            // 
            this.UserAgentHeader.Text = "User-Agent";
            this.UserAgentHeader.Width = 120;
            // 
            // pContextMenuStrip
            // 
            this.pContextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.DeleteToolStripMenuItem,
            this.CopyLinkToolStripMenuItem});
            this.pContextMenuStrip.Name = "pContextMenuStrip";
            this.pContextMenuStrip.Size = new System.Drawing.Size(130, 48);
            // 
            // DeleteToolStripMenuItem
            // 
            this.DeleteToolStripMenuItem.Name = "DeleteToolStripMenuItem";
            this.DeleteToolStripMenuItem.Size = new System.Drawing.Size(129, 22);
            this.DeleteToolStripMenuItem.Text = "Delete";
            this.DeleteToolStripMenuItem.Click += new System.EventHandler(this.DeleteToolStripMenuItem_Click);
            // 
            // CopyLinkToolStripMenuItem
            // 
            this.CopyLinkToolStripMenuItem.Name = "CopyLinkToolStripMenuItem";
            this.CopyLinkToolStripMenuItem.Size = new System.Drawing.Size(129, 22);
            this.CopyLinkToolStripMenuItem.Text = "CopyLink";
            this.CopyLinkToolStripMenuItem.Click += new System.EventHandler(this.CopyLinkToolStripMenuItem_Click);
            // 
            // UseSelectedServerCheckBox
            // 
            this.UseSelectedServerCheckBox.AutoSize = true;
            this.UseSelectedServerCheckBox.Location = new System.Drawing.Point(12, 396);
            this.UseSelectedServerCheckBox.Name = "UseSelectedServerCheckBox";
            this.UseSelectedServerCheckBox.Size = new System.Drawing.Size(285, 21);
            this.UseSelectedServerCheckBox.TabIndex = 9;
            this.UseSelectedServerCheckBox.Text = "Use Selected Server To Update Subscription";
            this.UseSelectedServerCheckBox.UseVisualStyleBackColor = true;
            // 
            // SubscribeForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(684, 431);
            this.Controls.Add(this.UseSelectedServerCheckBox);
            this.Controls.Add(this.SubscribeLinkListView);
            this.Controls.Add(this.ControlButton);
            this.Controls.Add(this.AddSubscriptionBox);
            this.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.MaximizeBox = false;
            this.Name = "SubscribeForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Subscribe";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.SubscribeForm_FormClosing);
            this.Load += new System.EventHandler(this.SubscribeForm_Load);
            this.AddSubscriptionBox.ResumeLayout(false);
            this.AddSubscriptionBox.PerformLayout();
            this.pContextMenuStrip.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.GroupBox AddSubscriptionBox;
        private System.Windows.Forms.Label RemarkLabel;
        private System.Windows.Forms.TextBox LinkTextBox;
        private System.Windows.Forms.Label LinkLabel;
        private System.Windows.Forms.TextBox RemarkTextBox;
        private System.Windows.Forms.Button AddButton;
        private System.Windows.Forms.Button ControlButton;
        private System.Windows.Forms.ListView SubscribeLinkListView;
        private System.Windows.Forms.ColumnHeader RemarkColumnHeader;
        private System.Windows.Forms.ColumnHeader LinkColumnHeader;
        private System.Windows.Forms.ContextMenuStrip pContextMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem DeleteToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem CopyLinkToolStripMenuItem;
        private System.Windows.Forms.Label UserAgentLabel;
        private System.Windows.Forms.TextBox UserAgentTextBox;
        private System.Windows.Forms.ColumnHeader UserAgentHeader;
        private System.Windows.Forms.CheckBox UseSelectedServerCheckBox;
    }
}