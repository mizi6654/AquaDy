using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using AquaDy.Models;
using AquaDy.Utils;

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
                AutomaticDecompression = DecompressionMethods.All
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
                    if (_taskCts.TryGetValue(t.Id, out var cts))
                    {
                        try { cts.Cancel(); } catch { }
                    }
                    if (t.Status == DownloadTaskStatus.Downloading || t.Status == DownloadTaskStatus.Parsing
                        || t.Status == DownloadTaskStatus.Waiting)
                    {
                        t.Status = DownloadTaskStatus.Paused;
                        RaiseUpdated(t);
                        PendingTaskStore.Upsert(t);   // ← 新增
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

            PendingTaskStore.CancelAndCleanup(_settings, tasks);
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
                    if (t.Status is DownloadTaskStatus.Paused or DownloadTaskStatus.Cancelled)
                    {
                        t.Status = DownloadTaskStatus.Waiting;
                        RaiseUpdated(t);
                    }
                }
            }
        }

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
                    if (t.Status is DownloadTaskStatus.Paused or DownloadTaskStatus.Cancelled
                        or DownloadTaskStatus.Failed or DownloadTaskStatus.Waiting)
                    {
                        bool wasCancelled = t.Status == DownloadTaskStatus.Cancelled;
                        t.Status = DownloadTaskStatus.Waiting;
                        if (wasCancelled) t.Progress = 0;
                        t.ErrorMessage = "";
                        RaiseUpdated(t);
                        PendingTaskStore.Upsert(t);
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
            await Task.WhenAll(tasks.Select(t => DownloadOneAsync(t, ct)));
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
                // 只在进入暂停态时通知一次 UI，循环里不再 RaiseUpdated（否则会不停抖列表）
                if (IsPaused(task))
                {
                    task.Status = DownloadTaskStatus.Paused;
                    task.Speed = "-";
                    RaiseUpdated(task);
                    PendingTaskStore.Upsert(task);
                    while (IsPaused(task) && !IsCancelled(task) && !ct.IsCancellationRequested)
                        await Task.Delay(400, CancellationToken.None);
                    ct.ThrowIfCancellationRequested();
                    if (IsCancelled(task))
                    {
                        task.Status = DownloadTaskStatus.Cancelled;
                        RaiseUpdated(task);
                        return;
                    }
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
                RaiseUpdated(task);
                PendingTaskStore.Upsert(task);

                var dir = DownloadPathHelper.GetSaveDirectory(_settings, task);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                if (task.Type == ContentType.Image && task.ImageUrls != null && task.ImageUrls.Count > 0)
                {
                    await DownloadImagesAsync(task, dir, token);
                    HistoryStore.Add(CloneForHistory(task));
                    PendingTaskStore.Remove(task);
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
                    PendingTaskStore.Remove(task);
                    Log?.Invoke("失败(无地址): " + task.Title);
                    return;
                }

                var fileName = DownloadPathHelper.BuildFileName(task, ".mp4");
                var savePath = Path.Combine(dir, fileName);
                var tempPath = savePath + ".part";
                task.SavePath = savePath;   // 非常重要！取消/暂停时才能找到对应的 .part

                if (_settings.SkipExisting && File.Exists(savePath))
                {
                    var fi = new FileInfo(savePath);
                    if (fi.Length > 1024 * 50)
                    {
                        task.Status = DownloadTaskStatus.Completed;
                        task.Progress = 100;
                        task.SavePath = savePath;
                        task.Size = FormatSize(fi.Length);
                        RaiseUpdated(task);
                        HistoryStore.Add(CloneForHistory(task));
                        PendingTaskStore.Remove(task);
                        Log?.Invoke("跳过已存在: " + fileName);
                        try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch { }
                        return;
                    }
                }

                long existing = 0;
                if (File.Exists(tempPath))
                {
                    try { existing = new FileInfo(tempPath).Length; } catch { existing = 0; }
                    if (existing < 1024)
                    {
                        try { File.Delete(tempPath); } catch { }
                        existing = 0;
                    }
                }

                // play 网关 → 跟到真实 CDN（浏览器能播、HttpClient 直接 400 的情况）
                if (!string.IsNullOrEmpty(task.DownloadUrl)
                    && (task.DownloadUrl.Contains("snssdk.com/aweme/v1/play", StringComparison.OrdinalIgnoreCase)
                        || task.DownloadUrl.Contains("iesdouyin.com/aweme/v1/play", StringComparison.OrdinalIgnoreCase)))
                {
                    var resolved = await ResolvePlayGatewayAsync(task.DownloadUrl, token);
                    if (!string.IsNullOrEmpty(resolved))
                        task.DownloadUrl = resolved;
                    else
                        ParseLogger.Op("play网关未解析成功", $"id={task.AwemeId} 仍用原地址 host={GetHost(task.DownloadUrl)}");
                }

                using var request = new HttpRequestMessage(HttpMethod.Get, task.DownloadUrl);
                if (existing > 0)
                    request.Headers.Range = new RangeHeaderValue(existing, null);

                using var resp = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);

                if (!resp.IsSuccessStatusCode && resp.StatusCode != HttpStatusCode.PartialContent)
                {
                    // 有本地 .part 时，无论 400 / 416 / 其它错误，都尝试删掉后重下一次
                    // 很多抖音 CDN 对 Range 会直接回 400，而不是 416
                    if (existing > 0)
                    {
                        ParseLogger.Op("续传被拒", $"HTTP {(int)resp.StatusCode} id={task.AwemeId}，删.part重下");
                        Log?.Invoke($"续传被拒绝 HTTP {(int)resp.StatusCode}，改为重新下载: {fileName}");
                        try { File.Delete(tempPath); } catch { }
                        existing = 0;

                        using var req2 = new HttpRequestMessage(HttpMethod.Get, task.DownloadUrl);
                        // 不要带 Range
                        using var resp2 = await _http.SendAsync(req2, HttpCompletionOption.ResponseHeadersRead, token);

                        if (!resp2.IsSuccessStatusCode && resp2.StatusCode != HttpStatusCode.PartialContent)
                        {
                            task.Status = DownloadTaskStatus.Failed;
                            task.ErrorMessage = $"HTTP {(int)resp2.StatusCode}";
                            task.Size = "-";
                            task.Speed = "-";
                            RaiseUpdated(task);
                            HistoryStore.Add(CloneForHistory(task));
                            PendingTaskStore.Remove(task);
                            var us = task.DownloadUrl ?? "";
                            if (us.Length > 100) us = us.Substring(0, 100) + "...";
                            ParseLogger.Fail($"下载失败 id={task.AwemeId} HTTP {(int)resp2.StatusCode} url={us}");
                            return;
                        }

                        await WriteResponseToFileAsync(task, resp2, tempPath, savePath, fileName, 0, linked, token);
                        return;
                    }

                    // 无本地进度：HTTP 400 → 重新解析换新地址再试一次（CDN 签名/冷节点也常 400）
                    if ((int)resp.StatusCode == 400 && !string.IsNullOrEmpty(task.Url))
                    {
                        try
                        {
                            ParseLogger.Op("400换链重试", $"id={task.AwemeId} oldHost={GetHost(task.DownloadUrl)}");
                            var parser = new LinkParser(_settings.DouyinCookie, _settings.TikTokCookie);
                            var again = await parser.ParseSingleAsync(task.Url);
                            var candidate = !string.IsNullOrEmpty(again.DownloadUrl) ? again.DownloadUrl : task.DownloadUrl;

                            var resolved = await ResolvePlayGatewayAsync(candidate!, token);
                            if (!string.IsNullOrEmpty(resolved))
                                candidate = resolved;

                            if (string.IsNullOrEmpty(candidate))
                            {
                                ParseLogger.Op("400换链无可用CDN", $"id={task.AwemeId}");
                            }
                            else
                            {
                                task.DownloadUrl = candidate;
                                using var req3 = new HttpRequestMessage(HttpMethod.Get, task.DownloadUrl);
                                req3.Headers.TryAddWithoutValidation("Referer", "https://www.douyin.com/");
                                using var resp3 = await _http.SendAsync(req3, HttpCompletionOption.ResponseHeadersRead, token);
                                if (resp3.IsSuccessStatusCode || resp3.StatusCode == HttpStatusCode.PartialContent)
                                {
                                    await WriteResponseToFileAsync(task, resp3, tempPath, savePath, fileName, 0, linked, token);
                                    return;
                                }
                                ParseLogger.Fail($"400换链仍失败 id={task.AwemeId} HTTP {(int)resp3.StatusCode} host={GetHost(task.DownloadUrl)}");
                            }
                        }
                        catch (Exception ex)
                        {
                            ParseLogger.Fail($"400换链异常 id={task.AwemeId}: {ex.Message}");
                        }
                    }

                    task.Status = DownloadTaskStatus.Failed;
                    task.ErrorMessage = $"HTTP {(int)resp.StatusCode}";
                    task.Size = "-";
                    task.Speed = "-";
                    RaiseUpdated(task);
                    HistoryStore.Add(CloneForHistory(task));
                    PendingTaskStore.Remove(task);
                    var u = task.DownloadUrl ?? "";
                    if (u.Length > 100) u = u.Substring(0, 100) + "...";
                    ParseLogger.Fail($"下载失败 id={task.AwemeId} HTTP {(int)resp.StatusCode} url={u}");
                    return;
                }

                bool isPartial = resp.StatusCode == HttpStatusCode.PartialContent;
                if (existing > 0 && !isPartial)
                {
                    // 未返回 206：当作不支持 Range，丢掉旧 .part，按完整流重写
                    Log?.Invoke("服务器未返回 206，覆盖重下: " + fileName);
                    try { File.Delete(tempPath); } catch { }
                    existing = 0;
                }

                await WriteResponseToFileAsync(task, resp, tempPath, savePath, fileName, existing, linked, token);
            }
            catch (OperationCanceledException)
            {
                if (IsPaused(task) && !IsCancelled(task))
                {
                    task.Status = DownloadTaskStatus.Paused;
                    task.Speed = "-";
                    RaiseUpdated(task);
                    PendingTaskStore.Upsert(task);
                }
                else
                {
                    PendingTaskStore.CancelAndCleanup(_settings, new[] { task });
                    task.Status = DownloadTaskStatus.Cancelled;
                    task.Progress = 0;
                    task.Speed = "-";
                    RaiseUpdated(task);
                }
            }
            catch (Exception ex)
            {
                task.Status = DownloadTaskStatus.Failed;
                task.ErrorMessage = ex.Message;
                task.Size = "-";
                task.Speed = "-";
                RaiseUpdated(task);
                HistoryStore.Add(CloneForHistory(task));
                PendingTaskStore.Remove(task);
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

        private async Task WriteResponseToFileAsync(
        DownloadTask task,
        HttpResponseMessage resp,
        string tempPath,
        string savePath,
        string fileName,
        long existing,
        CancellationTokenSource linked,
        CancellationToken token)
        {
            var contentType = resp.Content.Headers.ContentType?.MediaType ?? "";
            if (contentType.Contains("text/html") || contentType.Contains("application/json")
                || contentType.Contains("text/plain"))
            {
                task.Status = DownloadTaskStatus.Failed;
                task.ErrorMessage = "返回的不是视频（Content-Type=" + contentType + "），链接可能失效或需 Cookie";
                task.Size = "-";
                RaiseUpdated(task);
                HistoryStore.Add(CloneForHistory(task));
                PendingTaskStore.Remove(task);
                Log?.Invoke("失败(非视频): " + task.Title);
                return;
            }

            long total = 0;
            var contentRange = resp.Content.Headers.ContentRange;
            if (contentRange?.Length != null)
                total = contentRange.Length.Value;
            else
            {
                var len = resp.Content.Headers.ContentLength ?? 0;
                total = existing > 0 ? existing + len : len;
            }

            if (existing > 0 && total > 0)
                task.Progress = Math.Min(99, existing * 100.0 / total);

            // 关键：不要用 await using，改成手动控制关闭时机
            FileStream? output = null;
            try
            {
                var mode = existing > 0 ? FileMode.Append : FileMode.Create;
                output = new FileStream(tempPath, mode, FileAccess.Write, FileShare.None, 81920, true);

                await using var input = await resp.Content.ReadAsStreamAsync(token);

                var buffer = new byte[81920];
                long readTotal = existing;
                int read;
                var sw = System.Diagnostics.Stopwatch.StartNew();
                long lastBytes = existing;

                // 仅全新下载时检测是否是 HTML
                if (existing == 0)
                {
                    read = await input.ReadAsync(buffer.AsMemory(0, Math.Min(buffer.Length, 4096)), token);
                    if (read > 0)
                    {
                        var head = System.Text.Encoding.UTF8.GetString(buffer, 0, Math.Min(read, 200));
                        if (head.Contains("<!DOCTYPE", StringComparison.OrdinalIgnoreCase) ||
                            head.Contains("<html", StringComparison.OrdinalIgnoreCase))
                        {
                            task.Status = DownloadTaskStatus.Failed;
                            task.ErrorMessage = "下载到的是 HTML 页面，不是视频文件";
                            ParseLogger.Fail($"下载失败 id={task.AwemeId} {task.ErrorMessage}");
                            RaiseUpdated(task);
                            HistoryStore.Add(CloneForHistory(task));
                            PendingTaskStore.Remove(task);
                            return;
                        }
                        await output.WriteAsync(buffer.AsMemory(0, read), token);
                        readTotal += read;
                    }
                }

                while ((read = await input.ReadAsync(buffer.AsMemory(0, buffer.Length), token)) > 0)
                {
                    if (IsCancelled(task)) token.ThrowIfCancellationRequested();
                    if (IsPaused(task))
                    {
                        try { linked.Cancel(); } catch { }
                        token.ThrowIfCancellationRequested();
                    }

                    await output.WriteAsync(buffer.AsMemory(0, read), token);
                    readTotal += read;

                    // 更频繁刷盘（约每 128KB），资源管理器能更快看到文件变大
                    if (readTotal % (128 * 1024) < buffer.Length)
                    {
                        await output.FlushAsync(token);
                    }

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
                        task.Size = total > 0
                            ? $"{FormatSize(readTotal)} / {FormatSize(total)}"
                            : FormatSize(readTotal);
                        lastBytes = readTotal;
                        sw.Restart();
                        RaiseUpdated(task);
                        PendingTaskStore.Upsert(task);
                    }
                }

                // 下载结束，强制刷盘
                await output.FlushAsync(token);
                output.Close();          // 先关闭
                output.Dispose();
                output = null;           // 防止 finally 再关

                // 此时文件句柄已经释放，可以安全 Move
                if (readTotal < 50 * 1024)
                {
                    try { File.Delete(tempPath); } catch { }
                    task.Status = DownloadTaskStatus.Failed;
                    task.ErrorMessage = $"文件过小({FormatSize(readTotal)})，可能不是有效视频";
                    task.Size = FormatSize(readTotal);
                    RaiseUpdated(task);
                    HistoryStore.Add(CloneForHistory(task));
                    PendingTaskStore.Remove(task);
                    return;
                }

                if (IsCancelled(task))
                {
                    try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch { }
                    task.Status = DownloadTaskStatus.Cancelled;
                    task.Progress = 0;
                    RaiseUpdated(task);
                    PendingTaskStore.Remove(task);
                    return;
                }

                if (IsPaused(task))
                {
                    // 暂停：保留 .part，不删除
                    task.Status = DownloadTaskStatus.Paused;
                    task.Speed = "-";
                    RaiseUpdated(task);
                    PendingTaskStore.Upsert(task);
                    return;
                }

                // 正常完成 —— 现在句柄已关闭，Move 不会报“文件被占用”
                if (File.Exists(savePath))
                {
                    try { File.Delete(savePath); } catch { }
                }
                File.Move(tempPath, savePath);

                await DownloadExtrasAsync(task, Path.GetDirectoryName(savePath)!, token);

                task.Status = DownloadTaskStatus.Completed;
                task.Progress = 100;
                task.SavePath = savePath;
                task.Size = FormatSize(readTotal);
                task.Speed = "-";
                task.Remaining = "-";
                RaiseUpdated(task);
                HistoryStore.Add(CloneForHistory(task));
                PendingTaskStore.Remove(task);
                Log?.Invoke("完成: " + fileName);
                ParseLogger.Op("下载完成", fileName + " -> " + savePath);
            }
            finally
            {
                // 确保异常时也关闭流
                if (output != null)
                {
                    try { output.Flush(); } catch { }
                    try { output.Close(); } catch { }
                    try { output.Dispose(); } catch { }
                }
            }
        }

        private async Task DownloadImagesAsync(DownloadTask task, string dir, CancellationToken ct)
        {
            var folder = Path.Combine(dir, DownloadPathHelper.MakeSafeFileName(task.Title) + "_图集");
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
            => DownloadPathHelper.MakeSafeFileName(name);

        private static string FormatSize(long bytes)
        {
            if (bytes < 1024) return bytes + " B";
            if (bytes < 1024 * 1024) return (bytes / 1024.0).ToString("F1") + " KB";
            if (bytes < 1024L * 1024 * 1024) return (bytes / 1024.0 / 1024).ToString("F1") + " MB";
            return (bytes / 1024.0 / 1024 / 1024).ToString("F2") + " GB";
        }

        private async Task DownloadExtrasAsync(DownloadTask task, string dir, CancellationToken ct)
        {
            bool wantCover = task.DownloadCover || _settings.DownloadCover;
            if (!wantCover) return;

            if (string.IsNullOrEmpty(task.CoverUrl))
            {
                try
                {
                    if (!string.IsNullOrEmpty(task.Url) || !string.IsNullOrEmpty(task.AwemeId))
                    {
                        var parser = new LinkParser(_settings.DouyinCookie, _settings.TikTokCookie);
                        var url = !string.IsNullOrEmpty(task.Url)
                            ? task.Url
                            : ("https://www.douyin.com/video/" + task.AwemeId);
                        var parsed = await parser.ParseSingleAsync(url);
                        if (!string.IsNullOrEmpty(parsed.CoverUrl))
                            task.CoverUrl = parsed.CoverUrl;
                    }
                }
                catch { }
            }

            if (!string.IsNullOrEmpty(task.CoverUrl))
            {
                try
                {
                    var coverPath = Path.Combine(dir, DownloadPathHelper.BuildFileName(task, "") + "_cover.jpg");
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

        /// <summary>
        /// 把 aweme.snssdk / iesdouyin 的 play 网关跟到最终 CDN；失败则试几种常见变体。
        /// </summary>
        private async Task<string?> ResolvePlayGatewayAsync(string playUrl, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(playUrl)) return null;
            bool isGateway = playUrl.Contains("snssdk.com/aweme/v1/play", StringComparison.OrdinalIgnoreCase)
                          || playUrl.Contains("iesdouyin.com/aweme/v1/play", StringComparison.OrdinalIgnoreCase);
            if (!isGateway) return playUrl; // 已是 CDN，原样返回

            // 变体：不同 host / ratio
            var variants = new List<string> { playUrl };
            try
            {
                var m = System.Text.RegularExpressions.Regex.Match(playUrl, @"[?&]video_id=([^&]+)");
                if (m.Success)
                {
                    string enc = m.Groups[1].Value; // 已是编码或原样，拼进 query 即可
                    variants.Add($"https://www.iesdouyin.com/aweme/v1/play/?video_id={enc}&ratio=1080p&line=0");
                    variants.Add($"https://www.iesdouyin.com/aweme/v1/play/?video_id={enc}&ratio=720p&line=0");
                    variants.Add($"https://aweme.snssdk.com/aweme/v1/play/?video_id={enc}&ratio=1080p&line=0");
                    variants.Add($"https://aweme.snssdk.com/aweme/v1/play/?video_id={enc}&ratio=720p&line=1");
                }
            }
            catch { }

            variants = variants.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            foreach (var v in variants)
            {
                try
                {
                    using var req = new HttpRequestMessage(HttpMethod.Get, v);
                    req.Headers.TryAddWithoutValidation("User-Agent",
                        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36");
                    req.Headers.TryAddWithoutValidation("Referer", "https://www.douyin.com/");
                    req.Headers.TryAddWithoutValidation("Origin", "https://www.douyin.com");
                    req.Headers.TryAddWithoutValidation("Accept", "*/*");
                    // 不要把整站 Cookie 强加在 play 上有时反而 400；有 Cookie 可试一次，无则也试
                    if (!string.IsNullOrEmpty(_settings.DouyinCookie))
                        req.Headers.TryAddWithoutValidation("Cookie", _settings.DouyinCookie);

                    // 允许自动跳转：最终应落到 douyinvod / bdydns 等
                    using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
                    var final = resp.RequestMessage?.RequestUri?.AbsoluteUri ?? v;

                    if ((resp.IsSuccessStatusCode || resp.StatusCode == HttpStatusCode.PartialContent)
                        && !final.Contains("snssdk.com/aweme/v1/play", StringComparison.OrdinalIgnoreCase)
                        && !final.Contains("iesdouyin.com/aweme/v1/play", StringComparison.OrdinalIgnoreCase))
                    {
                        ParseLogger.Op("play网关已解析", $"-> {GetHost(final)}");
                        return final;
                    }

                    // 手动看 Location（个别环境不自动跟）
                    if ((int)resp.StatusCode is 301 or 302 or 303 or 307 or 308)
                    {
                        var loc = resp.Headers.Location?.ToString();
                        if (!string.IsNullOrEmpty(loc))
                        {
                            if (loc.StartsWith("/")) loc = "https://" + new Uri(v).Host + loc;
                            ParseLogger.Op("play网关Location", GetHost(loc));
                            return loc;
                        }
                    }

                    ParseLogger.Op("play网关尝试失败", $"HTTP {(int)resp.StatusCode} try={GetHost(v)} final={GetHost(final)}");
                }
                catch (Exception ex)
                {
                    ParseLogger.Op("play网关异常", ex.Message);
                }
            }
            return null;
        }

        private void RaiseUpdated(DownloadTask task)
        {
            TaskUpdated?.Invoke(task);
        }

        private static string GetHost(string? url)
        {
            if (string.IsNullOrEmpty(url)) return "";
            try { return new Uri(url).Host; } catch { return url.Length > 40 ? url.Substring(0, 40) : url; }
        }
    }
}