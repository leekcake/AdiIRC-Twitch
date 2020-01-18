namespace Twitch___AdiIRC.Forms
{
    partial class TwitchUserDetailForm
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
            this.UserNameLabel = new System.Windows.Forms.Label();
            this.BanButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // UserNameLabel
            // 
            this.UserNameLabel.AutoSize = true;
            this.UserNameLabel.Location = new System.Drawing.Point(12, 9);
            this.UserNameLabel.Name = "UserNameLabel";
            this.UserNameLabel.Size = new System.Drawing.Size(197, 12);
            this.UserNameLabel.TabIndex = 0;
            this.UserNameLabel.Text = "유저 아이디: 파란전기(@leekcake)";
            // 
            // BanButton
            // 
            this.BanButton.Location = new System.Drawing.Point(443, 136);
            this.BanButton.Name = "BanButton";
            this.BanButton.Size = new System.Drawing.Size(75, 23);
            this.BanButton.TabIndex = 1;
            this.BanButton.Text = "영구차단";
            this.BanButton.UseVisualStyleBackColor = true;
            // 
            // TwitchUserDetailForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(530, 171);
            this.Controls.Add(this.BanButton);
            this.Controls.Add(this.UserNameLabel);
            this.Name = "TwitchUserDetailForm";
            this.Text = "유저 정보";
            this.Load += new System.EventHandler(this.TwitchUserDetailForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label UserNameLabel;
        private System.Windows.Forms.Button BanButton;
    }
}