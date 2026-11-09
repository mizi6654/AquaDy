namespace AquaDy.Forms
{
    partial class UpdateProgressForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this._lbl = new System.Windows.Forms.Label();
            this._bar = new System.Windows.Forms.ProgressBar();
            this.SuspendLayout();
            // 
            // _lbl
            // 
            this._lbl.AutoSize = false;
            this._lbl.Font = new System.Drawing.Font("Microsoft YaHei UI", 11F);
            this._lbl.ForeColor = System.Drawing.Color.DarkBlue;
            this._lbl.Location = new System.Drawing.Point(30, 24);
            this._lbl.Name = "_lbl";
            this._lbl.Size = new System.Drawing.Size(520, 120);
            this._lbl.TabIndex = 0;
            this._lbl.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _bar
            // 
            this._bar.Location = new System.Drawing.Point(30, 160);
            this._bar.MarqueeAnimationSpeed = 40;
            this._bar.Name = "_bar";
            this._bar.Size = new System.Drawing.Size(520, 40);
            this._bar.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            this._bar.TabIndex = 1;
            // 
            // UpdateProgressForm
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.White;
            this.Controls.Add(this._bar);
            this.Controls.Add(this._lbl);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "UpdateProgressForm";
            this.ShowInTaskbar = false;
            this.Size = new System.Drawing.Size(580, 260);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "更新中...";
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Label _lbl;
        private System.Windows.Forms.ProgressBar _bar;
    }
}