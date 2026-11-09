using System.Drawing;
using System.Windows.Forms;
using AquaDy.Controls;
using AquaDy.Models;
using AquaDy.Services;
using AquaDy.Utils;

namespace AquaDy.Forms
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Panel sidebar;
        private System.Windows.Forms.Panel logoPanel;
        private System.Windows.Forms.Label logoIcon;
        private System.Windows.Forms.Label logoText;
        private System.Windows.Forms.Label logoSub;
        private System.Windows.Forms.Panel navContainer;
        private System.Windows.Forms.Panel bottomPanel;
        private System.Windows.Forms.Label lblVersion;
        private System.Windows.Forms.Panel contentPanel;
        private System.Windows.Forms.Panel statusBar;
        private System.Windows.Forms.Label lblStatus;
        private AquaDy.Forms.NavButton btnDownload;
        private AquaDy.Forms.NavButton btnAccount;
        private AquaDy.Forms.NavButton btnLive;
        private AquaDy.Forms.NavButton btnCompleted;
        private AquaDy.Forms.NavButton btnSettings;
        private AquaDy.Forms.NavButton btnAbout;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.sidebar = new System.Windows.Forms.Panel();
            this.logoPanel = new System.Windows.Forms.Panel();
            this.logoIcon = new System.Windows.Forms.Label();
            this.logoText = new System.Windows.Forms.Label();
            this.logoSub = new System.Windows.Forms.Label();
            this.navContainer = new System.Windows.Forms.Panel();
            this.bottomPanel = new System.Windows.Forms.Panel();
            this.lblVersion = new System.Windows.Forms.Label();
            this.contentPanel = new System.Windows.Forms.Panel();
            this.statusBar = new System.Windows.Forms.Panel();
            this.lblStatus = new System.Windows.Forms.Label();
            this.btnDownload = new AquaDy.Forms.NavButton();
            this.btnAccount = new AquaDy.Forms.NavButton();
            this.btnLive = new AquaDy.Forms.NavButton();
            this.btnCompleted = new AquaDy.Forms.NavButton();
            this.btnSettings = new AquaDy.Forms.NavButton();
            this.btnAbout = new AquaDy.Forms.NavButton();

            this.sidebar.SuspendLayout();
            this.logoPanel.SuspendLayout();
            this.navContainer.SuspendLayout();
            this.bottomPanel.SuspendLayout();
            this.statusBar.SuspendLayout();
            this.SuspendLayout();

            // sidebar
            this.sidebar.BackColor = AppTheme.SidebarBackground;
            this.sidebar.Controls.Add(this.navContainer);
            this.sidebar.Controls.Add(this.bottomPanel);
            this.sidebar.Controls.Add(this.logoPanel);
            this.sidebar.Dock = System.Windows.Forms.DockStyle.Left;
            this.sidebar.Location = new System.Drawing.Point(0, 0);
            this.sidebar.Name = "sidebar";
            this.sidebar.Padding = new System.Windows.Forms.Padding(12, 16, 12, 16);
            this.sidebar.Size = new System.Drawing.Size(220, 770);
            this.sidebar.TabIndex = 1;
            this.sidebar.Width = 220;

            // logoPanel
            this.logoPanel.BackColor = AppTheme.SidebarBackground;
            this.logoPanel.Controls.Add(this.logoIcon);
            this.logoPanel.Controls.Add(this.logoText);
            this.logoPanel.Controls.Add(this.logoSub);
            this.logoPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.logoPanel.Height = 60;
            this.logoPanel.Location = new System.Drawing.Point(12, 16);
            this.logoPanel.Name = "logoPanel";
            this.logoPanel.Size = new System.Drawing.Size(196, 60);
            this.logoPanel.TabIndex = 2;

            // logoIcon
            this.logoIcon.AutoSize = true;
            this.logoIcon.Font = new System.Drawing.Font("Segoe UI", 18F, System.Drawing.FontStyle.Bold);
            this.logoIcon.ForeColor = AppTheme.Primary;
            this.logoIcon.Location = new System.Drawing.Point(8, 12);
            this.logoIcon.Name = "logoIcon";
            this.logoIcon.Text = "↓";

            // logoText
            this.logoText.AutoSize = true;
            this.logoText.Font = new System.Drawing.Font("Microsoft YaHei UI", 11F, System.Drawing.FontStyle.Bold);
            this.logoText.ForeColor = AppTheme.TextPrimary;
            this.logoText.Location = new System.Drawing.Point(40, 10);
            this.logoText.Name = "logoText";
            this.logoText.Text = "AquaDy";

            // logoSub
            this.logoSub.AutoSize = true;
            this.logoSub.Font = new System.Drawing.Font("Microsoft YaHei UI", 8F);
            this.logoSub.ForeColor = AppTheme.TextMuted;
            this.logoSub.Location = new System.Drawing.Point(40, 32);
            this.logoSub.Name = "logoSub";
            this.logoSub.Text = "";

            // navContainer
            this.navContainer.BackColor = AppTheme.SidebarBackground;
            this.navContainer.Controls.Add(this.btnDownload);
            this.navContainer.Controls.Add(this.btnAccount);
            this.navContainer.Controls.Add(this.btnLive);
            this.navContainer.Controls.Add(this.btnCompleted);
            this.navContainer.Controls.Add(this.btnSettings);
            this.navContainer.Controls.Add(this.btnAbout);
            this.navContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.navContainer.Location = new System.Drawing.Point(12, 76);
            this.navContainer.Name = "navContainer";
            this.navContainer.Padding = new System.Windows.Forms.Padding(0, 10, 0, 0);
            this.navContainer.Size = new System.Drawing.Size(196, 628);
            this.navContainer.TabIndex = 1;

            // 6 个导航按钮（CreateNavButton 展开）
            this.btnDownload.Anchor = (AnchorStyles)(((AnchorStyles.Top | AnchorStyles.Left) | AnchorStyles.Right));
            this.btnDownload.Location = new System.Drawing.Point(0, 0);
            this.btnDownload.Name = "btnDownload";
            this.btnDownload.Size = new System.Drawing.Size(196, 42);
            this.btnDownload.TabIndex = 0;
            this.btnDownload.Text = "  ↓   下载任务";
            this.btnDownload.Click += new System.EventHandler(this.NavButton_Click);

            this.btnAccount.Anchor = (AnchorStyles)(((AnchorStyles.Top | AnchorStyles.Left) | AnchorStyles.Right));
            this.btnAccount.Location = new System.Drawing.Point(0, 48);
            this.btnAccount.Name = "btnAccount";
            this.btnAccount.Size = new System.Drawing.Size(196, 42);
            this.btnAccount.TabIndex = 1;
            this.btnAccount.Text = "  👤   账号批量";
            this.btnAccount.Click += new System.EventHandler(this.NavButton_Click);

            this.btnLive.Anchor = (AnchorStyles)(((AnchorStyles.Top | AnchorStyles.Left) | AnchorStyles.Right));
            this.btnLive.Location = new System.Drawing.Point(0, 96);
            this.btnLive.Name = "btnLive";
            this.btnLive.Size = new System.Drawing.Size(196, 42);
            this.btnLive.TabIndex = 2;
            this.btnLive.Text = "  ▶   直播下载";
            this.btnLive.Click += new System.EventHandler(this.NavButton_Click);

            this.btnCompleted.Anchor = (AnchorStyles)(((AnchorStyles.Top | AnchorStyles.Left) | AnchorStyles.Right));
            this.btnCompleted.Location = new System.Drawing.Point(0, 144);
            this.btnCompleted.Name = "btnCompleted";
            this.btnCompleted.Size = new System.Drawing.Size(196, 42);
            this.btnCompleted.TabIndex = 3;
            this.btnCompleted.Text = "  ✓   已完成";
            this.btnCompleted.Click += new System.EventHandler(this.NavButton_Click);

            this.btnSettings.Anchor = (AnchorStyles)(((AnchorStyles.Top | AnchorStyles.Left) | AnchorStyles.Right));
            this.btnSettings.Location = new System.Drawing.Point(0, 192);
            this.btnSettings.Name = "btnSettings";
            this.btnSettings.Size = new System.Drawing.Size(196, 42);
            this.btnSettings.TabIndex = 4;
            this.btnSettings.Text = "  ⚙   设置";
            this.btnSettings.Click += new System.EventHandler(this.NavButton_Click);

            this.btnAbout.Anchor = (AnchorStyles)(((AnchorStyles.Top | AnchorStyles.Left) | AnchorStyles.Right));
            this.btnAbout.Location = new System.Drawing.Point(0, 240);
            this.btnAbout.Name = "btnAbout";
            this.btnAbout.Size = new System.Drawing.Size(196, 42);
            this.btnAbout.TabIndex = 5;
            this.btnAbout.Text = "  ℹ   关于";
            this.btnAbout.Click += new System.EventHandler(this.NavButton_Click);

            // bottomPanel
            this.bottomPanel.BackColor = AppTheme.SidebarBackground;
            this.bottomPanel.Controls.Add(this.lblVersion);
            this.bottomPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.bottomPanel.Height = 50;
            this.bottomPanel.Location = new System.Drawing.Point(12, 704);
            this.bottomPanel.Name = "bottomPanel";
            this.bottomPanel.Size = new System.Drawing.Size(196, 50);
            this.bottomPanel.TabIndex = 1;

            // lblVersion
            this.lblVersion.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.lblVersion.Font = new System.Drawing.Font("Microsoft YaHei UI", 8F);
            this.lblVersion.ForeColor = AppTheme.TextMuted;
            this.lblVersion.Height = 8;
            this.lblVersion.Location = new System.Drawing.Point(0, 42);
            this.lblVersion.Name = "lblVersion";
            this.lblVersion.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblVersion.Size = new System.Drawing.Size(196, 8);
            this.lblVersion.Text = " ";
            this.lblVersion.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // contentPanel
            this.contentPanel.BackColor = AppTheme.Background;
            this.contentPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.contentPanel.Location = new System.Drawing.Point(220, 0);
            this.contentPanel.Name = "contentPanel";
            this.contentPanel.Padding = new System.Windows.Forms.Padding(0);
            this.contentPanel.Size = new System.Drawing.Size(1060, 770);
            this.contentPanel.TabIndex = 0;

            // statusBar
            this.statusBar.BackColor = AppTheme.SidebarBackground;
            this.statusBar.Controls.Add(this.lblStatus);
            this.statusBar.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.statusBar.Location = new System.Drawing.Point(0, 770);
            this.statusBar.Name = "statusBar";
            this.statusBar.Size = new System.Drawing.Size(1280, 30);
            this.statusBar.TabIndex = 2;

            // lblStatus
            this.lblStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStatus.Font = new System.Drawing.Font("Microsoft YaHei UI", 8.5F);
            this.lblStatus.ForeColor = AppTheme.Success;
            this.lblStatus.Location = new System.Drawing.Point(0, 0);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblStatus.Size = new System.Drawing.Size(1280, 30);
            this.lblStatus.Text = "就绪";
            this.lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // MainForm
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = AppTheme.Background;
            this.ClientSize = new System.Drawing.Size(1280, 800);
            this.Controls.Add(this.contentPanel);
            this.Controls.Add(this.sidebar);
            this.Controls.Add(this.statusBar);
            this.DoubleBuffered = true;
            this.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.MinimumSize = new System.Drawing.Size(1100, 700);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "AquaDy";

            this.sidebar.ResumeLayout(false);
            this.logoPanel.ResumeLayout(false);
            this.logoPanel.PerformLayout();
            this.navContainer.ResumeLayout(false);
            this.bottomPanel.ResumeLayout(false);
            this.statusBar.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion
    }
}