using AquaDy.Models;

namespace AquaDy.Utils
{
    /// <summary>
    /// 统一计算保存目录 / 文件名 / .part，供下载、续传、pending、孤儿扫描共用。
    /// </summary>
    public static class DownloadPathHelper
    {
        public static string GetSaveDirectory(AppSettings settings, DownloadTask task)
        {
            var rootDir = settings.DownloadPath ?? "";
            var parts = new List<string> { rootDir };

            string author = (task.Author ?? "").Trim();
            string mix = (task.MixName ?? "").Trim();
            if (author.Contains('/'))
            {
                var segs = author.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                if (segs.Length >= 1) author = segs[0].Trim();
                if (string.IsNullOrEmpty(mix) && segs.Length >= 2)
                    mix = segs[1].Trim();
            }
            if (!string.IsNullOrEmpty(author) && author.StartsWith("合集") && string.IsNullOrEmpty(mix))
            {
                mix = author;
                author = "";
            }
            if (mix.StartsWith("合集 · ")) mix = mix.Substring(4).Trim();
            if (mix.StartsWith("合集·")) mix = mix.Substring(3).Trim();

            bool hasAuthor = !string.IsNullOrWhiteSpace(author) && author != "未知作者" && !author.StartsWith("合集");
            bool hasMix = !string.IsNullOrWhiteSpace(mix);

            if (hasMix)
            {
                parts.Add(MakeSafeFileName(hasAuthor ? author : "未知作者"));
                parts.Add(MakeSafeFileName(mix));
            }
            else if (settings.FolderByAuthor && hasAuthor)
            {
                parts.Add(MakeSafeFileName(author));
            }

            return Path.Combine(parts.ToArray());
        }

        public static string BuildFileName(DownloadTask task, string ext)
        {
            var baseName = !string.IsNullOrWhiteSpace(task.Title)
                && task.Title != "解析中..." && task.Title != "等待解析..." && task.Title != "-"
                ? task.Title
                : (string.IsNullOrEmpty(task.AwemeId) ? task.Id : task.AwemeId);
            baseName = MakeSafeFileName(baseName);
            if (string.IsNullOrEmpty(baseName))
                baseName = string.IsNullOrEmpty(task.Id) ? Guid.NewGuid().ToString("N")[..8] : task.Id;
            return baseName + ext;
        }

        public static string GetMp4Path(AppSettings settings, DownloadTask task)
            => Path.Combine(GetSaveDirectory(settings, task), BuildFileName(task, ".mp4"));

        public static string GetPartPath(AppSettings settings, DownloadTask task)
            => GetMp4Path(settings, task) + ".part";

        public static string MakeSafeFileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return Guid.NewGuid().ToString("N")[..8];
            var invalid = Path.GetInvalidFileNameChars();
            var sb = new System.Text.StringBuilder(name.Length);
            foreach (var c in name.Trim())
            {
                if (invalid.Contains(c) || c < 32) continue;
                sb.Append(c);
            }
            var result = sb.ToString().Trim().Trim('.');
            if (result.Length > 80) result = result.Substring(0, 80).Trim();
            return string.IsNullOrWhiteSpace(result) ? Guid.NewGuid().ToString("N")[..8] : result;
        }
    }
}