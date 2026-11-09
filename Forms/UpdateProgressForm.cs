using System.Drawing;
using System.Windows.Forms;
using AquaDy.Services;

namespace AquaDy.Forms
{
    public partial class UpdateProgressForm : Form
    {
        private readonly string _sourceName;

        public UpdateProgressForm(string sourceName = "GitHub")
        {
            _sourceName = sourceName ?? "GitHub";
            InitializeComponent();

            _lbl.Text =
                $"正在从 {_sourceName} 下载更新包...\r\n" +
                "(网络环境不同，下载速度可能较慢，请耐心等待)\r\n" +
                "请勿关闭此窗口";

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