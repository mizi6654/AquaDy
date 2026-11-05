using System;
using System.IO;
using Newtonsoft.Json;
using AquaDy.Utils;

namespace AquaDy.Models
{
    public class AppSettings
    {
        // 下载相关
        public string DownloadPath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "Downloader");
        public string NameTemplate { get; set; } = "{create_time}_{nickname}_{desc}";
        public bool SkipExisting { get; set; } = true;
        public bool DownloadCover { get; set; } = false;
        /// <summary>是否记录操作与解析日志到 log/ 目录</summary>
        public bool EnableLogging { get; set; } = true;
        public bool DownloadAudio { get; set; } = false;
        /// <summary>按作者名建子目录</summary>
        public bool FolderByAuthor { get; set; } = true;
        public int MaxConcurrent { get; set; } = 3;
        public int MaxRetry { get; set; } = 3;
        public int TimeoutSeconds { get; set; } = 30;

        // Cookie（与原项目一致：完整 Cookie 字符串，格式 key1=value1; key2=value2; ...）
        // 原项目 settings.json 的 cookie 可为 string 或 dict，这里统一存字符串，请求时再解析
        public string DouyinCookie { get; set; } = "";
        public string TikTokCookie { get; set; } = "";
        public string TikTokDeviceId { get; set; } = "";
        /// <summary>Cookie 校验后缓存的抖音昵称</summary>
        public string CookieUserName { get; set; } = "";
        /// <summary>Cookie 校验后缓存的抖音号</summary>
        public string CookieUniqueId { get; set; } = "";

        /// <summary>抖音 Cookie 是否已配置</summary>
        [JsonIgnore]
        public bool HasDouyinCookie => !string.IsNullOrWhiteSpace(DouyinCookie) && DouyinCookie.Length > 30;

        /// <summary>TikTok Cookie 是否已配置</summary>
        [JsonIgnore]
        public bool HasTikTokCookie => !string.IsNullOrWhiteSpace(TikTokCookie) && TikTokCookie.Length > 30;

        // 代理
        public string Proxy { get; set; } = "";
        public bool UseProxy { get; set; } = false;

        // 其他
        public string FFmpegPath { get; set; } = "";
        /// <summary>yt-dlp 可执行文件路径（可选，优先用于直播）</summary>
        public string YtDlpPath { get; set; } = "";
        public bool AutoCheckUpdate { get; set; } = true;
        public string Theme { get; set; } = "Dark";

        private static string SettingsPath => AppPaths.SettingsFile;

        public static AppSettings Load()
        {
            try
            {
                AppPaths.MigrateFromAppDataIfNeeded();
                if (File.Exists(SettingsPath))
                {
                    var json = File.ReadAllText(SettingsPath);
                    return JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch { }
            return new AppSettings();
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(AppPaths.ConfigDir);
                var json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(SettingsPath, json);
            }
            catch { }
        }
    }
}
