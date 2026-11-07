using System.Text;

namespace AquaDy.Utils
{
    public static class Changelog
    {
        /// <summary>固定介绍（不常改）</summary>
        public static string IntroText =>
            "AquaDy（青抖助手）是面向抖音的桌面下载与直播录制工具，基于 C# WinForms。\n\n" +
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
            "仅供学习与个人备份，请遵守平台规则与当地法律。";

        /// <summary>从程序目录 changelog.txt 读取；没有则用内置默认</summary>
        public static string LoadReleaseNotes()
        {
            try
            {
                var dir = AppDomain.CurrentDomain.BaseDirectory;
                foreach (var name in new[] { "changelog.txt", "CHANGELOG.txt", Path.Combine("config", "changelog.txt") })
                {
                    var path = Path.Combine(dir, name);
                    if (!File.Exists(path)) continue;
                    var text = File.ReadAllText(path, Encoding.UTF8).Trim();
                    // 统一换行
                    text = text.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", Environment.NewLine);
                    return text;
                }
            }
            catch { }
            return DefaultNotes.Replace("\n", Environment.NewLine);
        }

        public static string BuildAboutBody()
        {
            var intro = IntroText.Replace("\n", Environment.NewLine);
            var notes = LoadReleaseNotes();
            return intro + Environment.NewLine + Environment.NewLine
                 + "【更新日志】" + Environment.NewLine
                 + notes;
        }

        public static string DefaultNotes =>
            "## v1.0.1\n" +
            "- 修复分段视频录制时下载速度状态不更新\n" +
            "- 去掉下载任务面板添加链接时的弹窗提示\n" +
            "- 优化下载任务：并发解析+下载；已完成自动取消勾选\n" +
            "- 优化账号批量：已完成取消勾选\n" +
            "- 优化已完成面板：重下时更新状态，去掉成功弹窗\n" +
            "- 优化直播录制选中提示（当前分段文件）\n" +
            "- 优化直播列表刷新抖动\n\n" +
            "## v1.0.0\n" +
            "- 首个公开版本\n";
    }
}