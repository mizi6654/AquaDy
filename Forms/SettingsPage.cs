using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using AquaDy.Controls;
using AquaDy.Models;
using AquaDy.Services;

namespace AquaDy.Forms
{
    /// <summary>
    /// 设置页面（参考第二张截图的分 Tab 风格）
    /// </summary>
    public class SettingsPage : UserControl
    {
        private AppSettings settings;
        private TabControl tabControl;
        private DarkButton btnSave;
        private DarkButton btnReset;

        // 下载与处理
        private TextBox txtDownloadPath;
        private TextBox txtNameTemplate;
        private NumericUpDown numConcurrent;
        private NumericUpDown numRetry;
        private CheckBox chkSkipExisting;
        private CheckBox chkDownloadCover;
        private CheckBox chkEnableLogging;

        // Cookie
        private TextBox txtDouyinCookie;
        private TextBox txtTikTokCookie;
        private TextBox txtDeviceId;
        private DarkButton btnPasteCookie;
        private DarkButton btnCheckCookie;

        // 网络
        private CheckBox chkUseProxy;
        private TextBox txtProxy;
        private TextBox txtFFmpegPath;
        
        public SettingsPage(AppSettings settings)
        {
            this.settings = settings;
            InitializeComponent();
            LoadSettings();
        }

        private void InitializeComponent()
        {
            this.BackColor = AppTheme.Background;
            this.Padding = new Padding(24, 16, 24, 16);

            // 标题栏
            var title = new Label
            {
                Text = "设置",
                Font = new Font("Microsoft YaHei UI", 16F, FontStyle.Bold),
                ForeColor = AppTheme.TextPrimary,
                AutoSize = true,
                Location = new Point(24, 12)
            };

            var sub = new Label
            {
                Text = "下载、网络、工具和外观。",
                Font = new Font("Microsoft YaHei UI", 9F),
                ForeColor = AppTheme.TextSecondary,
                AutoSize = true,
                Location = new Point(24, 44)
            };

            // 右上角按钮
            btnSave = new DarkButton
            {
                Text = "保存设置",
                IsPrimary = true,
                Size = new Size(110, 36),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnSave.Location = new Point(this.Width - 140, 16);
            btnSave.Click += BtnSave_Click;

            btnReset = new DarkButton
            {
                Text = "恢复默认",
                Size = new Size(100, 36),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnReset.Location = new Point(this.Width - 260, 16);
            btnReset.Click += (s, e) =>
            {
                if (MessageBox.Show("恢复为默认设置？（不会自动保存，需再点保存）", "恢复默认",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
                var d = new AppSettings();
                txtDownloadPath.Text = d.DownloadPath;
                if (txtNameTemplate != null) txtNameTemplate.Text = d.NameTemplate;
                numConcurrent.Value = Math.Max(numConcurrent.Minimum, Math.Min(numConcurrent.Maximum, d.MaxConcurrent));
                chkSkipExisting.Checked = d.SkipExisting;
                chkDownloadCover.Checked = d.DownloadCover;
                MessageBox.Show("已填入默认值，请点击「保存设置」生效", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            // TabControl
            tabControl = new TabControl
            {
                Location = new Point(24, 80),
                Size = new Size(this.Width - 48, this.Height - 120),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Font = new Font("Microsoft YaHei UI", 9.5F)
            };

            // 自定义 Tab 颜色需要 owner-draw，这里先用系统样式，后续可美化
            var tabDownload = new TabPage("下载与处理") { BackColor = AppTheme.Background, Padding = new Padding(16) };
            var tabCookie = new TabPage("网络与 Cookie") { BackColor = AppTheme.Background, Padding = new Padding(16) };
            var tabTools = new TabPage("工具与更新") { BackColor = AppTheme.Background, Padding = new Padding(16) };

            BuildDownloadTab(tabDownload);
            BuildCookieTab(tabCookie);
            BuildToolsTab(tabTools);

            tabControl.TabPages.Add(tabDownload);
            tabControl.TabPages.Add(tabCookie);
            tabControl.TabPages.Add(tabTools);

            this.Controls.AddRange(new Control[] { title, sub, btnSave, btnReset, tabControl });

            this.Resize += (s, e) =>
            {
                btnSave.Left = this.Width - 140;
                btnReset.Left = this.Width - 260;
                tabControl.Width = this.Width - 48;
                tabControl.Height = this.Height - 120;
            };
        }

        private void BuildDownloadTab(TabPage page)
        {
            int y = 10;

            AddLabel(page, "下载保存路径", y);
            y += 24;
            txtDownloadPath = AddTextBox(page, y, 400);
            var btnBrowse = new DarkButton
            {
                Text = "浏览...",
                Size = new Size(80, 32),
                Location = new Point(420, y)
            };
            btnBrowse.Click += (s, e) =>
            {
                using var dlg = new FolderBrowserDialog();
                if (dlg.ShowDialog() == DialogResult.OK)
                    txtDownloadPath.Text = dlg.SelectedPath;
            };
            page.Controls.Add(btnBrowse);
            y += 50;

            AddLabel(page, "作品文件命名（下载任务 / 账号批量，可改）", y);
            y += 24;
            txtNameTemplate = AddTextBox(page, y, 500);
            y += 36;
            var lblNameTip = new Label
            {
                Text = "变量：{create_time} {nickname} {desc} {id}。改错会导致文件名异常，可点「恢复默认」。\n直播录制不走此模板。下方「封面」「作者子目录」对下载任务与账号批量全局生效。",
                ForeColor = AppTheme.TextSecondary,
                Location = new Point(16, y),
                Size = new Size(560, 40)
            };
            page.Controls.Add(lblNameTip);
            y += 50;

            AddLabel(page, "最大并发下载数", y);
            y += 24;
            numConcurrent = new NumericUpDown
            {
                Location = new Point(16, y),
                Size = new Size(80, 28),
                Minimum = 1,
                Maximum = 10,
                Value = 3,
                BackColor = AppTheme.CardBackground,
                ForeColor = AppTheme.TextPrimary
            };
            page.Controls.Add(numConcurrent);
            y += 50;

            AddLabel(page, "失败重试次数", y);
            y += 24;
            numRetry = new NumericUpDown
            {
                Location = new Point(16, y),
                Size = new Size(80, 28),
                Minimum = 0,
                Maximum = 10,
                Value = 3,
                BackColor = AppTheme.CardBackground,
                ForeColor = AppTheme.TextPrimary
            };
            page.Controls.Add(numRetry);
            y += 50;

            chkSkipExisting = new CheckBox
            {
                Text = "自动跳过已存在的文件",
                ForeColor = AppTheme.TextPrimary,
                Location = new Point(16, y),
                AutoSize = true,
                Checked = true,
                FlatStyle = FlatStyle.Flat
            };
            page.Controls.Add(chkSkipExisting);
            y += 30;

            chkDownloadCover = new CheckBox
            {
                Text = "默认下载封面图",
                ForeColor = AppTheme.TextPrimary,
                Location = new Point(16, y),
                AutoSize = true,
                FlatStyle = FlatStyle.Flat
            };
            page.Controls.Add(chkDownloadCover);
            y += 30;

            chkEnableLogging = new CheckBox
            {
                Text = "记录操作日志（解析/下载/暂停，写入程序目录 log/）",
                ForeColor = AppTheme.TextPrimary,
                Location = new Point(16, y),
                AutoSize = true,
                Checked = true,
                FlatStyle = FlatStyle.Flat
            };
            page.Controls.Add(chkEnableLogging);
            y += 30;
            y += 10;

            var chkFolderByAuthor = new CheckBox
            {
                Text = "按作者名创建子目录保存",
                ForeColor = AppTheme.TextPrimary,
                Location = new Point(16, y),
                AutoSize = true,
                Checked = true,
                FlatStyle = FlatStyle.Flat
            };
            page.Controls.Add(chkFolderByAuthor);
        }

        private void BuildCookieTab(TabPage page)
        {
            int y = 10;

            AddLabel(page, "抖音 Cookie（完整字符串，格式 key1=value1; key2=value2; ... 与原项目一致）", y);
            y += 24;
            txtDouyinCookie = new TextBox
            {
                Location = new Point(16, y),
                Size = new Size(page.Width - 40, 80),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = AppTheme.CardBackground,
                ForeColor = AppTheme.TextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            page.Controls.Add(txtDouyinCookie);
            y += 95;

            btnPasteCookie = new DarkButton
            {
                Text = "从剪贴板粘贴",
                Size = new Size(120, 32),
                Location = new Point(16, y)
            };
            btnPasteCookie.Click += (s, e) =>
            {
                if (Clipboard.ContainsText())
                    txtDouyinCookie.Text = Clipboard.GetText();
            };
            page.Controls.Add(btnPasteCookie);

            btnCheckCookie = new DarkButton
            {
                Text = "主页校验",
                Size = new Size(110, 32),
                Location = new Point(150, y)
            };
            btnCheckCookie.Click += async (s, e) =>
            {
                btnCheckCookie.Enabled = false;
                btnCheckCookie.Text = "校验中...";
                try
                {
                    var basic = CookieHelper.ValidateDouyin(txtDouyinCookie.Text);
                    var profile = await CookieHelper.CheckDouyinProfileAsync(txtDouyinCookie.Text);
                    var tt = CookieHelper.ValidateTikTok(txtTikTokCookie.Text);

                    if (profile.ok)
                    {
                        settings.CookieUserName = profile.userName;
                        settings.CookieUniqueId = profile.uniqueId;
                        settings.DouyinCookie = txtDouyinCookie.Text.Trim();
                        settings.Save();
                        if (FindForm() is MainForm main)
                            main.RefreshCookieStatus();
                    }

                    var msg =
                        "【主页校验】\n" + (profile.ok ? "✓ " : "✗ ") + profile.message + "\n\n" +
                        "【字段检查】\n" + (basic.ok ? "✓ " : "✗ ") + basic.message;
                    if (!string.IsNullOrEmpty(txtTikTokCookie.Text))
                        msg += "\n\n【TikTok】\n" + (tt.ok ? "✓ " : "✗ ") + tt.message;
                    MessageBox.Show(msg, "主页校验结果", MessageBoxButtons.OK,
                        profile.ok ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
                }
                finally
                {
                    btnCheckCookie.Enabled = true;
                    btnCheckCookie.Text = "主页校验";
                }
            };
            page.Controls.Add(btnCheckCookie);
            y += 50;

            AddLabel(page, "TikTok Cookie", y);
            y += 24;
            txtTikTokCookie = new TextBox
            {
                Location = new Point(16, y),
                Size = new Size(page.Width - 40, 60),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = AppTheme.CardBackground,
                ForeColor = AppTheme.TextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            page.Controls.Add(txtTikTokCookie);
            y += 75;

            AddLabel(page, "TikTok device_id（部分接口需要）", y);
            y += 24;
            txtDeviceId = AddTextBox(page, y, 400);
        }

        private void BuildToolsTab(TabPage page)
        {
            int y = 10;

            chkUseProxy = new CheckBox
            {
                Text = "启用代理",
                ForeColor = AppTheme.TextPrimary,
                Location = new Point(16, y),
                AutoSize = true,
                FlatStyle = FlatStyle.Flat
            };
            page.Controls.Add(chkUseProxy);
            y += 30;

            AddLabel(page, "代理地址（例：http://127.0.0.1:7890）", y);
            y += 24;
            txtProxy = AddTextBox(page, y, 400);
            y += 50;

            AddLabel(page, "ffmpeg 路径（直播录制，指向 ffmpeg.exe 或 bin 目录）", y);
            y += 24;
            txtFFmpegPath = AddTextBox(page, y, 360);
            var btnFf = new DarkButton { Text = "浏览", Size = new Size(70, 28), Location = new Point(390, y) };
            btnFf.Click += (s, e) =>
            {
                using var dlg = new OpenFileDialog { Filter = "ffmpeg|ffmpeg.exe|所有|*.*" };
                if (dlg.ShowDialog() == DialogResult.OK) txtFFmpegPath.Text = dlg.FileName;
            };
            page.Controls.Add(btnFf);
            y += 40;


            var lblTip = new Label
            {
                Text = "提示：你本机若已有 ffmpeg 解压包，请在设置中选择 bin\\ffmpeg.exe。\n" +
                       "直播录制只使用 ffmpeg：选择解压包内 bin\ffmpeg.exe 后保存即可。",
                ForeColor = AppTheme.TextMuted,
                Location = new Point(16, y),
                Size = new Size(520, 50)
            };
            page.Controls.Add(lblTip);
        }

        private void AddLabel(Control parent, string text, int y)
        {
            var lbl = new Label
            {
                Text = text,
                ForeColor = AppTheme.TextSecondary,
                Location = new Point(16, y),
                AutoSize = true,
                Font = new Font("Microsoft YaHei UI", 9F)
            };
            parent.Controls.Add(lbl);
        }

        private TextBox AddTextBox(Control parent, int y, int width)
        {
            var tb = new TextBox
            {
                Location = new Point(16, y),
                Size = new Size(width, 28),
                BackColor = AppTheme.CardBackground,
                ForeColor = AppTheme.TextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Microsoft YaHei UI", 9.5F)
            };
            parent.Controls.Add(tb);
            return tb;
        }

        private void LoadSettings()
        {
            txtDownloadPath.Text = settings.DownloadPath;
            txtNameTemplate.Text = settings.NameTemplate;
            numConcurrent.Value = settings.MaxConcurrent;
            numRetry.Value = settings.MaxRetry;
            chkSkipExisting.Checked = settings.SkipExisting;
            chkDownloadCover.Checked = settings.DownloadCover;
            if (chkEnableLogging != null) chkEnableLogging.Checked = settings.EnableLogging;
            
            // FolderByAuthor 若控件存在则同步
            foreach (Control c in tabControl.TabPages[0].Controls)
                if (c is CheckBox cb && cb.Text.Contains("作者"))
                    cb.Checked = settings.FolderByAuthor;
            txtDouyinCookie.Text = settings.DouyinCookie;
            txtTikTokCookie.Text = settings.TikTokCookie;
            txtDeviceId.Text = settings.TikTokDeviceId;
            chkUseProxy.Checked = settings.UseProxy;
            txtProxy.Text = settings.Proxy;
            txtFFmpegPath.Text = settings.FFmpegPath;
            
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            settings.DownloadPath = txtDownloadPath.Text.Trim();
            settings.NameTemplate = txtNameTemplate.Text.Trim();
            settings.MaxConcurrent = (int)numConcurrent.Value;
            settings.MaxRetry = (int)numRetry.Value;
            settings.SkipExisting = chkSkipExisting.Checked;
            settings.DownloadCover = chkDownloadCover.Checked;
            if (chkEnableLogging != null) settings.EnableLogging = chkEnableLogging.Checked;
            ParseLogger.Enabled = settings.EnableLogging;
            
            foreach (Control c in tabControl.TabPages[0].Controls)
                if (c is CheckBox cb && cb.Text.Contains("作者"))
                    settings.FolderByAuthor = cb.Checked;
            settings.DouyinCookie = txtDouyinCookie.Text.Trim();
            settings.TikTokCookie = txtTikTokCookie.Text.Trim();
            settings.TikTokDeviceId = txtDeviceId.Text.Trim();
            settings.UseProxy = chkUseProxy.Checked;
            settings.Proxy = txtProxy.Text.Trim();
            settings.FFmpegPath = txtFFmpegPath.Text.Trim();
            if (!string.IsNullOrWhiteSpace(settings.FFmpegPath) && settings.FFmpegPath != "ffmpeg" && !File.Exists(settings.FFmpegPath) && !Directory.Exists(settings.FFmpegPath))
            {
                MessageBox.Show("ffmpeg 路径无效：\n" + settings.FFmpegPath, "路径错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            settings.Save();

            // 通知主窗体刷新 Cookie 状态显示
            if (FindForm() is MainForm main)
                main.RefreshCookieStatus();

            MessageBox.Show("设置已保存", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
