using System.Drawing;
using System.Windows.Forms;
using AquaDy.Services;

namespace AquaDy.Forms
{
    /// <summary>更新进度窗：固定大小、居中、勿关闭提示。</summary>
    public class UpdateProgressForm : Form
    {
        private readonly Label _lbl;
        private readonly ProgressBar _bar;
        private readonly string _sourceName;

        public UpdateProgressForm(string sourceName = "GitHub")
        {
            _sourceName = sourceName ?? "GitHub";
            Text = "更新中...";
            Size = new Size(580, 260);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ControlBox = true;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.White;
            ShowInTaskbar = false;

            _lbl = new Label
            {
                Location = new Point(30, 24),
                Size = new Size(520, 120),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.DarkBlue,
                Font = new Font("Microsoft YaHei UI", 11F),
                Text =
                    $"正在从 {_sourceName} 下载更新包...\r\n" +
                    "(网络环境不同，下载速度可能较慢，请耐心等待)\r\n" +
                    "请勿关闭此窗口"
            };
            Controls.Add(_lbl);

            _bar = new ProgressBar
            {
                Location = new Point(30, 160),
                Size = new Size(520, 40),
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 40
            };
            Controls.Add(_bar);

            ParseLogger.Op("更新进度窗", "已打开 source=" + _sourceName);
            FormClosed += (_, _) => ParseLogger.Op("更新进度窗", "已关闭");
        }

        public void UpdateStatus(string message, int? percent = null)
        {
            void Apply()
            {
                _lbl.Text = message;
                if (percent.HasValue)
                {
                    if (_bar.Style != ProgressBarStyle.Continuous)
                    {
                        _bar.Style = ProgressBarStyle.Continuous;
                        _bar.Minimum = 0;
                        _bar.Maximum = 100;
                    }
                    _bar.Value = Math.Max(0, Math.Min(100, percent.Value));
                }
            }

            // 进度百分比只在 0/25/50/75/100 打日志，避免刷屏
            if (!percent.HasValue || percent == 0 || percent == 25 || percent == 50 ||
                percent == 75 || percent >= 99)
            {
                var oneLine = (message ?? "").Replace("\r", " ").Replace("\n", " ");
                if (oneLine.Length > 120) oneLine = oneLine.Substring(0, 120) + "…";
                ParseLogger.Info($"更新进度 pct={percent?.ToString() ?? "-"} {oneLine}");
            }

            if (IsDisposed) return;
            if (InvokeRequired) BeginInvoke(Apply);
            else Apply();
        }

        public void Complete(string message)
        {
            ParseLogger.Op("更新进度窗", "Complete: " + (message ?? ""));
            void Apply()
            {
                _lbl.Text = message;
                _bar.Style = ProgressBarStyle.Continuous;
                _bar.Value = 100;
            }
            if (IsDisposed) return;
            if (InvokeRequired) Invoke(Apply);
            else Apply();
        }
    }
}