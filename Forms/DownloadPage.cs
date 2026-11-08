using System.Drawing.Drawing2D;
using AquaDy.Controls;
using AquaDy.Models;
using AquaDy.Services;

namespace AquaDy.Forms
{
    /// <summary>
    /// 第一阶段：快速下载页面（单链接 / 批量作品链接）
    /// 注意：主页链接请到「账号批量」面板
    /// </summary>
    public class DownloadPage : UserControl
    {
        private TextBox txtLinks;
        private DarkButton btnAdd;
        private ListView taskList;
        private Panel detailPanel;
        private Label lblDetailTitle;
        private Label lblDetailInfo;
        private ProgressBar detailProgress;
        private DarkButton btnDownloadSelected;
        private DarkButton btnPause;
        private DarkButton btnCancel;
        private DownloadService? _activeDownloader;
        private bool _batchPaused;
        private CancellationTokenSource? _batchCts;
        private DarkButton btnDownloadAll;
        private DarkButton btnSelectAll;
        private DarkButton btnClear;
        private ComboBox cmbQuality;
        private CheckBox chkCover;
        private Panel cardNew;

        private List<DownloadTask> tasks = new();
        private readonly object _parseLock = new object();

        public DownloadPage()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.BackColor = AppTheme.Background;
            this.Padding = new Padding(0);
            this.DoubleBuffered = true;

            // ========== 标题区 ==========
            var titleLabel = new Label
            {
                Text = "下载任务",
                Font = new Font("Microsoft YaHei UI", 16F, FontStyle.Bold),
                ForeColor = AppTheme.TextPrimary,
                AutoSize = true,
                Location = new Point(24, 16)
            };

            var subTitle = new Label
            {
                Text = "仅支持单个作品链接（www.douyin.com/video/… 或 v.douyin.com/…）。主页/合集请到「账号批量」。",
                Font = new Font("Microsoft YaHei UI", 9F),
                ForeColor = AppTheme.TextSecondary,
                AutoSize = true,
                Location = new Point(24, 48)
            };

            // ========== 新建下载卡片（加高以支持多行输入） ==========
            cardNew = CreateCard(24, 80, 700, 200);

            var cardTitle = new Label
            {
                Text = "新建下载 · 支持单条作品 / 批量链接（每行一条）",
                Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold),
                ForeColor = AppTheme.TextPrimary,
                AutoSize = true,
                Location = new Point(16, 12)
            };

            // 真正的多行输入框
            txtLinks = new TextBox
            {
                Location = new Point(16, 42),
                Size = new Size(Math.Max(200, cardNew.Width - 32), 100),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                AcceptsReturn = true,
                WordWrap = false,
                BackColor = Color.FromArgb(28, 28, 36),
                ForeColor = AppTheme.TextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Consolas", 9.5F),
                PlaceholderText = "每行粘贴一条作品链接，例如：\r\nhttps://www.douyin.com/video/7687902923120532723\r\nhttps://v.douyin.com/xxxxx/"
            };

            cmbQuality = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(16, 155),
                Size = new Size(160, 28),
                BackColor = AppTheme.CardBackground,
                ForeColor = AppTheme.TextPrimary,
                FlatStyle = FlatStyle.Flat
            };
            cmbQuality.Items.AddRange(new object[] { "最高画质 (自动)", "1080P", "720P", "原画" });
            cmbQuality.SelectedIndex = 0;

            chkCover = new CheckBox
            {
                Text = "下载封面",
                ForeColor = AppTheme.TextSecondary,
                Location = new Point(200, 157),
                AutoSize = true,
                FlatStyle = FlatStyle.Flat
            };

            // 添加下载：紧跟「下载封面」右侧
            btnAdd = new DarkButton
            {
                Text = "+ 添加下载",
                IsPrimary = true,
                Size = new Size(110, 32)
            };
            btnAdd.Click += BtnAdd_Click;
            // 先加入控件再根据封面勾选实际宽度定位

            cardNew.Controls.AddRange(new Control[] {
                cardTitle, txtLinks, cmbQuality, chkCover, btnAdd
            });
            chkCover.Location = new Point(200, 157);
            btnAdd.Location = new Point(chkCover.Left + 90, 152); // 封面勾选右侧
            // 布局后精确贴在封面后面
            cardNew.Layout += (s, e) =>
            {
                btnAdd.Location = new Point(chkCover.Right + 16, chkCover.Top - 2);
                txtLinks.Width = Math.Max(200, cardNew.ClientSize.Width - 32);
            };

            // ========== 任务列表标题 + 工具栏 ==========
            var listHeader = new Label
            {
                Text = "任务列表",
                Font = new Font("Microsoft YaHei UI", 11F, FontStyle.Bold),
                ForeColor = AppTheme.TextPrimary,
                AutoSize = true,
                Location = new Point(24, 295)
            };

            var toolPanel = new Panel
            {
                Location = new Point(24, 325),
                Size = new Size(720, 40),
                BackColor = AppTheme.Background
            };

            btnSelectAll = new DarkButton
            {
                Text = "全选",
                Size = new Size(70, 32),
                Location = new Point(0, 4)
            };
            btnSelectAll.Click += (s, e) =>
            {
                foreach (ListViewItem item in taskList.Items)
                    item.Checked = true;
            };

            var btnDeselect = new DarkButton
            {
                Text = "取消全选",
                Size = new Size(90, 32),
                Location = new Point(80, 4)
            };
            btnDeselect.Click += (s, e) =>
            {
                foreach (ListViewItem item in taskList.Items)
                    item.Checked = false;
            };

            btnDownloadSelected = new DarkButton
            {
                Text = "下载选中",
                Size = new Size(100, 32),
                Location = new Point(180, 4)
            };
            btnDownloadSelected.Click += BtnDownloadSelected_Click;

            btnDownloadAll = new DarkButton
            {
                Text = "全部下载",
                IsPrimary = true,
                Size = new Size(100, 32),
                Location = new Point(290, 4)
            };
            btnDownloadAll.Click += BtnDownloadAll_Click;

            btnPause = new DarkButton
            {
                Text = "暂停/继续",
                IsPrimary = false,
                Size = new Size(100, 32),
                Location = new Point(400, 4)
            };
            btnPause.Click += BtnPause_Click;

            btnCancel = new DarkButton
            {
                Text = "取消下载",
                Size = new Size(100, 32),
                Location = new Point(510, 4)
            };
            btnCancel.Click += BtnCancelDl_Click;

            // 清空在「取消」右侧，不要再用 510
            btnClear = new DarkButton
            {
                Text = "清空列表",
                Size = new Size(90, 32),
                Location = new Point(620, 4)
            };
            btnClear.Click += (s, e) =>
            {
                tasks.Clear();
                RefreshTaskList();
            };

            toolPanel.Controls.AddRange(new Control[] {
                btnSelectAll, btnDeselect, btnDownloadSelected, btnDownloadAll, btnPause, btnCancel, btnClear
            });

            // ========== 任务列表 ==========
            taskList = new ListView
            {
                Location = new Point(24, 370),
                Size = new Size(700, 300),
                View = View.Details,
                FullRowSelect = true,
                GridLines = false,
                BorderStyle = BorderStyle.None,
                BackColor = AppTheme.CardBackground,
                ForeColor = AppTheme.TextPrimary,
                Font = new Font("Microsoft YaHei UI", 9F),
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
                CheckBoxes = true
            };
            taskList.MultiSelect = true;
            taskList.Columns.Add("#", 40);
            taskList.Columns.Add("链接", 220);
            taskList.Columns.Add("标题", 120);
            taskList.Columns.Add("作者", 80);
            taskList.Columns.Add("描述", 120);
            taskList.Columns.Add("发布时间", 120);
            taskList.Columns.Add("状态", 70);
            taskList.Columns.Add("进度", 55);
            taskList.Columns.Add("大小", 70);
            taskList.MouseDoubleClick += TaskList_MouseDoubleClick;
            taskList.MouseUp += TaskList_MouseUp;
            taskList.SelectedIndexChanged += TaskList_SelectedIndexChanged;
            taskList.ColumnClick += TaskList_ColumnClick;

            // ========== 右侧详情面板 ==========
            const int detailW = 240;
            detailPanel = new Panel
            {
                Width = detailW,
                BackColor = AppTheme.CardBackground,
                Padding = new Padding(12)
            };

            var detailTitle = new Label
            {
                Text = "已选任务",
                Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold),
                ForeColor = AppTheme.TextPrimary,
                Location = new Point(12, 12),
                AutoSize = true
            };

            lblDetailTitle = new Label
            {
                Text = "选中左侧任务后显示：标题、作者、描述、链接、错误信息",
                Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold),
                ForeColor = AppTheme.TextPrimary,
                Location = new Point(12, 45),
                Size = new Size(detailW - 24, 50)
            };

            lblDetailInfo = new Label
            {
                Text = "",
                Font = new Font("Microsoft YaHei UI", 8.5F),
                ForeColor = AppTheme.TextSecondary,
                Location = new Point(12, 100),
                Size = new Size(detailW - 24, 180)
            };

            detailProgress = new ProgressBar
            {
                Location = new Point(12, 290),
                Size = new Size(detailW - 24, 8),
                Style = ProgressBarStyle.Continuous,
                Maximum = 100
            };

            var btnOpenLink = new DarkButton
            {
                Text = "打开作品链接",
                Size = new Size(detailW - 24, 32),
                Location = new Point(12, 300)
            };
            btnOpenLink.Click += (s, e) =>
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
            };

            var btnOpenFolder = new DarkButton
            {
                Text = "打开保存目录",
                Size = new Size(detailW - 24, 32),
                Location = new Point(12, 340),
                IsPrimary = true
            };
            btnOpenFolder.Click += (s, e) =>
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
            };

            detailPanel.Controls.AddRange(new Control[] {
                detailTitle, lblDetailTitle, lblDetailInfo, detailProgress, btnOpenLink, btnOpenFolder
            });

            this.Controls.AddRange(new Control[] {
                titleLabel, subTitle, cardNew, listHeader, toolPanel, taskList, detailPanel
            });

            void Relayout()
            {
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
                txtLinks.Width = Math.Max(300, contentW - 150);
                btnAdd.Left = contentW - 130;

                toolPanel.Width = contentW;
                taskList.Size = new Size(contentW, Math.Max(150, h - 400));
            }

            this.Resize += (s, e) =>
            {
                try
                {
                    if (txtLinks != null && cardNew != null)
                        txtLinks.Width = Math.Max(200, cardNew.Width - 32);
                }
                catch { }
            };
            this.Resize += (s, e) => Relayout();
            this.Load += (s, e) => Relayout();
            Relayout();
        }

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

        private void BtnAdd_Click(object? sender, EventArgs e)
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

                // 去重
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
                // 自动全选新加的任务，方便一键下载
                foreach (ListViewItem item in taskList.Items)
                    item.Checked = true;

                //MessageBox.Show($"已添加 {added} 条作品链接到任务列表。\n已自动全选，可直接点「下载选中」或「全部下载」。",
                //    "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                // 不弹窗，列表已可见
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
            // 允许：/video/ 、/note/ 、短链 v.douyin.com
            if (u.Contains("/video/") || u.Contains("/note/")) return false;
            if (u.Contains("v.douyin.com/") || u.Contains("v.tiktok.com/")) return false;
            // 其余：主页、合集、直播等都不支持
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

        private void TaskList_DoubleClick(object? sender, EventArgs e)
        {
            // 仅左键双击打开；右键不打开（由 MouseUp 处理菜单）
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
                BtnDownloadSelected_Click(s, ev);
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

        private bool _sortAsc = false;
        private void TaskList_ColumnClick(object? sender, ColumnClickEventArgs e)
        {
            // 点击「发布时间」列排序
            if (e.Column != 5) return;
            tasks = _sortAsc
                ? tasks.OrderBy(x => x.PublishTime ?? DateTime.MinValue).ToList()
                : tasks.OrderByDescending(x => x.PublishTime ?? DateTime.MinValue).ToList();
            _sortAsc = !_sortAsc;
            RefreshTaskList();
        }

        /// <summary>只更新一行，避免整表刷新闪烁</summary>
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
                    // # 链接 标题 作者 描述 发布时间 状态 进度 大小
                    while (item.SubItems.Count < 9) item.SubItems.Add("");
                    item.SubItems[1].Text = string.IsNullOrEmpty(task.Url) ? "-" : task.Url;
                    item.SubItems[2].Text = Clip(task.Title, 40);
                    item.SubItems[3].Text = Clip(task.Author, 20);
                    item.SubItems[4].Text = Clip(task.Description, 40);
                    item.SubItems[5].Text = task.PublishTimeText;
                    item.SubItems[6].Text = GetStatusText(task.Status);

                    // 已完成：取消勾选，避免再次「全部下载」误下
                    if (task.Status == DownloadTaskStatus.Completed)
                        item.Checked = false;
                    else if (task.Status == DownloadTaskStatus.Failed
                          || task.Status == DownloadTaskStatus.Cancelled
                          || task.Status == DownloadTaskStatus.Waiting)
                    {
                        // 失败保留勾选，方便直接点「下载选中」重试
                        // item.Checked = true; // 可选
                    }

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

        private void BtnCancelDl_Click(object? sender, EventArgs e)
        {
            var selected = taskList.CheckedItems.Cast<ListViewItem>()
                .Select(i => i.Tag as DownloadTask)
                .Where(t => t != null).Cast<DownloadTask>().ToList();
            if (selected.Count == 0)
                selected = taskList.SelectedItems.Cast<ListViewItem>()
                    .Select(i => i.Tag as DownloadTask).Where(t => t != null).Cast<DownloadTask>().ToList();
            if (selected.Count == 0)
            {
                selected = tasks.ToList(); // 无勾选则整表
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

        private void BtnPause_Click(object? sender, EventArgs e)
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
                // 暂停/取消后继续：重新入队（从当前进度重下；不支持断点续传时会从 0 开始）
                btnPause.Text = "暂停/继续";
                _batchPaused = false;
                _ = _activeDownloader.ResumeAndEnqueueAsync(selected);
            }
            foreach (var t in selected) try { UpdateTaskRow(t); } catch { }
        }

        
        private async void BtnDownloadSelected_Click(object? sender, EventArgs e)
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
                // 每个并发任务单独 Parser，避免共享 HttpClient 并发解析出 400
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
                        // 已完成且无错误：跳过（全部下载时可能混进）
                        if (task.Status == DownloadTaskStatus.Completed
                            && string.IsNullOrEmpty(task.ErrorMessage))
                            return;

                        task.Status = DownloadTaskStatus.Parsing;
                        task.Progress = 0;
                        task.ErrorMessage = "";
                        task.DownloadUrl = "";
                        task.ImageUrls = null;
                        try { UpdateTaskRow(task); } catch { }

                        // 独立 Parser，与已完成一致：每次重下都重新解析
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

        private void BtnDownloadAll_Click(object? sender, EventArgs e)
        {
            if (tasks.Count == 0)
            {
                MessageBox.Show("任务列表为空", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // 全部下载 = 所有未完成项（等待/失败/取消/暂停），已完成不勾、不下
            foreach (ListViewItem item in taskList.Items)
            {
                if (item.Tag is DownloadTask t)
                {
                    bool done = t.Status == DownloadTaskStatus.Completed
                                && string.IsNullOrEmpty(t.ErrorMessage);
                    item.Checked = !done;
                }
            }

            BtnDownloadSelected_Click(sender, e);
        }
    }
}
