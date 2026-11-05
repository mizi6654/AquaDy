using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using AquaDy.Models;

using AquaDy.Utils;

namespace AquaDy.Services
{
    public class LiveRoomInfo
    {
        public string PageUrl { get; set; } = "";
        public string WebRid { get; set; } = "";
        public string RoomId { get; set; } = "";
        public string Anchor { get; set; } = "未知主播";
        public string RoomName { get; set; } = "直播间";
        public string StatusText { get; set; } = "未知";
        public bool IsLive { get; set; }
        public string StreamUrl { get; set; } = "";
        public string Avatar { get; set; } = "";
    }

    public class LiveRoomService
    {
        private readonly string _cookie;

        public LiveRoomService(string cookie)
        {
            _cookie = cookie ?? "";
        }

        public async Task<LiveRoomInfo> ParseAsync(string inputUrl)
        {
            var info = new LiveRoomInfo();
            var url = LinkParser.ExtractUrl(inputUrl);
            if (string.IsNullOrEmpty(url))
                throw new Exception("链接无效");

            var mWeb = Regex.Match(url, @"live\.douyin\.com/([0-9A-Za-z_\-]+)");
            if (mWeb.Success) info.WebRid = mWeb.Groups[1].Value;

            var mRoom = Regex.Match(url, @"[?&]room_id=(\d+)");
            if (mRoom.Success) info.RoomId = mRoom.Groups[1].Value;

            info.PageUrl = !string.IsNullOrEmpty(info.WebRid)
                ? "https://live.douyin.com/" + info.WebRid
                : url;

            using var http = CreateClient();

            string html;
            try
            {
                var resp = await http.GetAsync(info.PageUrl);
                html = await resp.Content.ReadAsStringAsync();
                if (resp.RequestMessage?.RequestUri != null)
                    info.PageUrl = resp.RequestMessage.RequestUri.ToString();
            }
            catch (Exception ex)
            {
                throw new Exception("打开直播页失败: " + ex.Message);
            }

            // 优先 RENDER_DATA（抖音直播页常见）
            ExtractRenderData(html, info);
            ExtractFromHtml(html, info);

            if (string.IsNullOrEmpty(info.RoomId))
            {
                var mRid = Regex.Match(html, "\"roomId\"\\s*:\\s*\"?(\\d{8,})\"");
                if (!mRid.Success) mRid = Regex.Match(html, "\"room_id\"\\s*:\\s*\"?(\\d{8,})\"");
                if (mRid.Success) info.RoomId = mRid.Groups[1].Value;
            }

            if (!string.IsNullOrEmpty(info.RoomId) || !string.IsNullOrEmpty(info.WebRid))
            {
                try { await TryEnterApiAsync(http, info); } catch { }
            }

            // title 兜底
            if (info.Anchor == "未知主播" || string.IsNullOrWhiteSpace(info.Anchor))
            {
                var m = Regex.Match(html, @"<title>([^<]+)</title>", RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    var t = m.Groups[1].Value
                        .Replace("的抖音直播间", "")
                        .Replace("- 抖音", "")
                        .Replace("-抖音", "")
                        .Replace("抖音", "")
                        .Trim();
                    if (!string.IsNullOrEmpty(t) && t.Length < 40 && t != "直播间")
                        info.Anchor = t;
                }
            }

            if (!string.IsNullOrEmpty(info.StreamUrl))
            {
                info.IsLive = true;
                info.StatusText = "直播中";
            }
            else if (info.IsLive)
                info.StatusText = "直播中(无直链)";
            else
                info.StatusText = "未开播";

            return info;
        }

        private void ExtractRenderData(string html, LiveRoomInfo info)
        {
            // <script id="RENDER_DATA" type="application/json">url-encoded-json</script>
            var m = Regex.Match(html, @"id=[""']RENDER_DATA[""'][^>]*>([^<]+)</script>", RegexOptions.IgnoreCase);
            if (!m.Success)
                m = Regex.Match(html, @"id=RENDER_DATA[^>]*>([^<]+)</script>", RegexOptions.IgnoreCase);
            if (!m.Success) return;

            try
            {
                var decoded = Uri.UnescapeDataString(m.Groups[1].Value.Trim());
                var root = JToken.Parse(decoded);
                WalkJson(root, info);
            }
            catch
            {
                try
                {
                    // 有时不是 url-encoded
                    var root = JToken.Parse(m.Groups[1].Value.Trim());
                    WalkJson(root, info);
                }
                catch { }
            }
        }

        private void WalkJson(JToken token, LiveRoomInfo info)
        {
            if (token is JObject o)
            {
                // nickname
                if ((info.Anchor == "未知主播" || string.IsNullOrEmpty(info.Anchor)))
                {
                    var nick = o.Value<string>("nickname") ?? o.Value<string>("anchor_name") ?? o.Value<string>("nickName");
                    if (!string.IsNullOrEmpty(nick) && nick.Length < 40)
                        info.Anchor = nick;
                }
                // title
                if (info.RoomName == "直播间" || string.IsNullOrEmpty(info.RoomName))
                {
                    var title = o.Value<string>("title") ?? o.Value<string>("room_title") ?? o.Value<string>("live_title");
                    if (!string.IsNullOrEmpty(title) && title.Length < 80)
                        info.RoomName = title;
                }
                // status
                var st = o.Value<int?>("status") ?? o.Value<int?>("room_status");
                if (st == 2) info.IsLive = true;

                // room id
                if (string.IsNullOrEmpty(info.RoomId))
                {
                    var rid = o.Value<string>("roomId") ?? o.Value<string>("room_id") ?? o.Value<string>("id_str");
                    if (!string.IsNullOrEmpty(rid) && Regex.IsMatch(rid, @"^\d{8,}$"))
                        info.RoomId = rid;
                }

                // stream maps
                if (string.IsNullOrEmpty(info.StreamUrl))
                {
                    var flv = o["flv_pull_url"] as JObject;
                    if (flv != null)
                    {
                        foreach (var key in new[] { "ORIGIN", "FULL_HD1", "HD1", "SD1", "SD2", "AO" })
                        {
                            var u = flv.Value<string>(key);
                            if (!string.IsNullOrEmpty(u) && u.StartsWith("http"))
                            {
                                info.StreamUrl = u;
                                info.IsLive = true;
                                break;
                            }
                        }
                        if (string.IsNullOrEmpty(info.StreamUrl))
                        {
                            foreach (var p in flv.Properties())
                            {
                                var u = p.Value?.ToString();
                                if (!string.IsNullOrEmpty(u) && u.StartsWith("http") && u.Contains(".flv"))
                                {
                                    info.StreamUrl = u;
                                    info.IsLive = true;
                                    break;
                                }
                            }
                        }
                    }
                }
                if (string.IsNullOrEmpty(info.StreamUrl))
                {
                    var hls = o["hls_pull_url_map"] as JObject;
                    if (hls != null)
                    {
                        foreach (var key in new[] { "FULL_HD1", "HD1", "SD1" })
                        {
                            var u = hls.Value<string>(key);
                            if (!string.IsNullOrEmpty(u) && u.StartsWith("http"))
                            {
                                info.StreamUrl = u;
                                info.IsLive = true;
                                break;
                            }
                        }
                    }
                }
                if (string.IsNullOrEmpty(info.StreamUrl))
                {
                    var hlsOne = o.Value<string>("hls_pull_url");
                    if (!string.IsNullOrEmpty(hlsOne) && hlsOne.StartsWith("http"))
                    {
                        info.StreamUrl = hlsOne;
                        info.IsLive = true;
                    }
                }

                // stream_data nested string
                var streamData = o.Value<string>("stream_data");
                if (string.IsNullOrEmpty(info.StreamUrl) && !string.IsNullOrEmpty(streamData))
                {
                    try
                    {
                        var sd = JObject.Parse(streamData);
                        var found = FindStreamInJson(sd);
                        if (!string.IsNullOrEmpty(found))
                        {
                            info.StreamUrl = found;
                            info.IsLive = true;
                        }
                    }
                    catch { }
                }

                foreach (var p in o.Properties())
                    WalkJson(p.Value, info);
            }
            else if (token is JArray arr)
            {
                foreach (var x in arr) WalkJson(x, info);
            }
        }

        private void ExtractFromHtml(string html, LiveRoomInfo info)
        {
            if (info.Anchor == "未知主播")
            {
                foreach (var pat in new[]
                {
                    "\"nickname\"\\s*:\\s*\"([^\"]+)\"",
                    "\"anchor_name\"\\s*:\\s*\"([^\"]+)\""
                })
                {
                    var m = Regex.Match(html, pat);
                    if (m.Success)
                    {
                        var v = Unescape(m.Groups[1].Value);
                        if (!string.IsNullOrEmpty(v) && v.Length < 40) { info.Anchor = v; break; }
                    }
                }
            }

            if (info.RoomName == "直播间")
            {
                foreach (var pat in new[]
                {
                    "\"title\"\\s*:\\s*\"([^\"]+)\"",
                    "\"room_title\"\\s*:\\s*\"([^\"]+)\""
                })
                {
                    var m = Regex.Match(html, pat);
                    if (m.Success)
                    {
                        var v = Unescape(m.Groups[1].Value);
                        if (!string.IsNullOrEmpty(v) && v.Length < 80) { info.RoomName = v; break; }
                    }
                }
            }

            var st = Regex.Match(html, "\"status\"\\s*:\\s*(\\d+)");
            if (st.Success && st.Groups[1].Value == "2") info.IsLive = true;

            if (string.IsNullOrEmpty(info.StreamUrl))
            {
                // 优先 or4 / origin / FULL_HD
                string best = "";
                int bestScore = -1;
                foreach (Match m in Regex.Matches(html, @"https:\\?/\\?/[^""'\s<>]+?\.(?:flv|m3u8)[^""'\s<>]*", RegexOptions.IgnoreCase))
                {
                    var u = Unescape(m.Value).Replace("\\/", "/");
                    if (!u.StartsWith("http")) continue;
                    if (u.Contains("rtmp")) continue;
                    int score = 0;
                    var low = u.ToLowerInvariant();
                    if (low.Contains("or4") || low.Contains("origin") || low.Contains("full_hd")) score = 3;
                    else if (low.Contains("hd")) score = 2;
                    else if (low.Contains("sd")) score = 1;
                    if (score > bestScore) { bestScore = score; best = u; }
                }
                if (!string.IsNullOrEmpty(best))
                {
                    info.StreamUrl = best;
                    info.IsLive = true;
                }
            }
        }

        private async Task TryEnterApiAsync(HttpClient http, LiveRoomInfo info)
        {
            var api =
                "https://live.douyin.com/webcast/room/web/enter/?aid=6383&device_platform=web&language=zh-CN" +
                "&enter_from=web_live&cookie_enabled=true&screen_width=1920&screen_height=1080" +
                "&browser_language=zh-CN&browser_platform=Win32&browser_name=Chrome&browser_version=122.0.0.0" +
                "&web_rid=" + Uri.EscapeDataString(info.WebRid ?? "") +
                "&room_id_str=" + Uri.EscapeDataString(info.RoomId ?? "") +
                "&enter_source=&is_need_double_stream=false";

            http.DefaultRequestHeaders.Remove("Referer");
            http.DefaultRequestHeaders.TryAddWithoutValidation("Referer", info.PageUrl);
            var json = await http.GetStringAsync(api);
            var root = JObject.Parse(json);
            var data = root["data"];
            if (data == null) return;

            var room = data["data"]?[0] as JObject ?? data["room"] as JObject ?? data as JObject;
            if (room == null) return;

            var owner = room["owner"] as JObject;
            if (owner != null)
            {
                var nick = owner.Value<string>("nickname");
                if (!string.IsNullOrEmpty(nick)) info.Anchor = nick;
            }
            var title = room.Value<string>("title");
            if (!string.IsNullOrEmpty(title)) info.RoomName = title;

            var status = room.Value<int?>("status") ?? 0;
            info.IsLive = status == 2;

            var streamUrl = room["stream_url"] as JObject;
            if (streamUrl != null)
            {
                var flv = streamUrl["flv_pull_url"] as JObject;
                if (flv != null)
                {
                    foreach (var p in new[] { "ORIGIN", "FULL_HD1", "HD1", "SD1", "SD2" })
                    {
                        var u = flv.Value<string>(p);
                        if (!string.IsNullOrEmpty(u)) { info.StreamUrl = u; break; }
                    }
                }
                if (string.IsNullOrEmpty(info.StreamUrl))
                {
                    var hls = streamUrl["hls_pull_url_map"] as JObject;
                    if (hls != null)
                    {
                        foreach (var p in new[] { "ORIGIN", "FULL_HD1", "HD1", "SD1" })
                        {
                            var u = hls.Value<string>(p);
                            if (!string.IsNullOrEmpty(u)) { info.StreamUrl = u; break; }
                        }
                    }
                }
                if (string.IsNullOrEmpty(info.StreamUrl))
                    info.StreamUrl = streamUrl.Value<string>("hls_pull_url") ?? "";
            }
        }

        private static string? FindStreamInJson(JToken token)
        {
            if (token is JObject o)
            {
                foreach (var p in o.Properties())
                {
                    if (p.Value.Type == JTokenType.String)
                    {
                        var s = p.Value.ToString();
                        if (s.StartsWith("http") && (s.Contains(".flv") || s.Contains("m3u8")))
                            return s;
                    }
                    var inner = FindStreamInJson(p.Value);
                    if (inner != null) return inner;
                }
            }
            else if (token is JArray arr)
            {
                foreach (var x in arr)
                {
                    var inner = FindStreamInJson(x);
                    if (inner != null) return inner;
                }
            }
            return null;
        }

        private HttpClient CreateClient()
        {
            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = true,
                AutomaticDecompression = System.Net.DecompressionMethods.All
            };
            var http = new HttpClient(handler);
            http.Timeout = TimeSpan.FromSeconds(25);
            http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36");
            http.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "text/html,application/json,*/*");
            http.DefaultRequestHeaders.TryAddWithoutValidation("Referer", "https://live.douyin.com/");
            if (!string.IsNullOrEmpty(_cookie))
                http.DefaultRequestHeaders.TryAddWithoutValidation("Cookie", _cookie);
            return http;
        }

        private static string Unescape(string s)
        {
            try { return Regex.Unescape(s.Replace("\\/", "/")); }
            catch { return s.Replace("\\/", "/"); }
        }
    }

    public static class LiveRoomStore
    {
        private static string PathFile => AppPaths.LiveRoomsFile;

        public class SavedRoom
        {
            public string Url { get; set; } = "";
            public string Anchor { get; set; } = "";
            public string RoomName { get; set; } = "";
            public int IntervalMin { get; set; } = 2;
            public string Quality { get; set; } = "原画";
            public bool AutoRecord { get; set; }
            public DateTime CreateTime { get; set; } = DateTime.Now;
        }

        public static List<SavedRoom> Load()
        {
            try
            {
                if (File.Exists(PathFile))
                {
                    var json = File.ReadAllText(PathFile);
                    return Newtonsoft.Json.JsonConvert.DeserializeObject<List<SavedRoom>>(json)
                           ?? new List<SavedRoom>();
                }
            }
            catch { }
            return new List<SavedRoom>();
        }

        public static void Save(List<SavedRoom> list)
        {
            try
            {
                var dir = Path.GetDirectoryName(PathFile);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(PathFile, Newtonsoft.Json.JsonConvert.SerializeObject(list, Newtonsoft.Json.Formatting.Indented));
            }
            catch { }
        }
    }
}
