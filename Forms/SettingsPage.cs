using AquaDy.Models;
using AquaDy.Services;

namespace AquaDy.Forms
{
    public partial class SettingsPage : UserControl
    {
        private AppSettings settings;

        public SettingsPage(AppSettings settings)
        {
            this.settings = settings;
            InitializeComponent();
            LoadSettings();
        }

        private void btnBrowseDownload_Click(object? sender, EventArgs e)
        {
            using var dlg = new FolderBrowserDialog();
            if (dlg.ShowDialog() == DialogResult.OK)
                txtDownloadPath.Text = dlg.SelectedPath;
        }

        private void btnFf_Click(object? sender, EventArgs e)
        {
            using var dlg = new OpenFileDialog { Filter = "ffmpeg|ffmpeg.exe|所有|*.*" };
            if (dlg.ShowDialog() == DialogResult.OK) txtFFmpegPath.Text = dlg.FileName;
        }

        private async void btnDownloadFfmpeg_Click(object? sender, EventArgs e)
        {
            btnDownloadFfmpeg.Enabled = false;
            btnFf.Enabled = false;
            try
            {
                var prog = new Progress<(int percent, string status)>(p =>
                {
                    if (lblToolsTip != null)
                        lblToolsTip.Text = p.status;
                });
                var path = await FfmpegHelper.DownloadAndInstallAsync(prog);
                txtFFmpegPath.Text = path;
                settings.FFmpegPath = path;
                settings.Save();
                MessageBox.Show("FFmpeg 已安装并保存：\n" + path, "完成",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                if (lblToolsTip != null)
                    lblToolsTip.Text = "已安装到 Lib\\ffmpeg.exe。直播录制将自动使用此路径。";
            }
            catch (Exception ex)
            {
                MessageBox.Show("下载失败：\n" + ex.Message, "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnDownloadFfmpeg.Enabled = true;
                btnFf.Enabled = true;
            }
        }

        private void btnReset_Click(object? sender, EventArgs e)
        {
            if (MessageBox.Show("恢复为默认设置？（不会自动保存，需再点保存）", "恢复默认",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            var d = new AppSettings();
            txtDownloadPath.Text = d.DownloadPath;
            if (txtNameTemplate != null) txtNameTemplate.Text = d.NameTemplate;
            numConcurrent.Value = Math.Max(numConcurrent.Minimum, Math.Min(numConcurrent.Maximum, d.MaxConcurrent));
            chkSkipExisting.Checked = d.SkipExisting;
            chkDownloadCover.Checked = d.DownloadCover;
            txtFFmpegPath.Text = File.Exists(AppSettings.DefaultFFmpegPath)
                ? AppSettings.DefaultFFmpegPath : "";
            MessageBox.Show("已填入默认值，请点击「保存设置」生效", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnPasteCookie_Click(object? sender, EventArgs e)
        {
            if (Clipboard.ContainsText())
                txtDouyinCookie.Text = Clipboard.GetText();
        }

        private async void btnCheckCookie_Click(object? sender, EventArgs e)
        {
            btnCheckCookie.Enabled = false;
            btnCheckCookie.Text = "校验中...";
            try
            {
                var basic = CookieHelper.ValidateDouyin(txtDouyinCookie.Text);
                var profile = await CookieHelper.CheckDouyinProfileAsync(txtDouyinCookie.Text);

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
                    "【字段检查】\n" + (basic.ok ? "✓ " : "✗ ") + basic.message + "\n\n" +
                    "【说明】单作品多数可不填 Cookie；主页/合集建议使用已登录 Cookie；直播录制通常不需要。";

                MessageBox.Show(msg, "主页校验结果", MessageBoxButtons.OK,
                    profile.ok ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            }
            finally
            {
                btnCheckCookie.Enabled = true;
                btnCheckCookie.Text = "主页校验";
            }
        }

        private void LoadSettings()
        {
            txtDownloadPath.Text = string.IsNullOrWhiteSpace(settings.DownloadPath)
                ? AppSettings.DefaultDownloadPath
                : settings.DownloadPath;
            txtNameTemplate.Text = settings.NameTemplate;
            numConcurrent.Value = settings.MaxConcurrent;
            numRetry.Value = settings.MaxRetry;
            chkSkipExisting.Checked = settings.SkipExisting;
            chkDownloadCover.Checked = settings.DownloadCover;
            if (chkEnableLogging != null) chkEnableLogging.Checked = settings.EnableLogging;
            if (chkFolderByAuthor != null) chkFolderByAuthor.Checked = settings.FolderByAuthor;

            txtDouyinCookie.Text = settings.DouyinCookie;
            chkUseProxy.Checked = settings.UseProxy;
            txtProxy.Text = settings.Proxy;
            txtFFmpegPath.Text = string.IsNullOrWhiteSpace(settings.FFmpegPath)
                ? (File.Exists(AppSettings.DefaultFFmpegPath) ? AppSettings.DefaultFFmpegPath : "")
                : settings.FFmpegPath;
        }

        private void btnSave_Click(object? sender, EventArgs e)
        {
            settings.DownloadPath = txtDownloadPath.Text.Trim();
            if (string.IsNullOrWhiteSpace(settings.DownloadPath))
                settings.DownloadPath = AppSettings.DefaultDownloadPath;
            settings.NameTemplate = txtNameTemplate.Text.Trim();
            settings.MaxConcurrent = (int)numConcurrent.Value;
            settings.MaxRetry = (int)numRetry.Value;
            settings.SkipExisting = chkSkipExisting.Checked;
            settings.DownloadCover = chkDownloadCover.Checked;
            if (chkEnableLogging != null) settings.EnableLogging = chkEnableLogging.Checked;
            ParseLogger.Enabled = settings.EnableLogging;

            if (chkFolderByAuthor != null) settings.FolderByAuthor = chkFolderByAuthor.Checked;

            settings.DouyinCookie = txtDouyinCookie.Text.Trim();
            settings.UseProxy = chkUseProxy.Checked;
            settings.Proxy = txtProxy.Text.Trim();
            settings.FFmpegPath = txtFFmpegPath.Text.Trim();
            if (!string.IsNullOrWhiteSpace(settings.FFmpegPath)
                && settings.FFmpegPath != "ffmpeg"
                && !File.Exists(settings.FFmpegPath)
                && !Directory.Exists(settings.FFmpegPath))
            {
                MessageBox.Show("ffmpeg 路径无效：\n" + settings.FFmpegPath, "路径错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            settings.Save();

            if (FindForm() is MainForm main)
                main.RefreshCookieStatus();

            MessageBox.Show("设置已保存", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}