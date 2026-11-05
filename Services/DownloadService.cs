using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AquaDy.Models;

namespace AquaDy.Services
{
    public class DownloadService
    {
        private readonly AppSettings _settings;
        private readonly SemaphoreSlim _semaphore;
        private readonly HashSet<string> _pausedIds = new();
        private readonly object _pauseLock = new();
        private readonly ConcurrentDictionary<string, DownloadTask> _running = new();
        private readonly ConcurrentDictionary<string, CancellationTokenSource> _taskCts = new();
        private readonly HashSet<string> _cancelledIds = new();
        private readonly HttpClient _http;

        public event Action<DownloadTask>? TaskUpdated;
        public event Action<string>? Log;

        public DownloadService(AppSettings settings)
        {
            _settings = settings;
            _semaphore = new SemaphoreSlim(Math.Max(1, settings.MaxConcurrent));

            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = true,
                AutomaticDecompression = System.Net.DecompressionMethods.All
            };
            _http = new HttpClient(handler);
            _http.Timeout = TimeSpan.FromMinutes(15);
            _http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36");
            _http.DefaultRequestHeaders.TryAddWithoutValidation("Referer", "https://www.douyin.com/");
            if (!string.IsNullOrEmpty(settings.DouyinCookie))
                _http.DefaultRequestHeaders.TryAddWithoutValidation("Cookie", settings.DouyinCookie);
        }

        public void PauseTasks(IEnumerable<DownloadTask> tasks)
        {
            lock (_pauseLock)
            {
                foreach (var t in tasks)
                {
                    if (t == null) continue;
                    _pausedIds.Add(t.Id);
                    if (!string.IsNullOrEmpty(t.AwemeId)) _pausedIds.Add(t.AwemeId);
                    // 中断正在传输的流
                    if (_taskCts.TryGetValue(t.Id, out var cts))
                    {
                        try { cts.Cancel(); } catch { }
                    }
                    if (t.Status == DownloadTaskStatus.Downloading || t.Status == DownloadTaskStatus.Parsing
                        || t.Status == DownloadTaskStatus.Waiting)
                    {
                        t.Status = DownloadTaskStatus.Paused;
                        RaiseUpdated(t);
                    }
                }
            }
        }

        public void CancelTasks(IEnumerable<DownloadTask> tasks)
        {
            lock (_pauseLock)
            {
                foreach (var t in tasks)
                {
                    if (t == null) continue;
                    _cancelledIds.Add(t.Id);
                    if (!string.IsNullOrEmpty(t.AwemeId)) _cancelledIds.Add(t.AwemeId);
                    _pausedIds.Remove(t.Id);
                    if (!string.IsNullOrEmpty(t.AwemeId)) _pausedIds.Remove(t.AwemeId);
                    if (_taskCts.TryGetValue(t.Id, out var cts))
                    {
                        try { cts.Cancel(); } catch { }
                    }
                    // 也按 AwemeId 找 CTS
                    foreach (var kv in _taskCts.ToArray())
                    {
                        if (kv.Key == t.Id || kv.Key == t.AwemeId)
                        {
                            try { kv.Value.Cancel(); } catch { }
                        }
                    }
                    if (t.Status != DownloadTaskStatus.Completed && t.Status != DownloadTaskStatus.Failed)
                    {
                        t.Status = DownloadTaskStatus.Cancelled;
                        t.Speed = "-";
                        RaiseUpdated(t);
                    }
                }
            }
        }

        public void ResumeTasks(IEnumerable<DownloadTask> tasks)
        {
            lock (_pauseLock)
            {
                foreach (var t in tasks)
                {
                    if (t == null) continue;
                    _pausedIds.Remove(t.Id);
                    if (!string.IsNullOrEmpty(t.AwemeId)) _pausedIds.Remove(t.AwemeId);
                    _cancelledIds.Remove(t.Id);
                    if (!string.IsNullOrEmpty(t.AwemeId)) _cancelledIds.Remove(t.AwemeId);
                    if (t.Status == DownloadTaskStatus.Paused || t.Status == DownloadTaskStatus.Cancelled)
                    {
                        t.Status = DownloadTaskStatus.Waiting;
                        RaiseUpdated(t);
                    }
                }
            }
        }

        /// <summary>继续下载：清除暂停标记并重新入队未完成项</summary>
        public async Task ResumeAndEnqueueAsync(IEnumerable<DownloadTask> tasks, CancellationToken ct = default)
        {
            var list = tasks.Where(t => t != null).Cast<DownloadTask>().ToList();
            lock (_pauseLock)
            {
                foreach (var t in list)
                {
                    _pausedIds.Remove(t.Id);
                    _cancelledIds.Remove(t.Id);
                    if (!string.IsNullOrEmpty(t.AwemeId))
                    {
                        _pausedIds.Remove(t.AwemeId);
                        _cancelledIds.Remove(t.AwemeId);
                    }
                    if (t.Status == DownloadTaskStatus.Paused ||
                        t.Status == DownloadTaskStatus.Cancelled ||
                        t.Status == DownloadTaskStatus.Failed ||
                        t.Status == DownloadTaskStatus.Waiting)
                    {
                        t.Status = DownloadTaskStatus.Waiting;
                        t.Progress = 0;
                        t.ErrorMessage = "";
                        RaiseUpdated(t);
                    }
                }
            }
            var again = list.Where(t =>
                t.Status != DownloadTaskStatus.Completed &&
                t.Status != DownloadTaskStatus.Downloading).ToList();
            if (again.Count > 0)
                await EnqueueAsync(again, ct);
        }

        private bool IsPaused(DownloadTask task)
        {
            lock (_pauseLock)
            {
                if (_pausedIds.Contains(task.Id)) return true;
                if (!string.IsNullOrEmpty(task.AwemeId) && _pausedIds.Contains(task.AwemeId)) return true;
                return false;
            }
        }

        private bool IsCancelled(DownloadTask task)
        {
            lock (_pauseLock)
            {
                if (_cancelledIds.Contains(task.Id)) return true;
                if (!string.IsNullOrEmpty(task.AwemeId) && _cancelledIds.Contains(task.AwemeId)) return true;
                return false;
            }
        }

        public async Task EnqueueAsync(IEnumerable<DownloadTask> tasks, CancellationToken ct = default)
        {
            // 并发由 SemaphoreSlim(MaxConcurrent) 控制
            await Task.WhenAll(tasks.Select(t => DownloadOneAsync(t, ct)));
        }

        private string GetSaveDirectory(DownloadTask task)
        {
            var rootDir = _settings.DownloadPath;
            var parts = new List<string> { rootDir };

            // 规范化：Author 里不要带「作者/合集」拼接串
            string author = (task.Author ?? "").Trim();
            string mix = (task.MixName ?? "").Trim();
            if (author.Contains('/'))
            {
                var segs = author.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                if (segs.Length >= 1) author = segs[0].Trim();
                if (string.IsNullOrEmpty(mix) && segs.Length >= 2)
                    mix = segs[1].Trim();
            }
            // Author 误写成合集名时挪到 mix
            if (!string.IsNullOrEmpty(author) && author.StartsWith("合集") && string.IsNullOrEmpty(mix))
            {
                mix = author;
                author = "";
            }
            if (mix.StartsWith("合集 · ")) mix = mix.Substring(4).Trim();
            if (mix.StartsWith("合集·")) mix = mix.Substring(3).Trim();

            bool hasAuthor = !string.IsNullOrWhiteSpace(author) && author != "未知作者" && !author.StartsWith("合集");
            bool hasMix = !string.IsNullOrWhiteSpace(mix);

            // 统一：有合集时必须 作者/合集，避免根目录再出现同级「合集xxx」
            if (hasMix)
            {
                parts.Add(MakeSafeFileName(hasAuthor ? author : "未知作者"));
                parts.Add(MakeSafeFileName(mix));
            }
            else if (_settings.FolderByAuthor && hasAuthor)
            {
                parts.Add(MakeSafeFileName(author));
            }

            return Path.Combine(parts.ToArray());
        }

        private string BuildFileName(DownloadTask task, string ext)
        {
            // 优先标题，其次 awemeId
            var baseName = !string.IsNullOrWhiteSpace(task.Title) && task.Title != "解析中..." && task.Title != "等待解析..."
                ? task.Title
                : (task.AwemeId ?? task.Id);
            baseName = MakeSafeFileName(baseName);
            if (string.IsNullOrEmpty(baseName))
                baseName = task.Id;
            return baseName + ext;
        }

        private async Task DownloadOneAsync(DownloadTask task, CancellationToken ct)
        {
            while (IsPaused(task) && !IsCancelled(task) && !ct.IsCancellationRequested)
                await Task.Delay(300, CancellationToken.None);
            if (IsCancelled(task) || ct.IsCancellationRequested)
            {
                task.Status = DownloadTaskStatus.Cancelled;
                RaiseUpdated(task);
                return;
            }

            using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct);
            _taskCts[task.Id] = linked;
            var token = linked.Token;

            task.DownloadCover = task.DownloadCover || _settings.DownloadCover;
            task.DownloadAudio = task.DownloadAudio || _settings.DownloadAudio;
            await _semaphore.WaitAsync(ct);
            try
            {
                while (IsPaused(task))
                {
                    task.Status = DownloadTaskStatus.Paused;
                    RaiseUpdated(task);
                    await Task.Delay(400, ct);
                    ct.ThrowIfCancellationRequested();
                }
                _running[task.Id] = task;
                if (IsCancelled(task) || ct.IsCancellationRequested)
            {
                task.Status = DownloadTaskStatus.Cancelled;
                RaiseUpdated(task);
                return;
            }
            if (IsPaused(task))
            {
                task.Status = DownloadTaskStatus.Paused;
                RaiseUpdated(task);
                return;
            }
            task.Status = DownloadTaskStatus.Downloading;
                task.Progress = 0;
                RaiseUpdated(task);

                var dir = GetSaveDirectory(task);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                if (task.Type == ContentType.Image && task.ImageUrls != null && task.ImageUrls.Count > 0)
                {
                    await DownloadImagesAsync(task, dir, ct);
                    HistoryStore.Add(CloneForHistory(task));
                    return;
                }

                if (string.IsNullOrEmpty(task.DownloadUrl))
                {
                    task.Status = DownloadTaskStatus.Failed;
                    task.ErrorMessage = "无下载地址（解析未成功）";
                    ParseLogger.Fail($"无下载地址 id={task.AwemeId} url={task.Url}");
                    task.Size = "-";
                    RaiseUpdated(task);
                    HistoryStore.Add(CloneForHistory(task));
                    Log?.Invoke("失败(无地址): " + task.Title);
                    return;
                }

                var fileName = BuildFileName(task, ".mp4");
                var savePath = Path.Combine(dir, fileName);
                var tempPath = savePath + ".part";

                if (_settings.SkipExisting && File.Exists(savePath))
                {
                    var fi = new FileInfo(savePath);
                    if (fi.Length > 1024 * 50) // 至少 50KB 才算有效视频
                    {
                        task.Status = DownloadTaskStatus.Completed;
                        task.Progress = 100;
                        task.SavePath = savePath;
                        task.Size = FormatSize(fi.Length);
                        RaiseUpdated(task);
                        HistoryStore.Add(CloneForHistory(task));
                        Log?.Invoke("跳过已存在: " + fileName);
                        return;
                    }
                }

                using var resp = await _http.GetAsync(task.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, token);
                if (!resp.IsSuccessStatusCode)
                {
                    task.Status = DownloadTaskStatus.Failed;
                    task.ErrorMessage = $"HTTP {(int)resp.StatusCode}";
                    RaiseUpdated(task);
                    HistoryStore.Add(CloneForHistory(task));
                    return;
                }

                var contentType = resp.Content.Headers.ContentType?.MediaType ?? "";
                // 拒绝 HTML/JSON 错误页（常见约几十~几百 KB）
                if (contentType.Contains("text/html") || contentType.Contains("application/json") || contentType.Contains("text/plain"))
                {
                    task.Status = DownloadTaskStatus.Failed;
                    task.ErrorMessage = "返回的不是视频（Content-Type=" + contentType + "），链接可能失效或需 Cookie";
                    task.Size = "-";
                    RaiseUpdated(task);
                    HistoryStore.Add(CloneForHistory(task));
                    Log?.Invoke("失败(非视频): " + task.Title);
                    return;
                }

                var total = resp.Content.Headers.ContentLength ?? 0;
                await using var input = await resp.Content.ReadAsStreamAsync(token);
                await using var output = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);

                var buffer = new byte[81920];
                long readTotal = 0;
                int read;
                var sw = System.Diagnostics.Stopwatch.StartNew();
                long lastBytes = 0;

                // 先读一小段判断是否是 HTML
                read = await input.ReadAsync(buffer.AsMemory(0, Math.Min(buffer.Length, 4096)), token);
                if (read > 0)
                {
                    var head = System.Text.Encoding.UTF8.GetString(buffer, 0, Math.Min(read, 200));
                    if (head.Contains("<!DOCTYPE", StringComparison.OrdinalIgnoreCase) ||
                        head.Contains("<html", StringComparison.OrdinalIgnoreCase))
                    {
                        task.Status = DownloadTaskStatus.Failed;
                        task.ErrorMessage = "下载到的是 HTML 页面，不是视频文件";
                        RaiseUpdated(task);
                        HistoryStore.Add(CloneForHistory(task));
                        try { File.Delete(tempPath); } catch { }
                        return;
                    }
                    await output.WriteAsync(buffer.AsMemory(0, read), token);
                    readTotal += read;
                }

                while ((read = await input.ReadAsync(buffer.AsMemory(0, buffer.Length), token)) > 0)
                {
                    if (IsCancelled(task)) { token.ThrowIfCancellationRequested(); }
                    if (IsPaused(task))
                    {
                        try { linked.Cancel(); } catch { }
                        token.ThrowIfCancellationRequested();
                    }
                    await output.WriteAsync(buffer.AsMemory(0, read), token);
                    readTotal += read;

                    if (total > 0)
                        task.Progress = Math.Min(99, readTotal * 100.0 / total);
                    else
                        task.Progress = Math.Min(99, task.Progress + 0.3);

                    if (sw.ElapsedMilliseconds >= 800)
                    {
                        var speed = (readTotal - lastBytes) * 1000.0 / Math.Max(1, sw.ElapsedMilliseconds);
                        task.Speed = FormatSize((long)speed) + "/s";
                        if (total > 0 && speed > 0)
                        {
                            var remain = (total - readTotal) / speed;
                            task.Remaining = remain > 60 ? $"{remain / 60:F0}分" : $"{remain:F0}秒";
                        }
                        task.Size = total > 0 ? $"{FormatSize(readTotal)} / {FormatSize(total)}" : FormatSize(readTotal);
                        lastBytes = readTotal;
                        sw.Restart();
                        RaiseUpdated(task);
                    }
                }

                await output.FlushAsync(token);
                output.Close();

                // 过小文件视为失败（常见错误页）
                if (readTotal < 50 * 1024)
                {
                    try { File.Delete(tempPath); } catch { }
                    task.Status = DownloadTaskStatus.Failed;
                    task.ErrorMessage = $"文件过小({FormatSize(readTotal)})，可能不是有效视频";
                    task.Size = FormatSize(readTotal);
                    RaiseUpdated(task);
                    HistoryStore.Add(CloneForHistory(task));
                    return;
                }

                if (IsCancelled(task) || IsPaused(task))
                {
                    try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch { }
                    task.Status = IsCancelled(task) ? DownloadTaskStatus.Cancelled : DownloadTaskStatus.Paused;
                    RaiseUpdated(task);
                    return;
                }

                if (File.Exists(savePath)) File.Delete(savePath);
                File.Move(tempPath, savePath);

                await DownloadExtrasAsync(task, dir, token);

                task.Status = DownloadTaskStatus.Completed;
                task.Progress = 100;
                task.SavePath = savePath;
                task.Size = FormatSize(readTotal);
                task.Speed = "-";
                task.Remaining = "-";
                RaiseUpdated(task);
                HistoryStore.Add(CloneForHistory(task));
                Log?.Invoke("完成: " + fileName);
                ParseLogger.Op("下载完成", fileName + " -> " + savePath);
            }
            catch (OperationCanceledException)
            {
                if (IsPaused(task) && !IsCancelled(task))
                {
                    task.Status = DownloadTaskStatus.Paused;
                    task.Speed = "-";
                    RaiseUpdated(task);
                }
                else
                {
                    task.Status = DownloadTaskStatus.Cancelled;
                    task.Speed = "-";
                    RaiseUpdated(task);
                }
            }
            catch (Exception ex)
            {
                task.Status = DownloadTaskStatus.Failed;
                task.ErrorMessage = ex.Message;
                RaiseUpdated(task);
                HistoryStore.Add(CloneForHistory(task));
                Log?.Invoke("失败: " + task.Title + " - " + ex.Message);
                ParseLogger.Fail($"下载失败 id={task.AwemeId} {ex.Message}");
            }
            finally
            {
                _taskCts.TryRemove(task.Id, out _);
                _running.TryRemove(task.Id, out _);
                _semaphore.Release();
            }
        }

        private async Task DownloadImagesAsync(DownloadTask task, string dir, CancellationToken ct)
        {
            var folder = Path.Combine(dir, MakeSafeFileName(task.Title) + "_图集");
            Directory.CreateDirectory(folder);
            int ok = 0;
            for (int i = 0; i < task.ImageUrls.Count; i++)
            {
                ct.ThrowIfCancellationRequested();
                var url = task.ImageUrls[i];
                var ext = ".jpg";
                if (url.Contains(".png")) ext = ".png";
                else if (url.Contains(".webp")) ext = ".webp";
                var path = Path.Combine(folder, $"{i + 1:D3}{ext}");
                try
                {
                    var bytes = await _http.GetByteArrayAsync(url, ct);
                    if (bytes.Length > 1000)
                    {
                        await File.WriteAllBytesAsync(path, bytes, ct);
                        ok++;
                    }
                }
                catch { }
                task.Progress = (i + 1) * 100.0 / task.ImageUrls.Count;
                RaiseUpdated(task);
            }
            task.Status = ok > 0 ? DownloadTaskStatus.Completed : DownloadTaskStatus.Failed;
            task.SavePath = folder;
            task.Size = $"{ok}/{task.ImageUrls.Count} 张";
            task.Progress = 100;
            RaiseUpdated(task);
            Log?.Invoke($"图集: {task.Title} ({ok}张)");
        }

        private static DownloadTask CloneForHistory(DownloadTask t) => new DownloadTask
        {
            Id = t.Id,
            Title = t.Title,
            Author = t.Author,
            Url = t.Url,
            Type = t.Type,
            Status = t.Status,
            Progress = t.Progress,
            Size = t.Size,
            SavePath = t.SavePath,
            CreateTime = DateTime.Now,
            ErrorMessage = t.ErrorMessage,
            AwemeId = t.AwemeId,
            Description = t.Description
        };

        public static string MakeSafeFileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return Guid.NewGuid().ToString("N")[..8];
            // Windows 非法字符: \ / : * ? " < > |
            var invalid = Path.GetInvalidFileNameChars();
            var sb = new System.Text.StringBuilder(name.Length);
            foreach (var c in name.Trim())
            {
                if (invalid.Contains(c) || c < 32)
                    continue; // 直接删除，不用下划线堆叠
                sb.Append(c);
            }
            var result = sb.ToString().Trim();
            // 去掉首尾点和空格
            result = result.Trim().Trim('.');
            if (result.Length > 80) result = result.Substring(0, 80).Trim();
            return string.IsNullOrWhiteSpace(result) ? Guid.NewGuid().ToString("N")[..8] : result;
        }

        private static string FormatSize(long bytes)
        {
            if (bytes < 1024) return bytes + " B";
            if (bytes < 1024 * 1024) return (bytes / 1024.0).ToString("F1") + " KB";
            if (bytes < 1024L * 1024 * 1024) return (bytes / 1024.0 / 1024).ToString("F1") + " MB";
            return (bytes / 1024.0 / 1024 / 1024).ToString("F2") + " GB";
        }


        /// <summary>封面图 + 配乐（原声 BGM），由设置或任务勾选控制</summary>
        private async Task DownloadExtrasAsync(DownloadTask task, string dir, CancellationToken ct)
        {
            bool wantCover = task.DownloadCover || _settings.DownloadCover;
            if (!wantCover) return;

            // 若解析阶段没带上封面/原声，再快速拉一次作品页补全（原项目也是 detail 取 music/cover）
            if (wantCover && string.IsNullOrEmpty(task.CoverUrl))
            {
                try
                {
                    if (!string.IsNullOrEmpty(task.Url) || !string.IsNullOrEmpty(task.AwemeId))
                    {
                        var parser = new LinkParser(_settings.DouyinCookie, _settings.TikTokCookie);
                        var url = !string.IsNullOrEmpty(task.Url) ? task.Url : ("https://www.douyin.com/video/" + task.AwemeId);
                        var parsed = await parser.ParseSingleAsync(url);
                        if (!string.IsNullOrEmpty(parsed.CoverUrl))
                            task.CoverUrl = parsed.CoverUrl;
                    }
                }
                catch { }
            }

            if (wantCover && !string.IsNullOrEmpty(task.CoverUrl))
            {
                try
                {
                    var coverPath = Path.Combine(dir, BuildFileName(task, "") + "_cover.jpg");
                    if (!File.Exists(coverPath))
                    {
                        using var req = new HttpRequestMessage(HttpMethod.Get, task.CoverUrl);
                        if (!string.IsNullOrEmpty(_settings.DouyinCookie))
                            req.Headers.TryAddWithoutValidation("Cookie", _settings.DouyinCookie);
                        req.Headers.TryAddWithoutValidation("Referer", "https://www.douyin.com/");
                        using var resp = await _http.SendAsync(req, ct);
                        if (resp.IsSuccessStatusCode)
                        {
                            var bytes = await resp.Content.ReadAsByteArrayAsync(ct);
                            if (bytes.Length > 500)
                                await File.WriteAllBytesAsync(coverPath, bytes, ct);
                        }
                    }
                }
                catch { }
            }
        }

        private void RaiseUpdated(DownloadTask task)
        {
            // 已取消则不再推送进度，避免 UI 继续跳动
            if (task.Status == DownloadTaskStatus.Cancelled) { TaskUpdated?.Invoke(task); return; }
            TaskUpdated?.Invoke(task);
        }
    }
}
