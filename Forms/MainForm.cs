using AquaDy.Models;
using AquaDy.Services;
using AquaDy.Utils;

namespace AquaDy.Forms
{
    public partial class MainForm : Form
    {
        private DownloadPage downloadPage = null!;
        private CompletedPage completedPage = null!;
        private AccountPage accountPage = null!;
        private LivePage livePage = null!;
        private SettingsPage settingsPage = null!;
        private AboutPage aboutPage = null!;

        private AppSettings settings;

        public MainForm()
        {
            settings = AppSettings.Load();
            InitializeComponent();

            // 创建页面（其中 SettingsPage 需要参数，无法在设计器里 new，全部在 .cs 里完成）
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

            btnDownload.IsSelected = true;
            SwitchPage(downloadPage);

            // 版本号放到 Load 里（依赖运行时值）
            this.Load += MainForm_Load;

            if (settings.AutoCheckUpdate)
            {
                Shown += async (s, e) =>
                {
                    await Task.Delay(1500);
                    try { await UpdateChecker.CheckAsync(silentIfLatest: true, this); }
                    catch { }
                };
            }
        }

        private void MainForm_Load(object? sender, EventArgs e)
        {
            logoSub.Text = AppInfo.Subtitle + "  " + AppInfo.VersionText;
            // 所有控件创建完成后再刷新 Cookie 状态（避免 lblStatus 为 null）
            RefreshCookieStatus();
        }

        private void NavButton_Click(object? sender, EventArgs e)
        {
            if (sender is not NavButton btn) return;
            foreach (Control c in btn.Parent!.Controls)
                if (c is NavButton nb) nb.IsSelected = false;
            btn.IsSelected = true;

            if (btn == btnDownload) SwitchPage(downloadPage);
            else if (btn == btnAccount) SwitchPage(accountPage);
            else if (btn == btnLive) SwitchPage(livePage);
            else if (btn == btnCompleted) SwitchPage(completedPage);
            else if (btn == btnSettings) SwitchPage(settingsPage);
            else if (btn == btnAbout) SwitchPage(aboutPage);
        }

        private void SwitchPage(Control page)
        {
            foreach (Control c in contentPanel.Controls)
                c.Visible = false;
            page.Visible = true;
            page.BringToFront();

            if (page is LivePage live)
                live.RefreshSavePathFromSettings();
        }

        public void UpdateStatus(string text)
        {
            if (lblStatus == null) return;
            if (!text.StartsWith("v", StringComparison.OrdinalIgnoreCase))
                text = AppInfo.VersionText + "  |  " + text.TrimStart('●', ' ');
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

        public void UpdateTaskInfo(int total, int active) { /* 原注释保持 */ }

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
            string text = AppInfo.VersionText + "  |  " + cookiePart;
            void Apply()
            {
                lblStatus.Text = text;
                lblStatus.ForeColor = color;
            }
            if (lblStatus.InvokeRequired) lblStatus.Invoke(Apply);
            else Apply();
        }
    }
}