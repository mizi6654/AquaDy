using Newtonsoft.Json;
using AquaDy.Utils;

namespace AquaDy.Models
{
    public class AppSettings
    {
        public static string DefaultDownloadPath =>
            Path.Combine(AppPaths.Root.TrimEnd('\\', '/'), "Douyin");

        public static string DefaultFFmpegPath =>
            Path.Combine(AppPaths.Root.TrimEnd('\\', '/'), "Lib", "ffmpeg.exe");

        // 下载相关
        public string DownloadPath { get; set; } = DefaultDownloadPath;
        public string NameTemplate { get; set; } = "{create_time}_{nickname}_{desc}";
        public bool SkipExisting { get; set; } = true;
        public bool DownloadCover { get; set; } = false;
        public bool EnableLogging { get; set; } = true;
        public bool DownloadAudio { get; set; } = false;
        public bool FolderByAuthor { get; set; } = true;
        public int MaxConcurrent { get; set; } = 3;
        public int MaxRetry { get; set; } = 3;
        public int TimeoutSeconds { get; set; } = 30;

        public string DouyinCookie { get; set; } = "";
        // 保留字段兼容旧配置，UI 不再展示
        public string TikTokCookie { get; set; } = "";
        public string TikTokDeviceId { get; set; } = "";
        public string CookieUserName { get; set; } = "";
        public string CookieUniqueId { get; set; } = "";

        [JsonIgnore]
        public bool HasDouyinCookie => !string.IsNullOrWhiteSpace(DouyinCookie) && DouyinCookie.Length > 30;

        [JsonIgnore]
        public bool HasTikTokCookie => !string.IsNullOrWhiteSpace(TikTokCookie) && TikTokCookie.Length > 30;

        public string Proxy { get; set; } = "";
        public bool UseProxy { get; set; } = false;

        public string FFmpegPath { get; set; } = "";
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
                    var s = JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
                    if (string.IsNullOrWhiteSpace(s.DownloadPath))
                        s.DownloadPath = DefaultDownloadPath;
                    if (string.IsNullOrWhiteSpace(s.FFmpegPath) && File.Exists(DefaultFFmpegPath))
                        s.FFmpegPath = DefaultFFmpegPath;
                    return s;
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