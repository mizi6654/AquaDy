using System.Drawing;
using System.Windows.Forms;
using AquaDy.Controls;
using AquaDy.Models;
using AquaDy.Services;
using AquaDy.Utils;

namespace AquaDy.Forms
{
    partial class AboutPage
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

        #region Component Designer generated code

        private void InitializeComponent()
        {
            watermark = new Label();
            titleLabel = new Label();
            versionLabel = new Label();
            desc = new TextBox();
            btnMine = new DarkButton();
            btnUpdate = new DarkButton();
            SuspendLayout();
            // 
            // watermark
            // 
            watermark.AutoSize = true;
            watermark.Font = new Font("Microsoft YaHei UI", 48F, FontStyle.Bold);
            watermark.ForeColor = Color.FromArgb(45, 48, 58);
            watermark.Location = new Point(40, 220);
            watermark.Name = "watermark";
            watermark.Size = new Size(294, 86);
            watermark.TabIndex = 8;
            watermark.Text = "AquaDy";
            // 
            // titleLabel
            // 
            titleLabel.AutoSize = true;
            titleLabel.Font = new Font("Microsoft YaHei UI", 20F, FontStyle.Bold);
            titleLabel.ForeColor = Color.FromArgb(139, 92, 246);
            titleLabel.Location = new Point(40, 36);
            titleLabel.Name = "titleLabel";
            titleLabel.Size = new Size(124, 36);
            titleLabel.TabIndex = 7;
            titleLabel.Text = "AquaDy";
            // 
            // versionLabel
            // 
            versionLabel.AutoSize = true;
            versionLabel.Font = new Font("Microsoft YaHei UI", 10F);
            versionLabel.ForeColor = Color.FromArgb(160, 160, 175);
            versionLabel.Location = new Point(40, 78);
            versionLabel.Name = "versionLabel";
            versionLabel.Size = new Size(0, 20);
            versionLabel.TabIndex = 6;
            // 
            // desc
            // 
            desc.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            desc.BackColor = Color.FromArgb(32, 34, 42);
            desc.BorderStyle = BorderStyle.None;
            desc.Font = new Font("Microsoft YaHei UI", 9.5F);
            desc.ForeColor = Color.FromArgb(240, 240, 245);
            desc.Location = new Point(40, 115);
            desc.Multiline = true;
            desc.Name = "desc";
            desc.ReadOnly = true;
            desc.ScrollBars = ScrollBars.Vertical;
            desc.Size = new Size(1491, 444);
            desc.TabIndex = 3;
            desc.TabStop = false;
            // 
            // btnMine
            // 
            btnMine.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnMine.BackColor = Color.FromArgb(32, 32, 40);
            btnMine.FlatStyle = FlatStyle.Flat;
            btnMine.Font = new Font("Microsoft YaHei UI", 9F);
            btnMine.ForeColor = Color.FromArgb(240, 240, 245);
            btnMine.IsPrimary = true;
            btnMine.Location = new Point(40, 573);
            btnMine.Name = "btnMine";
            btnMine.Padding = new Padding(12, 6, 12, 6);
            btnMine.Size = new Size(120, 36);
            btnMine.TabIndex = 4;
            btnMine.Text = "Github主页";
            btnMine.UseVisualStyleBackColor = false;
            btnMine.Click += btnMine_Click;
            // 
            // btnUpdate
            // 
            btnUpdate.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnUpdate.BackColor = Color.FromArgb(32, 32, 40);
            btnUpdate.FlatStyle = FlatStyle.Flat;
            btnUpdate.Font = new Font("Microsoft YaHei UI", 9F);
            btnUpdate.ForeColor = Color.FromArgb(240, 240, 245);
            btnUpdate.IsPrimary = true;
            btnUpdate.Location = new Point(170, 573);
            btnUpdate.Name = "btnUpdate";
            btnUpdate.Padding = new Padding(12, 6, 12, 6);
            btnUpdate.Size = new Size(120, 36);
            btnUpdate.TabIndex = 5;
            btnUpdate.Text = "检查更新";
            btnUpdate.UseVisualStyleBackColor = false;
            btnUpdate.Click += btnUpdate_Click;
            // 
            // AboutPage
            // 
            AutoScaleMode = AutoScaleMode.None;
            BackColor = Color.FromArgb(32, 34, 42);
            Controls.Add(btnUpdate);
            Controls.Add(btnMine);
            Controls.Add(desc);
            Controls.Add(versionLabel);
            Controls.Add(titleLabel);
            Controls.Add(watermark);
            Name = "AboutPage";
            Size = new Size(1571, 629);
            Resize += AboutPage_Resize;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label watermark;
        private System.Windows.Forms.Label titleLabel;
        private System.Windows.Forms.Label versionLabel;
        private System.Windows.Forms.TextBox desc;
        private AquaDy.Controls.DarkButton btnMine;
        private AquaDy.Controls.DarkButton btnUpdate;
    }
}