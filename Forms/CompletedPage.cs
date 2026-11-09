using AquaDy.Controls;
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
            if (Visible) Reload();
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
                // 路径列优先 SavePath；失败且无路径才显示错误
                if (!string.IsNullOrEmpty(t.SavePath))
                    item.SubItems.Add(t.SavePath);
                else if (t.Status == DownloadTaskStatus.Failed && !string.IsNullOrEmpty(t.ErrorMessage))
                    item.SubItems.Add(t.ErrorMessage);
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
                _activeDownloader.ResumeTasks(tasks);
                btnPause.Text = "暂停/继续";
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
                // 仅「未勾选」保留提示；成功结束不弹窗
                MessageBox.Show("请先勾选要重新下载的记录", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            btnRedownload.Enabled = false;
            try
            {
                var settings = AppSettings.Load();
                var parser = new LinkParser(settings.DouyinCookie, settings.TikTokCookie);
                var downloader = new DownloadService(settings);
                _activeDownloader = downloader;
                _batchPaused = false;

                downloader.TaskUpdated += tsk =>
                {
                    if (IsDisposed) return;
                    try
                    {
                        BeginInvoke(new Action(() =>
                        {
                            foreach (ListViewItem row in historyList.Items)
                            {
                                if (row.Tag is DownloadTask t && ReferenceEquals(t, tsk))
                                {
                                    row.SubItems[4].Text = tsk.Status == DownloadTaskStatus.Completed ? "完成"
                                        : tsk.Status == DownloadTaskStatus.Failed ? "失败"
                                        : tsk.Status == DownloadTaskStatus.Downloading ? "下载中"
                                        : tsk.Status == DownloadTaskStatus.Parsing ? "解析中"
                                        : tsk.Status.ToString();
                                    if (tsk.Status == DownloadTaskStatus.Completed && !string.IsNullOrEmpty(tsk.Size) && tsk.Size != "-")
                                        row.SubItems[5].Text = tsk.Size;
                                    if (!string.IsNullOrEmpty(tsk.SavePath))
                                        row.SubItems[7].Text = tsk.SavePath;
                                    else if (!string.IsNullOrEmpty(tsk.ErrorMessage))
                                        row.SubItems[7].Text = tsk.ErrorMessage;
                                    row.ForeColor = tsk.Status == DownloadTaskStatus.Failed
                                        ? Color.FromArgb(255, 120, 120)
                                        : AppTheme.TextPrimary;
                                    break;
                                }
                            }
                        }));
                    }
                    catch { }
                };

                foreach (var task in selected)
                {
                    task.Status = DownloadTaskStatus.Parsing;
                    task.Progress = 0;
                    task.ErrorMessage = "";
                    task.DownloadUrl = "";
                    // 立即反映「解析中」
                    foreach (ListViewItem row in historyList.Items)
                    {
                        if (row.Tag == task && row.SubItems.Count > 4)
                        {
                            row.SubItems[4].Text = "解析中";
                            break;
                        }
                    }

                    var parsed = await parser.ParseSingleAsync(task.Url);
                    task.Title = parsed.Title;
                    task.Author = parsed.Author;
                    task.Description = parsed.Description;
                    task.DownloadUrl = parsed.DownloadUrl;
                    task.ImageUrls = parsed.ImageUrls;
                    task.Type = parsed.Type;
                    task.AwemeId = parsed.AwemeId;
                    task.Status = parsed.Status;
                    task.ErrorMessage = parsed.ErrorMessage;
                }

                await downloader.EnqueueAsync(selected);
                Reload(); // 结束后静默刷新，不弹窗
            }
            catch (Exception ex)
            {
                MessageBox.Show("重新下载失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnRedownload.Enabled = true;
            }
        }

        private static string FormatSizeLocal(long bytes)
        {
            if (bytes < 1024) return bytes + " B";
            if (bytes < 1024 * 1024) return (bytes / 1024.0).ToString("0.0") + " KB";
            if (bytes < 1024L * 1024 * 1024) return (bytes / 1024.0 / 1024).ToString("0.00") + " MB";
            return (bytes / 1024.0 / 1024 / 1024).ToString("0.00") + " GB";
        }
    }
}