using AquaDy.Controls;
using AquaDy.Models;
using AquaDy.Services;
using AquaDy.Utils;
using System.Diagnostics;

namespace AquaDy.Forms
{
    public class AboutPage : UserControl
    {
        public AboutPage()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.BackColor = Color.FromArgb(32, 34, 42);
            this.Dock = DockStyle.Fill;

            var watermark = new Label
            {
                Text = "AquaDy",
                Font = new Font("Microsoft YaHei UI", 48F, FontStyle.Bold),
                ForeColor = Color.FromArgb(45, 48, 58),
                Location = new Point(40, 220),
                AutoSize = true
            };

            var title = new Label
            {
                Text = "AquaDy",
                Font = new Font("Microsoft YaHei UI", 20F, FontStyle.Bold),
                ForeColor = AppTheme.Primary,
                Location = new Point(40, 36),
                AutoSize = true
            };

            var version = new Label
            {
                Text = AppInfo.VersionText + "  ·  " + AppInfo.Subtitle,
                Font = new Font("Microsoft YaHei UI", 10F),
                ForeColor = AppTheme.TextSecondary,
                Location = new Point(40, 78),
                AutoSize = true
            };

            // 中间可滚动：介绍 + 更新日志（不会被按钮挡住）
            var desc = new TextBox
            {
                Text = Changelog.BuildAboutBody(),
                Font = new Font("Microsoft YaHei UI", 9.5F),
                ForeColor = AppTheme.TextPrimary,
                BackColor = Color.FromArgb(32, 34, 42),
                BorderStyle = BorderStyle.None,
                Multiline = true,
                ReadOnly = true,
                WordWrap = true,                    // 必须：按宽度换行
                ScrollBars = ScrollBars.Vertical,
                Location = new Point(40, 115),
                Size = new Size(Math.Max(400, this.ClientSize.Width - 80),
                   Math.Max(200, this.ClientSize.Height - 195)),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                TabStop = false
            };

            var btnMine = new DarkButton
            {
                Text = "Github主页",
                Size = new Size(120, 36),
                Location = new Point(40, Math.Max(480, this.ClientSize.Height - 56)),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
                IsPrimary = true
            };
            btnMine.Click += (s, e) =>
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = AppInfo.GitHubRepo,
                        UseShellExecute = true
                    });
                }
                catch { }
            };

            var btnUpdate = new DarkButton
            {
                Text = "检查更新",
                Size = new Size(120, 36),
                Location = new Point(170, Math.Max(480, this.ClientSize.Height - 56)),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
                IsPrimary = true
            };
            btnUpdate.Click += async (s, e) =>
            {
                btnUpdate.Enabled = false;
                try { await UpdateChecker.CheckAsync(silentIfLatest: false, this.FindForm()); }
                finally { btnUpdate.Enabled = true; }
            };

            this.Controls.Add(watermark);
            this.Controls.Add(title);
            this.Controls.Add(version);
            this.Controls.Add(desc);
            this.Controls.Add(btnMine);
            this.Controls.Add(btnUpdate);
            watermark.SendToBack();

            // 尺寸变化时保证说明区不压到按钮
            this.Resize += (s, e) =>
            {
                int bottomReserve = 70;
                desc.Height = Math.Max(180, this.ClientSize.Height - desc.Top - bottomReserve);
                desc.Width = Math.Max(400, this.ClientSize.Width - 80);
                btnMine.Top = this.ClientSize.Height - 56;
                btnUpdate.Top = this.ClientSize.Height - 56;
            };

            //var version = new Label
            //{
            //    Text = AppInfo.VersionText + "  ·  " + AppInfo.Subtitle,
            //    Font = new Font("Microsoft YaHei UI", 10F),
            //    ForeColor = AppTheme.TextSecondary,
            //    Location = new Point(40, 78),
            //    AutoSize = true
            //};

            //// 动态：固定介绍 + changelog.txt
            //var desc = new Label
            //{
            //    Text = Changelog.BuildAboutBody(),
            //    Font = new Font("Microsoft YaHei UI", 9.5F),
            //    ForeColor = AppTheme.TextPrimary,
            //    Location = new Point(40, 120),
            //    Size = new Size(720, 400)
            //};

            //// 预留给作者自己的 GitHub 项目主页（请改成你的地址）
            //var btnMine = new DarkButton
            //{
            //    Text = "Github主页",
            //    Size = new Size(120, 36),
            //    Location = new Point(40, 520),
            //    IsPrimary = true
            //};
            //btnMine.Click += (s, e) =>
            //{
            //    try
            //    {
            //        Process.Start(new ProcessStartInfo
            //        {
            //            FileName = AppInfo.GitHubRepo,
            //            UseShellExecute = true
            //        });
            //    }
            //    catch { }
            //};

            //var btnUpdate = new DarkButton
            //{
            //    Text = "检查更新",
            //    Size = new Size(120, 36),
            //    Location = new Point(170, 520),
            //    IsPrimary = true
            //};
            //btnUpdate.Click += async (s, e) =>
            //{
            //    btnUpdate.Enabled = false;
            //    try { await UpdateChecker.CheckAsync(silentIfLatest: false, this.FindForm()); }
            //    finally { btnUpdate.Enabled = true; }
            //};

            //this.Controls.Add(watermark);
            //this.Controls.Add(title);
            //this.Controls.Add(version);
            //this.Controls.Add(desc);
            //this.Controls.Add(btnMine);
            //this.Controls.Add(btnUpdate);
            //watermark.SendToBack();
            //this.ResumeLayout(false);
        }
    }
}
