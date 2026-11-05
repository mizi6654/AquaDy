using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using Newtonsoft.Json.Linq;
using AquaDy.Models;

namespace AquaDy.Services
{
    public enum AccountParseMode
    {
        /// <summary>浏览器打开主页，滚动并拦截列表接口响应（默认推荐）</summary>
        BrowserIntercept = 0,
        /// <summary>浏览器内 fetch 调用 /aweme/v1/web/aweme/post 翻页（页内签名环境）</summary>
        ApiInBrowser = 1
    }

    /// <summary>
    /// 主页双模式解析（WebView2）。须在 UI 线程调用。
    /// </summary>
    public class AccountParseService
    {
        private readonly string _cookie;

        public AccountParseService(string douyinCookie)
        {
            _cookie = douyinCookie ?? "";
        }

        public async Task<List<DownloadTask>> ParseAsync(
            string homepageUrl,
            AccountParseMode mode,
            int maxPages,
            bool fullMode,
            IProgress<string>? status = null,
            CancellationToken ct = default)
        {
            homepageUrl = LinkParser.ExtractUrl(homepageUrl);
            if (string.IsNullOrEmpty(homepageUrl))
                return new List<DownloadTask>();

            var secMatch = Regex.Match(homepageUrl, @"/user/([A-Za-z0-9_\-]+)");
            if (!secMatch.Success)
                return new List<DownloadTask>();
            string secUid = secMatch.Groups[1].Value;
            int pages = maxPages > 0 ? maxPages : (fullMode ? 20 : 3);

            // 隐藏宿主窗体承载 WebView2（需 UI 线程）
            using var host = new Form
            {
                ShowInTaskbar = false,
                FormBorderStyle = FormBorderStyle.FixedToolWindow,
                Opacity = 0,
                Size = new System.Drawing.Size(1280, 900),
                StartPosition = FormStartPosition.Manual,
                Location = new System.Drawing.Point(-20000, -20000),
                Text = "解析中…"
            };
            var web = new WebView2 { Dock = DockStyle.Fill };
            host.Controls.Add(web);
            host.Show();

            try
            {
                status?.Report("初始化 WebView2...");
                var userData = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "AquaDy", "WebView2Profile");
                System.IO.Directory.CreateDirectory(userData);
                var env = await CoreWebView2Environment.CreateAsync(null, userData);
                await web.EnsureCoreWebView2Async(env);

                web.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                web.CoreWebView2.Settings.IsStatusBarEnabled = false;

                var idSet = new HashSet<string>();
                var results = new List<DownloadTask>();
                string authorName = "";
                var gate = new object();

                web.CoreWebView2.WebResourceResponseReceived += async (s, e) =>
                {
                    try
                    {
                        var uri = e.Request.Uri ?? "";
                        if (uri.IndexOf("aweme/post", StringComparison.OrdinalIgnoreCase) < 0)
                            return;
                        using var stream = await e.Response.GetContentAsync();
                        if (stream == null) return;
                        using var reader = new System.IO.StreamReader(stream);
                        var json = await reader.ReadToEndAsync();
                        lock (gate)
                        {
                            MergeAwemeList(json, idSet, results, ref authorName);
                        }
                        status?.Report($"已拦截列表数据：{results.Count} 条");
                    }
                    catch { }
                };

                await ApplyCookiesAsync(web.CoreWebView2);
                var userUrl = "https://www.douyin.com/user/" + secUid + "?from_tab_name=main&showTab=post";
                status?.Report("打开用户主页...");
                web.CoreWebView2.Navigate(userUrl);
                await WaitNavAsync(web, 20000, ct);
                await Task.Delay(1800, ct);

                if (mode == AccountParseMode.BrowserIntercept)
                {
                    status?.Report("滚动加载并拦截作品列表...");
                    int rounds = Math.Max(pages, 3);
                    for (int i = 0; i < rounds; i++)
                    {
                        ct.ThrowIfCancellationRequested();
                        await web.CoreWebView2.ExecuteScriptAsync(
                            "window.scrollTo(0, document.body.scrollHeight || document.documentElement.scrollHeight);");
                        await Task.Delay(1400, ct);
                        status?.Report($"滚动 {i + 1}/{rounds}，当前 {results.Count} 条");
                        if (!fullMode && results.Count >= 18) break;
                    }
                }
                else
                {
                    status?.Report("页内 API 翻页拉取作品列表...");
                    long cursor = 0;
                    for (int page = 0; page < pages; page++)
                    {
                        ct.ThrowIfCancellationRequested();
                        var script = BuildFetchScript(secUid, cursor);
                        var encoded = await web.CoreWebView2.ExecuteScriptAsync(script);
                        var json = DecodeScriptResult(encoded);
                        int before = results.Count;
                        long next = 0;
                        bool hasMore = false;
                        try
                        {
                            var root = JObject.Parse(json);
                            lock (gate)
                            {
                                MergeAwemeList(json, idSet, results, ref authorName);
                            }
                            hasMore = root.Value<bool?>("has_more") == true || root.Value<int?>("has_more") == 1;
                            next = root.Value<long?>("max_cursor") ?? 0;
                        }
                        catch
                        {
                            await web.CoreWebView2.ExecuteScriptAsync(
                                "window.scrollTo(0, document.body.scrollHeight);");
                            await Task.Delay(1000, ct);
                        }

                        status?.Report($"API 第 {page + 1} 页，累计 {results.Count} 条");
                        if (results.Count == before && page > 0) break;
                        if (!hasMore || next <= 0 || next == cursor) break;
                        cursor = next;
                        await Task.Delay(600, ct);
                    }
                }

                // HTML 兜底
                try
                {
                    var htmlEnc = await web.CoreWebView2.ExecuteScriptAsync("document.documentElement.outerHTML");
                    var html = DecodeScriptResult(htmlEnc);
                    foreach (Match m in Regex.Matches(html ?? "", "\"aweme_id\"\\s*:\\s*\"?(\\d{15,})\"?"))
                    {
                        var id = m.Groups[1].Value;
                        if (!idSet.Add(id)) continue;
                        results.Add(new DownloadTask
                        {
                            AwemeId = id,
                            Title = "作品 " + id,
                            Author = authorName,
                            Url = "https://www.douyin.com/video/" + id,
                            Status = DownloadTaskStatus.Waiting,
                            Type = ContentType.Video
                        });
                    }
                }
                catch { }

                status?.Report($"完成：{results.Count} 条");
                return results.OrderByDescending(x => x.PublishTime ?? DateTime.MinValue).ToList();
            }
            finally
            {
                try { host.Close(); } catch { }
            }
        }

        private async Task ApplyCookiesAsync(CoreWebView2 core)
        {
            if (string.IsNullOrWhiteSpace(_cookie)) return;
            try
            {
                core.Navigate("https://www.douyin.com/");
                await Task.Delay(1000);
                foreach (var part in _cookie.Split(';', StringSplitOptions.RemoveEmptyEntries))
                {
                    var kv = part.Trim().Split(new[] { '=' }, 2);
                    if (kv.Length != 2) continue;
                    try
                    {
                        var c = core.CookieManager.CreateCookie(kv[0].Trim(), kv[1].Trim(), ".douyin.com", "/");
                        core.CookieManager.AddOrUpdateCookie(c);
                    }
                    catch { }
                }
            }
            catch { }
        }

        private static async Task WaitNavAsync(WebView2 web, int timeoutMs, CancellationToken ct)
        {
            var tcs = new TaskCompletionSource<bool>();
            void Handler(object? s, CoreWebView2NavigationCompletedEventArgs e)
            {
                tcs.TrySetResult(e.IsSuccess);
            }
            web.CoreWebView2.NavigationCompleted += Handler;
            try
            {
                var finished = await Task.WhenAny(tcs.Task, Task.Delay(timeoutMs, ct));
                if (finished != tcs.Task) { /* timeout */ }
            }
            finally
            {
                web.CoreWebView2.NavigationCompleted -= Handler;
            }
        }

        private static string BuildFetchScript(string secUid, long cursor)
        {
            return $@"
(async () => {{
  try {{
    const params = new URLSearchParams({{
      device_platform: 'webapp',
      aid: '6383',
      channel: 'channel_pc_web',
      sec_user_id: '{secUid}',
      max_cursor: '{cursor}',
      locate_query: 'false',
      show_live_replay_switch: '1',
      need_time_list: '1',
      time_list_query: '0',
      count: '18',
      publish_video_strategy_type: '2',
      from_user_page: '1',
      update_version_code: '170400'
    }});
    const res = await fetch('https://www.douyin.com/aweme/v1/web/aweme/post/?' + params.toString(), {{
      method: 'GET',
      credentials: 'include',
      headers: {{
        'Accept': 'application/json, text/plain, */*',
        'Referer': 'https://www.douyin.com/user/{secUid}'
      }}
    }});
    return await res.text();
  }} catch (e) {{
    return JSON.stringify({{ error: String(e), aweme_list: [] }});
  }}
}})()
";
        }

        private static string DecodeScriptResult(string? scriptResult)
        {
            if (string.IsNullOrEmpty(scriptResult)) return "";
            try { return JToken.Parse(scriptResult).ToString() ?? ""; }
            catch
            {
                return scriptResult.Trim().Trim('"')
                    .Replace("\\r", "").Replace("\\n", "\n").Replace("\\\"", "\"").Replace("\\\\", "\\");
            }
        }

        private static void MergeAwemeList(
            string json,
            HashSet<string> idSet,
            List<DownloadTask> results,
            ref string authorName)
        {
            if (string.IsNullOrWhiteSpace(json) || json.Length < 10) return;
            JObject root;
            try { root = JObject.Parse(json); }
            catch { return; }

            var list = root["aweme_list"] as JArray ?? root["data"]?["aweme_list"] as JArray;
            if (list == null) return;

            foreach (var token in list)
            {
                if (token is not JObject item) continue;
                var id = item.Value<string>("aweme_id") ?? "";
                if (string.IsNullOrEmpty(id) || !idSet.Add(id)) continue;

                var task = new DownloadTask
                {
                    AwemeId = id,
                    Url = "https://www.douyin.com/video/" + id,
                    Status = DownloadTaskStatus.Waiting
                };
                ApplyItem(task, item);
                if ((string.IsNullOrEmpty(task.Author) || task.Author == "未知作者") && !string.IsNullOrEmpty(authorName))
                    task.Author = authorName;
                else if (!string.IsNullOrEmpty(task.Author) && task.Author != "未知作者")
                    authorName = task.Author;
                results.Add(task);
            }
        }

        private static void ApplyItem(DownloadTask task, JObject item)
        {
            var desc = item.Value<string>("desc") ?? "";
            task.Description = desc;
            task.Title = string.IsNullOrWhiteSpace(desc)
                ? ("作品 " + task.AwemeId)
                : (desc.Length > 80 ? desc.Substring(0, 80) + "..." : desc);

            var author = item["author"] as JObject;
            task.Author = author?.Value<string>("nickname") ?? "未知作者";

            var ct = item.Value<long?>("create_time");
            if (ct.HasValue && ct.Value > 0)
                task.PublishTime = DateTimeOffset.FromUnixTimeSeconds(ct.Value).LocalDateTime;

            var images = item["images"] as JArray;
            if (images != null && images.Count > 0)
            {
                task.Type = ContentType.Image;
                task.ImageUrls = new List<string>();
                foreach (var img in images)
                {
                    var u = (img?["url_list"] as JArray)?.FirstOrDefault()?.ToString();
                    if (!string.IsNullOrEmpty(u)) task.ImageUrls.Add(u!);
                }
            }

            var video = item["video"] as JObject;
            string? play = null;
            if (video != null)
            {
                play = (video["play_addr"]?["url_list"] as JArray)?.FirstOrDefault()?.ToString()
                    ?? (video["download_addr"]?["url_list"] as JArray)?.FirstOrDefault()?.ToString();
                if (string.IsNullOrEmpty(play) && video["bit_rate"] is JArray br)
                {
                    foreach (var b in br)
                    {
                        var u = (b?["play_addr"]?["url_list"] as JArray)?.FirstOrDefault()?.ToString();
                        if (!string.IsNullOrEmpty(u)) { play = u; break; }
                    }
                }
            }
            if (!string.IsNullOrEmpty(play))
            {
                task.DownloadUrl = play!.Replace("/playwm/", "/play/").Replace("playwm", "play");
                if ((task.ImageUrls?.Count ?? 0) == 0) task.Type = ContentType.Video;
            }
        }
    
        /// <summary>
        /// 解析合集页 https://www.douyin.com/collection/{mixId}
        /// </summary>
        /// <summary>
        /// 合集解析：严格只走官方 /aweme/v1/web/mix/aweme/ 翻页（与 TikTokDownloader / f2 一致）。
        /// 禁止用整页 DOM 抽链，避免把推荐/相关视频混进来。
        /// </summary>
        /// <summary>
        /// 合集：series/aweme（主）+ mix/aweme 拦截 + 右侧列表加载更多。
        /// 基于 1.34 可稳定出数的流程，补齐合集中文名/作者与 total_episode。
        /// </summary>
        public async Task<List<DownloadTask>> ParseCollectionAsync(
            string collectionUrl,
            int maxPages,
            bool fullMode,
            IProgress<string>? status = null,
            CancellationToken ct = default)
        {
            ParseLogger.Op("合集解析", $"url={collectionUrl}");

            var results = new List<DownloadTask>();
            var idSet = new HashSet<string>();
            string mixName = "";
            string mixAuthor = "";
            int totalEpisode = 0;
            try
            {
                collectionUrl = LinkParser.ExtractUrl(collectionUrl) ?? collectionUrl;
                var m = Regex.Match(collectionUrl ?? "", @"collection/(\d+)|/mix/(\d+)");
                if (!m.Success)
                {
                    status?.Report("无法提取合集ID");
                    return results;
                }
                string seriesId = m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value;
                mixName = "合集" + seriesId;
                string pageUrl = "https://www.douyin.com/collection/" + seriesId;
                int pages = maxPages > 0 ? maxPages : (fullMode ? 25 : 15);

                status?.Report("合集 " + seriesId + " …");

                using var host = new Form
                {
                    ShowInTaskbar = false,
                    WindowState = FormWindowState.Minimized,
                    FormBorderStyle = FormBorderStyle.FixedToolWindow,
                    Size = new System.Drawing.Size(1400, 900),
                    Opacity = 0,
                    Text = "合集解析"
                };
                var web = new WebView2 { Dock = DockStyle.Fill };
                host.Controls.Add(web);
                host.Show();

                try
                {
                    var env = await CoreWebView2Environment.CreateAsync(
                        null, Path.Combine(Path.GetTempPath(), "ttcs_wv2_series2"));
                    await web.EnsureCoreWebView2Async(env);
                    try { web.CoreWebView2.IsMuted = true; } catch { }
                    await ApplyCookiesAsync(web.CoreWebView2);

                    var gate = new object();
                    web.CoreWebView2.WebResourceResponseReceived += (s, e) =>
                    {
                        try
                        {
                            var uri = e.Request?.Uri ?? "";
                            if (uri.IndexOf("series/aweme", StringComparison.OrdinalIgnoreCase) < 0
                                && uri.IndexOf("mix/aweme", StringComparison.OrdinalIgnoreCase) < 0)
                                return;
                            var resp = e.Response;
                            if (resp == null) return;
                            _ = Task.Run(async () =>
                            {
                                try
                                {
                                    System.IO.Stream? stream = null;
                                    try { stream = await resp.GetContentAsync(); }
                                    catch (NotImplementedException) { return; }
                                    catch { return; }
                                    if (stream == null) return;
                                    using var reader = new StreamReader(stream);
                                    var json = await reader.ReadToEndAsync();
                                    lock (gate)
                                    {
                                        PullSeriesMeta(json, ref mixName, ref mixAuthor, ref totalEpisode);
                                        int b = results.Count;
                                        ParseAwemeListJson(json, results, idSet, mixName);
                                        FillAuthorMix(results, mixAuthor, mixName);
                                        if (results.Count > b)
                                            status?.Report("拦截累计 " + results.Count + (totalEpisode > 0 ? "/" + totalEpisode : ""));
                                    }
                                }
                                catch { }
                            });
                        }
                        catch { }
                    };

                    web.CoreWebView2.Navigate(pageUrl);
                    await WaitNavAsync(web, 25000, ct);
                    await Task.Delay(2000, ct);
                    try { web.CoreWebView2.IsMuted = true; } catch { }
                    try
                    {
                        await web.CoreWebView2.ExecuteScriptAsync(
                            "document.querySelectorAll('video').forEach(v=>{try{v.muted=true;v.pause();}catch(e){}});");
                    }
                    catch { }

                    // 合集标题 h4：合集 · xxx
                    try
                    {
                        var h4js = await web.CoreWebView2.ExecuteScriptAsync(
                            @"(() => {
                              const pick = (sel) => { const el = document.querySelector(sel); return (el && (el.innerText||'')).trim(); };
                              let t = pick('#related-card-list-container h2') || pick('#related-card-list-container h4')
                                   || pick('#douyin-right-container h2');
                              if (!t) {
                                for (const n of document.querySelectorAll('h2,h4')) {
                                  const s = (n.innerText||'').trim();
                                  if (s.includes('合集')) { t = s; break; }
                                }
                              }
                              return t || '';
                            })()");
                        var h4 = DecodeScriptResult(h4js)?.Trim().Trim('"') ?? "";
                        if (!string.IsNullOrEmpty(h4))
                        {
                            h4 = h4.Replace("合集 · ", "").Replace("合集·", "").Replace("合集 ", "").Trim();
                            if (!string.IsNullOrEmpty(h4) && h4.Length < 80) mixName = h4;
                        }
                    }
                    catch { }


                    // ---------- HttpClient + Cookie 拉 series/aweme ----------
                    try
                    {
                        using var http = new System.Net.Http.HttpClient(new System.Net.Http.HttpClientHandler
                        {
                            AutomaticDecompression = System.Net.DecompressionMethods.All
                        });
                        http.Timeout = TimeSpan.FromSeconds(25);
                        http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent",
                            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36");
                        http.DefaultRequestHeaders.TryAddWithoutValidation("Referer", pageUrl);
                        http.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/json, text/plain, */*");
                        if (!string.IsNullOrEmpty(_cookie))
                            http.DefaultRequestHeaders.TryAddWithoutValidation("Cookie", _cookie);

                        long hCursor = 0;
                        for (int page = 0; page < Math.Min(pages, 30); page++)
                        {
                            ct.ThrowIfCancellationRequested();
                            var api = "https://www.douyin.com/aweme/v1/web/series/aweme/?device_platform=webapp&aid=6383"
                                + "&channel=channel_pc_web&series_id=" + seriesId
                                + "&pull_type=2&cursor=" + hCursor + "&source=playlet_homepage_hot&count=20"
                                + "&version_code=170400&version_name=17.4.0&pc_client_type=1";
                            ParseLogger.Op("HTTP合集", $"page={page} cursor={hCursor}");
                            string body;
                            try { body = await http.GetStringAsync(api, ct); }
                            catch (Exception ex)
                            {
                                ParseLogger.Fail("HTTP合集失败: " + ex.Message);
                                break;
                            }
                            ParseLogger.SaveJson("http_series_aweme", seriesId + "_p" + page, body);
                            if (string.IsNullOrWhiteSpace(body) || body.Length < 50)
                            {
                                ParseLogger.Warn($"HTTP合集返回过短 len={body?.Length ?? 0}，转 WebView");
                                break;
                            }
                            int before, after;
                            lock (gate)
                            {
                                before = results.Count;
                                PullSeriesMeta(body, ref mixName, ref mixAuthor, ref totalEpisode);
                                ParseAwemeListJson(body, results, idSet, mixName);
                                FillAuthorMix(results, mixAuthor, mixName);
                                after = results.Count;
                            }
                            status?.Report($"HTTP series {page + 1} 累计 {after}" + (totalEpisode > 0 ? "/" + totalEpisode : ""));
                            bool hasMore = false;
                            long next = hCursor;
                            ParseCursor(body, ref hasMore, ref next, hCursor);
                            if (totalEpisode > 0 && after >= totalEpisode) break;
                            if (!hasMore || after == before) break;
                            hCursor = next == hCursor ? hCursor + 20 : next;
                        }
                    }
                    catch (Exception ex)
                    {
                        ParseLogger.Fail("HTTP合集异常: " + ex.Message);
                    }

                    // ---------- 主：series/aweme 翻页 ----------
                    long cursor = 0;
                    for (int page = 0; page < Math.Min(pages, 30); page++)
                    {
                        ct.ThrowIfCancellationRequested();
                        string js = $@"
(async () => {{
  try {{
    const params = new URLSearchParams({{
      device_platform: 'webapp', aid: '6383', channel: 'channel_pc_web',
      series_id: '{seriesId}', pull_type: '2', cursor: String({cursor}),
      source: 'playlet_homepage_hot', count: '20',
      version_code: '170400', version_name: '17.4.0'
    }});
    const res = await fetch('https://www.douyin.com/aweme/v1/web/series/aweme/?' + params.toString(), {{
      credentials: 'include',
      headers: {{ 'Accept': 'application/json', 'Referer': '{pageUrl}' }}
    }});
    return await res.text();
  }} catch (e) {{ return JSON.stringify({{ has_more: 0, aweme_list: [] }}); }}
}})()";
                        string text = await EvalJsonAsync(web, js);
                        int before, after;
                        lock (gate)
                        {
                            before = results.Count;
                            PullSeriesMeta(text, ref mixName, ref mixAuthor, ref totalEpisode);
                            ParseAwemeListJson(text, results, idSet, mixName);
                            FillAuthorMix(results, mixAuthor, mixName);
                            after = results.Count;
                        }
                        status?.Report($"series {page + 1} 累计 {after}" + (totalEpisode > 0 ? "/" + totalEpisode : "")
                            + (string.IsNullOrEmpty(mixAuthor) ? "" : (" " + mixAuthor))
                            + (mixName.StartsWith("合集") ? "" : ("/" + mixName)));

                        bool hasMore = false;
                        long next = cursor;
                        ParseCursor(text, ref hasMore, ref next, cursor);
                        if (totalEpisode > 0 && after >= totalEpisode) break;
                        if (!hasMore && after > before) break;
                        if (!hasMore && after == before && page > 0) break;
                        if (after == before && page > 2) break;
                        cursor = next == cursor ? cursor + 20 : next;
                        await Task.Delay(400, ct);
                    }

                    // ---------- 辅：mix/aweme ----------
                    if (totalEpisode <= 0 || results.Count < totalEpisode)
                    {
                        cursor = 0;
                        for (int page = 0; page < 10; page++)
                        {
                            ct.ThrowIfCancellationRequested();
                            string js = $@"
(async () => {{
  try {{
    const params = new URLSearchParams({{
      device_platform: 'webapp', aid: '6383', channel: 'channel_pc_web',
      mix_id: '{seriesId}', cursor: String({cursor}), count: '20',
      version_code: '170400', version_name: '17.4.0'
    }});
    const res = await fetch('https://www.douyin.com/aweme/v1/web/mix/aweme/?' + params.toString(), {{
      credentials: 'include',
      headers: {{ 'Accept': 'application/json', 'Referer': '{pageUrl}' }}
    }});
    return await res.text();
  }} catch (e) {{ return JSON.stringify({{ has_more: 0, aweme_list: [] }}); }}
}})()";
                            string text = await EvalJsonAsync(web, js);
                            int before, after;
                            lock (gate)
                            {
                                before = results.Count;
                                PullSeriesMeta(text, ref mixName, ref mixAuthor, ref totalEpisode);
                                ParseAwemeListJson(text, results, idSet, mixName);
                                FillAuthorMix(results, mixAuthor, mixName);
                                after = results.Count;
                            }
                            status?.Report($"mix {page + 1} 累计 {after}");
                            bool hasMore = false;
                            long next = cursor;
                            ParseCursor(text, ref hasMore, ref next, cursor);
                            if (totalEpisode > 0 && after >= totalEpisode) break;
                            if (!hasMore || (after == before && page > 0)) break;
                            cursor = next == cursor ? cursor + 20 : next;
                            await Task.Delay(350, ct);
                        }
                    }

                    // ---------- DOM 加载更多（仅当 API 不足）----------
                    int stable = 0, last = 0;
                    if (totalEpisode > 0 && results.Count >= totalEpisode)
                    {
                        status?.Report($"API 已满足 {results.Count}/{totalEpisode}，跳过 DOM");
                    }
                    for (int i = 0; totalEpisode <= 0 || results.Count < totalEpisode; i++)
                    {
                        if (i >= pages) break;
                        ct.ThrowIfCancellationRequested();
                        if (totalEpisode > 0 && results.Count >= totalEpisode) break;
                        try
                        {
                            await web.CoreWebView2.ExecuteScriptAsync(@"
(() => {
  document.querySelectorAll('video').forEach(v=>{try{v.muted=true;v.pause();}catch(e){}});
  // 精准 ul + 相关容器
  const ul = document.evaluate(
    '//*[@id=""douyin-right-container""]/div[2]/div/div/div/div/div[2]/div/div[3]/ul',
    document, null, XPathResult.FIRST_ORDERED_NODE_TYPE, null).singleNodeValue;
  const roots = [ul, document.querySelector('#related-card-list-container'), document.querySelector('#douyin-right-container')].filter(Boolean);
  for (const root of roots) {
    for (const el of root.querySelectorAll('button,div,span,a')) {
      const t = (el.innerText||'').trim();
      if (t && t.length < 14 && (t.indexOf('加载更多')>=0 || t==='更多')) { try{el.click();}catch(e){} }
    }
  }
})();");
                        }
                        catch { }
                        await Task.Delay(1100, ct);

                        try
                        {
                            var hrefJs = await web.CoreWebView2.ExecuteScriptAsync(@"
(() => {
  const ul = document.evaluate(
    '//*[@id=""douyin-right-container""]/div[2]/div/div/div/div/div[2]/div/div[3]/ul',
    document, null, XPathResult.FIRST_ORDERED_NODE_TYPE, null).singleNodeValue;
  let root = ul || document.querySelector('#related-card-list-container');
  if (!root) root = document.querySelector('#douyin-right-container');
  if (!root) return JSON.stringify({items:[]});
  const map = {};
  root.querySelectorAll('a[href*=""/video/""]').forEach(a => {
    const href = a.getAttribute('href') || '';
    const m = href.match(/\/video\/(\d{15,})/);
    if (!m) return;
    const id = m[1];
    const li = a.closest('li') || a.parentElement;
    const h3 = li ? li.querySelector('h3') : null;
    const title = ((h3 && h3.innerText) || a.innerText || '').trim().split('\n')[0];
    if (!map[id] || title.length > (map[id].title||'').length)
      map[id] = { id, title };
  });
  return JSON.stringify({ items: Object.values(map) });
})()");
                            var json = DecodeScriptResult(hrefJs) ?? "{}";
                            if (json.StartsWith("\""))
                            {
                                try { json = Newtonsoft.Json.JsonConvert.DeserializeObject<string>(json) ?? json; }
                                catch { }
                            }
                            lock (gate)
                            {
                                foreach (Match mm in Regex.Matches(json, "\"id\"\\s*:\\s*\"(\\d{15,})\""))
                                {
                                    var id = mm.Groups[1].Value;
                                    string title = "";
                                    int len = Math.Min(400, json.Length - mm.Index);
                                    if (len > 0)
                                    {
                                        var tm = Regex.Match(json.Substring(mm.Index, len), "\"title\"\\s*:\\s*\"((?:\\\\.|[^\"\\\\])*)\"");
                                        if (tm.Success)
                                        {
                                            try { title = Regex.Unescape(tm.Groups[1].Value); } catch { title = tm.Groups[1].Value; }
                                        }
                                    }
                                    if (!idSet.Add(id))
                                    {
                                        if (!string.IsNullOrWhiteSpace(title))
                                        {
                                            var exist = results.FirstOrDefault(x => x.AwemeId == id);
                                            if (exist != null && (exist.Title.StartsWith("作品 ") || string.IsNullOrEmpty(exist.Title)))
                                                exist.Title = title;
                                        }
                                        continue;
                                    }
                                    results.Add(new DownloadTask
                                    {
                                        AwemeId = id,
                                        Title = string.IsNullOrWhiteSpace(title) ? ("作品 " + id) : title,
                                        Author = mixAuthor,
                                        Url = "https://www.douyin.com/video/" + id,
                                        MixName = mixName,
                                        Status = DownloadTaskStatus.Waiting,
                                        Type = ContentType.Video
                                    });
                                }
                            }
                        }
                        catch { }

                        int count;
                        lock (gate) count = results.Count;
                        status?.Report($"列表 {i + 1}/{pages} 累计 {count}" + (totalEpisode > 0 ? "/" + totalEpisode : ""));
                        if (totalEpisode > 0 && count >= totalEpisode) break;
                        if (count > last) { last = count; stable = 0; }
                        else
                        {
                            stable++;
                            if (stable >= 4 && count > 0) break;
                        }
                    }

                    List<DownloadTask> output;
                    lock (gate)
                    {
                        FillAuthorMix(results, mixAuthor, mixName);
                        // 仅当 total_episode>0 且「第N集」数量>=total 时按集过滤，避免误杀
                        var episode = results.Where(t => Regex.IsMatch(t.Title ?? "", @"第\s*\d+\s*集")).ToList();
                        if (totalEpisode > 0 && episode.Count >= totalEpisode)
                        {
                            output = episode
                                .OrderBy(t =>
                                {
                                    var em = Regex.Match(t.Title ?? "", @"第\s*(\d+)\s*集");
                                    return em.Success ? int.Parse(em.Groups[1].Value) : 9999;
                                })
                                .GroupBy(t => t.AwemeId).Select(g => g.First())
                                .Take(totalEpisode).ToList();
                        }
                        else if (totalEpisode > 0)
                        {
                            // 有官方总集数：绝不超过，优先第N集
                            if (episode.Count >= totalEpisode)
                                output = episode.OrderBy(t => {
                                    var em = Regex.Match(t.Title ?? "", @"第\s*(\d+)\s*集");
                                    return em.Success ? int.Parse(em.Groups[1].Value) : 9999;
                                }).GroupBy(t => t.AwemeId).Select(g => g.First()).Take(totalEpisode).ToList();
                            else
                                output = results.GroupBy(t => t.AwemeId).Select(g => g.First()).Take(totalEpisode).ToList();
                        }
                        else if (episode.Count > 0 && episode.Count < results.Count && episode.Count <= 40)
                        {
                            // 无 total 但有明显「第N集」子集 → 用子集（防推荐混入）
                            output = episode.GroupBy(t => t.AwemeId).Select(g => g.First()).ToList();
                        }
                        else
                        {
                            output = results.GroupBy(t => t.AwemeId).Select(g => g.First()).ToList();
                        }
                        foreach (var t in output)
                        {
                            if (string.IsNullOrEmpty(t.MixName)) t.MixName = mixName;
                            if (string.IsNullOrEmpty(t.Author)) t.Author = mixAuthor;
                        }
                    }

                    var label = (!string.IsNullOrEmpty(mixAuthor) ? mixAuthor + "/" : "") + mixName;
                    // 第N集硬过滤：仅当匹配到的集数足够时采用（防推荐）；匹配过少则保留原结果（防 0 条）
                    {
                        var epOnly = output.Where(t => Regex.IsMatch(t.Title ?? "", @"第\s*\d+\s*集")).ToList();
                        if (epOnly.Count >= 3 && epOnly.Count >= output.Count * 0.5)
                        {
                            output = epOnly
                                .GroupBy(t => {
                                    var em = Regex.Match(t.Title ?? "", @"第\s*(\d+)\s*集");
                                    return em.Success ? em.Groups[1].Value : t.AwemeId;
                                })
                                .Select(g => g.First())
                                .OrderBy(t => {
                                    var em = Regex.Match(t.Title ?? "", @"第\s*(\d+)\s*集");
                                    return em.Success ? int.Parse(em.Groups[1].Value) : 9999;
                                }).ToList();
                        }
                    }
                    if (string.IsNullOrEmpty(mixAuthor))
                    {
                        var a = output.Select(t => t.Author).FirstOrDefault(x => !string.IsNullOrEmpty(x) && x != "未知作者");
                        if (!string.IsNullOrEmpty(a)) mixAuthor = a;
                    }
                    if (!string.IsNullOrEmpty(mixAuthor) || !string.IsNullOrEmpty(mixName))
                        FillAuthorMix(output, mixAuthor, mixName);
                    label = (!string.IsNullOrEmpty(mixAuthor) ? mixAuthor + "/" : "") + mixName;
                    
                    // 缺描述/时间/下载地址时，解析阶段补全（下载时不再二次解析）
                    var need = output.Where(t =>
                        string.IsNullOrEmpty(t.Description) || t.PublishTime == null
                        || string.IsNullOrEmpty(t.Author) || t.Author == "未知作者"
                        || string.IsNullOrEmpty(t.DownloadUrl)).ToList();
                    if (need.Count > 0)
                    {
                        status?.Report($"补全详情 {need.Count} 条…");
                        ParseLogger.Op("合集补全", $"need={need.Count}");
                        var parser = new LinkParser(_cookie, "");
                        var sem = new System.Threading.SemaphoreSlim(3);
                        await Task.WhenAll(need.Select(async t =>
                        {
                            await sem.WaitAsync(ct);
                            try
                            {
                                var p = await parser.ParseSingleAsync(t.Url);
                                if (string.IsNullOrEmpty(t.Description) && !string.IsNullOrEmpty(p.Description))
                                    t.Description = p.Description;
                                if (t.PublishTime == null && p.PublishTime != null)
                                    t.PublishTime = p.PublishTime;
                                if ((string.IsNullOrEmpty(t.Author) || t.Author == "未知作者") && !string.IsNullOrEmpty(p.Author))
                                    t.Author = p.Author;
                                if (string.IsNullOrEmpty(t.DownloadUrl) && !string.IsNullOrEmpty(p.DownloadUrl))
                                    t.DownloadUrl = p.DownloadUrl;
                                if (string.IsNullOrEmpty(t.CoverUrl) && !string.IsNullOrEmpty(p.CoverUrl))
                                    t.CoverUrl = p.CoverUrl;
                                if ((string.IsNullOrEmpty(t.MixName) || t.MixName.StartsWith("合集")) && !string.IsNullOrEmpty(p.MixName))
                                    t.MixName = p.MixName;
                                ParseLogger.Info($"补全 id={t.AwemeId} author={t.Author} descLen={t.Description?.Length ?? 0} hasUrl={!string.IsNullOrEmpty(t.DownloadUrl)} mix={t.MixName}");
                            }
                            catch (Exception ex) { ParseLogger.Fail($"补全失败 {t.AwemeId}: {ex.Message}"); }
                            finally { sem.Release(); }
                        }));
                        if (!string.IsNullOrEmpty(mixAuthor) || !string.IsNullOrEmpty(mixName))
                            FillAuthorMix(output, mixAuthor, mixName);
                    }
                    ParseLogger.Op("合集解析完成", $"count={output.Count} mix={mixName} author={mixAuthor}");
                    foreach (var t in output)
                        ParseLogger.Info($"列表项最终 id={t.AwemeId} title={t.Title} author={t.Author} mix={t.MixName} descLen={t.Description?.Length ?? 0} pub={t.PublishTime} url={t.Url}");

                    status?.Report($"合集完成：{output.Count} 条（{label}）");
                    return output;
                }
                finally
                {
                    try { host.Close(); } catch { }
                    try { host.Dispose(); } catch { }
                }
            }
            catch (Exception ex)
            {
                status?.Report("合集异常: " + ex.Message);
                return results;
            }
        }

        private static async Task<string> EvalJsonAsync(WebView2 web, string js)
        {
            try
            {
                var raw = await web.CoreWebView2.ExecuteScriptAsync(js);
                ParseLogger.Info($"EvalJson rawLen={raw?.Length ?? 0} head={(string.IsNullOrEmpty(raw) ? "" : raw.Substring(0, Math.Min(120, raw.Length)))}");
                if (string.IsNullOrWhiteSpace(raw) || raw == "null")
                    return "";
                string text = raw;
                try
                {
                    // string 返回值会被再次 JSON 编码
                    if (raw.Length >= 2 && raw[0] == '"')
                        text = Newtonsoft.Json.JsonConvert.DeserializeObject<string>(raw) ?? raw;
                    else
                        text = DecodeScriptResult(raw) ?? raw;
                }
                catch
                {
                    text = DecodeScriptResult(raw) ?? raw;
                }
                if (string.IsNullOrWhiteSpace(text) || text == "{}" || text == "null")
                {
                    ParseLogger.Warn("EvalJson 解码后为空");
                    return "";
                }
                ParseLogger.Info($"EvalJson decodedLen={text.Length}");
                return text;
            }
            catch (Exception ex)
            {
                ParseLogger.Fail("EvalJson: " + ex.Message);
                return "";
            }
        }

        private static void ParseCursor(string text, ref bool hasMore, ref long next, long cursor)
        {
            try
            {
                var root = Newtonsoft.Json.Linq.JObject.Parse(text);
                var hm = root["has_more"];
                if (hm != null)
                    hasMore = hm.Type == Newtonsoft.Json.Linq.JTokenType.Boolean ? hm.Value<bool>() : hm.Value<int>() != 0;
                next = root.Value<long?>("cursor") ?? root.Value<long?>("max_cursor") ?? cursor;
            }
            catch
            {
                var hm = Regex.Match(text ?? "", "\"has_more\"\\s*:\\s*(true|false|1|0)");
                if (hm.Success) hasMore = hm.Groups[1].Value is "true" or "1";
                var cm = Regex.Match(text ?? "", "\"cursor\"\\s*:\\s*\"?(\\d+)");
                if (cm.Success) long.TryParse(cm.Groups[1].Value, out next);
            }
        }

        private static void PullSeriesMeta(string? json, ref string mixName, ref string mixAuthor, ref int totalEpisode)
        {
            if (string.IsNullOrWhiteSpace(json)) return;
            try
            {
                if (json.StartsWith("\""))
                {
                    try { json = Newtonsoft.Json.JsonConvert.DeserializeObject<string>(json) ?? json; }
                    catch { }
                }
                // 优先 JSON 路径（原项目 mix_info.mix_name / series_info.series_name）
                try
                {
                    var root = Newtonsoft.Json.Linq.JObject.Parse(json);
                    string? name =
                        root.SelectToken("series_info.series_name")?.ToString()
                        ?? root.SelectToken("mix_info.mix_name")?.ToString()
                        ?? root.SelectToken("aweme_list[0].mix_info.mix_name")?.ToString()
                        ?? root.SelectToken("aweme_list[0].series_info.series_name")?.ToString()
                        ?? root.SelectToken("aweme_list[0].mix_info.mix_name_text")?.ToString();
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        name = name.Replace("合集 · ", "").Replace("合集·", "").Trim();
                        if (name.Length > 0 && name.Length < 80
                            && (mixName.StartsWith("合集") || string.IsNullOrEmpty(mixName) || mixName.Length < name.Length))
                            mixName = name;
                    }
                    string? author =
                        root.SelectToken("aweme_list[0].author.nickname")?.ToString()
                        ?? root.SelectToken("series_info.author.nickname")?.ToString()
                        ?? root.SelectToken("mix_info.author.nickname")?.ToString();
                    if (!string.IsNullOrWhiteSpace(author) && (string.IsNullOrEmpty(mixAuthor) || mixAuthor == "未知作者"))
                        mixAuthor = author.Trim();
                    int? te = root.Value<int?>("total_episode")
                        ?? root.SelectToken("series_info.total_episode")?.Value<int?>()
                        ?? root.SelectToken("mix_info.statis.current_episode")?.Value<int?>();
                    if (te.HasValue && te.Value > 0) totalEpisode = te.Value;
                }
                catch { }

                // 正则兜底
                foreach (var key in new[] { "series_name", "mix_name", "mix_name_text", "series_name_text" })
                {
                    var m = Regex.Match(json, "\"" + key + "\"\\s*:\\s*\"((?:\\\\.|[^\"\\\\])*)\"");
                    if (!m.Success) continue;
                    try
                    {
                        var name = Regex.Unescape(m.Groups[1].Value).Replace("合集 · ", "").Replace("合集·", "").Trim();
                        if (string.IsNullOrEmpty(name) || name.Length >= 80) continue;
                        if (mixName.StartsWith("合集") || string.IsNullOrEmpty(mixName) || mixName.Length < name.Length)
                            mixName = name;
                        break;
                    }
                    catch { }
                }
                if (string.IsNullOrEmpty(mixAuthor))
                {
                    var am = Regex.Match(json, "\"nickname\"\\s*:\\s*\"((?:\\\\.|[^\"\\\\])*)\"");
                    if (am.Success)
                    {
                        try { mixAuthor = Regex.Unescape(am.Groups[1].Value); } catch { mixAuthor = am.Groups[1].Value; }
                    }
                }
                var tm = Regex.Match(json, "\"total_episode\"\\s*:\\s*(\\d+)");
                if (tm.Success && int.TryParse(tm.Groups[1].Value, out var te2) && te2 > 0)
                    totalEpisode = te2;
            }
            catch { }
        }

        private static void FillAuthorMix(List<DownloadTask> list, string author, string mix)
        {
            foreach (var t in list)
            {
                if (!string.IsNullOrEmpty(mix) && (string.IsNullOrEmpty(t.MixName) || t.MixName.StartsWith("合集")))
                    t.MixName = mix;
                if (!string.IsNullOrEmpty(author) && string.IsNullOrEmpty(t.Author))
                    t.Author = author;
            }
        }

        private static void ParseAwemeListJson(string json, List<DownloadTask> results, HashSet<string> idSet, string mixName)
        {
            if (string.IsNullOrWhiteSpace(json)) return;
            try
            {
                if (string.IsNullOrWhiteSpace(json) || json.Trim() == "{}")
                {
                    ParseLogger.Warn($"ParseAwemeListJson 跳过空 JSON mix={mixName}");
                    return;
                }
                ParseLogger.SaveJson("series_or_mix_aweme", mixName, json);
                ParseLogger.Info($"ParseAwemeListJson 入口 mix={mixName} jsonLen={json.Length}");

                // 去掉可能的包装
                json = json.Trim();
                if (json.StartsWith("\"") && json.Contains("aweme_list"))
                {
                    try
                    {
                        var un = Newtonsoft.Json.JsonConvert.DeserializeObject<string>(json);
                        if (!string.IsNullOrEmpty(un)) json = un;
                    }
                    catch { }
                }

                var root = Newtonsoft.Json.Linq.JObject.Parse(json);
                var list = root["aweme_list"] as Newtonsoft.Json.Linq.JArray
                           ?? root["data"]?["aweme_list"] as Newtonsoft.Json.Linq.JArray;
                if (list == null) return;

                // 合集名优先用接口返回
                var mixFromApi = root["mix_info"]?["mix_name"]?.ToString()
                                 ?? root["mix_info"]?["desc"]?.ToString();
                if (!string.IsNullOrWhiteSpace(mixFromApi))
                    mixName = mixFromApi!;

                foreach (var item in list)
                {
                    var id = item.Value<string>("aweme_id") ?? item.Value<string>("awemeId") ?? "";
                    if (string.IsNullOrEmpty(id) || !idSet.Add(id)) continue;

                    var desc = item.Value<string>("desc") ?? "";
                    try
                    {
                        var prefix = item.SelectToken("series_play_info.item_title_prefix.text")?.ToString();
                        if (!string.IsNullOrEmpty(prefix) && !desc.StartsWith(prefix))
                            desc = prefix + " | " + desc;
                    }
                    catch { }
                    var author = item["author"]?.Value<string>("nickname")
                        ?? item.SelectToken("author.nickname")?.ToString()
                        ?? "";
                    // 原项目 Extractor: mix_title = mix_info.mix_name
                    try
                    {
                        var mn = item.SelectToken("mix_info.mix_name")?.ToString()
                              ?? item.SelectToken("series_info.series_name")?.ToString();
                        if (!string.IsNullOrWhiteSpace(mn) && mn.Length < 80)
                            mixName = mn.Split('#')[0].Trim();
                    }
                    catch { }
                    long? ctUnix = item.Value<long?>("create_time");
                    DateTime? pub = null;
                    if (ctUnix.HasValue && ctUnix > 0)
                        pub = DateTimeOffset.FromUnixTimeSeconds(ctUnix.Value).LocalDateTime;

                    string cover = "";
                    try
                    {
                        var covers = item["video"]?["cover"]?["url_list"] as Newtonsoft.Json.Linq.JArray
                                     ?? item["video"]?["origin_cover"]?["url_list"] as Newtonsoft.Json.Linq.JArray
                                     ?? item["cover"]?["url_list"] as Newtonsoft.Json.Linq.JArray;
                        if (covers != null && covers.Count > 0)
                            cover = covers[0]?.ToString() ?? "";
                    }
                    catch { }

                    string music = "";
                    try
                    {
                        var urls = item["music"]?["play_url"]?["url_list"] as Newtonsoft.Json.Linq.JArray;
                        if (urls != null && urls.Count > 0)
                            music = urls[0]?.ToString() ?? "";
                    }
                    catch { }

                    // 列表阶段尽量带上播放地址，下载时可跳过二次解析
                    string play = "";
                    try
                    {
                        var v = item["video"];
                        var urls = v?["play_addr"]?["url_list"] as Newtonsoft.Json.Linq.JArray
                                   ?? v?["download_addr"]?["url_list"] as Newtonsoft.Json.Linq.JArray
                                   ?? v?["play_addr_h264"]?["url_list"] as Newtonsoft.Json.Linq.JArray;
                        if (urls != null && urls.Count > 0)
                            play = urls[0]?.ToString() ?? "";
                        if (string.IsNullOrEmpty(play) && v?["bit_rate"] is Newtonsoft.Json.Linq.JArray br && br.Count > 0)
                        {
                            var u0 = br[0]?["play_addr"]?["url_list"] as Newtonsoft.Json.Linq.JArray;
                            if (u0 != null && u0.Count > 0) play = u0[0]?.ToString() ?? "";
                        }
                    }
                    catch { }

                    var title = desc;
                    try
                    {
                        var prefix = item.SelectToken("series_play_info.item_title_prefix.text")?.ToString();
                        if (!string.IsNullOrEmpty(prefix))
                            title = prefix + (string.IsNullOrEmpty(desc) ? "" : (" " + desc));
                    }
                    catch { }
                    if (title.Length > 80) title = title.Substring(0, 80);

                    results.Add(new DownloadTask
                    {
                        AwemeId = id,
                        Title = string.IsNullOrWhiteSpace(title) ? ("作品 " + id) : title.Trim(),
                        Description = desc,
                        Author = author,
                        Url = "https://www.douyin.com/video/" + id,
                        MixName = mixName,
                        CoverUrl = cover,
                        MusicUrl = music,
                        DownloadUrl = play,
                        PublishTime = pub,
                        Status = DownloadTaskStatus.Waiting,
                        Type = ContentType.Video
                    });
                    ParseLogger.Info($"列表项 id={id} author={author} mix={mixName} descLen={desc.Length} pub={pub} hasPlay={!string.IsNullOrEmpty(play)} title={title}");
                }
            }
            catch { }
        }


    }
}
