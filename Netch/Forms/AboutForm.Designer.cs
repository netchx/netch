namespace Netch.Forms
{
    partial class AboutForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AboutForm));
            this.SponsorGroupBox = new System.Windows.Forms.GroupBox();
            this.SponsorPictureBox = new System.Windows.Forms.PictureBox();
            this.ChannelLabel = new System.Windows.Forms.LinkLabel();
            this.NetchPictureBox = new System.Windows.Forms.PictureBox();
            this.SponsorGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.SponsorPictureBox)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.NetchPictureBox)).BeginInit();
            this.SuspendLayout();
            // 
            // SponsorGroupBox
            // 
            this.SponsorGroupBox.Controls.Add(this.SponsorPictureBox);
            this.SponsorGroupBox.Location = new System.Drawing.Point(12, 235);
            this.SponsorGroupBox.Name = "SponsorGroupBox";
            this.SponsorGroupBox.Size = new System.Drawing.Size(314, 229);
            this.SponsorGroupBox.TabIndex = 2;
            this.SponsorGroupBox.TabStop = false;
            this.SponsorGroupBox.Text = "Sponsor";
            // 
            // SponsorPictureBox
            // 
            this.SponsorPictureBox.Cursor = System.Windows.Forms.Cursors.Hand;
            this.SponsorPictureBox.Image = global::Netch.Properties.Resources.Sponsor;
            this.SponsorPictureBox.Location = new System.Drawing.Point(6, 22);
            this.SponsorPictureBox.Name = "SponsorPictureBox";
            this.SponsorPictureBox.Size = new System.Drawing.Size(300, 200);
            this.SponsorPictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.SponsorPictureBox.TabIndex = 1;
            this.SponsorPictureBox.TabStop = false;
            this.SponsorPictureBox.Click += new System.EventHandler(this.SponsorPictureBox_Click);
            // 
            // ChannelLabel
            // 
            this.ChannelLabel.AutoSize = true;
            this.ChannelLabel.Location = new System.Drawing.Point(116, 215);
            this.ChannelLabel.Name = "ChannelLabel";
            this.ChannelLabel.Size = new System.Drawing.Size(113, 17);
            this.ChannelLabel.TabIndex = 5;
            this.ChannelLabel.TabStop = true;
            this.ChannelLabel.Text = "Telegram Channel";
            this.ChannelLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.ChannelLabel_LinkClicked);
            // 
            // NetchPictureBox
            // 
            this.NetchPictureBox.Cursor = System.Windows.Forms.Cursors.Hand;
            this.NetchPictureBox.Image = global::Netch.Properties.Resources.Netch;
            this.NetchPictureBox.Location = new System.Drawing.Point(72, 12);
            this.NetchPictureBox.Name = "NetchPictureBox";
            this.NetchPictureBox.Size = new System.Drawing.Size(200, 200);
            this.NetchPictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.NetchPictureBox.TabIndex = 0;
            this.NetchPictureBox.TabStop = false;
            this.NetchPictureBox.Click += new System.EventHandler(this.NetchPictureBox_Click);
            // 
            // AboutForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(338, 474);
            this.Controls.Add(this.ChannelLabel);
            this.Controls.Add(this.SponsorGroupBox);
            this.Controls.Add(this.NetchPictureBox);
            this.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.MaximizeBox = false;
            this.Name = "AboutForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "About";
            this.Load += new System.EventHandler(this.AboutForm_Load);
            this.SponsorGroupBox.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.SponsorPictureBox)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.NetchPictureBox)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox NetchPictureBox;
        private System.Windows.Forms.PictureBox SponsorPictureBox;
        private System.Windows.Forms.GroupBox SponsorGroupBox;
        private System.Windows.Forms.LinkLabel ChannelLabel;
    }
}