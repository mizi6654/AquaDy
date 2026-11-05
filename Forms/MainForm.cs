using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using AquaDy.Controls;
using AquaDy.Models;

namespace AquaDy.Forms
{
    public partial class MainForm : Form
    {
        private Panel sidebar;
        private Panel contentPanel;
        private Panel statusBar;
        private Label lblStatus;
        private Label lblVersion;
        private Label lblTaskInfo;

        // 导航按钮
        private NavButton btnDownload;
        private NavButton btnCompleted;
        private NavButton btnAccount;
        private NavButton btnLive;
        private NavButton btnSettings;
        private NavButton btnAbout;

        // 内容页
        private DownloadPage downloadPage;
        private CompletedPage completedPage;
        private AccountPage accountPage;
        private LivePage livePage;
        private SettingsPage settingsPage;
        private AboutPage aboutPage;

        private AppSettings settings;

        public MainForm()
        {
            settings = AppSettings.Load();
            InitializeComponent();
            SwitchPage(downloadPage);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // 主窗体
            this.Text = "AquaDy";
            this.Size = new Size(1280, 800);
            this.MinimumSize = new Size(1100, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = AppTheme.Background;
            this.Font = new Font("Microsoft YaHei UI", 9F);
            this.DoubleBuffered = true;

            // ========== 左侧导航栏 ==========
            sidebar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 220,
                BackColor = AppTheme.SidebarBackground,
                Padding = new Padding(12, 16, 12, 16)
            };

            // Logo 区域
            var logoPanel = new Panel
            {
                Height = 60,
                Dock = DockStyle.Top,
                BackColor = AppTheme.SidebarBackground
            };

            var logoIcon = new Label
            {
                Text = "↓",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = AppTheme.Primary,
                AutoSize = true,
                Location = new Point(8, 12)
            };

            var logoText = new Label
            {
                Text = "AquaDy",
                Font = new Font("Microsoft YaHei UI", 11F, FontStyle.Bold),
                ForeColor = AppTheme.TextPrimary,
                AutoSize = true,
                Location = new Point(40, 10)
            };

            var logoSub = new Label
            {
                Text = "抖音下载录制  v1.0.0",
                Font = new Font("Microsoft YaHei UI", 8F),
                ForeColor = AppTheme.TextMuted,
                AutoSize = true,
                Location = new Point(40, 32)
            };

            logoPanel.Controls.Add(logoIcon);
            logoPanel.Controls.Add(logoText);
            logoPanel.Controls.Add(logoSub);

            // 导航按钮容器
            var navContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = AppTheme.SidebarBackground,
                Padding = new Padding(0, 10, 0, 0)
            };

            btnDownload = CreateNavButton("下载任务", "↓", 0);
            btnAccount = CreateNavButton("账号批量", "👤", 1);
            btnLive = CreateNavButton("直播下载", "▶", 2);
            btnCompleted = CreateNavButton("已完成", "✓", 3);
            btnSettings = CreateNavButton("设置", "⚙", 4);
            btnAbout = CreateNavButton("关于", "ℹ", 5);

            btnDownload.IsSelected = true;

            navContainer.Controls.Add(btnDownload);
            navContainer.Controls.Add(btnAccount);
            navContainer.Controls.Add(btnLive);
            navContainer.Controls.Add(btnCompleted);
            navContainer.Controls.Add(btnSettings);
            navContainer.Controls.Add(btnAbout);

            // 底部版本信息
            var bottomPanel = new Panel
            {
                Height = 50,
                Dock = DockStyle.Bottom,
                BackColor = AppTheme.SidebarBackground
            };

            lblVersion = new Label
            {
                Text = " ",
                Font = new Font("Microsoft YaHei UI", 8F),
                ForeColor = AppTheme.TextMuted,
                Dock = DockStyle.Bottom,
                Height = 8,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0)
            };
            bottomPanel.Controls.Add(lblVersion);

            sidebar.Controls.Add(navContainer);
            sidebar.Controls.Add(bottomPanel);
            sidebar.Controls.Add(logoPanel);

            // ========== 右侧内容区 ==========
            contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = AppTheme.Background,
                Padding = new Padding(0)
            };

            // ========== 底部状态栏 ==========
            statusBar = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 30,
                BackColor = AppTheme.SidebarBackground
            };

            // 通栏底栏，文字从最左侧开始（侧栏下方也能看到）
            lblStatus = new Label
            {
                Text = "就绪",
                Font = new Font("Microsoft YaHei UI", 8.5F),
                ForeColor = AppTheme.Success,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            };
            lblTaskInfo = lblStatus;

            statusBar.Controls.Add(lblStatus);

            // 创建各页面
            downloadPage = new DownloadPage { Dock = DockStyle.Fill, Visible = false };
            completedPage = new CompletedPage { Dock = DockStyle.Fill, Visible = false };
            accountPage = new AccountPage { Dock = DockStyle.Fill, Visible = false };
            livePage = new LivePage { Dock = DockStyle.Fill, Visible = false };
            settingsPage = new SettingsPage(settings) { Dock = DockStyle.Fill, Visible = false };
            aboutPage = new AboutPage { Dock = DockStyle.Fill, Visible = false };

            contentPanel.Controls.Add(downloadPage);
            contentPanel.Controls.Add(completedPage);
            contentPanel.Controls.Add(accountPage);
            contentPanel.Controls.Add(livePage);
            contentPanel.Controls.Add(settingsPage);
            contentPanel.Controls.Add(aboutPage);

            // 添加控件到主窗体：先内容与侧栏，最后状态栏，保证底栏通栏且文案可靠左
            this.Controls.Add(contentPanel);
            this.Controls.Add(sidebar);
            this.Controls.Add(statusBar);

            this.ResumeLayout(false);

            // 所有控件创建完成后再刷新 Cookie 状态（避免 lblStatus 为 null）
            RefreshCookieStatus();
        }

        private NavButton CreateNavButton(string text, string icon, int index)
        {
            var btn = new NavButton
            {
                Text = $"  {icon}   {text}",
                Location = new Point(0, index * 48),
                Size = new Size(196, 42),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            btn.Click += (s, e) =>
            {
                // 取消其他选中
                foreach (Control c in btn.Parent.Controls)
                    if (c is NavButton nb) nb.IsSelected = false;
                btn.IsSelected = true;

                switch (index)
                {
                    case 0: SwitchPage(downloadPage); break;
                    case 1: SwitchPage(accountPage); break;
                    case 2: SwitchPage(livePage); break;
                    case 3: SwitchPage(completedPage); break;
                    case 4: SwitchPage(settingsPage); break;
                    case 5: SwitchPage(aboutPage); break;
                }
            };
            return btn;
        }

        private void SwitchPage(Control page)
        {
            foreach (Control c in contentPanel.Controls)
                c.Visible = false;
            page.Visible = true;
            page.BringToFront();
        }

        public void UpdateStatus(string text)
        {
            if (lblStatus == null) return;
            if (!text.StartsWith("v1."))
                text = "v1.0.0  |  " + text.TrimStart('●', ' ');
            void Apply()
            {
                lblStatus.Text = text;
                if (text.Contains("有效") || text.Contains("已配置"))
                    lblStatus.ForeColor = AppTheme.Success;
                else if (text.Contains("未") || text.Contains("失败"))
                    lblStatus.ForeColor = AppTheme.Warning;
            }
            if (lblStatus.InvokeRequired) lblStatus.Invoke(Apply);
            else Apply();
        }

        public void UpdateTaskInfo(int total, int active)
        {
            // 底部状态栏只保留 Cookie 信息，不再显示任务计数
        }

        /// <summary>
        /// 根据当前 Cookie 配置刷新侧边栏与状态栏显示
        /// </summary>
        public void RefreshCookieStatus()
        {
            if (lblStatus == null) return;
            var s = AppSettings.Load();
            bool dy = !string.IsNullOrWhiteSpace(s.DouyinCookie);
            bool tt = !string.IsNullOrWhiteSpace(s.TikTokCookie);
            string cookiePart;
            Color color;
            if (dy && !string.IsNullOrWhiteSpace(s.CookieUserName))
            {
                cookiePart = "Cookie 有效 · 抖音信息：" + s.CookieUserName +
                    (string.IsNullOrWhiteSpace(s.CookieUniqueId) ? "" : "（" + s.CookieUniqueId + "）");
                color = AppTheme.Success;
            }
            else if (dy || tt)
            {
                cookiePart = "Cookie 已配置" + (dy ? "（抖音）" : "") + (tt ? "（TikTok）" : "") + " · 可在设置中点「主页校验」确认";
                color = AppTheme.Success;
            }
            else
            {
                cookiePart = "未配置 Cookie，请先在「设置」中填写后再解析/下载";
                color = AppTheme.Warning;
            }
            string text = "v1.0.0  |  " + cookiePart;
            void Apply()
            {
                lblStatus.Text = text;
                lblStatus.ForeColor = color;
            }
            if (lblStatus.InvokeRequired) lblStatus.Invoke(Apply);
            else Apply();
        }
    }

    /// <summary>
    /// 自定义导航按钮
    /// </summary>
    public class NavButton : Control
    {
        private bool _isSelected;
        private bool _isHover;

        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; Invalidate(); }
        }

        public NavButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Cursor = Cursors.Hand;
            Font = new Font("Microsoft YaHei UI", 9.5F);
            Height = 42;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _isHover = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _isHover = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(AppTheme.SidebarBackground);

            Color bg = _isSelected ? Color.FromArgb(45, 35, 70) :
                       _isHover ? AppTheme.NavHover : Color.Transparent;

            if (bg != Color.Transparent)
            {
                using var path = RoundedRect(new Rectangle(0, 2, Width - 1, Height - 4), 8);
                using var brush = new SolidBrush(bg);
                g.FillPath(brush, path);

                if (_isSelected)
                {
                    // 左侧紫色指示条
                    using var indicator = new SolidBrush(AppTheme.Primary);
                    g.FillRectangle(indicator, 0, 8, 3, Height - 16);
                }
            }

            Color textColor = _isSelected ? AppTheme.Primary : AppTheme.TextSecondary;
            TextRenderer.DrawText(g, Text, Font, new Rectangle(12, 0, Width - 24, Height),
                textColor, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
        }

        private GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
