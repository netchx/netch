namespace Netch.Forms
{
    partial class MessageForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MessageForm));
            this.MessageLabel = new System.Windows.Forms.Label();
            this.MessageLinkLabel = new System.Windows.Forms.LinkLabel();
            this.MessageYesButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // MessageLabel
            // 
            this.MessageLabel.AutoSize = true;
            this.MessageLabel.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.MessageLabel.Location = new System.Drawing.Point(121, 57);
            this.MessageLabel.Name = "MessageLabel";
            this.MessageLabel.Size = new System.Drawing.Size(382, 21);
            this.MessageLabel.TabIndex = 0;
            this.MessageLabel.Text = "Not available for current version. More details on ";
            this.MessageLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // MessageLinkLabel
            // 
            this.MessageLinkLabel.AutoSize = true;
            this.MessageLinkLabel.BackColor = System.Drawing.Color.Transparent;
            this.MessageLinkLabel.Cursor = System.Windows.Forms.Cursors.Hand;
            this.MessageLinkLabel.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.MessageLinkLabel.Location = new System.Drawing.Point(144, 82);
            this.MessageLinkLabel.Name = "MessageLinkLabel";
            this.MessageLinkLabel.Size = new System.Drawing.Size(332, 21);
            this.MessageLinkLabel.TabIndex = 1;
            this.MessageLinkLabel.TabStop = true;
            this.MessageLinkLabel.Text = "https://github.com/netchx/Netch/releases";
            this.MessageLinkLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.MessageLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.MessageLinkLabel_LinkClicked);
            // 
            // MessageYesButton
            // 
            this.MessageYesButton.Location = new System.Drawing.Point(274, 137);
            this.MessageYesButton.Name = "MessageYesButton";
            this.MessageYesButton.Size = new System.Drawing.Size(75, 27);
            this.MessageYesButton.TabIndex = 2;
            this.MessageYesButton.Text = "Yes";
            this.MessageYesButton.UseVisualStyleBackColor = true;
            this.MessageYesButton.Click += new System.EventHandler(this.ButtonYes_Click);
            // 
            // MessageForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.ClientSize = new System.Drawing.Size(608, 176);
            this.Controls.Add(this.MessageYesButton);
            this.Controls.Add(this.MessageLinkLabel);
            this.Controls.Add(this.MessageLabel);
            this.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.MaximizeBox = false;
            this.Name = "MessageForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "MessageForm";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MessageForm_FormClosing);
            this.Load += new System.EventHandler(this.MessageForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label MessageLabel;
        private System.Windows.Forms.LinkLabel MessageLinkLabel;
        private System.Windows.Forms.Button MessageYesButton;
    }
}