namespace AquaDy.Utils
{
    /// <summary>统一使用程序运行目录下的 config/、log/</summary>
    public static class AppPaths
    {
        public static string Root => AppDomain.CurrentDomain.BaseDirectory;

        public static string ConfigDir
        {
            get
            {
                var d = Path.Combine(Root, "config");
                Directory.CreateDirectory(d);
                return d;
            }
        }

        public static string LogDir
        {
            get
            {
                var d = Path.Combine(Root, "log");
                Directory.CreateDirectory(d);
                return d;
            }
        }

        public static string SettingsFile => Path.Combine(ConfigDir, "settings.json");
        public static string HistoryFile => Path.Combine(ConfigDir, "history.json");
        public static string LiveRoomsFile => Path.Combine(ConfigDir, "live_rooms.json");

        /// <summary>升级兼容，可长期保留，无副作用。</summary>
        public static void MigrateFromAppDataIfNeeded()
        {
            try
            {
                var oldRoot = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "AquaDy");
                if (!Directory.Exists(oldRoot)) return;

                void TryCopy(string name, string dest)
                {
                    var src = Path.Combine(oldRoot, name);
                    if (File.Exists(src) && !File.Exists(dest))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                        File.Copy(src, dest, false);
                    }
                }
                TryCopy("settings.json", SettingsFile);
                TryCopy("history.json", HistoryFile);
                TryCopy("live_rooms.json", LiveRoomsFile);
            }
            catch { }
        }
    }
}
