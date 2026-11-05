using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using AquaDy.Models;
using AquaDy.Utils;

namespace AquaDy.Services
{
    /// <summary>
    /// 已完成/失败任务历史（下载任务 + 账号批量共用）
    /// </summary>
    public static class HistoryStore
    {
        private static string PathFile => AppPaths.HistoryFile;

        private static readonly object _lock = new();

        public static List<DownloadTask> Load()
        {
            try
            {
                if (!File.Exists(PathFile)) return new List<DownloadTask>();
                var json = File.ReadAllText(PathFile);
                return JsonConvert.DeserializeObject<List<DownloadTask>>(json) ?? new List<DownloadTask>();
            }
            catch { return new List<DownloadTask>(); }
        }

        public static void Add(DownloadTask task)
        {
            lock (_lock)
            {
                var list = Load();
                // 同 AwemeId 或同 Url 覆盖旧记录
                list.RemoveAll(t =>
                    (!string.IsNullOrEmpty(task.AwemeId) && t.AwemeId == task.AwemeId) ||
                    (!string.IsNullOrEmpty(task.Url) && t.Url == task.Url));
                list.Insert(0, task);
                if (list.Count > 500) list = list.Take(500).ToList();
                Save(list);
            }
        }

        public static void Clear()
        {
            lock (_lock)
            {
                Save(new List<DownloadTask>());
            }
        }

        private static void Save(List<DownloadTask> list)
        {
            var dir = Path.GetDirectoryName(PathFile);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(PathFile, JsonConvert.SerializeObject(list, Formatting.Indented));
        }
    }
}
