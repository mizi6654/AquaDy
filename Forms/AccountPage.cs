using System;
using System.Drawing;
using System.Linq;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using AquaDy.Controls;
using AquaDy.Models;
using AquaDy.Services;
using System.Threading.Tasks;

namespace AquaDy.Forms
{
    /// <summary>
    /// 第二阶段：账号批量下载页面（重点解决主页数量不一致问题）
    /// </summary>
    public class AccountPage : UserControl
    {
        private DarkInputPanel inputUrl;
        private DarkButton btnParse;
        private CheckedListBox typeList;
        private NumericUpDown numMaxPages;
        private CheckBox chkFullMode;
        private CheckBox chkIncremental;
        private DateTimePicker dtpStart;
        private DateTimePicker dtpEnd;
        private ListView resultList;
        private Label lblParseResult;
        private DarkButton btnDownload = null!;
        private DarkButton btnPause = null!;
        private DarkButton btnCancel = null!;
        private DarkButton btnSelectAll = null!, btnDeselect = null!, btnClearList = null!;
        private DownloadService? _activeDownloader;
        private bool _batchPaused;
        private CancellationTokenSource? _batchCts;
        private List<DownloadTask>? _batchTasks;
        private ProgressBar parseProgress;
        private RadioButton radioBrowser;
        private RadioButton radioApi;

        public AccountPage()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.BackColor = AppTheme.Background;
            this.Padding = new Padding(24, 20, 24, 20);
            this.DoubleBuffered = true;

            // 标题
            var title = new Label
            {
                Text = "账号批量下载",
                Font = new Font("Microsoft YaHei UI", 16F, FontStyle.Bold),
                ForeColor = AppTheme.TextPrimary,
                AutoSize = true,
                Location = new Point(24, 16)
            };

            var sub = new Label
            {
                Text = "支持：① 主页链接 → 发布作品；② 合集链接（douyin.com/collection/数字）→ 合集内作品。可多行粘贴多个合集。",
                Font = new Font("Microsoft YaHei UI", 9F),
                ForeColor = AppTheme.TextSecondary,
                AutoSize = true,
                Location = new Point(24, 48)
            };

            // ========== 输入卡片 ==========
            var card = CreateCard(24, 80, this.Width - 48, 320);
            card.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            var lblUrl = new Label
            {
                Text = "主页链接 或 合集链接（可多行）",
                Font = new Font("Microsoft YaHei UI", 9F),
                ForeColor = AppTheme.TextSecondary,
                Location = new Point(20, 16),
                AutoSize = true
            };

            inputUrl = new DarkInputPanel
            {
                Location = new Point(20, 42),
                Size = new Size(card.Width - 160, 78),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Placeholder = "粘贴抖音或 TikTok 账号主页链接..."
            };

            btnParse = new DarkButton
            {
                Text = "开始解析",
                IsPrimary = true,
                Location = new Point(card.Width - 130, 42),
                Size = new Size(110, 40),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            inputUrl.Multiline = true;
            btnParse.Click += BtnParse_Click;

            var lblMode = new Label
            {
                Text = "解析方式",
                Font = new Font("Microsoft YaHei UI", 9F),
                ForeColor = AppTheme.TextSecondary,
                Location = new Point(20, 155),
                AutoSize = true
            };
            radioBrowser = new RadioButton
            {
                Text = "浏览器拦截（推荐）",
                Checked = true,
                ForeColor = AppTheme.TextPrimary,
                Location = new Point(90, 153),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            radioApi = new RadioButton
            {
                Text = "页内接口翻页",
                ForeColor = AppTheme.TextPrimary,
                Location = new Point(250, 153),
                AutoSize = true,
                BackColor = Color.Transparent
            };

            // 类型选择
            var lblType = new Label
            {
                Text = "作品类型",
                Font = new Font("Microsoft YaHei UI", 9F),
                ForeColor = AppTheme.TextSecondary,
                Location = new Point(20, 185),
                AutoSize = true
            };

            typeList = new CheckedListBox
            {
                Location = new Point(20, 207),
                Size = new Size(160, 90),
                BackColor = AppTheme.CardBackground,
                ForeColor = AppTheme.TextPrimary,
                BorderStyle = BorderStyle.None,
                CheckOnClick = true,
                IntegralHeight = true
            };
            typeList.Items.AddRange(new object[] { "发布作品", "合集作品" });
            typeList.SetItemChecked(0, true);
            typeList.SetItemChecked(1, true); // 可同时勾选；贴合集链接时自动走合集解析

            // 选项
            chkFullMode = new CheckBox
            {
                Text = "完整模式（直到无更多数据）",
                ForeColor = AppTheme.TextPrimary,
                Location = new Point(220, 207),
                AutoSize = true,
                Checked = true,
                FlatStyle = FlatStyle.Flat
            };

            chkIncremental = new CheckBox
            {
                Text = "增量下载（跳过已下载）",
                ForeColor = AppTheme.TextPrimary,
                Location = new Point(220, 233),
                AutoSize = true,
                Checked = true,
                FlatStyle = FlatStyle.Flat
            };

            var lblMax = new Label
            {
                Text = "最大页数(0=不限)",
                ForeColor = AppTheme.TextSecondary,
                Location = new Point(400, 235),
                AutoSize = true
            };

            numMaxPages = new NumericUpDown
            {
                Minimum = 0,
                Maximum = 500,
                Value = 0,
                Width = 56,
                Location = new Point(520, 232),
                BackColor = AppTheme.CardBackground,
                ForeColor = AppTheme.TextPrimary
            };

            card.Controls.AddRange(new Control[] {
                lblUrl, inputUrl, btnParse,
                lblMode, radioBrowser, radioApi,
                lblType, typeList,
                chkFullMode, chkIncremental, lblMax, numMaxPages
            });

            // ========== 解析结果提示 ==========
            lblParseResult = new Label
            {
                Text = "解析完成后将显示：浏览器验证数量 vs 实际获取数量",
                Font = new Font("Microsoft YaHei UI", 9F),
                ForeColor = AppTheme.TextMuted,
                Location = new Point(24, 410),
                AutoSize = true
            };

            parseProgress = new ProgressBar
            {
                Location = new Point(24, 435),
                Size = new Size(this.Width - 48, 8),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Style = ProgressBarStyle.Continuous,
                Maximum = 100,
                Visible = false
            };

            // ========== 结果列表 ==========
            resultList = new ListView
            {
                Location = new Point(24, 455),
                Size = new Size(this.Width - 48, this.Height - 560),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                View = View.Details,
                FullRowSelect = true,
                CheckBoxes = true,
                BorderStyle = BorderStyle.None,
                BackColor = AppTheme.CardBackground,
                ForeColor = AppTheme.TextPrimary,
                Font = new Font("Microsoft YaHei UI", 9F),
                HeaderStyle = ColumnHeaderStyle.Nonclickable
            };
            resultList.MultiSelect = true;
            resultList.Columns.Add("#", 40);
            resultList.Columns.Add("作品链接", 220);
            resultList.Columns.Add("标题", 120);
            resultList.Columns.Add("作者", 80);
            resultList.Columns.Add("描述", 140);
            resultList.Columns.Add("发布时间", 120);
            resultList.Columns.Add("类型", 60);
            resultList.Columns.Add("状态", 70);
            resultList.Columns.Add("进度", 70);
            resultList.MouseDoubleClick += (s, e) =>
            {
                if (e.Button != MouseButtons.Left) return;
                if (resultList.SelectedItems.Count == 0) return;
                if (resultList.SelectedItems[0].Tag is DownloadTask task && !string.IsNullOrEmpty(task.Url))
                {
                    try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(task.Url) { UseShellExecute = true }); }
                    catch { }
                }
            };

            // 底部按钮
            btnDownload = new DarkButton
            {
                Text = "下载选中作品",
                IsPrimary = true,
                Size = new Size(140, 40),
                Location = new Point(24, this.Height - 70),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            btnDownload.Click += async (s, e) => await DownloadSelectedAsync();

            var cms = new ContextMenuStrip();
            cms.Items.Add("下载选中", null, async (s, e) =>
            {
                // 右键拖选时同步勾选，避免看起来没勾选却在下载
                foreach (ListViewItem it in resultList.SelectedItems) it.Checked = true;
                await DownloadSelectedAsync(preferSelection: true);
            });
            cms.Items.Add("删除选中", null, (s, e) =>
            {
                var del = resultList.SelectedItems.Cast<ListViewItem>().ToList();
                if (del.Count == 0) del = resultList.CheckedItems.Cast<ListViewItem>().ToList();
                foreach (var it in del) resultList.Items.Remove(it);
                int n = 1;
                foreach (ListViewItem it in resultList.Items) { it.Text = n.ToString(); n++; }
                lblParseResult.Text = $"已删除，剩余 {resultList.Items.Count} 条";
            });
            cms.Items.Add("全选", null, (s, e) =>
            {
                foreach (ListViewItem it in resultList.Items) it.Checked = true;
            });
            cms.Items.Add("取消全选", null, (s, e) =>
            {
                foreach (ListViewItem it in resultList.Items) it.Checked = false;
            });
            resultList.ContextMenuStrip = cms;

            btnSelectAll = new DarkButton
            {
                Text = "全选",
                Size = new Size(80, 40),
                Location = new Point(180, this.Height - 70),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            btnSelectAll.Click += (s, e) =>
            {
                foreach (ListViewItem item in resultList.Items)
                    item.Checked = true;
            };

            btnDeselect = new DarkButton
            {
                Text = "取消全选",
                Size = new Size(90, 40),
                Location = new Point(270, this.Height - 70),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            btnDeselect.Click += (s, e) =>
            {
                foreach (ListViewItem item in resultList.Items)
                    item.Checked = false;
            };

            btnPause = new DarkButton
            {
                Text = "暂停/继续",
                Size = new Size(100, 40),
                Location = new Point(370, this.Height - 70),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            btnPause.Click += BtnPause_Click;
            btnCancel = new DarkButton
            {
                Text = "取消下载",
                Size = new Size(100, 36)
            };
            btnCancel.Click += BtnCancel_Click;

            btnClearList = new DarkButton
            {
                Text = "清空列表",
                Size = new Size(90, 40),
                Location = new Point(480, this.Height - 70),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            btnClearList.Click += (s, e) =>
            {
                resultList.Items.Clear();
                lblParseResult.Text = "列表已清空";
                lblParseResult.ForeColor = AppTheme.TextMuted;
            };

            this.Controls.AddRange(new Control[] {
                title, sub, card, lblParseResult, parseProgress,
                resultList, btnDownload, btnSelectAll, btnDeselect, btnPause, btnCancel, btnClearList
            });

            this.Resize += (s, e) =>
            {
                card.Width = this.Width - 48;
                inputUrl.Width = Math.Max(200, card.Width - 40);
                // 开始解析：页内接口翻页右侧
                btnParse.Location = new Point(Math.Min(card.Width - 120, 400), 148);
                parseProgress.Width = this.Width - 48;
                resultList.Width = this.Width - 48;
                resultList.Height = this.Height - 560;
                if (btnCancel != null && btnPause != null)
                    btnCancel.Location = new Point(btnPause.Right + 8, btnPause.Top);
            };
        }


        private void BtnPause_Click(object? sender, EventArgs e)
        {
            // 优先当前勾选项；无勾选时退回本批
            var items = resultList.CheckedItems.Count > 0
                ? resultList.CheckedItems.Cast<ListViewItem>().ToList()
                : resultList.SelectedItems.Cast<ListViewItem>().ToList();
            var tasks = items.Select(i => i.Tag as DownloadTask).Where(t => t != null).Cast<DownloadTask>().ToList();
            if (tasks.Count == 0 && _batchTasks != null)
                tasks = _batchTasks.ToList();
            if (tasks.Count == 0)
            {
                lblParseResult.Text = "没有可暂停/继续的任务";
                return;
            }
            if (_activeDownloader == null)
            {
                lblParseResult.Text = "当前没有进行中的下载批次";
                return;
            }

            bool anyRunning = tasks.Any(t =>
                t.Status == DownloadTaskStatus.Downloading
                || t.Status == DownloadTaskStatus.Parsing
                || t.Status == DownloadTaskStatus.Waiting);

            if (anyRunning && !_batchPaused)
            {
                // 暂停整批：标记暂停 + 取消批次 Token，让 WhenAll 干净结束
                _batchPaused = true;
                _activeDownloader.PauseTasks(tasks);
                try { _batchCts?.Cancel(); } catch { }
                foreach (var t in tasks)
                {
                    if (t.Status == DownloadTaskStatus.Downloading || t.Status == DownloadTaskStatus.Waiting
                        || t.Status == DownloadTaskStatus.Parsing)
                        t.Status = DownloadTaskStatus.Paused;
                }
                RefreshStatusRows(tasks);
                btnPause.Text = "继续下载";
                lblParseResult.Text = $"已暂停 {tasks.Count} 项（勾选），可点「继续下载」重新入队";
                ParseLogger.Op("暂停", $"count={tasks.Count}");
            }
            else
            {
                // 继续：只重新入队未完成项（新批次）
                _batchPaused = false;
                btnPause.Text = "暂停/继续";
                var again = tasks.Where(t =>
                    t.Status != DownloadTaskStatus.Completed).ToList();
                lblParseResult.Text = $"继续下载，重新入队 {again.Count} 项…";
                try { _batchCts?.Dispose(); } catch { }
                _batchCts = new CancellationTokenSource();
                _ = ResumeBatchAsync(again, _batchCts.Token);
            }
        }

        private async Task ResumeBatchAsync(List<DownloadTask> tasks, CancellationToken ct)
        {
            if (_activeDownloader == null) return;
            try
            {
                await _activeDownloader.ResumeAndEnqueueAsync(tasks, ct);
                int ok = tasks.Count(t => t.Status == DownloadTaskStatus.Completed);
                int fail = tasks.Count(t => t.Status == DownloadTaskStatus.Failed);
                int paused = tasks.Count(t => t.Status == DownloadTaskStatus.Paused);
                int cancelled = tasks.Count(t => t.Status == DownloadTaskStatus.Cancelled);
                if (IsDisposed) return;
                void Done()
                {
                    lblParseResult.Text = $"批次结束：成功 {ok}｜失败 {fail}｜暂停 {paused}｜取消 {cancelled}";
                    btnDownload.Enabled = true;
                }
                if (InvokeRequired) BeginInvoke(Done); else Done();
            }
            catch (OperationCanceledException)
            {
                if (!IsDisposed)
                    lblParseResult.Text = "下载已暂停或取消";
            }
            catch (Exception ex)
            {
                if (!IsDisposed && ex is not OperationCanceledException)
                    lblParseResult.Text = "继续下载异常: " + ex.Message;
            }
        }

        private void RefreshStatusRows(IEnumerable<DownloadTask> tasks)
        {
            var set = tasks.GroupBy(t => t.Id).ToDictionary(g => g.Key, g => g.First());
            void Apply()
            {
                resultList.BeginUpdate();
                try
                {
                    foreach (ListViewItem it in resultList.Items)
                    {
                        if (it.Tag is DownloadTask dt && set.ContainsKey(dt.Id))
                        {
                            var t = set[dt.Id];
                            while (it.SubItems.Count < 10) it.SubItems.Add("");
                            var st = StatusText(t.Status);
                            if (it.SubItems[7].Text != st) it.SubItems[7].Text = st;
                            var prog = t.Status == DownloadTaskStatus.Cancelled ? "-" : t.Progress.ToString("0") + "%";
                            if (it.SubItems[8].Text != prog) it.SubItems[8].Text = prog;
                        }
                    }
                }
                finally { resultList.EndUpdate(); }
            }
            if (resultList.InvokeRequired) resultList.BeginInvoke(Apply); else Apply();
        }

        private void BtnCancel_Click(object? sender, EventArgs e)
        {
            var items = resultList.CheckedItems.Count > 0
                ? resultList.CheckedItems.Cast<ListViewItem>().ToList()
                : resultList.SelectedItems.Cast<ListViewItem>().ToList();
            var tasks = items.Select(i => i.Tag as DownloadTask).Where(t => t != null).Cast<DownloadTask>().ToList();
            // 无勾选时取消整批
            if (tasks.Count == 0 && _batchTasks != null)
                tasks = _batchTasks.ToList();
            if (tasks.Count == 0)
            {
                lblParseResult.Text = "没有可取消的任务";
                return;
            }
            try { _batchCts?.Cancel(); } catch { }
            if (_activeDownloader != null)
                _activeDownloader.CancelTasks(tasks);
            foreach (var t in tasks)
                t.Status = DownloadTaskStatus.Cancelled;
            RefreshStatusRows(tasks);
            lblParseResult.Text = $"已取消 {tasks.Count} 项（可点「继续下载」重新入队）";
            ParseLogger.Op("取消", $"count={tasks.Count}");
            btnPause.Text = "继续下载";
            _batchPaused = true;
        }

        private async Task DownloadSelectedAsync(bool preferSelection = false)
        {
            List<ListViewItem> items;
            if (preferSelection && resultList.SelectedItems.Count > 0)
                items = resultList.SelectedItems.Cast<ListViewItem>().ToList();
            else if (resultList.CheckedItems.Count > 0)
                items = resultList.CheckedItems.Cast<ListViewItem>().ToList();
            else
                items = resultList.SelectedItems.Cast<ListViewItem>().ToList();
            if (items.Count == 0)
            {
                lblParseResult.Text = "请先勾选或选中要下载的作品";
                return;
            }

            btnDownload.Enabled = false;
            parseProgress.Visible = true;
            parseProgress.Value = 0;
            lblParseResult.ForeColor = AppTheme.Info;

            try
            {
                var settings = AppSettings.Load();
                var parser = new LinkParser(settings.DouyinCookie, settings.TikTokCookie);
                var downloader = new DownloadService(settings);

                var lastUi = DateTime.MinValue;
                downloader.TaskUpdated += task =>
                {
                    void Apply()
                    {
                        foreach (ListViewItem it in resultList.Items)
                        {
                            if (it.Tag is DownloadTask dt &&
                                (dt.Id == task.Id || dt.AwemeId == task.AwemeId || dt.Url == task.Url))
                            {
                                while (it.SubItems.Count < 10) it.SubItems.Add("");
                                var st = StatusText(task.Status);
                                if (it.SubItems[7].Text != st) it.SubItems[7].Text = st;
                                string prog = task.Status == DownloadTaskStatus.Cancelled
                                    ? "-"
                                    : task.Progress.ToString("0") + "%";
                                if (it.SubItems[8].Text != prog) it.SubItems[8].Text = prog;
                                // 进度回调不改标题/作者，避免整表视觉跳动
                                break;
                            }
                        }
                        // 仅在状态变化或间隔足够时更新顶栏，避免狂刷导致列表「抖」
                        bool important = task.Status is DownloadTaskStatus.Completed
                            or DownloadTaskStatus.Failed
                            or DownloadTaskStatus.Paused
                            or DownloadTaskStatus.Cancelled;
                        if (!important && (DateTime.Now - lastUi).TotalMilliseconds < 1500)
                            return;
                        lastUi = DateTime.Now;
                        int active = 0, done = 0, fail = 0, paused = 0, cancelled = 0;
                        foreach (ListViewItem it2 in resultList.Items)
                        {
                            if (it2.Tag is DownloadTask d2)
                            {
                                if (d2.Status == DownloadTaskStatus.Downloading) active++;
                                else if (d2.Status == DownloadTaskStatus.Completed) done++;
                                else if (d2.Status == DownloadTaskStatus.Failed) fail++;
                                else if (d2.Status == DownloadTaskStatus.Paused) paused++;
                                else if (d2.Status == DownloadTaskStatus.Cancelled) cancelled++;
                            }
                        }
                        lblParseResult.Text =
                            $"下载中 {active}｜完成 {done}｜失败 {fail}｜暂停 {paused}｜取消 {cancelled}";
                    }
                    if (resultList.IsDisposed) return;
                    if (resultList.InvokeRequired) { try { resultList.BeginInvoke(Apply); } catch { } }
                    else Apply();
                };

                int total = items.Count;
                var tasks = new List<DownloadTask>();
                // 先并发解析（限流），再整体 Enqueue 并发下载
                var parseSem = new System.Threading.SemaphoreSlim(Math.Max(1, Math.Min(settings.MaxConcurrent, 3)));
                var parseJobs = items.Select(async item =>
                {
                    if (item.Tag is not DownloadTask task) return;
                    await parseSem.WaitAsync();
                    try
                    {
                        // 列表阶段已有下载地址则跳过二次解析
                        if (!string.IsNullOrEmpty(task.DownloadUrl) || (task.ImageUrls != null && task.ImageUrls.Count > 0))
                        {
                            ParseLogger.Op("下载跳过二次解析", $"id={task.AwemeId}");
                            lock (tasks) tasks.Add(task);
                            UpdateItemStatus(item, "排队中", 1);
                            return;
                        }

                        UpdateItemStatus(item, "解析中", 0);
                        ParseLogger.Op("二次解析", $"url={task.Url}");
                        var parsed = await parser.ParseSingleAsync(task.Url);
                        bool weakTitle = string.IsNullOrEmpty(task.Title) || task.Title.StartsWith("作品 ")
                            || task.Title == "解析中..." || task.Title == "等待解析...";
                        if (weakTitle && !string.IsNullOrEmpty(parsed.Title))
                            task.Title = parsed.Title;
                        if (!string.IsNullOrEmpty(parsed.Author) &&
                            (string.IsNullOrEmpty(task.Author) || task.Author == "未知作者"))
                            task.Author = parsed.Author;
                        if (!string.IsNullOrEmpty(parsed.Description))
                            task.Description = parsed.Description;
                        task.DownloadUrl = parsed.DownloadUrl;
                        task.ImageUrls = parsed.ImageUrls;
                        task.CoverUrl = parsed.CoverUrl ?? task.CoverUrl;
                        task.MusicUrl = parsed.MusicUrl ?? task.MusicUrl;
                        task.Type = parsed.Type;
                        task.PublishTime = parsed.PublishTime ?? task.PublishTime;
                        if (!string.IsNullOrEmpty(parsed.AwemeId)) task.AwemeId = parsed.AwemeId;
                        try
                        {
                            var authorCol = task.Author ?? "";
                            if (!string.IsNullOrEmpty(task.MixName))
                                authorCol = string.IsNullOrEmpty(authorCol) ? task.MixName : (authorCol + "/" + task.MixName);
                            if (item.SubItems.Count > 3)
                                item.SubItems[3].Text = Clip(string.IsNullOrEmpty(authorCol) ? "-" : authorCol, 40);
                            if (item.SubItems.Count > 2 && !string.IsNullOrEmpty(task.Title))
                                item.SubItems[2].Text = Clip(task.Title, 40);
                            if (item.SubItems.Count > 4)
                                item.SubItems[4].Text = Clip(task.Description ?? "", 40);
                            if (item.SubItems.Count > 5)
                                item.SubItems[5].Text = task.PublishTimeText ?? "";
                        }
                        catch { }
                        if (string.IsNullOrEmpty(task.DownloadUrl) && (task.ImageUrls == null || task.ImageUrls.Count == 0))
                            ParseLogger.Fail($"二次解析无地址 id={task.AwemeId} url={task.Url}");
                        else
                            ParseLogger.Info($"二次解析成功 id={task.AwemeId}");
                        lock (tasks) tasks.Add(task);
                        UpdateItemStatus(item, "排队中", 1);
                    }
                    catch (Exception ex)
                    {
                        task.Status = DownloadTaskStatus.Failed;
                        task.ErrorMessage = ex.Message;
                        ParseLogger.Fail($"二次解析异常 id={task.AwemeId}: {ex.Message}");
                        UpdateItemStatus(item, "失败", 0);
                    }
                    finally { parseSem.Release(); }
                });
                await Task.WhenAll(parseJobs);

                _activeDownloader = downloader;
                _batchTasks = tasks;
                _batchPaused = false;
                lblParseResult.Text = $"开始并发下载（最大 {settings.MaxConcurrent}）共 {tasks.Count} 个…";
                ParseLogger.Op("开始下载", $"count={tasks.Count} concurrent={settings.MaxConcurrent}");
                var batchAuthor = tasks.Select(t => t.Author)
                    .FirstOrDefault(a => !string.IsNullOrEmpty(a) && a != "未知作者" && !a.StartsWith("合集"));
                foreach (var t in tasks)
                {
                    t.DownloadCover = settings.DownloadCover;
                    if ((string.IsNullOrEmpty(t.Author) || t.Author == "未知作者") && !string.IsNullOrEmpty(batchAuthor))
                        t.Author = batchAuthor;
                }
                _batchCts?.Dispose();
                _batchCts = new CancellationTokenSource();
                try
                {
                    await downloader.EnqueueAsync(tasks, _batchCts.Token);
                }
                catch (OperationCanceledException)
                {
                    // 暂停/取消
                }

                int ok = 0, fail = 0, paused = 0, cancelled = 0;
                foreach (var item in items)
                {
                    if (item.Tag is DownloadTask d)
                    {
                        if (d.Status == DownloadTaskStatus.Completed) ok++;
                        else if (d.Status == DownloadTaskStatus.Failed) fail++;
                        else if (d.Status == DownloadTaskStatus.Paused) paused++;
                        else if (d.Status == DownloadTaskStatus.Cancelled) cancelled++;
                    }
                }
                lblParseResult.Text =
                    $"批次结束：成功 {ok}｜失败 {fail}｜暂停 {paused}｜取消 {cancelled}｜共 {total}";
                lblParseResult.ForeColor = (fail == 0 && paused == 0 && cancelled == 0)
                    ? AppTheme.Success : AppTheme.Warning;
            }
            catch (OperationCanceledException)
            {
                lblParseResult.Text = "下载已暂停或取消";
            }
            catch (Exception ex)
            {
                lblParseResult.Text = "下载异常: " + ex.Message;
                ParseLogger.Fail("DownloadSelected: " + ex.Message);
            }

            finally
            {
                btnDownload.Enabled = true;
            }
        }

        private void UpdateItemStatus(ListViewItem item, string status, int progress)
        {
            void Apply()
            {
                while (item.SubItems.Count < 10) item.SubItems.Add("");
                item.SubItems[7].Text = status;
                item.SubItems[8].Text = progress + "%";
            }
            if (resultList.InvokeRequired) resultList.Invoke(Apply); else Apply();
        }

        private static string Clip(string? s, int n)
        {
            if (string.IsNullOrEmpty(s)) return "-";
            s = s.Replace("\r", " ").Replace("\n", " ").Trim();
            return s.Length <= n ? s : s.Substring(0, n) + "...";
        }

        private static string StatusText(DownloadTaskStatus s) => s switch
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

        private Panel CreateCard(int x, int y, int w, int h)
        {
            var card = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(w, h),
                BackColor = AppTheme.CardBackground
            };
            card.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = RoundedRect(card.ClientRectangle, 10);
                using var brush = new SolidBrush(AppTheme.CardBackground);
                e.Graphics.FillPath(brush, path);
                using var pen = new Pen(AppTheme.Border, 1);
                e.Graphics.DrawPath(pen, path);
            };
            return card;
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

        private async void BtnParse_Click(object? sender, EventArgs e)
        {
            var url = inputUrl.Text.Trim();
            if (string.IsNullOrEmpty(url))
            {
                MessageBox.Show("请输入账号主页链接", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            parseProgress.Visible = true;
            parseProgress.Value = 10;
            var mode = (radioBrowser != null && radioBrowser.Checked)
                ? AccountParseMode.BrowserIntercept
                : AccountParseMode.ApiInBrowser;
            lblParseResult.Text = mode == AccountParseMode.BrowserIntercept
                ? "浏览器拦截模式：打开主页并滚动加载..."
                : "页内接口模式：在浏览器环境中翻页请求列表...";
            lblParseResult.ForeColor = AppTheme.Info;
            btnParse.Enabled = false;
            resultList.Items.Clear();
            ParseLogger.Op("开始解析", inputUrl.Text?.Trim() ?? "");
            ParseLogger.ReloadSetting();
            bool parseUiDone = false;

            try
            {
                var settings = AppSettings.Load();
                if (string.IsNullOrWhiteSpace(settings.DouyinCookie))
                {
                    MessageBox.Show("请先在「设置」中填写抖音 Cookie", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                IProgress<string> progress = new Progress<string>(msg =>
                {
                    if (parseUiDone) return;
                    lblParseResult.Text = msg;
                    if (parseProgress.Value < 90) parseProgress.Value += 5;
                });

                var accountParser = new AccountParseService(settings.DouyinCookie);
                var list = new List<DownloadTask>();
                var inputText = inputUrl.Text ?? url ?? "";
                var lines = inputText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length == 0) lines = new[] { url };

                bool wantPost = typeList.Items.Count > 0 && typeList.GetItemChecked(0);
                bool wantMix = typeList.Items.Count > 1 && typeList.GetItemChecked(1);

                foreach (var raw in lines)
                {
                    var one = LinkParser.ExtractUrl(raw.Trim());
                    if (string.IsNullOrEmpty(one)) one = raw.Trim();
                    if (string.IsNullOrEmpty(one)) continue;

                    bool isMix = one.IndexOf("/collection/", StringComparison.OrdinalIgnoreCase) >= 0;
                    if (isMix)
                    {
                        progress.Report("解析合集: " + one);
                        var part = await accountParser.ParseCollectionAsync(
                            one, (int)numMaxPages.Value, chkFullMode.Checked, progress, CancellationToken.None);
                        list.AddRange(part);
                    }
                    else if (wantPost || !wantMix)
                    {
                        progress.Report("解析主页发布: " + one);
                        var part = await accountParser.ParseAsync(
                            one, mode, (int)numMaxPages.Value, chkFullMode.Checked, progress, CancellationToken.None);
                        list.AddRange(part);
                    }
                    else
                    {
                        progress.Report("仅合集模式请粘贴 collection 链接，已跳过主页: " + one);
                    }
                }

                // 按作品去重
                var seen = new HashSet<string>();
                list = list.Where(t =>
                {
                    var key = !string.IsNullOrEmpty(t.AwemeId) ? t.AwemeId : t.Url;
                    return !string.IsNullOrEmpty(key) && seen.Add(key);
                }).ToList();

                // 纯 HTTP 兜底（浏览器失败时）
                if (list.Count == 0)
                {
                    lblParseResult.Text = "浏览器模式无数据，尝试 HTTP 兜底...";
                    var httpParser = new LinkParser(settings.DouyinCookie, settings.TikTokCookie);
                    list = await httpParser.ParseAccountAsync(url, fullMode: chkFullMode.Checked,
                        maxPages: (int)numMaxPages.Value);
                }

                if (list.Count == 0)
                {
                    string info = "https://www.douyin.com/collection/";
                    MessageBox.Show($"解析结果为 0 条。合集请使用：{info}数字 并勾选「合集作品」。", "解析为空", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                parseProgress.Value = 80;
                int i = 1;
                foreach (var tsk in list)
                {
var item = new ListViewItem(i.ToString());
                    item.SubItems.Add(string.IsNullOrEmpty(tsk.Url) ? "-" : tsk.Url);
                    item.SubItems.Add(Clip(tsk.Title, 40));
                    var authorCol = tsk.Author ?? "";
                    if (authorCol == "未知作者") authorCol = "";
                    var mixShow = tsk.MixName ?? "";
                    if (!string.IsNullOrEmpty(mixShow))
                    {
                        if (string.IsNullOrEmpty(authorCol))
                            authorCol = mixShow; // 保留 合集xxxx 或中文合集名
                        else
                            authorCol = authorCol + "/" + mixShow;
                    }
                    if (string.IsNullOrEmpty(authorCol)) authorCol = "-";
                    item.SubItems.Add(Clip(authorCol, 40));
                    item.SubItems.Add(Clip(string.IsNullOrEmpty(tsk.Description) ? "" : tsk.Description, 40));
                    item.SubItems.Add(tsk.PublishTimeText);
                    item.SubItems.Add(string.IsNullOrEmpty(tsk.MixName) ? (tsk.Type == ContentType.Video ? "视频" : tsk.Type.ToString()) : "合集");
                    item.SubItems.Add("待下载");
                    item.SubItems.Add("0%");
                    item.Tag = tsk;
                    item.Checked = true;
                    resultList.Items.Add(item);
                    i++;
                }
                parseProgress.Value = 100;
                var groups = list
                    .Select(x => {
                        var a = string.IsNullOrEmpty(x.Author) ? "" : x.Author;
                        var m = string.IsNullOrEmpty(x.MixName) ? "" : x.MixName;
                        if (!string.IsNullOrEmpty(a) && !string.IsNullOrEmpty(m)) return a + "/" + m;
                        if (!string.IsNullOrEmpty(m)) return m;
                        if (!string.IsNullOrEmpty(a)) return a;
                        return "";
                    })
                    .Where(x => !string.IsNullOrEmpty(x))
                    .Distinct().Take(4).ToList();
                // 以最终列表条数为准（多合集合并后）；先锁定 UI 再写，避免被迟到的 progress 覆盖
                parseUiDone = true;
                int mixCnt = list.Count(x => !string.IsNullOrEmpty(x.MixName));
                int postCnt = list.Count - mixCnt;
                string summary = groups.Count > 0
                    ? $"解析完成：共 {list.Count} 条（{string.Join("；", groups)}）"
                    : $"解析完成：共 {list.Count} 条";
                if (mixCnt > 0 && postCnt > 0)
                    summary = $"解析完成：共 {list.Count} 条（发布约 {postCnt} + 合集约 {mixCnt}）";
                lblParseResult.Text = summary;
                lblParseResult.ForeColor = list.Count > 0 ? AppTheme.Success : AppTheme.Warning;
            }
            catch (Exception ex)
            {
                parseUiDone = true;
                lblParseResult.Text = "解析失败: " + ex.Message;
                lblParseResult.ForeColor = AppTheme.Error;
            }
            finally
            {
                btnParse.Enabled = true;
                parseProgress.Visible = false;
            }
        }
    }
}