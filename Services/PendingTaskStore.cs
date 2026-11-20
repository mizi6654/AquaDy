using System.Text.Json;
using AquaDy.Models;
using AquaDy.Utils;

namespace AquaDy.Services
{
    /// <summary>
    /// 未完成任务持久化（暂停 / 退出 / 崩溃后可恢复）
    /// </summary>
    public static class PendingTaskStore
    {
        private static readonly object _lock = new();
        private static readonly JsonSerializerOptions _opts = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        public static bool IsPersistable(DownloadTaskStatus status) =>
            status is DownloadTaskStatus.Waiting
                or DownloadTaskStatus.Parsing
                or DownloadTaskStatus.Downloading
                or DownloadTaskStatus.Paused
                or DownloadTaskStatus.Processing;

        public static List<DownloadTask> Load()
        {
            lock (_lock)
            {
                try
                {
                    var path = AppPaths.PendingTasksFile;
                    if (!File.Exists(path)) return new List<DownloadTask>();
                    var json = File.ReadAllText(path);
                    var list = JsonSerializer.Deserialize<List<DownloadTask>>(json, _opts);
                    return list ?? new List<DownloadTask>();
                }
                catch
                {
                    return new List<DownloadTask>();
                }
            }
        }

        /// <summary>启动时恢复：把可续传的任务统一标记为 Paused</summary>
        public static List<DownloadTask> LoadForRestore()
        {
            var list = Load();
            foreach (var t in list)
            {
                if (IsPersistable(t.Status) && t.Status != DownloadTaskStatus.Paused)
                    t.Status = DownloadTaskStatus.Paused;
                t.Progress = Math.Min(t.Progress, 99);
            }
            return list.Where(t => IsPersistable(t.Status)).ToList();
        }

        public static void SaveAll(IEnumerable<DownloadTask> tasks)
        {
            lock (_lock)
            {
                try
                {
                    var list = tasks
                        .Where(t => t != null && IsPersistable(t.Status))
                        .Select(Clone)
                        .ToList();
                    var path = AppPaths.PendingTasksFile;
                    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                    File.WriteAllText(path, JsonSerializer.Serialize(list, _opts));
                }
                catch { }
            }
        }

        public static void Upsert(DownloadTask task)
        {
            if (task == null || !IsPersistable(task.Status)) return;
            lock (_lock)
            {
                var all = Load();
                all.RemoveAll(x =>
                    x.Id == task.Id ||
                    (!string.IsNullOrEmpty(task.AwemeId) && x.AwemeId == task.AwemeId) ||
                    (!string.IsNullOrEmpty(task.Url) && x.Url == task.Url));
                all.Add(Clone(task));
                try
                {
                    var path = AppPaths.PendingTasksFile;
                    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                    File.WriteAllText(path, JsonSerializer.Serialize(all, _opts));
                }
                catch { }
            }
        }

        public static void Remove(DownloadTask task)
        {
            if (task == null) return;
            lock (_lock)
            {
                var all = Load();
                all.RemoveAll(x =>
                    x.Id == task.Id ||
                    (!string.IsNullOrEmpty(task.AwemeId) && x.AwemeId == task.AwemeId) ||
                    (!string.IsNullOrEmpty(task.Url) && x.Url == task.Url));
                try
                {
                    File.WriteAllText(AppPaths.PendingTasksFile, JsonSerializer.Serialize(all, _opts));
                }
                catch { }
            }
        }

        /// <summary>扫描下载目录下无法关联到当前任务的 .part 文件</summary>
        public static List<string> FindOrphanPartFiles(AppSettings settings, IEnumerable<DownloadTask> knownTasks)
        {
            var orphans = new List<string>();
            try
            {
                var root = settings.DownloadPath;
                if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root)) return orphans;

                var knownParts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var t in knownTasks)
                {
                    if (!string.IsNullOrEmpty(t.SavePath))
                        knownParts.Add(t.SavePath + ".part");
                }

                foreach (var file in Directory.EnumerateFiles(root, "*.part", SearchOption.AllDirectories))
                {
                    if (!knownParts.Contains(file))
                        orphans.Add(file);
                }
            }
            catch { }
            return orphans;
        }

        public static int DeleteFiles(IEnumerable<string> paths)
        {
            int n = 0;
            foreach (var p in paths)
            {
                try
                {
                    if (File.Exists(p))
                    {
                        File.Delete(p);
                        n++;
                    }
                }
                catch { }
            }
            return n;
        }

        private static DownloadTask Clone(DownloadTask t) => new()
        {
            Id = t.Id,
            Title = t.Title,
            Author = t.Author,
            Url = t.Url,
            CoverUrl = t.CoverUrl,
            MusicUrl = t.MusicUrl,
            MixName = t.MixName,
            Type = t.Type,
            Status = t.Status,
            Progress = t.Progress,
            Size = t.Size,
            SavePath = t.SavePath,
            CreateTime = t.CreateTime,
            PublishTime = t.PublishTime,
            ErrorMessage = t.ErrorMessage,
            AwemeId = t.AwemeId,
            Quality = t.Quality,
            DownloadCover = t.DownloadCover,
            DownloadAudio = t.DownloadAudio,
            DownloadUrl = t.DownloadUrl,
            ImageUrls = t.ImageUrls != null ? new List<string>(t.ImageUrls) : new(),
            Description = t.Description
        };

        /// <summary>
        /// 取消任务时：删除对应 .part 文件并清理持久化记录
        /// </summary>
        public static void CancelAndCleanup(AppSettings settings, IEnumerable<DownloadTask> tasks)
        {
            foreach (var t in tasks)
            {
                if (t == null) continue;
                try
                {
                    // 优先用 SavePath
                    if (!string.IsNullOrEmpty(t.SavePath))
                    {
                        var part = t.SavePath + ".part";
                        if (File.Exists(part)) File.Delete(part);
                    }
                    else
                    {
                        // 兜底：根据作者/标题尝试找 .part（可选）
                        // 一般有 SavePath 就够了
                    }
                }
                catch { }

                Remove(t);
            }
        }
    }
}