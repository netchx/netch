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
            this.ProjectLabel = new System.Windows.Forms.LinkLabel();
            this.NetchPictureBox = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.NetchPictureBox)).BeginInit();
            this.SuspendLayout();
            // 
            // ProjectLabel
            // 
            this.ProjectLabel.AutoSize = true;
            this.ProjectLabel.Location = new System.Drawing.Point(101, 215);
            this.ProjectLabel.Name = "ProjectLabel";
            this.ProjectLabel.Size = new System.Drawing.Size(136, 17);
            this.ProjectLabel.TabIndex = 5;
            this.ProjectLabel.TabStop = true;
            this.ProjectLabel.Text = "Project: cbear2024/netch";
            this.ProjectLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.ProjectLabel_LinkClicked);
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
            this.ClientSize = new System.Drawing.Size(338, 244);
            this.Controls.Add(this.ProjectLabel);
            this.Controls.Add(this.NetchPictureBox);
            this.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
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
        private System.Windows.Forms.LinkLabel ProjectLabel;
    }
}
