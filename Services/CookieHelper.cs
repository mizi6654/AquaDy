using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace AquaDy.Services
{
    public static class CookieHelper
    {
        public static Dictionary<string, string> Parse(string cookieString)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(cookieString)) return dict;
            cookieString = cookieString.Trim();
            if (cookieString.StartsWith("{")) return dict;

            foreach (var part in cookieString.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var kv = part.Trim().Split(new[] { '=' }, 2);
                if (kv.Length == 2 && !string.IsNullOrEmpty(kv[0].Trim()))
                    dict[kv[0].Trim()] = kv[1].Trim();
            }
            return dict;
        }

        public static (bool ok, string message) ValidateDouyin(string cookieString)
        {
            if (string.IsNullOrWhiteSpace(cookieString) || cookieString.Length < 30)
                return (false, "Cookie 为空或过短");
            var dict = Parse(cookieString);
            if (dict.Count == 0)
                return (false, "无法解析，格式应为 key1=value1; key2=value2; ...");
            string[] keys = { "odin_tt", "sessionid", "sessionid_ss", "passport_csrf_token", "msToken", "ttwid", "LOGIN_STATUS" };
            var found = keys.Where(k => dict.ContainsKey(k)).ToList();
            if (found.Count == 0)
                return (false, "未检测到关键字段（odin_tt / sessionid / msToken / ttwid 等）");
            bool hasLogin = dict.ContainsKey("sessionid") || dict.ContainsKey("sessionid_ss") ||
                            (dict.ContainsKey("LOGIN_STATUS") && dict["LOGIN_STATUS"] == "1");
            return (true, $"字段解析成功，共 {dict.Count} 个" + (hasLogin ? "（含登录态）" : "（建议使用已登录 Cookie）"));
        }

        public static (bool ok, string message) ValidateTikTok(string cookieString)
        {
            if (string.IsNullOrWhiteSpace(cookieString) || cookieString.Length < 30)
                return (false, "Cookie 为空或过短");
            var dict = Parse(cookieString);
            if (dict.Count == 0) return (false, "无法解析 Cookie");
            string[] keys = { "sessionid", "sessionid_ss", "tt_csrf_token", "tt_chain_token", "msToken" };
            if (!keys.Any(k => dict.ContainsKey(k)))
                return (false, "未检测到关键字段");
            return (true, $"字段解析成功，共 {dict.Count} 个");
        }

        /// <summary>
        /// 访问主页/接口提取昵称与抖音号
        /// </summary>
        public static async Task<(bool ok, string userName, string uniqueId, string message)> CheckDouyinProfileAsync(string cookieString)
        {
            if (string.IsNullOrWhiteSpace(cookieString) || cookieString.Length < 30)
                return (false, "", "", "请先填写 Cookie");

            var basic = ValidateDouyin(cookieString);
            var dict = Parse(cookieString);

            try
            {
                var handler = new HttpClientHandler
                {
                    AllowAutoRedirect = true,
                    AutomaticDecompression = System.Net.DecompressionMethods.All
                };
                using var http = new HttpClient(handler);
                http.Timeout = TimeSpan.FromSeconds(20);
                http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent",
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36");
                http.DefaultRequestHeaders.TryAddWithoutValidation("Referer", "https://www.douyin.com/");
                http.DefaultRequestHeaders.TryAddWithoutValidation("Cookie", cookieString);
                http.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "text/html,application/json,*/*");

                string userName = "";
                string uniqueId = "";

                // 方法1：页面 HTML + RENDER_DATA
                string[] pages =
                {
                    "https://www.douyin.com/user/self?from_tab_name=main&showTab=post",
                    "https://www.douyin.com/user/self",
                    "https://www.douyin.com/"
                };
                foreach (var url in pages)
                {
                    try
                    {
                        var html = await http.GetStringAsync(url);
                        ExtractUserFromHtml(html, ref userName, ref uniqueId);
                        if (!string.IsNullOrEmpty(userName) || !string.IsNullOrEmpty(uniqueId))
                            break;
                    }
                    catch { }
                }

                // 方法2：尝试 web 接口（部分环境无需 a_bogus 也能返回）
                if (string.IsNullOrEmpty(userName) && string.IsNullOrEmpty(uniqueId))
                {
                    try
                    {
                        var api = "https://www.douyin.com/aweme/v1/web/user/profile/self/?device_platform=webapp&aid=6383";
                        using var req = new HttpRequestMessage(HttpMethod.Get, api);
                        req.Headers.TryAddWithoutValidation("Referer", "https://www.douyin.com/user/self");
                        using var resp = await http.SendAsync(req);
                        var json = await resp.Content.ReadAsStringAsync();
                        if (json.Contains("nickname") || json.Contains("unique_id"))
                        {
                            try
                            {
                                var root = JObject.Parse(json);
                                var user = root["user"] as JObject ?? root["user_info"] as JObject;
                                if (user != null)
                                {
                                    userName = user.Value<string>("nickname") ?? "";
                                    uniqueId = user.Value<string>("unique_id") ?? user.Value<string>("short_id") ?? "";
                                }
                            }
                            catch
                            {
                                ExtractUserFromHtml(json, ref userName, ref uniqueId);
                            }
                        }
                    }
                    catch { }
                }

                if (!string.IsNullOrEmpty(userName) || !string.IsNullOrEmpty(uniqueId))
                {
                    var msg = $"有效 · 昵称: {(string.IsNullOrEmpty(userName) ? "-" : userName)} · 抖音号: {(string.IsNullOrEmpty(uniqueId) ? "-" : uniqueId)}";
                    return (true, userName, uniqueId, msg);
                }

                // 方法3：Cookie 本身含登录态时，降级为「字段有效」
                bool hasSession = dict.ContainsKey("sessionid") || dict.ContainsKey("sessionid_ss");
                bool loginStatus = dict.ContainsKey("LOGIN_STATUS") && dict["LOGIN_STATUS"] == "1";
                if (hasSession || loginStatus)
                {
                    return (true, "", "",
                        "Cookie 含登录态字段，但主页未返回明文昵称（强反爬）。可先试下载单条作品验证。");
                }

                if (basic.ok)
                    return (false, "", "", "字段存在但未登录态且主页无用户信息。请用「已登录」浏览器复制完整 Cookie（含 sessionid）。");

                return (false, "", "", "校验失败：请重新获取登录后的 Cookie");
            }
            catch (Exception ex)
            {
                return (false, "", "", "校验异常: " + ex.Message);
            }
        }

        private static void ExtractUserFromHtml(string html, ref string userName, ref string uniqueId)
        {
            if (string.IsNullOrEmpty(html)) return;

            // RENDER_DATA / _ROUTER_DATA 常为 URL 编码 JSON
            foreach (Match m in Regex.Matches(html, @"id=""RENDER_DATA""[^>]*>([^<]+)</script>", RegexOptions.IgnoreCase))
            {
                try
                {
                    var decoded = Uri.UnescapeDataString(m.Groups[1].Value);
                    ExtractUserFromHtml(decoded, ref userName, ref uniqueId);
                    if (!string.IsNullOrEmpty(userName)) return;
                }
                catch { }
            }

            userName = ExtractJsonString(html, "nickname") ?? ExtractJsonString(html, "nickName") ?? userName;
            uniqueId = ExtractJsonString(html, "unique_id")
                       ?? ExtractJsonString(html, "uniqueId")
                       ?? ExtractJsonString(html, "short_id")
                       ?? uniqueId;

            // 抖音号：文本
            if (string.IsNullOrEmpty(uniqueId))
            {
                var m = Regex.Match(html, @"抖音号[：:\s]*([A-Za-z0-9_\.]+)");
                if (m.Success) uniqueId = m.Groups[1].Value;
            }
        }

        private static string? ExtractJsonString(string html, string key)
        {
            var m = Regex.Match(html, $"\"{key}\"\\s*:\\s*\"((?:\\\\.|[^\"])*)\"");
            if (!m.Success) return null;
            var v = m.Groups[1].Value;
            try { v = Regex.Unescape(v); } catch { }
            if (string.IsNullOrWhiteSpace(v) || v.Length > 80) return null;
            return v;
        }
    }
}
