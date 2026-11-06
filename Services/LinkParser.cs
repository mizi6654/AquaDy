using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using AquaDy.Models;

namespace AquaDy.Services
{
    /// <summary>
    /// 抖音解析（参考开源思路：
    /// - iesdouyin 分享页 + _ROUTER_DATA / RENDER_DATA
    /// - 移动端 feed
    /// - play_addr.uri 构造无水印 play 地址
    /// 禁止取页面上第一个 nickname（会串成登录账号自己）
    /// </summary>
    public class LinkParser
    {
        private readonly string _cookie;
        private static readonly string MobileUA =
            "Mozilla/5.0 (iPhone; CPU iPhone OS 16_6 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/16.6 Mobile/15E148 Safari/604.1";
        private static readonly string DesktopUA =
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36";

        public LinkParser(string douyinCookie = "", string tiktokCookie = "")
        {
            _cookie = douyinCookie ?? "";
        }

        private HttpClient CreateClient(bool mobile)
        {
            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = true,
                AutomaticDecompression = System.Net.DecompressionMethods.All,
                UseCookies = true
            };
            var http = new HttpClient(handler);
            http.Timeout = TimeSpan.FromSeconds(25);
            http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", mobile ? MobileUA : DesktopUA);
            http.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "text/html,application/json,*/*");
            http.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Language", "zh-CN,zh;q=0.9,en;q=0.8");
            if (!string.IsNullOrEmpty(_cookie))
                http.DefaultRequestHeaders.TryAddWithoutValidation("Cookie", _cookie);
            return http;
        }

        public static string ExtractUrl(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";
            var patterns = new[]
            {
                @"https?://v\.douyin\.com/[A-Za-z0-9_\-]+/?",
                @"https?://www\.douyin\.com/(?:video|note)/[0-9]+",
                @"https?://www\.douyin\.com/user/[^\s?#]+",
                @"https?://vm\.tiktok\.com/[A-Za-z0-9]+/?",
                @"https?://vt\.tiktok\.com/[A-Za-z0-9]+/?",
                @"https?://www\.tiktok\.com/@[^/\s]+/video/[0-9]+"
            };
            foreach (var p in patterns)
            {
                var m = Regex.Match(text, p);
                if (m.Success) return m.Value.TrimEnd('/', '?', '#');
            }
            return text.Trim().StartsWith("http") ? text.Trim() : "";
        }

        public static bool IsHomepageUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return false;
            var u = url.ToLowerInvariant();
            if (u.Contains("/user/") && !u.Contains("/video/") && !u.Contains("/note/")) return true;
            if (u.Contains("tiktok.com/@") && !u.Contains("/video/")) return true;
            return false;
        }

        public async Task<DownloadTask> ParseSingleAsync(string rawUrl)
        {
            var task = new DownloadTask
            {
                Url = ExtractUrl(rawUrl),
                Status = DownloadTaskStatus.Parsing,
                Title = "-"
            };
            if (string.IsNullOrEmpty(task.Url))
            {
                task.Status = DownloadTaskStatus.Failed;
                task.ErrorMessage = "无效链接";
                return task;
            }

            try
            {
                using var http = CreateClient(mobile: true);

                // 短链还原
                string pageUrl = task.Url;
                if (pageUrl.Contains("v.douyin.com") || pageUrl.Contains("vm.tiktok.com") || pageUrl.Contains("vt.tiktok.com"))
                {
                    pageUrl = await ResolveShortUrlAsync(http, pageUrl) ?? pageUrl;
                    task.Url = pageUrl;
                }

                var idMatch = Regex.Match(pageUrl, @"(?:video|note)/(\d{6,})");
                if (idMatch.Success) task.AwemeId = idMatch.Groups[1].Value;
                // 短链跳转后可能在 modal_id / item_ids
                if (string.IsNullOrEmpty(task.AwemeId))
                {
                    var m2 = Regex.Match(pageUrl, @"(?:modal_id|item_ids)=(\d{6,})");
                    if (m2.Success) task.AwemeId = m2.Groups[1].Value;
                }

                if (string.IsNullOrEmpty(task.AwemeId))
                {
                    task.Status = DownloadTaskStatus.Failed;
                    task.ErrorMessage = "无法从链接提取作品 ID";
                    return task;
                }

                // 路径1：移动端分享页（多数无签名工具主力）
                if (await TryParseSharePageAsync(http, task))
                    return task;

                // 路径2：iesdouyin iteminfo
                if (await TryParseItemInfoAsync(http, task))
                    return task;

                // 路径3：移动端 feed
                if (await TryParseFeedAsync(http, task))
                    return task;

                // 路径4：桌面网页
                using var desk = CreateClient(mobile: false);
                if (await TryParseDesktopPageAsync(desk, task))
                    return task;

                task.Status = DownloadTaskStatus.Failed;
                task.ErrorMessage = "所有解析通道均失败。请确认链接有效，Cookie 含登录态。";
                task.Title = "解析失败";
                return task;
            }
            catch (Exception ex)
            {
                task.Status = DownloadTaskStatus.Failed;
                task.ErrorMessage = ex.Message;
                task.Title = "解析失败";
                return task;
            }
        }

        /// <summary>
        /// https://www.iesdouyin.com/share/video/{id}
        /// </summary>
        private async Task<bool> TryParseSharePageAsync(HttpClient http, DownloadTask task)
        {
            try
            {
                var url = "https://www.iesdouyin.com/share/video/" + task.AwemeId;
                using var req = new HttpRequestMessage(HttpMethod.Get, url);
                req.Headers.TryAddWithoutValidation("Referer", "https://www.douyin.com/");
                using var resp = await http.SendAsync(req);
                var html = await resp.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(html) || html.Length < 200) return false;

                html = ExpandEmbeddedJson(html);

                // 优先完整 JSON 对象
                var obj = FindAwemeObject(html, task.AwemeId);
                if (obj != null)
                {
                    ApplyAwemeJson(task, obj);
                    ParseLogger.Info($"ApplyAwemeJson ok id={task.AwemeId} title={task.Title} author={task.Author} hasUrl={!string.IsNullOrEmpty(task.DownloadUrl)}");
                    if (HasMedia(task)) return true;
                    ParseLogger.Fail($"HasMedia=false after ApplyAwemeJson id={task.AwemeId}");
                }

                // 正则兜底（仍限定 aweme_id 附近）
                return ApplyNearIdRegex(html, task) && HasMedia(task);
            }
            catch { return false; }
        }

        private async Task<bool> TryParseItemInfoAsync(HttpClient http, DownloadTask task)
        {
            try
            {
                var api = "https://www.iesdouyin.com/web/api/v2/aweme/iteminfo/?item_ids=" + task.AwemeId;
                using var req = new HttpRequestMessage(HttpMethod.Get, api);
                req.Headers.TryAddWithoutValidation("Referer", "https://www.iesdouyin.com/");
                using var resp = await http.SendAsync(req);
                var json = await resp.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(json)) return false;

                var root = JObject.Parse(json);
                var list = root["item_list"] as JArray;
                if (list == null || list.Count == 0) return false;
                var item = list[0] as JObject;
                if (item == null) return false;

                var rid = item.Value<string>("aweme_id") ?? "";
                if (!string.IsNullOrEmpty(rid) && rid != task.AwemeId) return false;

                ApplyAwemeJson(task, item);
                return HasMedia(task);
            }
            catch { return false; }
        }

        private async Task<bool> TryParseFeedAsync(HttpClient http, DownloadTask task)
        {
            try
            {
                var api = "https://aweme.snssdk.com/aweme/v1/feed/?aweme_id=" + task.AwemeId + "&aid=1128&version_code=190500&version_name=19.5.0";
                using var req = new HttpRequestMessage(HttpMethod.Get, api);
                req.Headers.TryAddWithoutValidation("Referer", "https://www.douyin.com/");
                using var resp = await http.SendAsync(req);
                var json = await resp.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(json)) return false;

                var root = JObject.Parse(json);
                var list = root["aweme_list"] as JArray;
                if (list == null || list.Count == 0) return false;
                var item = list[0] as JObject;
                if (item == null) return false;
                ApplyAwemeJson(task, item);
                return HasMedia(task);
            }
            catch { return false; }
        }

        private async Task<bool> TryParseDesktopPageAsync(HttpClient http, DownloadTask task)
        {
            try
            {
                var url = "https://www.douyin.com/video/" + task.AwemeId;
                using var resp = await http.GetAsync(url);
                var html = ExpandEmbeddedJson(await resp.Content.ReadAsStringAsync());
                ParseLogger.SaveHtml("video_page", task.AwemeId, html);
                var obj = FindAwemeObject(html, task.AwemeId);
                if (obj != null) ParseLogger.SaveJson("aweme", task.AwemeId, obj.ToString());
                if (obj != null)
                {
                    ApplyAwemeJson(task, obj);
                    ParseLogger.Info($"ApplyAwemeJson ok id={task.AwemeId} title={task.Title} author={task.Author} hasUrl={!string.IsNullOrEmpty(task.DownloadUrl)}");
                    if (HasMedia(task)) return true;
                    ParseLogger.Fail($"HasMedia=false after ApplyAwemeJson id={task.AwemeId}");
                }
                return ApplyNearIdRegex(html, task) && HasMedia(task);
            }
            catch { return false; }
        }

        private static bool HasMedia(DownloadTask task) =>
            !string.IsNullOrEmpty(task.DownloadUrl) || (task.ImageUrls?.Count ?? 0) > 0;

        private static void ApplyAwemeJson(DownloadTask task, JObject item)
        {
            var desc = item.Value<string>("desc") ?? "";
            task.Description = desc;

            // 标题优先 share_title；描述只取首行短标题
            var shareTitle = item.Value<string>("share_title")
                ?? item.Value<string>("share_title_text")
                ?? item.SelectToken("share_info.share_title")?.ToString();
            if (!string.IsNullOrWhiteSpace(shareTitle) && shareTitle.Length <= 100)
                task.Title = Truncate(shareTitle.Trim(), 60);
            else if (!string.IsNullOrWhiteSpace(desc))
            {
                var line = desc.Replace('\r', '\n');
                var idxLine = line.IndexOf('\n');
                var first = (idxLine >= 0 ? line.Substring(0, idxLine) : line).Trim();
                task.Title = Truncate(first, 40);
            }
            else if (string.IsNullOrWhiteSpace(task.Title))
                task.Title = "作品 " + (task.AwemeId ?? "");

            var author = item["author"] as JObject;
            var nick = author?.Value<string>("nickname");
            // 不要用「未知作者」覆盖合集阶段已有的真实作者
            if (!string.IsNullOrWhiteSpace(nick))
                task.Author = nick;
            else if (string.IsNullOrWhiteSpace(task.Author))
                task.Author = "未知作者";

            // 发布时间
            var ct = item.Value<long?>("create_time");
            if (ct.HasValue && ct.Value > 0)
                task.PublishTime = DateTimeOffset.FromUnixTimeSeconds(ct.Value).LocalDateTime;

            // 合集名（若 JSON 带 mix_info）
            try
            {
                var mixName = item.SelectToken("mix_info.mix_name")?.ToString()
                    ?? item.SelectToken("series_info.series_name")?.ToString();
                if (!string.IsNullOrWhiteSpace(mixName) &&
                    (string.IsNullOrEmpty(task.MixName) || task.MixName.StartsWith("合集")))
                    task.MixName = mixName.Replace("合集 · ", "").Replace("合集·", "").Trim();
            }
            catch { }

            try
            {
                var cover = FirstHttpUrl(item.SelectToken("video.cover.url_list"))
                         ?? FirstHttpUrl(item.SelectToken("video.origin_cover.url_list"))
                         ?? item.SelectToken("video.cover.url_list[0]")?.ToString()
                         ?? item.SelectToken("video.origin_cover.url_list[0]")?.ToString();
                if (!string.IsNullOrEmpty(cover)) task.CoverUrl = cover;
                string? music = FirstHttpUrl(item.SelectToken("music.play_url.url_list"))
                         ?? item.SelectToken("music.play_url.url_list[0]")?.ToString()
                         ?? FirstHttpUrl(item.SelectToken("music.download_url_list"))
                         ?? FirstHttpUrl(item.SelectToken("music.play_url_low.url_list"))
                         ?? item.SelectToken("music.play_url.uri")?.ToString();
                // 部分接口 play_url.uri 是 tos 路径，拼 CDN
                if (!string.IsNullOrEmpty(music) && !music.StartsWith("http") && music.Contains("tos-"))
                    music = "https://sf3-cdn-tos.douyinstatic.com/obj/" + music.TrimStart('/');
                if (!string.IsNullOrEmpty(music) && music.StartsWith("http"))
                    task.MusicUrl = music;
            }
            catch { }

            // 图集
            var images = item["images"] as JArray;
            if (images != null && images.Count > 0)
            {
                task.Type = ContentType.Image;
                task.ImageUrls = new List<string>();
                foreach (var img in images)
                {
                    var u = FirstHttpUrl(img?["url_list"]) ?? FirstHttpUrl((img as JObject)?["download_url_list"]);
                    if (string.IsNullOrEmpty(u) && img is JObject jo)
                        u = FirstHttpUrl(jo["url_list"]);
                    if (!string.IsNullOrEmpty(u) && !task.ImageUrls.Contains(u))
                        task.ImageUrls.Add(u);
                }
            }

            var video = item["video"] as JObject;
            string? play = null;
            string? uri = null;
            if (video != null)
            {
                play = FirstHttpUrl(video["play_addr"]?["url_list"])
                    ?? FirstHttpUrl(video["download_addr"]?["url_list"])
                    ?? FirstHttpUrl(video["play_addr_h264"]?["url_list"]);
                uri = video["play_addr"]?["uri"]?.ToString()
                    ?? video["download_addr"]?["uri"]?.ToString();

                if (string.IsNullOrEmpty(play) && video["bit_rate"] is JArray br)
                {
                    long best = -1;
                    foreach (var b in br)
                    {
                        var rate = b?.Value<long?>("bit_rate") ?? 0;
                        var u = FirstHttpUrl(b?["play_addr"]?["url_list"]);
                        if (!string.IsNullOrEmpty(u) && rate >= best)
                        {
                            best = rate;
                            play = u;
                            uri = b?["play_addr"]?["uri"]?.ToString() ?? uri;
                        }
                    }
                }
            }

            // 无水印：优先 bit_rate/play；否则用 uri 构造
            if (!string.IsNullOrEmpty(play))
            {
                play = play.Replace("/playwm/", "/play/").Replace("playwm", "play");
                task.DownloadUrl = play;
            }
            else if (!string.IsNullOrEmpty(uri))
            {
                // 开源常用构造
                task.DownloadUrl =
                    "https://aweme.snssdk.com/aweme/v1/play/?video_id=" + Uri.EscapeDataString(uri) +
                    "&ratio=1080p&line=0";
            }

            if (!string.IsNullOrEmpty(task.DownloadUrl) && (task.ImageUrls?.Count ?? 0) == 0)
                task.Type = ContentType.Video;

            task.Status = DownloadTaskStatus.Waiting;
            task.ErrorMessage = HasMedia(task) ? "" : "JSON 中无媒体地址";
        }

        private static string? FirstHttpUrl(JToken? token)
        {
            if (token is JArray arr)
            {
                foreach (var x in arr)
                {
                    var s = x?.ToString();
                    if (!string.IsNullOrEmpty(s) && s.StartsWith("http"))
                        return s;
                }
            }
            return null;
        }

        private static bool ApplyNearIdRegex(string html, DownloadTask task)
        {
            var id = task.AwemeId;
            if (string.IsNullOrEmpty(id)) return false;

            var nearDesc = Regex.Match(html,
                "\"aweme_id\"\\s*:\\s*\"?" + Regex.Escape(id) + "\"?.{0,1200}?\"desc\"\\s*:\\s*\"((?:\\\\.|[^\"])*)\"",
                RegexOptions.Singleline);
            if (nearDesc.Success)
            {
                task.Description = Unescape(nearDesc.Groups[1].Value);
                task.Title = Truncate(task.Description, 80);
            }
            else task.Title = "作品 " + id;

            var nearAuthor = Regex.Match(html,
                "\"aweme_id\"\\s*:\\s*\"?" + Regex.Escape(id) + "\"?.{0,2500}?\"nickname\"\\s*:\\s*\"((?:\\\\.|[^\"])*)\"",
                RegexOptions.Singleline);
            task.Author = nearAuthor.Success ? Unescape(nearAuthor.Groups[1].Value) : "未知作者";

            var nearPlay = Regex.Match(html,
                "\"aweme_id\"\\s*:\\s*\"?" + Regex.Escape(id) + "\"?.{0,8000}?\"play_addr\".{0,400}?\"url_list\"\\s*:\\s*\\[\\s*\"(https?://[^\"]+)\"",
                RegexOptions.Singleline);
            if (nearPlay.Success)
            {
                task.DownloadUrl = nearPlay.Groups[1].Value.Replace("\\u002F", "/")
                    .Replace("/playwm/", "/play/").Replace("playwm", "play");
                task.Type = ContentType.Video;
            }

            // uri 构造
            if (string.IsNullOrEmpty(task.DownloadUrl))
            {
                var nearUri = Regex.Match(html,
                    "\"aweme_id\"\\s*:\\s*\"?" + Regex.Escape(id) + "\"?.{0,8000}?\"play_addr\".{0,200}?\"uri\"\\s*:\\s*\"([^\"]+)\"",
                    RegexOptions.Singleline);
                if (nearUri.Success)
                {
                    task.DownloadUrl =
                        "https://aweme.snssdk.com/aweme/v1/play/?video_id=" +
                        Uri.EscapeDataString(nearUri.Groups[1].Value) + "&ratio=1080p&line=0";
                    task.Type = ContentType.Video;
                }
            }

            task.Status = DownloadTaskStatus.Waiting;
            task.ErrorMessage = HasMedia(task) ? "" : "邻近字段未找到媒体";
            return HasMedia(task);
        }

        private static string ExpandEmbeddedJson(string html)
        {
            var patterns = new[]
            {
                "id=\"RENDER_DATA\"[^>]*>([^<]+)</script>",
                "id='RENDER_DATA'[^>]*>([^<]+)</script>",
                "id=\"_ROUTER_DATA\"[^>]*>([^<]+)</script>",
                "window\\._ROUTER_DATA\\s*=\\s*(\\{.+?\\})\\s*;",
                "window\\.__INIT_PROPS__\\s*=\\s*(\\{.+?\\})\\s*</script>"
            };
            foreach (var pat in patterns)
            {
                foreach (Match m in Regex.Matches(html, pat, RegexOptions.IgnoreCase | RegexOptions.Singleline))
                {
                    try
                    {
                        var raw = m.Groups[1].Value.Trim();
                        string decoded = raw;
                        try { decoded = Uri.UnescapeDataString(raw); } catch { }
                        if (decoded.Length > 80)
                            html = decoded + "\n" + html;
                    }
                    catch { }
                }
            }
            return html;
        }

        private static JObject? FindAwemeObject(string html, string awemeId)
        {
            foreach (Match m in Regex.Matches(html, "\"aweme_id\"\\s*:\\s*\"?" + Regex.Escape(awemeId) + "\"?"))
            {
                int start = html.LastIndexOf('{', m.Index);
                if (start < 0) continue;
                int depth = 0, end = -1;
                for (int i = start; i < html.Length && i < start + 100000; i++)
                {
                    if (html[i] == '{') depth++;
                    else if (html[i] == '}')
                    {
                        depth--;
                        if (depth == 0) { end = i; break; }
                    }
                }
                if (end < 0) continue;
                try
                {
                    var obj = JObject.Parse(html.Substring(start, end - start + 1));
                    var id = obj.Value<string>("aweme_id") ?? "";
                    if (id == awemeId) return obj;
                }
                catch { }
            }
            return null;
        }

        public async Task<List<DownloadTask>> ParseAccountAsync(
    string homepageUrl,
    string tab = "post",
    int maxPages = 0,
    bool fullMode = true,
    IProgress<(int page, int total)>? progress = null)
        {
            var results = new List<DownloadTask>();
            var idSet = new HashSet<string>();
            homepageUrl = ExtractUrl(homepageUrl);
            if (string.IsNullOrEmpty(homepageUrl)) return results;

            try
            {
                using var httpMobile = CreateClient(mobile: true);
                using var httpDesk = CreateClient(mobile: false);

                if (homepageUrl.Contains("v.douyin.com"))
                    homepageUrl = await ResolveShortUrlAsync(httpMobile, homepageUrl) ?? homepageUrl;

                var secMatch = Regex.Match(homepageUrl, @"/user/([A-Za-z0-9_\-]+)");
                string secUid = secMatch.Success ? secMatch.Groups[1].Value : "";
                if (string.IsNullOrEmpty(secUid))
                    return results;

                string authorName = "";
                int limit = maxPages > 0 ? maxPages : (fullMode ? 30 : 3);

                // ========== 通道1：页面 SSR / RENDER_DATA 递归提取（不依赖 a_bogus）==========
                foreach (var useMobile in new[] { false, true })
                {
                    try
                    {
                        using var c = useMobile ? httpMobile : httpDesk;
                        var pageUrl = "https://www.douyin.com/user/" + secUid + "?from_tab_name=main&showTab=post";
                        if (useMobile)
                            pageUrl = "https://www.iesdouyin.com/share/user/" + secUid + "?sec_uid=" + Uri.EscapeDataString(secUid);

                        using var resp = await c.GetAsync(pageUrl);
                        var rawHtml = await resp.Content.ReadAsStringAsync();
                        CollectFromHtmlSsr(rawHtml, secUid, idSet, results, ref authorName);
                    }
                    catch { }
                }
                progress?.Report((1, results.Count));

                // ========== 通道2：iesdouyin web/api/v2/aweme/post （旧接口，部分环境仍可用）==========
                if (results.Count < 5)
                {
                    long maxCursor = 0;
                    for (int page = 0; page < limit; page++)
                    {
                        var api =
                            "https://www.iesdouyin.com/web/api/v2/aweme/post/?" +
                            "sec_uid=" + Uri.EscapeDataString(secUid) +
                            "&count=21&max_cursor=" + maxCursor +
                            "&aid=1128&reflow_source=reflow_page";
                        try
                        {
                            using var req = new HttpRequestMessage(HttpMethod.Get, api);
                            req.Headers.TryAddWithoutValidation("Referer", "https://www.iesdouyin.com/share/user/" + secUid);
                            req.Headers.TryAddWithoutValidation("Accept", "application/json");
                            using var resp = await httpMobile.SendAsync(req);
                            var json = await resp.Content.ReadAsStringAsync();
                            if (string.IsNullOrEmpty(json) || json.Length < 30) break;
                            JObject root;
                            try { root = JObject.Parse(json); } catch { break; }

                            var list = root["aweme_list"] as JArray;
                            if (list == null || list.Count == 0) break;

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
                                ApplyAwemeJson(task, item);
                                if ((string.IsNullOrEmpty(task.Author) || task.Author == "未知作者") && !string.IsNullOrEmpty(authorName))
                                    task.Author = authorName;
                                else if (!string.IsNullOrEmpty(task.Author) && task.Author != "未知作者")
                                    authorName = task.Author;
                                results.Add(task);
                            }
                            progress?.Report((page + 2, results.Count));
                            bool hasMore = root.Value<bool?>("has_more") == true || root.Value<int?>("has_more") == 1;
                            var next = root.Value<long?>("max_cursor") ?? 0;
                            if (!hasMore || next <= 0 || next == maxCursor) break;
                            maxCursor = next;
                            await Task.Delay(400);
                        }
                        catch { break; }
                    }
                }

                // ========== 通道3：web post 接口（强依赖 Cookie；无 a_bogus 时经常空，但仍尝试）==========
                if (results.Count < 3 && !string.IsNullOrEmpty(_cookie))
                {
                    long maxCursor = 0;
                    for (int page = 0; page < Math.Min(limit, 5); page++)
                    {
                        var api =
                            "https://www.douyin.com/aweme/v1/web/aweme/post/?" +
                            "device_platform=webapp&aid=6383&channel=channel_pc_web&" +
                            "sec_user_id=" + Uri.EscapeDataString(secUid) +
                            "&max_cursor=" + maxCursor +
                            "&locate_query=false&show_live_replay_switch=1&need_time_list=1&" +
                            "time_list_query=0&whale_cut_token=&cut_version=1&count=18&" +
                            "publish_video_strategy_type=2&from_user_page=1&update_version_code=170400";
                        try
                        {
                            using var req = new HttpRequestMessage(HttpMethod.Get, api);
                            req.Headers.TryAddWithoutValidation("Referer", "https://www.douyin.com/user/" + secUid);
                            req.Headers.TryAddWithoutValidation("Accept", "application/json, text/plain, */*");
                            using var resp = await httpDesk.SendAsync(req);
                            var json = await resp.Content.ReadAsStringAsync();
                            if (string.IsNullOrEmpty(json)) break;
                            JObject root;
                            try { root = JObject.Parse(json); } catch { break; }
                            var list = root["aweme_list"] as JArray;
                            if (list == null || list.Count == 0) break;
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
                                ApplyAwemeJson(task, item);
                                results.Add(task);
                            }
                            progress?.Report((page + 2, results.Count));
                            bool hasMore = root.Value<bool?>("has_more") == true || root.Value<int?>("has_more") == 1;
                            var next = root.Value<long?>("max_cursor") ?? 0;
                            if (!hasMore || next <= 0 || next == maxCursor) break;
                            maxCursor = next;
                            await Task.Delay(400);
                        }
                        catch { break; }
                    }
                }

                results = results
                    .OrderByDescending(x => x.PublishTime ?? DateTime.MinValue)
                    .ToList();
            }
            catch { }

            return results;
        }

        /// <summary>
        /// 从主页 HTML 的 RENDER_DATA / _ROUTER_DATA / 内嵌 JSON 递归提取作品
        /// </summary>
        private static void CollectFromHtmlSsr(
            string html,
            string secUid,
            HashSet<string> idSet,
            List<DownloadTask> results,
            ref string authorName)
        {
            if (string.IsNullOrEmpty(html)) return;

            // 1) RENDER_DATA 块（URL 编码 JSON）
            foreach (Match m in Regex.Matches(html,
                @"id=[""']?RENDER_DATA[""']?[^>]*>([^<]+)</script>",
                RegexOptions.IgnoreCase))
            {
                try
                {
                    var decoded = Uri.UnescapeDataString(m.Groups[1].Value.Trim());
                    TryWalkJsonForAwemes(decoded, secUid, idSet, results, ref authorName);
                }
                catch { }
            }

            // 2) _ROUTER_DATA / __INIT_PROPS__
            foreach (var pat in new[]
            {
                @"window\._ROUTER_DATA\s*=\s*(\{.+?\})\s*;",
                @"id=[""']?_ROUTER_DATA[""']?[^>]*>([^<]+)</script>",
                @"window\.__INIT_PROPS__\s*=\s*(\{.+?\})\s*</script>"
            })
            {
                foreach (Match m in Regex.Matches(html, pat, RegexOptions.IgnoreCase | RegexOptions.Singleline))
                {
                    try
                    {
                        var raw = m.Groups[1].Value.Trim();
                        try { raw = Uri.UnescapeDataString(raw); } catch { }
                        TryWalkJsonForAwemes(raw, secUid, idSet, results, ref authorName);
                    }
                    catch { }
                }
            }

            // 3) 整页里所有 aweme 对象兜底
            foreach (Match m in Regex.Matches(html, "\"aweme_id\"\\s*:\\s*\"?(\\d{15,})\"?"))
            {
                var id = m.Groups[1].Value;
                if (!idSet.Add(id)) continue;
                string desc = "";
                DateTime? pt = null;
                int s = Math.Max(0, m.Index - 500);
                var window = html.Substring(s, Math.Min(html.Length - s, 2800));
                var d = Regex.Match(window, "\"desc\"\\s*:\\s*\"((?:\\\\.|[^\"])*)\"");
                if (d.Success) desc = Unescape(d.Groups[1].Value);
                var ct = Regex.Match(window, "\"create_time\"\\s*:\\s*(\\d{10})");
                if (ct.Success && long.TryParse(ct.Groups[1].Value, out var unix))
                    pt = DateTimeOffset.FromUnixTimeSeconds(unix).LocalDateTime;
                results.Add(new DownloadTask
                {
                    AwemeId = id,
                    Title = string.IsNullOrWhiteSpace(desc) ? ("作品 " + id) : Truncate(desc, 80),
                    Description = desc,
                    Author = authorName,
                    Url = "https://www.douyin.com/video/" + id,
                    PublishTime = pt,
                    Status = DownloadTaskStatus.Waiting,
                    Type = ContentType.Video
                });
            }
        }

        private static void TryWalkJsonForAwemes(
            string jsonText,
            string secUid,
            HashSet<string> idSet,
            List<DownloadTask> results,
            ref string authorName)
        {
            if (string.IsNullOrEmpty(jsonText) || jsonText.Length < 20) return;
            JToken root;
            try { root = JToken.Parse(jsonText); }
            catch { return; }

            WalkToken(root, secUid, idSet, results, ref authorName);
        }

        private static void WalkToken(
            JToken token,
            string secUid,
            HashSet<string> idSet,
            List<DownloadTask> results,
            ref string authorName)
        {
            if (token == null) return;

            if (token is JObject obj)
            {
                var awemeId = obj.Value<string>("aweme_id");
                if (!string.IsNullOrEmpty(awemeId) && awemeId.Length >= 15)
                {
                    // 若有 author.sec_uid，尽量只收目标用户
                    var author = obj["author"] as JObject;
                    var aSec = author?.Value<string>("sec_uid") ?? author?.Value<string>("sec_user_id");
                    if (!string.IsNullOrEmpty(aSec) && !string.IsNullOrEmpty(secUid) && aSec != secUid)
                    {
                        // 不是目标用户的推荐内容，跳过
                    }
                    else if (idSet.Add(awemeId))
                    {
                        var task = new DownloadTask
                        {
                            AwemeId = awemeId,
                            Url = "https://www.douyin.com/video/" + awemeId,
                            Status = DownloadTaskStatus.Waiting
                        };
                        ApplyAwemeJson(task, obj);
                        if ((string.IsNullOrEmpty(task.Author) || task.Author == "未知作者") && !string.IsNullOrEmpty(authorName))
                            task.Author = authorName;
                        else if (!string.IsNullOrEmpty(task.Author) && task.Author != "未知作者")
                            authorName = task.Author;
                        results.Add(task);
                    }
                }
                else
                {
                    // 尝试用户昵称
                    if (string.IsNullOrEmpty(authorName))
                    {
                        var nick = obj.Value<string>("nickname");
                        var s = obj.Value<string>("sec_uid") ?? obj.Value<string>("secUid");
                        if (!string.IsNullOrEmpty(nick) && !string.IsNullOrEmpty(s) && s == secUid)
                            authorName = nick;
                    }
                }

                foreach (var prop in obj.Properties())
                    WalkToken(prop.Value, secUid, idSet, results, ref authorName);
            }
            else if (token is JArray arr)
            {
                foreach (var child in arr)
                    WalkToken(child, secUid, idSet, results, ref authorName);
            }
        }

        private static async Task<string?> ResolveShortUrlAsync(HttpClient http, string shortUrl)
        {
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, shortUrl);
                using var resp = await http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
                return resp.RequestMessage?.RequestUri?.AbsoluteUri;
            }
            catch { return shortUrl; }
        }

        private static string Unescape(string s)
        {
            try { return Regex.Unescape(s); }
            catch { return s.Replace("\\n", " ").Replace("\\\"", "\""); }
        }

        private static string Truncate(string s, int max)
        {
            if (string.IsNullOrEmpty(s) || s.Length <= max) return s;
            return s.Substring(0, max) + "...";
        }
    }
}
