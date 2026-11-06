using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;
using AquaDy.Forms;
using AquaDy.Utils;
using Newtonsoft.Json.Linq;

namespace AquaDy.Services
{
    /// <summary>
    /// 检查 GitHub Release → 下载 zip → 解压 → C# 覆盖 → PS/bat 重启。
    /// </summary>
    public static class UpdateChecker
    {
        private static readonly HttpClient Http = CreateHttp();

        private static HttpClient CreateHttp()
        {
            var h = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            h.DefaultRequestHeaders.UserAgent.ParseAdd("AquaDy-AutoUpdater/1.0");
            h.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/vnd.github+json");
            return h;
        }

        public static async Task CheckAsync(bool silentIfLatest, IWin32Window? owner = null)
        {
            ParseLogger.Op("检查更新", $"开始 silent={silentIfLatest} local={AppInfo.GetLocalVersion()}");
            try
            {
                string localVerStr = AppInfo.GetLocalVersion();
                var localVer = ParseVersion(localVerStr);

                string? json = null;
                Exception? lastEx = null;
                const int maxCheckRetry = 3;

                for (int i = 1; i <= maxCheckRetry; i++)
                {
                    try
                    {
                        ParseLogger.Info($"检查更新 API 请求第 {i}/{maxCheckRetry} 次 {AppInfo.GitHubApiLatest}");
                        json = await Http.GetStringAsync(AppInfo.GitHubApiLatest);
                        lastEx = null;
                        ParseLogger.Info($"检查更新 API 成功 len={json?.Length ?? 0}");
                        break;
                    }
                    catch (Exception ex)
                    {
                        lastEx = ex;
                        ParseLogger.Fail($"检查更新 API 第 {i}/{maxCheckRetry} 次失败: {ex.Message}");
                        if (i < maxCheckRetry)
                            await Task.Delay(1000 * i);
                    }
                }

                if (json == null || lastEx != null)
                {
                    ParseLogger.Fail("检查更新终止：GitHub 不可用 " + (lastEx?.Message ?? "未知"));
                    if (!silentIfLatest)
                    {
                        MessageBox.Show(owner,
                            "无法连接 GitHub 检查更新（已重试 " + maxCheckRetry + " 次）。\n\n" +
                            "错误：" + (lastEx?.Message ?? "未知") + "\n\n" +
                            "若在国内网络，可能需要代理后再试。",
                            "检查更新", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    return;
                }

                var release = JObject.Parse(json);
                string tag = release["tag_name"]?.ToString() ?? "";
                var remoteVer = ParseVersion(tag);
                ParseLogger.Info($"版本比较 本地={localVerStr} 远程tag={tag}");

                if (remoteVer <= localVer)
                {
                    ParseLogger.Info($"已是最新版本 local={localVerStr} remote={tag}");
                    if (!silentIfLatest)
                        MessageBox.Show(owner,
                            $"当前已是最新版本：{localVerStr}",
                            "检查更新", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                string changelog = release["body"]?.ToString()?.Replace("\r\n", "\n") ?? "无更新日志";
                if (changelog.Length > 400) changelog = changelog.Substring(0, 400) + "…";
                string releaseName = release["name"]?.ToString() ?? tag;

                string? zipUrl = null;
                string? zipName = null;
                if (release["assets"] is JArray assets)
                {
                    foreach (var asset in assets)
                    {
                        string name = asset["name"]?.ToString() ?? "";
                        string? url = asset["browser_download_url"]?.ToString();
                        if (string.IsNullOrEmpty(url)) continue;
                        if (!name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)) continue;
                        if (name.Contains("Source", StringComparison.OrdinalIgnoreCase)) continue;
                        zipUrl = url;
                        zipName = name;
                        if (name.Contains("AquaDy", StringComparison.OrdinalIgnoreCase))
                            break;
                    }
                }

                if (string.IsNullOrEmpty(zipUrl))
                {
                    ParseLogger.Warn($"发现新版本 {tag} 但无 zip 附件");
                    if (!silentIfLatest)
                    {
                        MessageBox.Show(owner,
                            $"发现新版本 {tag}，但 Release 中没有 zip 安装包。\n将打开 Releases 页面。",
                            "检查更新", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        try
                        {
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = AppInfo.GitHubReleases,
                                UseShellExecute = true
                            });
                        }
                        catch (Exception ex)
                        {
                            ParseLogger.Fail("打开 Releases 页失败: " + ex.Message);
                        }
                    }
                    return;
                }

                ParseLogger.Info($"发现新版本 name={releaseName} tag={tag} zip={zipName} url={zipUrl}");

                string msg =
                    $"发现新版本：{releaseName}\n\n" +
                    $"当前版本：{localVerStr}\n" +
                    $"更新来源：GitHub\n\n" +
                    $"更新内容：\n{changelog}\n\n" +
                    $"是否立即下载并自动更新？（更新后程序将自动重启）";

                if (MessageBox.Show(owner, msg, "软件更新",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                {
                    ParseLogger.Op("检查更新", "用户取消更新");
                    return;
                }

                ParseLogger.Op("检查更新", "用户确认开始自动更新");
                await PerformAutoUpdateAsync(zipUrl!, tag.TrimStart('v', 'V'), owner);
            }
            catch (Exception ex)
            {
                ParseLogger.Fail("检查更新异常: " + ex);
                if (!silentIfLatest)
                    MessageBox.Show(owner, "检查更新失败：\n" + ex.Message,
                        "检查更新", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private static async Task PerformAutoUpdateAsync(string zipUrl, string newVersion, IWin32Window? owner)
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "AquaDyUpdaterTemp");
            string zipPath = Path.Combine(tempDir, "update.zip");
            string extractPath = Path.Combine(tempDir, "extract");
            UpdateProgressForm? progressForm = null;

            string appDir = Path.GetDirectoryName(Environment.ProcessPath)
                         ?? AppDomain.CurrentDomain.BaseDirectory;
            appDir = Path.GetFullPath(appDir).TrimEnd('\\', '/');
            string exeName = Path.GetFileName(Environment.ProcessPath)
                          ?? Path.GetFileName(Application.ExecutablePath)
                          ?? "AquaDy.exe";
            string ver = (newVersion ?? "").Trim().TrimStart('v', 'V');
            if (string.IsNullOrEmpty(ver)) ver = "0.0.0";

            ParseLogger.Op("自动更新", $"开始 ver={ver} appDir={appDir} exe={exeName}");
            ParseLogger.Info($"zipUrl={zipUrl}");
            ParseLogger.Info($"tempDir={tempDir}");

            try
            {
                progressForm = new UpdateProgressForm("GitHub");
                if (owner is Form f) progressForm.Owner = f;
                progressForm.Show(owner);
                await Task.Delay(200);

                await Task.Run(async () =>
                {
                    if (Directory.Exists(tempDir))
                    {
                        try { Directory.Delete(tempDir, true); } catch { }
                    }
                    Directory.CreateDirectory(tempDir);
                    Directory.CreateDirectory(extractPath);

                    File.WriteAllText(
                        Path.Combine(tempDir, "target_appdir.txt"),
                        appDir + Environment.NewLine + exeName,
                        Encoding.UTF8);

                    // ----- 下载 -----
                    const int maxRetry = 3;
                    Exception? last = null;
                    for (int retry = 0; retry < maxRetry; retry++)
                    {
                        try
                        {
                            ParseLogger.Info($"下载更新包 第 {retry + 1}/{maxRetry} 次");
                            string freshUrl = zipUrl + (zipUrl.Contains("?") ? "&" : "?") +
                                              "t=" + DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                            using var req = new HttpRequestMessage(HttpMethod.Get, freshUrl);
                            using var resp = await Http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
                            resp.EnsureSuccessStatusCode();
                            var total = resp.Content.Headers.ContentLength ?? -1L;
                            ParseLogger.Info($"下载响应 OK contentLength={total}");

                            await using var net = await resp.Content.ReadAsStreamAsync();
                            await using var fs = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None);
                            var buf = new byte[81920];
                            long sum = 0;
                            int n;
                            while ((n = await net.ReadAsync(buf)) > 0)
                            {
                                await fs.WriteAsync(buf.AsMemory(0, n));
                                sum += n;
                                if (total > 0)
                                {
                                    int pct = (int)Math.Min(99, sum * 100 / total);
                                    progressForm.UpdateStatus(
                                        $"正在从 GitHub 下载中... {pct}%  ({sum / 1024.0 / 1024.0:F1} MB)\r\n请勿关闭窗口",
                                        pct);
                                }
                            }
                            ParseLogger.Info($"下载完成 bytes={sum} path={zipPath}");
                            last = null;
                            break;
                        }
                        catch (Exception ex)
                        {
                            last = ex;
                            ParseLogger.Fail($"下载失败 第 {retry + 1}/{maxRetry}: {ex.Message}");
                            progressForm.UpdateStatus($"下载失败，重试 {retry + 1}/{maxRetry}\r\n{ex.Message}");
                            await Task.Delay(2000 * (retry + 1));
                        }
                    }
                    if (last != null) throw last;

                    // ----- 解压 -----
                    progressForm.UpdateStatus("下载完成，正在解压到临时目录…");
                    ParseLogger.Info("开始解压 " + zipPath);
                    ExtractZipCompatible(zipPath, extractPath);

                    string extractRoot = extractPath;
                    var subs = Directory.GetDirectories(extractPath);
                    var topFiles = Directory.GetFiles(extractPath);
                    if (topFiles.Length == 0 && subs.Length == 1)
                        extractRoot = subs[0];
                    ParseLogger.Info($"解压完成 extractRoot={extractRoot} dirs={subs.Length} files={topFiles.Length}");

                    // ----- C# 覆盖 -----
                    progressForm.UpdateStatus("正在复制到程序目录：\r\n" + appDir);
                    ParseLogger.Info($"开始复制 {extractRoot} -> {appDir}");
                    CopyDirectoryUnicode(extractRoot, appDir);
                    ParseLogger.Info("复制阶段结束（占用中的 exe 可能未覆盖）");

                    try
                    {
                        File.WriteAllText(
                            Path.Combine(appDir, "version.txt"),
                            ver,
                            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                        ParseLogger.Info("已写入 version.txt = " + ver);
                    }
                    catch (Exception ex)
                    {
                        ParseLogger.Warn("写入 version.txt 失败（将由重启脚本再写）: " + ex.Message);
                    }

                    // ----- 重启：PS 优先，bat 兜底 -----
                    progressForm.UpdateStatus("准备重启…");
                    string ps1Path = Path.Combine(tempDir, "update.ps1");
                    string Q(string p) => "'" + (p ?? "").Replace("'", "''") + "'";
                    string ps =
                        "$ErrorActionPreference = 'Continue'\r\n" +
                        "Start-Sleep -Seconds 1.5\r\n" +
                        "$appDir = " + Q(appDir) + "\r\n" +
                        "$src = " + Q(extractRoot) + "\r\n" +
                        "$exe = " + Q(Path.Combine(appDir, exeName)) + "\r\n" +
                        "$ver = " + Q(ver) + "\r\n" +
                        "$tmp = " + Q(tempDir) + "\r\n" +
                        "if (Test-Path $src) {\r\n" +
                        "  Copy-Item -Path (Join-Path $src '*') -Destination $appDir -Recurse -Force -ErrorAction SilentlyContinue\r\n" +
                        "}\r\n" +
                        "Set-Content -Path (Join-Path $appDir 'version.txt') -Value $ver -Encoding UTF8\r\n" +
                        "if (Test-Path $exe) { Start-Process -FilePath $exe -WorkingDirectory $appDir }\r\n" +
                        "Start-Sleep -Seconds 1\r\n" +
                        "Remove-Item -Path $tmp -Recurse -Force -ErrorAction SilentlyContinue\r\n";
                    File.WriteAllText(ps1Path, ps, new UTF8Encoding(true));
                    ParseLogger.Info("已生成 update.ps1");

                    bool restarted = false;
                    try
                    {
                        var psi = new ProcessStartInfo
                        {
                            FileName = "powershell.exe",
                            Arguments = "-NoProfile -ExecutionPolicy Bypass -File \"" + ps1Path + "\"",
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            WorkingDirectory = tempDir
                        };
                        var p = Process.Start(psi);
                        restarted = p != null;
                        ParseLogger.Info(restarted
                            ? "已启动 PowerShell 重启脚本 pid=" + p!.Id
                            : "PowerShell Process.Start 返回 null");
                    }
                    catch (Exception ex)
                    {
                        restarted = false;
                        ParseLogger.Fail("启动 PowerShell 失败: " + ex.Message);
                    }

                    if (!restarted)
                    {
                        ParseLogger.Warn("改用短路径 bat 兜底重启");
                        string appS = ToShortPath(appDir);
                        string srcS = ToShortPath(extractRoot);
                        string tmpS = ToShortPath(tempDir);
                        string batPath = Path.Combine(tempDir, "update.bat");
                        string bat =
                            "@echo off\r\n" +
                            "timeout /t 2 /nobreak >nul\r\n" +
                            "set \"APPDIR=" + appS + "\"\r\n" +
                            "set \"SRC=" + srcS + "\"\r\n" +
                            "set \"EXE=" + exeName + "\"\r\n" +
                            "xcopy \"%SRC%\\*.*\" \"%APPDIR%\\\" /s /e /y /q /i /h >nul\r\n" +
                            "echo " + ver + ">%APPDIR%\\version.txt\r\n" +
                            "start \"\" /d \"%APPDIR%\" \"%APPDIR%\\%EXE%\"\r\n" +
                            "rd /s /q \"" + tmpS + "\" 2>nul\r\n" +
                            "exit\r\n";
                        File.WriteAllText(batPath, bat, Encoding.GetEncoding(0));
                        ParseLogger.Info($"已生成 update.bat APPDIR={appS}");
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = batPath,
                            UseShellExecute = true,
                            CreateNoWindow = true,
                            WindowStyle = ProcessWindowStyle.Hidden
                        });
                        ParseLogger.Info("已启动 bat 重启脚本");
                    }

                    try { progressForm.Invoke(() => progressForm.Complete("更新完成，正在重启…")); }
                    catch { }

                    ParseLogger.Op("自动更新", "即将 Environment.Exit(0)");
                    await Task.Delay(400);
                    Environment.Exit(0);
                });
            }
            catch (Exception ex)
            {
                ParseLogger.Fail("自动更新失败: " + ex);
                progressForm?.Complete("更新失败: " + ex.Message);
                MessageBox.Show(owner,
                    "自动更新失败：\n" + ex.Message +
                    "\n\n目标目录：\n" + appDir,
                    "更新失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void CopyDirectoryUnicode(string sourceDir, string destDir)
        {
            sourceDir = Path.GetFullPath(sourceDir);
            destDir = Path.GetFullPath(destDir);
            Directory.CreateDirectory(destDir);
            int ok = 0, skip = 0;

            foreach (string dir in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
            {
                string rel = Path.GetRelativePath(sourceDir, dir);
                Directory.CreateDirectory(Path.Combine(destDir, rel));
            }

            foreach (string file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
            {
                string rel = Path.GetRelativePath(sourceDir, file);
                string dest = Path.Combine(destDir, rel);
                string? parent = Path.GetDirectoryName(dest);
                if (!string.IsNullOrEmpty(parent))
                    Directory.CreateDirectory(parent);
                try
                {
                    File.Copy(file, dest, true);
                    ok++;
                }
                catch
                {
                    skip++;
                }
            }

            ParseLogger.Info($"CopyDirectoryUnicode 完成 ok={ok} skipLocked={skip}");
        }

        private static void ExtractZipCompatible(string zipPath, string extractPath)
        {
            Exception? last = null;
            try
            {
                ExtractWithEncoding(zipPath, extractPath, Encoding.GetEncoding(0));
                ParseLogger.Info("解压使用系统默认编码成功");
                return;
            }
            catch (Exception ex) { last = ex; ParseLogger.Warn("系统编码解压失败: " + ex.Message); }

            try
            {
                if (Directory.Exists(extractPath))
                {
                    try { Directory.Delete(extractPath, true); } catch { }
                }
                Directory.CreateDirectory(extractPath);
                ExtractWithEncoding(zipPath, extractPath, Encoding.GetEncoding(936));
                ParseLogger.Info("解压使用 GBK(936) 成功");
                return;
            }
            catch (Exception ex) { last = ex; ParseLogger.Warn("GBK 解压失败: " + ex.Message); }

            try
            {
                if (Directory.Exists(extractPath))
                {
                    try { Directory.Delete(extractPath, true); } catch { }
                }
                Directory.CreateDirectory(extractPath);
                ExtractWithEncoding(zipPath, extractPath, Encoding.UTF8);
                ParseLogger.Info("解压使用 UTF-8 成功");
                return;
            }
            catch (Exception ex) { last = ex; ParseLogger.Fail("UTF-8 解压失败: " + ex.Message); }

            throw new Exception("解压失败（编码不兼容）。请用 7-Zip 打包时启用 UTF-8，或包内仅用英文路径。\n" + last?.Message);
        }

        private static void ExtractWithEncoding(string zipPath, string extractPath, Encoding entryEncoding)
        {
            using var fs = File.OpenRead(zipPath);
            using var archive = new ZipArchive(fs, ZipArchiveMode.Read, leaveOpen: false, entryNameEncoding: entryEncoding);
            int count = 0;
            foreach (var entry in archive.Entries)
            {
                if (string.IsNullOrEmpty(entry.Name)) continue;
                string relative = entry.FullName.Replace('/', Path.DirectorySeparatorChar);
                if (Path.IsPathRooted(relative)) continue;
                string destPath = Path.GetFullPath(Path.Combine(extractPath, relative));
                string rootFull = Path.GetFullPath(extractPath).TrimEnd('\\', '/') + Path.DirectorySeparatorChar;
                if (!destPath.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(Path.GetDirectoryName(destPath), Path.GetFullPath(extractPath), StringComparison.OrdinalIgnoreCase))
                    continue;
                string? destDir = Path.GetDirectoryName(destPath);
                if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                    Directory.CreateDirectory(destDir);
                if (File.Exists(destPath)) File.Delete(destPath);
                entry.ExtractToFile(destPath);
                count++;
            }
            ParseLogger.Info($"ExtractWithEncoding 写出文件数={count} encoding={entryEncoding.WebName}");
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern uint GetShortPathName(
            string lpszLongPath,
            StringBuilder lpszShortPath,
            int cchBuffer);

        private static string ToShortPath(string longPath)
        {
            if (string.IsNullOrWhiteSpace(longPath)) return longPath;
            try
            {
                longPath = Path.GetFullPath(longPath.TrimEnd('\\', '/'));
                if (!Directory.Exists(longPath) && !File.Exists(longPath))
                    return longPath;
                var sb = new StringBuilder(512);
                uint n = GetShortPathName(longPath, sb, sb.Capacity);
                if (n == 0)
                {
                    sb = new StringBuilder(1024);
                    n = GetShortPathName(longPath, sb, sb.Capacity);
                }
                if (n > 0)
                {
                    string s = sb.ToString();
                    if (!string.IsNullOrEmpty(s))
                        return s.TrimEnd('\\', '/');
                }
            }
            catch (Exception ex)
            {
                ParseLogger.Warn("ToShortPath 失败: " + ex.Message);
            }
            return longPath.TrimEnd('\\', '/');
        }

        private static Version ParseVersion(string ver)
        {
            ver = (ver ?? "").Trim().TrimStart('v', 'V');
            return Version.TryParse(ver, out var v) ? v : new Version(0, 0);
        }
    }
}