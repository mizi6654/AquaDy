using System.IO.Compression;
using AquaDy.Models;
using AquaDy.Utils;

namespace AquaDy.Services
{
    public static class FfmpegHelper
    {
        /// <summary>
        /// BtbN GitHub latest win64 GPL 静态包（含 ffmpeg / ffprobe / ffplay）
        /// </summary>
        public const string DownloadUrl = "https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-win64-gpl.zip";

        public static string LibDir => Path.Combine(AppPaths.Root, "Lib");
        public static string DefaultExe => Path.Combine(LibDir, "ffmpeg.exe");

        public static string? ResolvePath(string? configured = null)
        {
            configured ??= AppSettings.Load().FFmpegPath;
            var list = new List<string>();
            if (!string.IsNullOrWhiteSpace(configured))
            {
                var c = configured.Trim().Trim('"');
                list.Add(c);
                if (Directory.Exists(c))
                {
                    list.Add(Path.Combine(c, "ffmpeg.exe"));
                    list.Add(Path.Combine(c, "bin", "ffmpeg.exe"));
                }
            }
            list.Add(DefaultExe);
            list.Add(Path.Combine(LibDir, "bin", "ffmpeg.exe"));
            list.Add(Path.Combine(AppPaths.Root, "ffmpeg.exe"));

            foreach (var p in list)
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(p) && File.Exists(p))
                        return Path.GetFullPath(p);
                }
                catch { }
            }
            return null;
        }

        public static bool IsAvailable(string? configured = null)
            => ResolvePath(configured) != null;

        public static async Task<string> DownloadAndInstallAsync(
            IProgress<(int percent, string status)>? progress = null,
            CancellationToken ct = default)
        {
            Directory.CreateDirectory(LibDir);
            var zipPath = Path.Combine(Path.GetTempPath(), "aquady_ffmpeg_essentials.zip");
            var extractRoot = Path.Combine(Path.GetTempPath(), "aquady_ffmpeg_extract_" + Guid.NewGuid().ToString("N"));

            try
            {
                if (Directory.Exists(extractRoot))
                {
                    try { Directory.Delete(extractRoot, true); } catch { }
                }
                Directory.CreateDirectory(extractRoot);

                progress?.Report((0, "正在连接 GitHub 受网络影响请等待 …"));

                using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(30) };
                http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent",
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AquaDy/1.0");

                using (var resp = await http.GetAsync(DownloadUrl, HttpCompletionOption.ResponseHeadersRead, ct))
                {
                    resp.EnsureSuccessStatusCode();
                    var total = resp.Content.Headers.ContentLength ?? -1L;
                    await using var net = await resp.Content.ReadAsStreamAsync(ct);
                    await using var fs = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);
                    var buf = new byte[81920];
                    long sum = 0;
                    int n;
                    while ((n = await net.ReadAsync(buf.AsMemory(0, buf.Length), ct)) > 0)
                    {
                        await fs.WriteAsync(buf.AsMemory(0, n), ct);
                        sum += n;
                        if (total > 0)
                        {
                            int pct = (int)Math.Min(90, sum * 90 / total);
                            progress?.Report((pct,
                                $"正在下载 FFmpeg… {pct}%  ({sum / 1024.0 / 1024.0:F1} / {total / 1024.0 / 1024.0:F1} MB)"));
                        }
                        else
                            progress?.Report((0, $"正在下载 FFmpeg… {sum / 1024.0 / 1024.0:F1} MB"));
                    }
                }

                progress?.Report((92, "下载完成，正在解压…"));
                ZipFile.ExtractToDirectory(zipPath, extractRoot, overwriteFiles: true);

                var exeSrc = Directory.GetFiles(extractRoot, "ffmpeg.exe", SearchOption.AllDirectories)
                    .FirstOrDefault();
                if (exeSrc == null)
                    throw new FileNotFoundException("压缩包中未找到 ffmpeg.exe");

                var binDir = Path.GetDirectoryName(exeSrc)!;
                foreach (var name in new[] { "ffmpeg.exe", "ffprobe.exe", "ffplay.exe" })
                {
                    var src = Path.Combine(binDir, name);
                    if (File.Exists(src))
                        File.Copy(src, Path.Combine(LibDir, name), overwrite: true);
                }

                if (!File.Exists(DefaultExe))
                    throw new FileNotFoundException("安装后未找到 " + DefaultExe);

                progress?.Report((100, "安装完成：" + DefaultExe));
                return Path.GetFullPath(DefaultExe);
            }
            finally
            {
                try { if (File.Exists(zipPath)) File.Delete(zipPath); } catch { }
                try { if (Directory.Exists(extractRoot)) Directory.Delete(extractRoot, true); } catch { }
            }
        }

        public static bool EnsureForRecording(IWin32Window? owner)
        {
            if (IsAvailable()) return true;
            using var dlg = new Forms.FfmpegSetupForm();
            dlg.ShowDialog(owner);
            return IsAvailable();
        }
    }
}