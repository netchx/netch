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
            this.ChannelLabel = new System.Windows.Forms.LinkLabel();
            this.NetchPictureBox = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.NetchPictureBox)).BeginInit();
            this.SuspendLayout();
            // 
            // ChannelLabel
            // 
            this.ChannelLabel.AutoSize = true;
            this.ChannelLabel.Location = new System.Drawing.Point(200, 642);
            this.ChannelLabel.Margin = new System.Windows.Forms.Padding(9, 0, 9, 0);
            this.ChannelLabel.Name = "ChannelLabel";
            this.ChannelLabel.Size = new System.Drawing.Size(333, 46);
            this.ChannelLabel.TabIndex = 5;
            this.ChannelLabel.TabStop = true;
            this.ChannelLabel.Text = "Telegram Channel";
            this.ChannelLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.ChannelLabel_LinkClicked);
            // 
            // NetchPictureBox
            // 
            this.NetchPictureBox.Cursor = System.Windows.Forms.Cursors.Hand;
            this.NetchPictureBox.Image = global::Netch.Properties.Resources.Netch;
            this.NetchPictureBox.Location = new System.Drawing.Point(75, 35);
            this.NetchPictureBox.Margin = new System.Windows.Forms.Padding(9, 9, 9, 9);
            this.NetchPictureBox.Name = "NetchPictureBox";
            this.NetchPictureBox.Size = new System.Drawing.Size(600, 600);
            this.NetchPictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.NetchPictureBox.TabIndex = 0;
            this.NetchPictureBox.TabStop = false;
            this.NetchPictureBox.Click += new System.EventHandler(this.NetchPictureBox_Click);
            // 
            // AboutForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(288F, 288F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(764, 697);
            this.Controls.Add(this.ChannelLabel);
            this.Controls.Add(this.NetchPictureBox);
            this.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(9, 12, 9, 12);
            this.MaximizeBox = false;
            this.Name = "AboutForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "About";
            this.Load += new System.EventHandler(this.AboutForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.NetchPictureBox)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox NetchPictureBox;
        private System.Windows.Forms.LinkLabel ChannelLabel;
    }
}