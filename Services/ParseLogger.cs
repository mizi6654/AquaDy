using System.Text;
using AquaDy.Models;
using AquaDy.Utils;

namespace AquaDy.Services
{
    /// <summary>全局操作日志。目录：程序目录/log/</summary>
    public static class ParseLogger
    {
        private static readonly object _lock = new();
        private static bool? _enabled;
        /// <summary>单个 JSON/HTML 最大保存字节（默认 256KB，避免几十兆日志）</summary>
        public const int MaxArtifactBytes = 256 * 1024;
        /// <summary>log 目录最大占用（默认 30MB）</summary>
        public const long MaxLogDirBytes = 30L * 1024 * 1024;
        /// <summary>日志保留天数</summary>
        public const int KeepDays = 3;

        public static bool Enabled
        {
            get
            {
                if (_enabled.HasValue) return _enabled.Value;
                try { _enabled = AppSettings.Load().EnableLogging; }
                catch { _enabled = true; }
                return _enabled ?? true;
            }
            set => _enabled = value;
        }

        public static void ReloadSetting()
        {
            try { _enabled = AppSettings.Load().EnableLogging; }
            catch { _enabled = true; }
        }

        private static string LogDir => AppPaths.LogDir;

        public static void Info(string message) => Write("INFO", message);
        public static void Warn(string message) => Write("WARN", message);
        public static void Fail(string message) => Write("FAIL", message);
        public static void Op(string action, string detail) => Write("OP", $"[{action}] {detail}");

        /// <summary>启动时清理过期/超体积日志</summary>
        public static void CleanupOnStartup()
        {
            try
            {
                var dir = LogDir;
                if (!Directory.Exists(dir)) return;
                var files = new DirectoryInfo(dir).GetFiles("*", SearchOption.TopDirectoryOnly)
                    .OrderBy(f => f.LastWriteTimeUtc).ToList();
                var cutoff = DateTime.UtcNow.AddDays(-KeepDays);
                foreach (var f in files.ToList())
                {
                    if (f.LastWriteTimeUtc < cutoff)
                    {
                        try { f.Delete(); files.Remove(f); } catch { }
                    }
                }
                long total = files.Sum(f => f.Length);
                while (total > MaxLogDirBytes && files.Count > 0)
                {
                    var oldest = files[0];
                    try
                    {
                        total -= oldest.Length;
                        oldest.Delete();
                        files.RemoveAt(0);
                    }
                    catch { break; }
                }
                Info($"日志清理完成 keepDays={KeepDays} maxMB={MaxLogDirBytes / 1024 / 1024} files={files.Count}");
            }
            catch { }
        }

        public static void SaveJson(string tag, string? id, string? json)
        {
            if (!Enabled || string.IsNullOrEmpty(json)) return;
            if (json.Trim() == "{}") return;
            try
            {
                var name = $"{DateTime.Now:yyyyMMdd_HHmmss}_{Safe(tag)}_{Safe(id ?? "x")}.json";
                if (json.Length > MaxArtifactBytes)
                    json = json.Substring(0, MaxArtifactBytes) + "\n/* truncated */\n";
                File.WriteAllText(Path.Combine(LogDir, name), json, Encoding.UTF8);
                Info($"保存JSON {name} len={json.Length}");
            }
            catch (Exception ex) { Write("FAIL", "SaveJson " + ex.Message); }
        }

        public static void SaveHtml(string tag, string? id, string? html)
        {
            if (!Enabled || string.IsNullOrEmpty(html)) return;
            try
            {
                var name = $"{DateTime.Now:yyyyMMdd_HHmmss}_{Safe(tag)}_{Safe(id ?? "x")}.html";
                if (html.Length > MaxArtifactBytes)
                    html = html.Substring(0, MaxArtifactBytes) + "\n<!-- truncated -->\n";
                File.WriteAllText(Path.Combine(LogDir, name), html, Encoding.UTF8);
                Info($"保存HTML {name} len={html.Length}");
            }
            catch (Exception ex) { Write("FAIL", "SaveHtml " + ex.Message); }
        }

        private static void Write(string level, string message)
        {
            if (!Enabled) return;
            try
            {
                lock (_lock)
                {
                    var file = Path.Combine(LogDir, $"aquady_{DateTime.Now:yyyyMMdd}.log");
                    File.AppendAllText(file,
                        $"{DateTime.Now:HH:mm:ss.fff} [{level}] {message}{Environment.NewLine}",
                        Encoding.UTF8);
                }
            }
            catch { }
        }

        private static string Safe(string s)
        {
            foreach (var c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
            if (s.Length > 48) s = s.Substring(0, 48);
            return string.IsNullOrEmpty(s) ? "x" : s;
        }
    }
}
