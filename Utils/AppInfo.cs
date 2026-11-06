namespace AquaDy.Utils
{
    /// <summary>
    /// 展示用版本与本地实际版本（version.txt）分开。
    /// 比较更新时一律用 GetLocalVersion()，不要只用常量。
    /// </summary>
    public static class AppInfo
    {
        /// <summary>编译期默认版本；首次运行若无 version.txt 会写入此值。</summary>
        public const string BuiltInVersion = "1.0.0";

        public const string DisplayName = "AquaDy";
        public const string Subtitle = "青抖助手";

        public const string GitHubRepo = "https://github.com/mizi6654/AquaDy";
        public const string GitHubApiLatest = "https://api.github.com/repos/mizi6654/AquaDy/releases/latest";
        public const string GitHubReleases = "https://github.com/mizi6654/AquaDy/releases";

        /// <summary>界面显示：优先 version.txt，否则 BuiltInVersion。</summary>
        public static string Version => GetLocalVersion();

        public static string VersionText => "v" + Version;
        public static string FullTitle => DisplayName + " · " + Subtitle + " " + VersionText;

        /// <summary>
        /// 与 LeagueHistory 一致：读程序目录 version.txt。
        /// </summary>
        public static string GetLocalVersion()
        {
            try
            {
                var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "version.txt");
                if (File.Exists(path))
                {
                    var t = File.ReadAllText(path).Trim().TrimStart('v', 'V');
                    if (!string.IsNullOrEmpty(t)) return t;
                }
            }
            catch { }
            return BuiltInVersion;
        }

        /// <summary>首次启动可调用：没有 version.txt 则写入内置版本。</summary>
        public static void EnsureVersionFile()
        {
            try
            {
                var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "version.txt");
                if (!File.Exists(path))
                    File.WriteAllText(path, BuiltInVersion);
            }
            catch { }
        }
    }
}