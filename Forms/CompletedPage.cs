using AquaDy.Models;
using AquaDy.Services;

namespace AquaDy.Forms
{
    public partial class CompletedPage : UserControl
    {
        private DownloadService? _activeDownloader;
        private bool _batchPaused;

        public CompletedPage()
        {
            InitializeComponent();
        }

        private void CompletedPage_Load(object? sender, EventArgs e)
        {
            Reload();
        }

        private void CompletedPage_VisibleChanged(object? sender, EventArgs e)
        {
            // 正在重新下载时不要整表 Reload，否则会闪
            if (Visible && _activeDownloader == null)
                Reload();
        }

        private void btnSelectAll_Click(object? sender, EventArgs e)
        {
            foreach (ListViewItem i in historyList.Items) i.Checked = true;
        }

        private void btnDeselect_Click(object? sender, EventArgs e)
        {
            foreach (ListViewItem i in historyList.Items) i.Checked = false;
        }

        private void btnRefresh_Click(object? sender, EventArgs e)
        {
            Reload();
        }

        private void btnOpenFolder_Click(object? sender, EventArgs e)
        {
            var path = AppSettings.Load().DownloadPath;
            if (!System.IO.Directory.Exists(path)) System.IO.Directory.CreateDirectory(path);
            try { System.Diagnostics.Process.Start("explorer.exe", path); } catch { }
        }

        private void btnClear_Click(object? sender, EventArgs e)
        {
            if (MessageBox.Show("确定清空全部历史？", "确认", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                HistoryStore.Clear();
                Reload();
            }
        }

        private void btnCancelDone_Click(object? sender, EventArgs e)
        {
            var items = historyList.CheckedItems.Count > 0
                ? historyList.CheckedItems.Cast<ListViewItem>().ToList()
                : historyList.SelectedItems.Cast<ListViewItem>().ToList();
            foreach (var it in items)
            {
                if (it.Tag is DownloadTask t)
                {
                    t.Status = DownloadTaskStatus.Cancelled;
                    // 保留你原来的后续逻辑（更新行 / CancelTasks 等）
                    if (it.SubItems.Count > 4)
                        it.SubItems[4].Text = "已取消";
                }
            }
            _activeDownloader?.CancelTasks(
                items.Select(i => i.Tag as DownloadTask).Where(t => t != null)!);
            btnPause.Text = "继续下载";
            _batchPaused = true;
        }

        private void cmsRedownload_Click(object? sender, EventArgs e)
        {
            foreach (ListViewItem it in historyList.SelectedItems) it.Checked = true;
            btnRedownload_Click(sender, e);
        }

        private void cmsDelete_Click(object? sender, EventArgs e)
        {
            var del = historyList.SelectedItems.Cast<ListViewItem>().ToList();
            if (del.Count == 0) del = historyList.CheckedItems.Cast<ListViewItem>().ToList();
            foreach (var it in del) historyList.Items.Remove(it);
            int n = 1;
            foreach (ListViewItem it in historyList.Items) { it.Text = n.ToString(); n++; }
        }

        private void cmsSelectAll_Click(object? sender, EventArgs e)
        {
            foreach (ListViewItem it in historyList.Items) it.Checked = true;
        }

        private void cmsDeselectAll_Click(object? sender, EventArgs e)
        {
            foreach (ListViewItem it in historyList.Items) it.Checked = false;
        }

        private void historyList_MouseDoubleClick(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            if (historyList.SelectedItems.Count == 0) return;
            if (historyList.SelectedItems[0].Tag is DownloadTask task && !string.IsNullOrEmpty(task.Url))
            {
                try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(task.Url) { UseShellExecute = true }); }
                catch { }
            }
        }

        public void Reload()
        {
            historyList.Items.Clear();
            var list = HistoryStore.Load();
            int i = 1;
            foreach (var t in list)
            {
                string Clip(string? s, int n)
                {
                    if (string.IsNullOrEmpty(s) || s == "-") return "-";
                    return s.Length <= n ? s : s.Substring(0, n) + "...";
                }
                var item = new ListViewItem(i.ToString());
                item.SubItems.Add(string.IsNullOrEmpty(t.Url) ? "-" : t.Url);
                item.SubItems.Add(Clip(t.Title, 40));
                item.SubItems.Add(Clip(t.Author, 20));
                item.SubItems.Add(t.Status == DownloadTaskStatus.Completed ? "完成" :
                    t.Status == DownloadTaskStatus.Failed ? "失败" : t.Status.ToString());
                item.SubItems.Add(t.Size ?? "-");
                item.SubItems.Add(t.CreateTime.ToString("MM-dd HH:mm"));
                // 失败优先显示错误；成功/进行中才显示路径
                if (t.Status == DownloadTaskStatus.Failed && !string.IsNullOrEmpty(t.ErrorMessage))
                    item.SubItems.Add(t.ErrorMessage);
                else if (!string.IsNullOrEmpty(t.SavePath))
                    item.SubItems.Add(t.SavePath);
                else
                    item.SubItems.Add("-");
                item.Tag = t;
                item.ForeColor = t.Status == DownloadTaskStatus.Failed ? AppTheme.Error : AppTheme.TextPrimary;
                historyList.Items.Add(item);
                i++;
            }
        }

        private void btnPause_Click(object? sender, EventArgs e)
        {
            var tasks = historyList.CheckedItems.Cast<ListViewItem>()
                .Select(i => i.Tag as DownloadTask).Where(t => t != null).Cast<DownloadTask>().ToList();
            if (tasks.Count == 0)
            {
                MessageBox.Show("请先勾选记录", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (_activeDownloader == null)
            {
                MessageBox.Show("当前没有进行中的重新下载", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _batchPaused = !_batchPaused;
            if (_batchPaused)
            {
                _activeDownloader.PauseTasks(tasks);
                btnPause.Text = "继续下载";
            }
            else
            {
                btnPause.Text = "暂停/继续";
                // 必须重新入队，不能只 ResumeTasks
                _ = _activeDownloader.ResumeAndEnqueueAsync(tasks);
            }
        }

        private async void btnRedownload_Click(object? sender, EventArgs e)
        {
            var selected = historyList.CheckedItems.Cast<ListViewItem>()
                .Select(i => i.Tag as DownloadTask)
                .Where(t => t != null && !string.IsNullOrEmpty(t.Url))
                .Cast<DownloadTask>()
                .ToList();

            if (selected.Count == 0)
            {
                MessageBox.Show("请先勾选要重新下载的记录", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            btnRedownload.Enabled = false;
            ParseLogger.Op("已完成-重新下载", $"count={selected.Count}");

            try
            {
                var settings = AppSettings.Load();
                var parser = new LinkParser(settings.DouyinCookie, settings.TikTokCookie);
                var downloader = new DownloadService(settings);
                _activeDownloader = downloader;
                _batchPaused = false;

                // 只更新单行，绝不调用 Reload()
                downloader.TaskUpdated += tsk =>
                {
                    if (IsDisposed) return;
                    try
                    {
                        void Apply()
                        {
                            foreach (ListViewItem row in historyList.Items)
                            {
                                if (row.Tag is not DownloadTask t) continue;
                                if (t.Id != tsk.Id && t.Url != tsk.Url && t.AwemeId != tsk.AwemeId) continue;

                                // 状态列（第5列，索引4）
                                if (row.SubItems.Count > 4)
                                {
                                    row.SubItems[4].Text = tsk.Status switch
                                    {
                                        DownloadTaskStatus.Completed => "完成",
                                        DownloadTaskStatus.Failed => "失败",
                                        DownloadTaskStatus.Downloading => "下载中",
                                        DownloadTaskStatus.Parsing => "解析中",
                                        DownloadTaskStatus.Paused => "已暂停",
                                        DownloadTaskStatus.Cancelled => "已取消",
                                        _ => tsk.Status.ToString()
                                    };
                                }

                                // 大小列（索引5）
                                //if (row.SubItems.Count > 5 && !string.IsNullOrEmpty(tsk.Size) && tsk.Size != "-")
                                //    row.SubItems[5].Text = tsk.Size;
                                if (row.SubItems.Count > 5)
                                {
                                    if (tsk.Status == DownloadTaskStatus.Failed)
                                        row.SubItems[5].Text = "-";
                                    else if (!string.IsNullOrEmpty(tsk.Size) && tsk.Size != "-")
                                        row.SubItems[5].Text = tsk.Size;
                                }

                                // 路径/错误列（索引7）—— 失败时显示错误原因，下载中显示速度
                                if (row.SubItems.Count > 7)
                                {
                                    if (tsk.Status == DownloadTaskStatus.Failed && !string.IsNullOrEmpty(tsk.ErrorMessage))
                                        row.SubItems[7].Text = tsk.ErrorMessage;
                                    else if (tsk.Status == DownloadTaskStatus.Downloading && !string.IsNullOrEmpty(tsk.Speed) && tsk.Speed != "-")
                                        row.SubItems[7].Text = $"{tsk.Progress:F0}% {tsk.Speed}";
                                    else if (tsk.Status == DownloadTaskStatus.Completed && !string.IsNullOrEmpty(tsk.SavePath))
                                        row.SubItems[7].Text = tsk.SavePath;
                                    else if (!string.IsNullOrEmpty(tsk.SavePath) && tsk.Status != DownloadTaskStatus.Failed)
                                        row.SubItems[7].Text = tsk.SavePath;
                                    else
                                        row.SubItems[7].Text = "-";
                                }

                                row.ForeColor = tsk.Status == DownloadTaskStatus.Failed
                                    ? Color.FromArgb(255, 120, 120)
                                    : AppTheme.TextPrimary;
                                break;
                            }
                        }

                        if (historyList.InvokeRequired)
                            historyList.BeginInvoke(Apply);
                        else
                            Apply();
                    }
                    catch { }
                };

                // 每个任务独立处理，一个失败不影响其他
                var sem = new SemaphoreSlim(Math.Max(1, Math.Min(settings.MaxConcurrent, 3)));
                var jobs = selected.Select(async task =>
                {
                    await sem.WaitAsync();
                    try
                    {
                        task.Status = DownloadTaskStatus.Parsing;
                        task.Progress = 0;
                        task.ErrorMessage = "";
                        task.DownloadUrl = "";
                        task.ImageUrls = null;
                        task.Speed = "-";
                        task.Size = "-";           // 清掉旧的 4.0MB/5.1MB
                        
                        // 重新下载：丢掉可能无效的 .part，避免 Range 400
                        try
                        {
                            if (!string.IsNullOrEmpty(task.SavePath))
                            {
                                var part = task.SavePath + ".part";
                                if (File.Exists(part)) File.Delete(part);
                            }
                        }
                        catch { }

                        // 立即更新这一行状态
                        UpdateHistoryRow(task, "解析中", "-", "解析中…");

                        ParseLogger.Op("已完成-二次解析", $"id={task.AwemeId} url={task.Url}");

                        var parsed = await parser.ParseSingleAsync(task.Url);
                        task.Title = string.IsNullOrEmpty(parsed.Title) ? task.Title : parsed.Title;
                        task.Author = string.IsNullOrEmpty(parsed.Author) ? task.Author : parsed.Author;
                        task.Description = string.IsNullOrEmpty(parsed.Description) ? task.Description : parsed.Description;
                        task.DownloadUrl = parsed.DownloadUrl;
                        task.ImageUrls = parsed.ImageUrls;
                        task.Type = parsed.Type;
                        if (!string.IsNullOrEmpty(parsed.AwemeId)) task.AwemeId = parsed.AwemeId;
                        task.Status = parsed.Status;
                        task.ErrorMessage = parsed.ErrorMessage;

                        if (string.IsNullOrEmpty(task.DownloadUrl) && (task.ImageUrls == null || task.ImageUrls.Count == 0))
                        {
                            task.Status = DownloadTaskStatus.Failed;
                            if (string.IsNullOrEmpty(task.ErrorMessage))
                                task.ErrorMessage = "解析未得到下载地址";
                            ParseLogger.Fail($"已完成-解析无地址 id={task.AwemeId}");
                            UpdateHistoryRow(task, "失败", null, task.ErrorMessage);
                            HistoryStore.Add(CloneForHistoryLocal(task));
                            return;
                        }

                        UpdateHistoryRow(task, "下载中", "0%", null);
                        await downloader.EnqueueAsync(new[] { task });
                    }
                    catch (Exception ex)
                    {
                        task.Status = DownloadTaskStatus.Failed;
                        task.ErrorMessage = ex.Message;
                        ParseLogger.Fail($"已完成-重新下载异常 id={task.AwemeId}: {ex.Message}");
                        UpdateHistoryRow(task, "失败", null, ex.Message);
                        HistoryStore.Add(CloneForHistoryLocal(task));
                    }
                    finally
                    {
                        sem.Release();
                    }
                });

                await Task.WhenAll(jobs);
                ParseLogger.Op("已完成-重新下载结束", $"count={selected.Count}");
            }
            catch (Exception ex)
            {
                ParseLogger.Fail("已完成-重新下载总异常: " + ex.Message);
                MessageBox.Show("重新下载失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnRedownload.Enabled = true;
                _batchPaused = false;
                btnPause.Text = "暂停/继续";
                // 延迟清空，避免 VisibleChanged 立刻整表 Reload
                _ = Task.Run(async () =>
                {
                    await Task.Delay(500);
                    try
                    {
                        if (!IsDisposed)
                            BeginInvoke(new Action(() => { _activeDownloader = null; }));
                    }
                    catch { }
                });
            }
        }

        // 辅助方法：只更新单行，不刷新整表
        private void UpdateHistoryRow(DownloadTask task, string statusText, string? sizeText, string? pathOrError)
        {
            void Apply()
            {
                foreach (ListViewItem row in historyList.Items)
                {
                    if (row.Tag is not DownloadTask t) continue;
                    if (t.Id != task.Id && t.Url != task.Url) continue;

                    if (row.SubItems.Count > 4) row.SubItems[4].Text = statusText;
                    if (sizeText != null && row.SubItems.Count > 5) row.SubItems[5].Text = sizeText;
                    if (pathOrError != null && row.SubItems.Count > 7) row.SubItems[7].Text = pathOrError;
                    row.ForeColor = statusText == "失败" ? Color.FromArgb(255, 120, 120) : AppTheme.TextPrimary;
                    break;
                }
            }
            if (historyList.InvokeRequired) historyList.BeginInvoke(Apply);
            else Apply();
        }

        private static DownloadTask CloneForHistoryLocal(DownloadTask t) => new()
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
    }
}