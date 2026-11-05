using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using AquaDy.Controls;
using AquaDy.Models;

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
                Text = "v1.0.0  ·  抖音下载录制",
                Font = new Font("Microsoft YaHei UI", 10F),
                ForeColor = AppTheme.TextSecondary,
                Location = new Point(40, 78),
                AutoSize = true
            };

            var desc = new Label
            {
                Text =
                    "AquaDy（Aqua + Dy）是面向抖音的桌面下载与直播录制工具，基于 C# WinForms。\n\n" +
                    "【能做什么】\n" +
                    "• 单作品 / 多链接批量下载（优先无水印，可选封面）\n" +
                    "• 主页作品、合集列表解析与勾选下载\n" +
                    "• 并发下载，支持暂停、继续、取消与已完成历史\n" +
                    "• 直播间监控与 ffmpeg 录制（可分段）\n" +
                    "• Cookie 校验、操作日志（程序目录 log/）\n\n" +
                    "【数据与配置】\n" +
                    "• 配置与历史：程序目录 config/\n" +
                    "• 日志：程序目录 log/（可在设置中开关）\n\n" +
                    "【参考开源】\n" +
                    "• JoeanAmier/TikTokDownloader — 合集与作品字段\n" +
                    "• Johnserf-Seed/TikTokDownload（F2）— 接口参数\n" +
                    "• 社区 douyin 下载 / 直播录制相关项目\n\n" +
                    "仅供学习与个人备份，请遵守平台规则与当地法律。",
                Font = new Font("Microsoft YaHei UI", 9.5F),
                ForeColor = AppTheme.TextPrimary,
                Location = new Point(40, 120),
                Size = new Size(720, 400)
            };

            // 预留给作者自己的 GitHub 项目主页（请改成你的地址）
            var btnMine = new DarkButton
            {
                Text = "我的项目主页",
                Size = new Size(140, 36),
                Location = new Point(40, 520),
                IsPrimary = true
            };
            btnMine.Click += (s, e) =>
            {
                try
                {
                    // TODO: 换成你的 GitHub 仓库地址
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "https://github.com/",
                        UseShellExecute = true
                    });
                }
                catch { }
            };

            this.Controls.Add(watermark);
            this.Controls.Add(title);
            this.Controls.Add(version);
            this.Controls.Add(desc);
            this.Controls.Add(btnMine);
            watermark.SendToBack();
            this.ResumeLayout(false);
        }
    }
}
