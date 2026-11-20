using AquaDy.Models;
using AquaDy.Services;

namespace AquaDy.Forms
{
    public class FfmpegSetupForm : Form
    {
        private readonly TextBox _txtPath;
        private readonly Button _btnBrowse;
        private readonly Button _btnDownload;
        private readonly Button _btnOk;
        private readonly Button _btnCancel;
        private readonly Label _lblTip;
        private readonly Label _lblStatus;
        private readonly ProgressBar _bar;
        private bool _busy;

        public FfmpegSetupForm()
        {
            Text = "需要配置 FFmpeg";
            ClientSize = new Size(520, 220);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            // 系统默认外观（不套深色主题）
            Font = SystemFonts.MessageBoxFont;
            AutoScaleMode = AutoScaleMode.Font;

            _lblTip = new Label
            {
                Text = "直播录制需要 ffmpeg.exe。可一键从 GitHub 下载到程序目录 Lib\\，或手动选择已有文件。",
                Location = new Point(14, 12),
                Size = new Size(490, 36),
                AutoSize = false
            };

            _txtPath = new TextBox
            {
                Location = new Point(14, 52),
                Size = new Size(330, 23),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            var cur = AppSettings.Load().FFmpegPath;
            if (!string.IsNullOrWhiteSpace(cur))
                _txtPath.Text = cur;
            else if (File.Exists(FfmpegHelper.DefaultExe))
                _txtPath.Text = FfmpegHelper.DefaultExe;

            _btnBrowse = new Button
            {
                Text = "浏览…",
                Location = new Point(350, 50),
                Size = new Size(75, 26),
                UseVisualStyleBackColor = true
            };
            _btnBrowse.Click += (_, _) =>
            {
                using var dlg = new OpenFileDialog
                {
                    Filter = "ffmpeg|ffmpeg.exe|所有文件|*.*",
                    Title = "选择 ffmpeg.exe"
                };
                if (dlg.ShowDialog(this) == DialogResult.OK)
                    _txtPath.Text = dlg.FileName;
            };

            _btnDownload = new Button
            {
                Text = "一键下载",
                Location = new Point(430, 50),
                Size = new Size(75, 26),
                UseVisualStyleBackColor = true
            };
            _btnDownload.Click += async (_, _) => await DownloadAsync();

            _bar = new ProgressBar
            {
                Location = new Point(14, 88),
                Size = new Size(490, 18),
                Minimum = 0,
                Maximum = 100,
                Style = ProgressBarStyle.Continuous
            };

            _lblStatus = new Label
            {
                Text = "就绪。下载源：GitHub BtbN win64-gpl（约 200MB 压缩包）→ 安装到 Lib\\",
                Location = new Point(14, 112),
                Size = new Size(490, 36),
                AutoSize = false
            };

            _btnOk = new Button
            {
                Text = "确定并保存",
                Location = new Point(320, 168),
                Size = new Size(100, 28),
                UseVisualStyleBackColor = true
            };
            _btnOk.Click += (_, _) =>
            {
                if (_busy) return;
                SaveAndClose();
            };

            _btnCancel = new Button
            {
                Text = "取消",
                Location = new Point(428, 168),
                Size = new Size(76, 28),
                UseVisualStyleBackColor = true,
                DialogResult = DialogResult.Cancel
            };
            CancelButton = _btnCancel;
            AcceptButton = _btnOk;

            Controls.AddRange(new Control[]
            {
                _lblTip, _txtPath, _btnBrowse, _btnDownload, _bar, _lblStatus, _btnOk, _btnCancel
            });

            FormClosing += (_, e) =>
            {
                if (_busy)
                {
                    e.Cancel = true;
                    MessageBox.Show(this, "正在下载，请稍候…", "提示",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };
        }

        private async Task DownloadAsync()
        {
            if (_busy) return;
            _busy = true;
            _btnDownload.Enabled = false;
            _btnBrowse.Enabled = false;
            _btnOk.Enabled = false;
            try
            {
                var prog = new Progress<(int percent, string status)>(p =>
                {
                    _bar.Value = Math.Max(0, Math.Min(100, p.percent));
                    _lblStatus.Text = p.status;
                });
                var path = await FfmpegHelper.DownloadAndInstallAsync(prog);
                _txtPath.Text = path;
                _lblStatus.Text = "已安装并填入路径，请点「确定并保存」";
                _bar.Value = 100;
            }
            catch (Exception ex)
            {
                _lblStatus.Text = "失败：" + ex.Message;
                MessageBox.Show(this, "下载/安装失败：\n" + ex.Message, "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _busy = false;
                _btnDownload.Enabled = true;
                _btnBrowse.Enabled = true;
                _btnOk.Enabled = true;
            }
        }

        private void SaveAndClose()
        {
            var path = _txtPath.Text.Trim();
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                MessageBox.Show(this, "请填写有效的 ffmpeg.exe 路径，或先一键下载。", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var s = AppSettings.Load();
            s.FFmpegPath = path;
            s.Save();
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}