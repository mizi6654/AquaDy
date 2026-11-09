using System.Drawing.Drawing2D;
using AquaDy.Controls;
using AquaDy.Models;
using AquaDy.Services;

namespace AquaDy.Forms
{
    public partial class DownloadPage : UserControl
    {
        private DownloadService? _activeDownloader;
        private bool _batchPaused;
        private CancellationTokenSource? _batchCts;
        private List<DownloadTask> tasks = new();
        private readonly object _parseLock = new object();
        private bool _sortAsc = false;

        public DownloadPage()
        {
            InitializeComponent();
        }

        private void DownloadPage_Load(object? sender, EventArgs e)
        {
            // cardNew 的自绘（原 CreateCard 返回 Panel 时挂的 Paint）
            cardNew.Paint += cardNew_Paint;
            // cardNew 布局事件（原代码 cardNew.Layout += ...）
            cardNew.Layout += cardNew_Layout;
            // txtLinks 额外宽度重算（原代码有两次 Resize += ...，第一次只改 txtLinks 宽度）
            Relayout();
        }

        private void DownloadPage_Resize(object? sender, EventArgs e)
        {
            // 原代码第二个 Resize：Relayout()
            Relayout();
        }

        private void cardNew_Layout(object? sender, LayoutEventArgs e)
        {
            if (btnAdd != null && chkCover != null)
                btnAdd.Location = new Point(chkCover.Right + 16, chkCover.Top - 2);
            if (txtLinks != null && cardNew != null)
                txtLinks.Width = Math.Max(200, cardNew.ClientSize.Width - 32);
        }

        private void cardNew_Paint(object? sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var path = RoundedRect(cardNew.ClientRectangle, 10);
            using var brush = new SolidBrush(AppTheme.CardBackground);
            e.Graphics.FillPath(brush, path);
            using var pen = new Pen(AppTheme.Border, 1);
            e.Graphics.DrawPath(pen, path);
        }

        private GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d - 1, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d - 1, bounds.Bottom - d - 1, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d - 1, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void Relayout()
        {
            const int detailW = 240;
            int w = this.ClientSize.Width;
            int h = this.ClientSize.Height;
            int right = detailW + 20;

            detailPanel.Location = new Point(w - detailW - 12, 80);
            detailPanel.Size = new Size(detailW, Math.Max(360, h - 100));

            int contentW = Math.Max(500, w - right - 36);
            cardNew.Size = new Size(contentW, 200);
            if (txtLinks != null)
                txtLinks.Width = Math.Max(200, cardNew.ClientSize.Width - 32);
            if (btnAdd != null && chkCover != null)
                btnAdd.Location = new Point(chkCover.Right + 16, chkCover.Top - 2);
            if (txtLinks != null) txtLinks.Width = Math.Max(300, contentW - 150);
            if (btnAdd != null) btnAdd.Left = contentW - 130;

            toolPanel.Width = contentW;
            taskList.Size = new Size(contentW, Math.Max(150, h - 390));
        }

        private void btnSelectAll_Click(object? sender, EventArgs e)
        {
            foreach (ListViewItem item in taskList.Items)
                item.Checked = true;
        }

        private void btnDeselect_Click(object? sender, EventArgs e)
        {
            foreach (ListViewItem item in taskList.Items)
                item.Checked = false;
        }

        private void btnClear_Click(object? sender, EventArgs e)
        {
            tasks.Clear();
            RefreshTaskList();
        }

        private void btnOpenLink_Click(object? sender, EventArgs e)
        {
            if (taskList.SelectedItems.Count == 0) return;
            if (taskList.SelectedItems[0].Tag is DownloadTask task && !string.IsNullOrEmpty(task.Url))
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = task.Url,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show("无法打开: " + ex.Message);
                }
            }
        }

        private void btnOpenFolder_Click(object? sender, EventArgs e)
        {
            var settings = AppSettings.Load();
            var path = settings.DownloadPath;
            if (!System.IO.Directory.Exists(path))
                System.IO.Directory.CreateDirectory(path);
            try
            {
                System.Diagnostics.Process.Start("explorer.exe", path);
            }
            catch { }
        }

        private void btnAdd_Click(object? sender, EventArgs e)
        {
            var text = txtLinks.Text.Trim();
            if (string.IsNullOrEmpty(text))
            {
                MessageBox.Show("请先粘贴链接（每行一条）", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            int added = 0;
            int skippedOther = 0;

            foreach (var line in lines)
            {
                var url = ExtractUrl(line.Trim());
                if (string.IsNullOrEmpty(url)) continue;

                if (IsUnsupportedForThisPanel(url))
                {
                    skippedOther++;
                    continue;
                }

                if (tasks.Any(t => t.Url == url)) continue;

                var task = new DownloadTask
                {
                    Title = "-",
                    Description = "",
                    Author = "",
                    Url = url,
                    Status = DownloadTaskStatus.Waiting,
                    DownloadCover = chkCover.Checked,
                };
                tasks.Add(task);
                added++;
            }

            txtLinks.Text = "";
            RefreshTaskList();

            if (skippedOther > 0)
            {
                MessageBox.Show(
                    $"检测到 {skippedOther} 个【主页链接】已跳过。\n\n主页批量请到左侧「账号批量」面板。\n本面板仅支持单个作品链接。",
                    "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            if (added > 0)
            {
                foreach (ListViewItem item in taskList.Items)
                    item.Checked = true;
            }
            else if (skippedOther == 0)
            {
                MessageBox.Show("未识别到有效作品链接。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private bool IsUnsupportedForThisPanel(string url)
        {
            if (string.IsNullOrEmpty(url)) return true;
            var u = url.ToLowerInvariant();
            if (u.Contains("/video/") || u.Contains("/note/")) return false;
            if (u.Contains("v.douyin.com/") || u.Contains("v.tiktok.com/")) return false;
            return true;
        }

        private string ExtractUrl(string text)
        {
            var patterns = new[]
            {
                @"https?://v\.douyin\.com/[A-Za-z0-9_\-]+/?",
                @"https?://www\.douyin\.com/(?:video|note)/[^\s?#]+",
                @"https?://www\.douyin\.com/user/[^\s?#]+",
                @"https?://vm\.tiktok\.com/[A-Za-z0-9]+/?",
                @"https?://vt\.tiktok\.com/[A-Za-z0-9]+/?",
                @"https?://www\.tiktok\.com/[^\s]+"
            };

            foreach (var p in patterns)
            {
                var m = System.Text.RegularExpressions.Regex.Match(text, p);
                if (m.Success) return m.Value.TrimEnd('/', '?', '#');
            }
            return text.StartsWith("http") ? text.Trim() : "";
        }

        private void RefreshTaskList()
        {
            var checkedUrls = new System.Collections.Generic.HashSet<string>();
            foreach (ListViewItem it in taskList.Items)
                if (it.Checked && it.Tag is DownloadTask dt)
                    checkedUrls.Add(dt.Url);

            taskList.Items.Clear();
            int i = 1;
            foreach (var tsk in tasks)
            {
                string Clip(string? s, int n)
                {
                    if (string.IsNullOrEmpty(s) || s == "解析中..." || s == "等待解析...") return "-";
                    return s.Length <= n ? s : s.Substring(0, n) + "...";
                }

                var item = new ListViewItem(i.ToString());
                item.SubItems.Add(string.IsNullOrEmpty(tsk.Url) ? "-" : tsk.Url);
                item.SubItems.Add(Clip(tsk.Title, 40));
                item.SubItems.Add(Clip(tsk.Author, 20));
                item.SubItems.Add(Clip(tsk.Description, 40));
                item.SubItems.Add(tsk.PublishTimeText);
                item.SubItems.Add(GetStatusText(tsk.Status));
                item.SubItems.Add($"{tsk.Progress:F0}%");
                item.SubItems.Add(string.IsNullOrEmpty(tsk.Size) ? "-" : tsk.Size);
                item.Tag = tsk;
                item.Checked = checkedUrls.Contains(tsk.Url);
                item.ForeColor = AppTheme.TextPrimary;
                taskList.Items.Add(item);
                i++;
            }
        }

        private void TaskList_MouseUp(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                if (taskList.GetItemAt(e.X, e.Y) is ListViewItem hit && !hit.Selected)
                {
                    taskList.SelectedItems.Clear();
                    hit.Selected = true;
                }
                ShowTaskContextMenu(e.Location);
                return;
            }
        }

        private void TaskList_MouseDoubleClick(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            if (taskList.SelectedItems.Count == 0) return;
            if (taskList.SelectedItems[0].Tag is DownloadTask task && !string.IsNullOrEmpty(task.Url))
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(task.Url) { UseShellExecute = true });
                }
                catch { }
            }
        }

        private void ShowTaskContextMenu(Point loc)
        {
            var menu = new ContextMenuStrip();
            menu.Items.Add("下载选中", null, (s, ev) =>
            {
                foreach (ListViewItem it in taskList.SelectedItems) it.Checked = true;
                btnDownloadSelected_Click(s, ev);
            });
            menu.Items.Add("删除选中", null, (s, ev) =>
            {
                var del = taskList.SelectedItems.Cast<ListViewItem>().ToList();
                if (del.Count == 0) del = taskList.CheckedItems.Cast<ListViewItem>().ToList();
                foreach (var it in del)
                {
                    if (it.Tag is DownloadTask t) tasks.RemoveAll(x => x.Id == t.Id);
                    taskList.Items.Remove(it);
                }
                RefreshTaskList();
            });
            menu.Items.Add("全选", null, (s, ev) =>
            {
                foreach (ListViewItem it in taskList.Items) it.Checked = true;
            });
            menu.Items.Add("打开链接", null, (s, ev) =>
            {
                if (taskList.SelectedItems.Count > 0 && taskList.SelectedItems[0].Tag is DownloadTask t && !string.IsNullOrEmpty(t.Url))
                {
                    try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(t.Url) { UseShellExecute = true }); }
                    catch { }
                }
            });
            menu.Show(taskList, loc);
        }

        private void TaskList_ColumnClick(object? sender, ColumnClickEventArgs e)
        {
            if (e.Column != 5) return;
            tasks = _sortAsc
                ? tasks.OrderBy(x => x.PublishTime ?? DateTime.MinValue).ToList()
                : tasks.OrderByDescending(x => x.PublishTime ?? DateTime.MinValue).ToList();
            _sortAsc = !_sortAsc;
            RefreshTaskList();
        }

        private void UpdateTaskRow(DownloadTask task)
        {
            if (taskList.InvokeRequired)
            {
                try { BeginInvoke(new Action(() => UpdateTaskRow(task))); } catch { }
                return;
            }
            foreach (ListViewItem item in taskList.Items)
            {
                if (item.Tag is DownloadTask t && t.Id == task.Id)
                {
                    string Clip(string? s, int n)
                    {
                        if (string.IsNullOrEmpty(s) || s == "解析中..." || s == "等待解析...") return "-";
                        return s.Length <= n ? s : s.Substring(0, n) + "...";
                    }
                    while (item.SubItems.Count < 9) item.SubItems.Add("");
                    item.SubItems[1].Text = string.IsNullOrEmpty(task.Url) ? "-" : task.Url;
                    item.SubItems[2].Text = Clip(task.Title, 40);
                    item.SubItems[3].Text = Clip(task.Author, 20);
                    item.SubItems[4].Text = Clip(task.Description, 40);
                    item.SubItems[5].Text = task.PublishTimeText;
                    item.SubItems[6].Text = GetStatusText(task.Status);

                    if (task.Status == DownloadTaskStatus.Completed)
                        item.Checked = false;

                    item.SubItems[7].Text = $"{task.Progress:F0}%";
                    item.SubItems[8].Text = string.IsNullOrEmpty(task.Size) ? "-" : task.Size;
                    return;
                }
            }
        }

        private string GetStatusText(DownloadTaskStatus status) => status switch
        {
            DownloadTaskStatus.Waiting => "等待中",
            DownloadTaskStatus.Parsing => "解析中",
            DownloadTaskStatus.Downloading => "下载中",
            DownloadTaskStatus.Processing => "处理中",
            DownloadTaskStatus.Paused => "已暂停",
            DownloadTaskStatus.Completed => "已完成",
            DownloadTaskStatus.Failed => "失败",
            DownloadTaskStatus.Cancelled => "已取消",
            _ => "未知"
        };

        private void TaskList_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (taskList.SelectedItems.Count == 0)
            {
                lblDetailTitle.Text = "选中左侧任务后显示：标题、作者、描述、链接、错误信息";
                lblDetailInfo.Text = "";
                detailProgress.Value = 0;
                return;
            }

            var task = taskList.SelectedItems[0].Tag as DownloadTask;
            if (task == null) return;

            lblDetailTitle.Text = (string.IsNullOrEmpty(task.Title) || task.Title == "-") ? "(未解析标题)" : task.Title;
            lblDetailInfo.Text =
                $"作者: {(string.IsNullOrEmpty(task.Author) ? "-" : task.Author)}\r\n" +
                $"状态: {GetStatusText(task.Status)}\r\n" +
                (string.IsNullOrEmpty(task.ErrorMessage) ? "" : $"错误: {task.ErrorMessage}\r\n") +
                $"进度: {task.Progress:F1}%  大小: {task.Size}\r\n" +
                $"类型: {task.Type}\r\n" +
                $"描述: {(string.IsNullOrEmpty(task.Description) ? "-" : task.Description)}\r\n\r\n" +
                $"链接(双击列表行可打开):\r\n{task.Url}\r\n\r\n" +
                (string.IsNullOrEmpty(task.SavePath) ? "" : $"保存: {task.SavePath}");
            detailProgress.Value = (int)Math.Min(100, Math.Max(0, task.Progress));
        }

        private void btnCancelDl_Click(object? sender, EventArgs e)
        {
            var selected = taskList.CheckedItems.Cast<ListViewItem>()
                .Select(i => i.Tag as DownloadTask)
                .Where(t => t != null).Cast<DownloadTask>().ToList();
            if (selected.Count == 0)
                selected = taskList.SelectedItems.Cast<ListViewItem>()
                    .Select(i => i.Tag as DownloadTask).Where(t => t != null).Cast<DownloadTask>().ToList();
            if (selected.Count == 0)
            {
                selected = tasks.ToList();
            }
            if (selected.Count == 0) return;
            if (_activeDownloader != null)
                _activeDownloader.CancelTasks(selected);
            foreach (var t in selected)
            {
                t.Status = DownloadTaskStatus.Cancelled;
                try { UpdateTaskRow(t); } catch { }
            }
            btnPause.Text = "继续下载";
            _batchPaused = true;
        }

        private void btnPause_Click(object? sender, EventArgs e)
        {
            var selected = taskList.CheckedItems.Cast<ListViewItem>()
                .Select(i => i.Tag as DownloadTask)
                .Where(t => t != null).Cast<DownloadTask>().ToList();
            if (selected.Count == 0)
                selected = taskList.SelectedItems.Cast<ListViewItem>()
                    .Select(i => i.Tag as DownloadTask).Where(t => t != null).Cast<DownloadTask>().ToList();
            if (selected.Count == 0)
                selected = tasks.ToList();
            if (selected.Count == 0) return;
            if (_activeDownloader == null) return;
            bool anyRunning = selected.Any(t =>
                t.Status == DownloadTaskStatus.Downloading || t.Status == DownloadTaskStatus.Parsing);
            if (anyRunning)
            {
                _activeDownloader.PauseTasks(selected);
                btnPause.Text = "继续下载";
                _batchPaused = true;
            }
            else
            {
                btnPause.Text = "暂停/继续";
                _batchPaused = false;
                _ = _activeDownloader.ResumeAndEnqueueAsync(selected);
            }
            foreach (var t in selected) try { UpdateTaskRow(t); } catch { }
        }

        private async void btnDownloadSelected_Click(object? sender, EventArgs e)
        {
            var selected = taskList.CheckedItems.Cast<ListViewItem>()
                .Select(i => i.Tag as DownloadTask)
                .Where(t => t != null)
                .Cast<DownloadTask>()
                .ToList();

            if (selected.Count == 0)
            {
                MessageBox.Show("请先勾选要下载的任务", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            btnDownloadSelected.Enabled = false;
            btnDownloadAll.Enabled = false;
            try
            {
                var settings = AppSettings.Load();
                int degree = Math.Max(1, settings.MaxConcurrent);
                bool wantCover = chkCover.Checked || settings.DownloadCover;
                var downloader = new DownloadService(settings);
                _activeDownloader = downloader;
                _batchPaused = false;

                downloader.TaskUpdated += tsk =>
                {
                    if (IsDisposed) return;
                    void Apply()
                    {
                        try { UpdateTaskRow(tsk); } catch { }
                    }
                    if (InvokeRequired) BeginInvoke(Apply);
                    else Apply();
                };

                using var sem = new SemaphoreSlim(degree);
                var workers = selected.Select(async task =>
                {
                    await sem.WaitAsync();
                    try
                    {
                        if (IsDisposed) return;
                        if (task.Status == DownloadTaskStatus.Downloading)
                            return;
                        if (task.Status == DownloadTaskStatus.Completed
                            && string.IsNullOrEmpty(task.ErrorMessage))
                            return;

                        task.Status = DownloadTaskStatus.Parsing;
                        task.Progress = 0;
                        task.ErrorMessage = "";
                        task.DownloadUrl = "";
                        task.ImageUrls = null;
                        try { UpdateTaskRow(task); } catch { }

                        var parser = new LinkParser(settings.DouyinCookie, settings.TikTokCookie);
                        var parsed = await parser.ParseSingleAsync(task.Url);

                        task.Title = parsed.Title;
                        task.Author = parsed.Author;
                        task.AwemeId = parsed.AwemeId;
                        task.DownloadUrl = parsed.DownloadUrl;
                        task.ImageUrls = parsed.ImageUrls;
                        task.Type = parsed.Type;
                        task.Description = parsed.Description;
                        task.Status = parsed.Status;
                        task.ErrorMessage = parsed.ErrorMessage;
                        task.PublishTime = parsed.PublishTime;
                        try { UpdateTaskRow(task); } catch { }

                        bool hasMedia = !string.IsNullOrEmpty(task.DownloadUrl)
                            || (task.ImageUrls != null && task.ImageUrls.Count > 0);
                        if (task.Status == DownloadTaskStatus.Failed || !hasMedia)
                        {
                            if (string.IsNullOrEmpty(task.ErrorMessage))
                                task.ErrorMessage = "解析未得到下载地址";
                            task.Status = DownloadTaskStatus.Failed;
                            try { UpdateTaskRow(task); } catch { }
                            return;
                        }

                        task.DownloadCover = wantCover;
                        await downloader.EnqueueAsync(new[] { task });
                        try { UpdateTaskRow(task); } catch { }
                    }
                    catch (Exception ex)
                    {
                        task.Status = DownloadTaskStatus.Failed;
                        task.ErrorMessage = ex.Message;
                        try { UpdateTaskRow(task); } catch { }
                    }
                    finally
                    {
                        sem.Release();
                    }
                });

                await Task.WhenAll(workers);
            }
            catch { }
            finally
            {
                btnDownloadSelected.Enabled = true;
                btnDownloadAll.Enabled = true;
            }
        }

        private void btnDownloadAll_Click(object? sender, EventArgs e)
        {
            if (tasks.Count == 0)
            {
                MessageBox.Show("任务列表为空", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            foreach (ListViewItem item in taskList.Items)
            {
                if (item.Tag is DownloadTask t)
                {
                    bool done = t.Status == DownloadTaskStatus.Completed
                                && string.IsNullOrEmpty(t.ErrorMessage);
                    item.Checked = !done;
                }
            }

            btnDownloadSelected_Click(sender, e);
        }
    }
}