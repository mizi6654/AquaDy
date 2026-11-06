using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Text.RegularExpressions;
using AquaDy.Controls;
using AquaDy.Models;
using AquaDy.Services;

namespace AquaDy.Forms
{
    public class LivePage : UserControl
    {
        private DarkInputPanel inputUrl = null!;
        private DarkButton btnAdd = null!;
        private ComboBox cmbQuality = null!;
        private ComboBox cmbFormat = null!;
        private NumericUpDown numInterval = null!;
        private NumericUpDown numSegment = null!;
        private CheckBox chkAutoDefault = null!;
        private ListView liveList = null!;
        private Label lblTip = null!;
        private Label lblSavePath = null!;
        private readonly List<LiveItem> _items = new();
        private System.Windows.Forms.Timer? _monitorTimer;
        private System.Windows.Forms.Timer? _speedTimer;
        private bool _refreshing;
        private bool _syncingUi;

        private class LiveItem
        {
            public string Id { get; set; } = Guid.NewGuid().ToString("N");
            public string Anchor { get; set; } = "";
            public string RoomName { get; set; } = "";
            public string Url { get; set; } = "";
            public string StreamUrl { get; set; } = "";
            public int IntervalSec { get; set; } = 30;
            public string Quality { get; set; } = "原画";
            public string Format { get; set; } = "FLV";
            public int SegmentMin { get; set; } // 0=不分段
            public string Status { get; set; } = "待命";
            public DateTime? StartTime { get; set; }
            public DateTime CreateTime { get; set; } = DateTime.Now;
            public bool AutoRecord { get; set; }
            public Process? Recorder { get; set; }
            public string OutputFile { get; set; } = "";
            public bool IsLive { get; set; }
            public string SpeedText { get; set; } = "-";
            public long LastFileSize { get; set; }
            public DateTime LastSizeTime { get; set; } = DateTime.Now;
            public DateTime NextCheckTime { get; set; } = DateTime.Now;
            /// <summary>
            /// 用户点了停止后为 true，监控不得自动再开录，直到用户再点开始或开启自动录制
            /// </summary>
            public bool UserStopped { get; set; }
        }

        public LivePage()
        {
            InitializeComponent();
            LoadSavedRooms();
            _monitorTimer = new System.Windows.Forms.Timer { Interval = 5_000 };
            _monitorTimer.Tick += async (s, e) => await MonitorTickAsync();
            _monitorTimer.Start();
            _speedTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _speedTimer.Tick += (s, e) => UpdateSpeedsOnly();
            _speedTimer.Start();
        }

        private void InitializeComponent()
        {
            this.BackColor = AppTheme.Background;
            this.Padding = new Padding(24, 20, 24, 20);

            var title = new Label
            {
                Text = "直播下载",
                Font = new Font("Microsoft YaHei UI", 16F, FontStyle.Bold),
                ForeColor = AppTheme.TextPrimary,
                AutoSize = true,
                Location = new Point(24, 16)
            };
            var sub = new Label
            {
                Text = "监控间隔单位=秒（≥10）。分段=分钟（0=不分段）。选中行可改封装/清晰度，下次开录生效。",
                Font = new Font("Microsoft YaHei UI", 9F),
                ForeColor = AppTheme.TextSecondary,
                AutoSize = true,
                Location = new Point(24, 48)
            };

            var card = CreateCard(24, 80, this.Width - 48, 118);
            card.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            var lbl = new Label { Text = "直播间链接", ForeColor = AppTheme.TextSecondary, Location = new Point(20, 10), AutoSize = true };
            inputUrl = new DarkInputPanel
            {
                Location = new Point(20, 32),
                Size = new Size(card.Width - 150, 34),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Placeholder = "https://live.douyin.com/房间号"
            };
            btnAdd = new DarkButton
            {
                Text = "添加监控",
                IsPrimary = true,
                Location = new Point(card.Width - 120, 32),
                Size = new Size(100, 34),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnAdd.Click += async (s, e) => await AddLiveAsync();

            // 第二行
            int y2 = 78;
            var lblQ = new Label { Text = "清晰度", ForeColor = AppTheme.TextSecondary, Location = new Point(20, y2), AutoSize = true };
            cmbQuality = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(70, y2 - 4), Size = new Size(72, 26),
                BackColor = AppTheme.CardBackground, ForeColor = AppTheme.TextPrimary, FlatStyle = FlatStyle.Flat
            };
            cmbQuality.Items.AddRange(new object[] { "原画", "超清", "高清", "标清" });
            cmbQuality.SelectedIndex = 0;
            cmbQuality.SelectedIndexChanged += (s, e) => ApplyUiToSelected(quality: true);

            var lblFmt = new Label { Text = "封装", ForeColor = AppTheme.TextSecondary, Location = new Point(150, y2), AutoSize = true };
            cmbFormat = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(190, y2 - 4), Size = new Size(64, 26),
                BackColor = AppTheme.CardBackground, ForeColor = AppTheme.TextPrimary, FlatStyle = FlatStyle.Flat
            };
            cmbFormat.Items.AddRange(new object[] { "FLV", "MP4" });
            cmbFormat.SelectedIndex = 1; // 默认 MP4
            cmbFormat.SelectedIndexChanged += (s, e) => ApplyUiToSelected(format: true);

            var lblIv = new Label { Text = "监控(秒)", ForeColor = AppTheme.TextSecondary, Location = new Point(265, y2), AutoSize = true };
            numInterval = new NumericUpDown
            {
                Location = new Point(330, y2 - 4), Size = new Size(58, 26),
                Minimum = 10, Maximum = 3600, Value = 10, Increment = 5,
                BackColor = AppTheme.CardBackground, ForeColor = AppTheme.TextPrimary
            };
            numInterval.ValueChanged += (s, e) => ApplyUiToSelected(interval: true);

            chkAutoDefault = new CheckBox
            {
                Text = "添加后自动录制",
                ForeColor = AppTheme.TextPrimary,
                Location = new Point(400, y2 - 2),
                AutoSize = true
            };

            var lblSeg = new Label { Text = "分段(分)", ForeColor = AppTheme.TextSecondary, Location = new Point(540, y2), AutoSize = true };
            numSegment = new NumericUpDown
            {
                Location = new Point(600, y2 - 4), Size = new Size(50, 26),
                Minimum = 0, Maximum = 240, Value = 30, Increment = 5,
                BackColor = AppTheme.CardBackground, ForeColor = AppTheme.TextPrimary
            };
            var lblSegTip = new Label
            {
                Text = "0=不分段；>0强制FLV",
                ForeColor = AppTheme.TextMuted,
                Location = new Point(655, y2),
                AutoSize = true
            };
            numSegment.ValueChanged += (s, e) => ApplyUiToSelected(segment: true);

            card.Controls.AddRange(new Control[]
            {
                lbl, inputUrl, btnAdd, lblQ, cmbQuality, lblFmt, cmbFormat,
                lblIv, numInterval, chkAutoDefault, lblSeg, numSegment, lblSegTip
            });

            lblSavePath = new Label
            {
                Text = "保存：" + Path.Combine(AppSettings.Load().DownloadPath, "Live"),
                ForeColor = AppTheme.TextMuted,
                Location = new Point(24, 206), AutoSize = true
            };
            lblTip = new Label
            {
                Text = "就绪。修改清晰度/封装/分段后，对选中行下次「开始录制」生效（不会自动重录）。",
                ForeColor = AppTheme.TextMuted,
                Location = new Point(24, 226), AutoSize = true
            };

            liveList = new ListView
            {
                Location = new Point(24, 250),
                Size = new Size(this.Width - 48, this.Height - 335),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                View = View.Details,
                FullRowSelect = true,
                MultiSelect = true,
                BorderStyle = BorderStyle.None,
                BackColor = AppTheme.CardBackground,
                ForeColor = AppTheme.TextPrimary,
                Font = new Font("Microsoft YaHei UI", 9F),
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
                HideSelection = false
            };
            // # 主播 房间 自动 清晰度 状态 开始 创建 链接 封装 速度
            liveList.Columns.Add("#", 36);
            liveList.Columns.Add("主播信息", 110);
            liveList.Columns.Add("直播间名称", 120);
            liveList.Columns.Add("自动录制", 90);
            liveList.Columns.Add("清晰度", 55);
            liveList.Columns.Add("录制状态", 90);
            liveList.Columns.Add("开始时间", 115);
            liveList.Columns.Add("创建时间", 115);
            liveList.Columns.Add("链接", 140);
            liveList.Columns.Add("封装", 50);
            liveList.Columns.Add("下载速度", 90);

            liveList.MouseDown += (s, e) =>
            {
                var hit = liveList.GetItemAt(e.X, e.Y);
                if (hit == null) return;
                if (e.Button == MouseButtons.Right && !hit.Selected)
                {
                    liveList.SelectedItems.Clear();
                    hit.Selected = true;
                }
            };
            liveList.MouseDoubleClick += (s, e) =>
            {
                if (e.Button != MouseButtons.Left) return;
                OpenSelectedRoom();
            };
            liveList.ItemSelectionChanged += (s, e) =>
            {
                if (!e.IsSelected || e.Item?.Tag is not LiveItem it) return;
                _syncingUi = true;
                try
                {
                    SelectCombo(cmbQuality, it.Quality);
                    SelectCombo(cmbFormat, it.Format);
                    numInterval.Value = Math.Max(numInterval.Minimum, Math.Min(numInterval.Maximum, it.IntervalSec));
                    numSegment.Value = Math.Max(0, Math.Min(numSegment.Maximum, it.SegmentMin));
                }
                finally { _syncingUi = false; }

                if (!string.IsNullOrEmpty(it.OutputFile))
                    if (it.SegmentMin > 0 && !string.IsNullOrEmpty(it.OutputFile))
                    {
                        var dirShow = Path.GetDirectoryName(it.OutputFile) ?? "";
                        lblTip.Text = "选中：" + it.Anchor + " → 分段目录 " + dirShow + "（当前/首段：" + Path.GetFileName(it.OutputFile) + "）[" + it.Format + "/" + it.Quality + "]";
                    }
                    else
                        lblTip.Text = "选中：" + it.Anchor + " → " + it.OutputFile + "  [" + it.Format + "/" + it.Quality + "]";
                else
                    lblTip.Text = "选中：" + it.Anchor + " | " + it.Status + " | " + it.Format + "/" + it.Quality + " | 分段" + (it.SegmentMin <= 0 ? "关" : it.SegmentMin + "分");
            };

            var cms = new ContextMenuStrip();
            cms.Items.Add("开始录制", null, async (s, e) => await StartRecordSelectedAsync());
            cms.Items.Add("停止录制", null, (s, e) => StopRecordSelected());
            cms.Items.Add("重新解析", null, async (s, e) => await ReparseSelectedAsync());
            cms.Items.Add("打开直播间", null, (s, e) => OpenSelectedRoom());
            cms.Items.Add(new ToolStripSeparator());
            cms.Items.Add("开启自动录制", null, (s, e) => SetAutoRecord(true));
            cms.Items.Add("关闭自动录制", null, (s, e) => SetAutoRecord(false));
            cms.Items.Add(new ToolStripSeparator());
            cms.Items.Add("删除选中", null, (s, e) => RemoveSelected());
            liveList.ContextMenuStrip = cms;

            var btnStart = MkBtn("开始录制", 24, true);
            btnStart.Click += async (s, e) => await StartRecordSelectedAsync();
            var btnStop = MkBtn("停止录制", 144, false);
            btnStop.Click += (s, e) => StopRecordSelected();
            var btnDel = MkBtn("删除选中", 264, false);
            btnDel.Click += (s, e) => RemoveSelected();
            var btnOpen = MkBtn("打开保存目录", 374, false);
            btnOpen.Click += (s, e) =>
            {
                var dir = Path.Combine(AppSettings.Load().DownloadPath, "Live");
                Directory.CreateDirectory(dir);
                try { Process.Start(new ProcessStartInfo("explorer.exe", dir) { UseShellExecute = true }); } catch { }
            };
            var btnConvert = MkBtn("格式转换", 504, false);
            btnConvert.Click += async (s, e) => await ConvertDialogAsync();

            this.Controls.AddRange(new Control[] { title, sub, card, lblSavePath, lblTip, liveList, btnStart, btnStop, btnDel, btnOpen, btnConvert });
            this.VisibleChanged += (s, e) =>
            {
                if (this.Visible)
                    RefreshSavePathFromSettings();
            };
            this.Resize += (s, e) =>
            {
                card.Width = this.Width - 48;
                inputUrl.Width = Math.Max(200, card.Width - 150);
                btnAdd.Left = card.Width - 120;
                liveList.Width = this.Width - 48;
                liveList.Height = Math.Max(120, this.Height - 335);
            };
        }

        private void ApplyUiToSelected(bool quality = false, bool format = false, bool interval = false, bool segment = false)
        {
            if (_syncingUi) return;
            var selected = GetSelectedItems();
            if (selected.Count == 0) return;
            foreach (var it in selected)
            {
                if (quality) it.Quality = cmbQuality.SelectedItem?.ToString() ?? it.Quality;
                if (format) it.Format = cmbFormat.SelectedItem?.ToString() ?? it.Format;
                if (interval) it.IntervalSec = (int)numInterval.Value;
                if (segment)
                {
                    it.SegmentMin = (int)numSegment.Value;
                    if (it.SegmentMin > 0)
                        it.Format = "FLV"; // 分段强制 flv
                    else if (format || cmbFormat.SelectedItem != null)
                        it.Format = cmbFormat.SelectedItem?.ToString() ?? it.Format;
                }
                else if (format && it.SegmentMin > 0)
                {
                    it.Format = "FLV";
                }
            }
            PersistRooms();
            UpdateAllRowStatus();
            if (format || quality || segment)
            {
                var parts = new List<string>();
                if (format) parts.Add("封装");
                if (quality) parts.Add("清晰度");
                if (segment) parts.Add(numSegment.Value > 0 ? "分段(强制FLV)" : "分段关闭");
                lblTip.Text = "已更新选中项（" + string.Join("/", parts) + "），下次开始录制生效。";
            }
        }

        private static void SelectCombo(ComboBox cmb, string value)
        {
            if (string.IsNullOrEmpty(value)) return;
            int idx = cmb.Items.IndexOf(value);
            if (idx >= 0) cmb.SelectedIndex = idx;
        }

        private DarkButton MkBtn(string text, int x, bool primary) => new DarkButton
        {
            Text = text, IsPrimary = primary,
            Size = new Size(text.Length > 4 ? 120 : 110, 36),
            Location = new Point(x, this.Height - 60),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left
        };

        private void LoadSavedRooms()
        {
            _items.Clear();
            foreach (var s in LiveRoomStore.Load())
            {
                // 兼容旧数据：IntervalMin 字段曾表示分钟或错误的秒
                int sec = s.IntervalMin;
                if (sec <= 0) sec = 30;
                if (sec < 10) sec = sec * 60; // 旧版「2分」等
                if (sec < 10) sec = 30;

                _items.Add(new LiveItem
                {
                    Url = s.Url,
                    Anchor = s.Anchor,
                    RoomName = s.RoomName,
                    IntervalSec = sec,
                    Quality = string.IsNullOrEmpty(s.Quality) ? "原画" : s.Quality,
                    Format = "MP4",
                    SegmentMin = 30,
                    AutoRecord = s.AutoRecord,
                    CreateTime = s.CreateTime,
                    Status = "待命"
                });
            }
            RebuildList();
        }

        private void PersistRooms()
        {
            LiveRoomStore.Save(_items.Select(it => new LiveRoomStore.SavedRoom
            {
                Url = it.Url,
                Anchor = it.Anchor,
                RoomName = it.RoomName,
                IntervalMin = it.IntervalSec, // 存秒
                Quality = it.Quality,
                AutoRecord = it.AutoRecord,
                CreateTime = it.CreateTime
            }).ToList());
        }

        private async Task AddLiveAsync()
        {
            var url = (inputUrl.Text ?? "").Trim();
            if (string.IsNullOrEmpty(url))
            {
                MessageBox.Show("请输入直播间链接", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var dlg = new Form
            {
                Text = "解析直播间", Size = new Size(420, 150),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false, MinimizeBox = false,
                BackColor = AppTheme.Background, ShowInTaskbar = false
            };
            var lbl = new Label
            {
                Text = "正在解析主播 / 房间 / 流地址…",
                ForeColor = AppTheme.TextPrimary, Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            dlg.Controls.Add(lbl);

            LiveRoomInfo? info = null; Exception? err = null;
            dlg.Shown += async (s, e) =>
            {
                try {
                    var q = cmbQuality.SelectedItem?.ToString() ?? "原画";
                    info = await new LiveRoomService(AppSettings.Load().DouyinCookie).ParseAsync(url, q);
                }
                catch (Exception ex) { err = ex; }
                dlg.Close();
            };
            dlg.ShowDialog(this.FindForm());

            if (err != null) { MessageBox.Show("解析失败：\n" + err.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (info == null) { MessageBox.Show("解析失败", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            if (_items.Any(x => UrlsMatch(x.Url, info.PageUrl)))
            {
                MessageBox.Show("该直播间已在列表中", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var item = new LiveItem
            {
                Url = info.PageUrl,
                Anchor = info.Anchor,
                RoomName = info.RoomName,
                StreamUrl = info.StreamUrl,
                IsLive = info.IsLive,
                IntervalSec = (int)numInterval.Value,
                Quality = cmbQuality.SelectedItem?.ToString() ?? "原画",
                Format = cmbFormat.SelectedItem?.ToString() ?? "MP4",
                SegmentMin = (int)numSegment.Value,
                AutoRecord = chkAutoDefault.Checked,
                Status = chkAutoDefault.Checked ? "监控中"
                    : (info.IsLive ? (string.IsNullOrEmpty(info.StreamUrl) ? "直播中(无直链)" : "直播中(可录制)") : "未开播"),
                CreateTime = DateTime.Now,
                NextCheckTime = DateTime.Now
            };
            _items.Add(item);
            PersistRooms();
            RebuildList();
            inputUrl.Text = "";
            lblTip.Text = "已添加：" + item.Anchor + " | " + item.RoomName + " | " + item.Format + "/" + item.Quality;
        }

        private static bool UrlsMatch(string a, string b)
        {
            if (string.Equals(a, b, StringComparison.OrdinalIgnoreCase)) return true;
            var ma = Regex.Match(a ?? "", @"live\.douyin\.com/([0-9A-Za-z_\-]+)");
            var mb = Regex.Match(b ?? "", @"live\.douyin\.com/([0-9A-Za-z_\-]+)");
            return ma.Success && mb.Success && ma.Groups[1].Value == mb.Groups[1].Value;
        }

        private void RebuildList()
        {
            if (_refreshing) return;
            _refreshing = true;
            try
            {
                var selectedIds = liveList.SelectedItems.Cast<ListViewItem>()
                    .Select(x => (x.Tag as LiveItem)?.Id).Where(x => x != null).ToHashSet();

                liveList.BeginUpdate();
                liveList.Items.Clear();
                int i = 1;
                foreach (var it in _items)
                {
                    SyncProcessState(it);
                    var row = new ListViewItem(i.ToString());
                    row.SubItems.Add(string.IsNullOrEmpty(it.Anchor) ? "-" : it.Anchor);
                    row.SubItems.Add(string.IsNullOrEmpty(it.RoomName) ? "-" : it.RoomName);
                    row.SubItems.Add(it.AutoRecord ? ("是/" + it.IntervalSec + "秒") : "否");
                    row.SubItems.Add(it.Quality);
                    row.SubItems.Add(it.Status);
                    row.SubItems.Add(it.StartTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "-");
                    row.SubItems.Add(it.CreateTime.ToString("yyyy-MM-dd HH:mm:ss"));
                    row.SubItems.Add(it.Url);
                    row.SubItems.Add(it.Format);
                    row.SubItems.Add(it.SpeedText);
                    row.Tag = it;
                    row.ForeColor = AppTheme.TextPrimary;
                    if (selectedIds.Contains(it.Id)) row.Selected = true;
                    liveList.Items.Add(row);
                    i++;
                }
                liveList.EndUpdate();
                RefreshSavePathFromSettings();
            }
            finally { _refreshing = false; }
        }

        public void RefreshSavePathFromSettings()
        {
            try
            {
                var path = Path.Combine(AppSettings.Load().DownloadPath, "Live");
                void Apply()
                {
                    if (lblSavePath != null && !lblSavePath.IsDisposed)
                        lblSavePath.Text = "保存：" + path;
                }
                if (IsDisposed) return;
                if (InvokeRequired) BeginInvoke((Action)Apply);
                else Apply();
            }
            catch { }
        }

        private void UpdateAllRowStatus()
        {
            foreach (ListViewItem row in liveList.Items)
            {
                if (row.Tag is not LiveItem it || row.SubItems.Count < 11) continue;
                SyncProcessState(it);

                void Set(int idx, string val)
                {
                    if (row.SubItems[idx].Text != val)
                        row.SubItems[idx].Text = val;
                }

                Set(1, string.IsNullOrEmpty(it.Anchor) ? "-" : it.Anchor);
                Set(2, string.IsNullOrEmpty(it.RoomName) ? "-" : it.RoomName);
                Set(3, it.AutoRecord ? ("是/" + it.IntervalSec + "秒") : "否");
                Set(4, it.Quality);
                Set(5, it.Status);
                Set(6, it.StartTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "-");
                Set(9, it.Format);
                Set(10, string.IsNullOrEmpty(it.SpeedText) ? "-" : it.SpeedText);
            }
        }

        private static void SyncProcessState(LiveItem it)
        {
            if (it.Recorder == null) return;
            if (!it.Recorder.HasExited) return;
            int code = -1;
            try { code = it.Recorder.ExitCode; } catch { }
            try { it.Recorder.Dispose(); } catch { }
            it.Recorder = null;
            it.SpeedText = "-";
            it.LastFileSize = 0;
            if (it.Status == "录制中")
                it.Status = code == 0 ? "录制结束" : "录制中断";
        }

        private void UpdateSpeedsOnly()
        {
            foreach (ListViewItem row in liveList.Items)
            {
                if (row.Tag is not LiveItem it || row.SubItems.Count < 11) continue;
                SyncProcessState(it);

                // 已不在录制：状态与速度必须落在 UI 上
                if (it.Recorder == null || it.Recorder.HasExited)
                {
                    if (row.SubItems[5].Text == "录制中" || row.SubItems[5].Text != it.Status)
                        row.SubItems[5].Text = it.Status;
                    if (row.SubItems[10].Text != "-" && row.SubItems[10].Text != it.SpeedText)
                        row.SubItems[10].Text = string.IsNullOrEmpty(it.SpeedText) ? "-" : it.SpeedText;
                    continue;
                }

                try
                {
                    var path = ResolveCurrentOutput(it);
                    if (string.IsNullOrEmpty(path) || !File.Exists(path)) continue;
                    var fi = new FileInfo(path);
                    fi.Refresh();
                    var sec = (DateTime.Now - it.LastSizeTime).TotalSeconds;
                    if (sec < 0.8) continue;
                    var delta = fi.Length - it.LastFileSize;
                    if (delta > 0)
                        it.SpeedText = FormatSpeed(delta / sec) + " / " + FormatSize(fi.Length);
                    else if (fi.Length > 0 && it.SpeedText == "连接中")
                        it.SpeedText = FormatSize(fi.Length);
                    it.LastFileSize = fi.Length;
                    it.LastSizeTime = DateTime.Now;
                    row.SubItems[10].Text = it.SpeedText;
                    row.SubItems[5].Text = "录制中";
                }
                catch { }
            }
        }

        private static string? ResolveCurrentOutput(LiveItem it)
        {
            if (!string.IsNullOrEmpty(it.OutputFile) && File.Exists(it.OutputFile))
                return it.OutputFile;
            if (string.IsNullOrEmpty(it.OutputFile)) return null;
            var dir = Path.GetDirectoryName(it.OutputFile);
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return null;
            try
            {
                var files = Directory.GetFiles(dir)
                    .Where(f => f.EndsWith(".flv", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase))
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.LastWriteTime)
                    .FirstOrDefault();
                return files?.FullName;
            }
            catch { return it.OutputFile; }
        }

        private static string FormatSpeed(double bps)
        {
            if (bps < 1024) return bps.ToString("0") + " B/s";
            if (bps < 1024 * 1024) return (bps / 1024).ToString("0.0") + " KB/s";
            return (bps / 1024 / 1024).ToString("0.00") + " MB/s";
        }

        private static string FormatSize(long bytes)
        {
            if (bytes < 1024) return bytes + " B";
            if (bytes < 1024 * 1024) return (bytes / 1024.0).ToString("0.0") + " KB";
            if (bytes < 1024L * 1024 * 1024) return (bytes / 1024.0 / 1024).ToString("0.00") + " MB";
            return (bytes / 1024.0 / 1024 / 1024).ToString("0.00") + " GB";
        }

        private List<LiveItem> GetSelectedItems()
        {
            return liveList.SelectedItems.Cast<ListViewItem>()
                .Select(x => x.Tag as LiveItem).Where(x => x != null).Cast<LiveItem>().ToList();
        }

        private async Task StartRecordSelectedAsync()
        {
            var selected = GetSelectedItems();
            if (selected.Count == 0)
            {
                MessageBox.Show("请先选中直播间", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // 把当前 UI 配置写回选中项
            foreach (var it in selected)
            {
                it.UserStopped = false;     // 清除 UserStopped
                it.Quality = cmbQuality.SelectedItem?.ToString() ?? it.Quality;
                it.IntervalSec = (int)numInterval.Value;
                it.SegmentMin = (int)numSegment.Value;
                if (it.SegmentMin > 0)
                    it.Format = "FLV";
                else
                    it.Format = cmbFormat.SelectedItem?.ToString() ?? it.Format;
            }
            foreach (var it in selected)
                await StartRecordAsync(it);
            UpdateAllRowStatus();   // 不要 RebuildList
        }

        private static string? ResolveFFmpeg(string configured)
        {
            var candidates = new List<string>();
            if (!string.IsNullOrWhiteSpace(configured))
            {
                configured = configured.Trim().Trim('"');
                candidates.Add(configured);
                if (Directory.Exists(configured))
                {
                    candidates.Add(Path.Combine(configured, "ffmpeg.exe"));
                    candidates.Add(Path.Combine(configured, "bin", "ffmpeg.exe"));
                }
            }
            candidates.Add(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg.exe"));
            foreach (var c in candidates)
            {
                try { if (File.Exists(c)) return Path.GetFullPath(c); } catch { }
            }
            return null;
        }

        private async Task StartRecordAsync(LiveItem it)
        {
            it.UserStopped = false;

            if (it.Recorder != null && !it.Recorder.HasExited)
            {
                it.Status = "录制中";
                return;
            }

            it.Status = "解析中";
            UpdateAllRowStatus();
            it.StreamUrl = "";

            try
            {
                var info = await new LiveRoomService(AppSettings.Load().DouyinCookie).ParseAsync(it.Url, it.Quality ?? "原画");
                if (!string.IsNullOrEmpty(info.StreamUrl)) it.StreamUrl = info.StreamUrl;
                it.IsLive = info.IsLive;
                if (!string.IsNullOrEmpty(info.Anchor) && info.Anchor != "未知主播") it.Anchor = info.Anchor;
                if (!string.IsNullOrEmpty(info.RoomName) && info.RoomName != "直播间") it.RoomName = info.RoomName;
            }
            catch { }

            var settings = AppSettings.Load();
            var ffmpeg = ResolveFFmpeg(settings.FFmpegPath);
            if (ffmpeg == null)
            {
                it.Status = "无 ffmpeg";
                MessageBox.Show("未找到 ffmpeg.exe", "需要 ffmpeg", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (string.IsNullOrEmpty(it.StreamUrl))
            {
                it.Status = it.IsLive ? "无流地址" : "未开播";
                if (!it.AutoRecord)
                    MessageBox.Show("未解析到流地址。主播=" + it.Anchor, "无法录制", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 路径只用英文：中文主播名会导致 ffmpeg/cmd 写头失败
            var roomKey = "r";
            var mWeb = Regex.Match(it.Url, @"live\.douyin\.com/([0-9A-Za-z_\-]+)");
            roomKey += mWeb.Success ? mWeb.Groups[1].Value : Math.Abs(it.Url.GetHashCode()).ToString();

            // 目录：中文主播无效则回退房间号；文件名始终英文
            var folderName = MakeSafe(it.Anchor);
            if (string.IsNullOrWhiteSpace(folderName) || folderName.Contains("未知") || folderName == "live")
                folderName = roomKey;
            var dir = Path.Combine(settings.DownloadPath, "Live", folderName);
            Directory.CreateDirectory(dir);

            var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var fileStem = roomKey + "_" + stamp;
            var fmt = (it.Format ?? "MP4").ToUpperInvariant();
            bool segmented = it.SegmentMin > 0;

            // 分段一律 flv（可播放）；不分段才按封装 mp4/flv
            string ext = segmented ? ".flv" : (fmt == "MP4" ? ".mp4" : ".flv");
            string outArg;
            if (segmented)
            {
                outArg = Path.Combine(dir, fileStem + "_%03d" + ext);
                it.OutputFile = Path.Combine(dir, fileStem + "_000" + ext);
            }
            else
            {
                outArg = Path.Combine(dir, fileStem + ext);
                it.OutputFile = outArg;
            }

            it.LastFileSize = 0;
            it.LastSizeTime = DateTime.Now;
            it.SpeedText = "连接中";

            try
            {
                // 直接启动 ffmpeg，禁止经 cmd（避免 %03d 被吃掉、中文路径乱码）
                var argList = new System.Text.StringBuilder();
                argList.Append("-hide_banner -loglevel warning -y ");
                // HTTP 断流时尽量自动重连（网络抖动时减少「录制结束」）
                argList.Append("-reconnect 1 -reconnect_at_eof 1 -reconnect_streamed 1 -reconnect_delay_max 60 ");
                argList.Append("-rw_timeout 30000000 ");
                argList.Append("-user_agent \"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36\" ");
                argList.Append("-headers \"Referer: https://live.douyin.com/\r\n\" ");
                argList.Append("-i \"").Append(it.StreamUrl.Replace("\"", "")).Append("\" ");

                if (segmented)
                {
                    int sec = Math.Max(60, it.SegmentMin * 60);
                    // 关键：-f segment + segment_format flv + 输出 %03d.flv
                    argList.Append("-c copy -f segment -segment_time ").Append(sec);
                    argList.Append(" -segment_format flv -reset_timestamps 1 ");
                    argList.Append("\"").Append(outArg).Append("\"");
                }
                else if (ext == ".mp4")
                {
                    argList.Append("-c copy -movflags +frag_keyframe+empty_moov+default_base_moof -f mp4 ");
                    argList.Append("\"").Append(outArg).Append("\"");
                }
                else
                {
                    argList.Append("-c copy -f flv \"").Append(outArg).Append("\"");
                }

                var psi = new ProcessStartInfo
                {
                    FileName = ffmpeg,
                    Arguments = argList.ToString(),
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    WorkingDirectory = dir
                };

                it.Recorder = Process.Start(psi);
                if (it.Recorder == null) throw new Exception("无法启动 ffmpeg");
                it.StartTime = DateTime.Now;
                it.Status = "录制中";
                try {
                    foreach (ListViewItem row in liveList.Items)
                    {
                        if (row.Tag == it && row.SubItems.Count > 9)
                        {
                            row.SubItems[9].Text = it.Format;
                            break;
                        }
                    }
                } catch { }
                lblTip.Text = "录制中 " + it.Anchor + " [" + (segmented ? "FLV分段" + it.SegmentMin + "分" : fmt) + "] → " + it.OutputFile;

                var proc = it.Recorder;
                _ = Task.Run(() =>
                {
                    try
                    {
                        var err = proc.StandardError.ReadToEnd();
                        proc.WaitForExit();
                        if (IsHandleCreated && !string.IsNullOrWhiteSpace(err) && proc.ExitCode != 0)
                        {
                            var line = err.Trim().Replace("\r", " ").Replace("\n", " ");
                            if (line.Length > 200) line = line.Substring(line.Length - 200);
                            BeginInvoke(new Action(() => { lblTip.Text = "ffmpeg: " + line; }));
                        }
                    }
                    catch { }
                });
            }
            catch (Exception ex)
            {
                it.Status = "启动失败";
                MessageBox.Show("启动失败：\n" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task ReparseSelectedAsync()
        {
            var selected = GetSelectedItems();
            if (selected.Count == 0)
            {
                MessageBox.Show("请先选中一行", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            lblTip.Text = "重新解析中…";
            foreach (var it in selected)
            {
                try
                {
                    it.Status = "解析中";
                    UpdateAllRowStatus();
                    var info = await new LiveRoomService(AppSettings.Load().DouyinCookie).ParseAsync(it.Url, it.Quality ?? "原画");
                    if (!string.IsNullOrEmpty(info.StreamUrl)) it.StreamUrl = info.StreamUrl;
                    it.IsLive = info.IsLive;
                    if (!string.IsNullOrEmpty(info.Anchor) && info.Anchor != "未知主播") it.Anchor = info.Anchor;
                    if (!string.IsNullOrEmpty(info.RoomName) && info.RoomName != "直播间") it.RoomName = info.RoomName;
                    it.Status = info.IsLive
                        ? (string.IsNullOrEmpty(info.StreamUrl) ? "直播中(无直链)" : "直播中(可录制)")
                        : "未开播";
                }
                catch (Exception ex)
                {
                    it.Status = "解析失败";
                    lblTip.Text = "解析失败: " + ex.Message;
                }
            }
            PersistRooms();
            RebuildList();
            lblTip.Text = "重新解析完成";
        }

        private void OpenSelectedRoom()
        {
            var selected = GetSelectedItems();
            if (selected.Count == 0) return;
            try { Process.Start(new ProcessStartInfo(selected[0].Url) { UseShellExecute = true }); } catch { }
        }

        private void StopRecordSelected()
        {
            foreach (var it in GetSelectedItems())
            {
                try { if (it.Recorder != null && !it.Recorder.HasExited) it.Recorder.Kill(true); } catch { }
                try { it.Recorder?.Dispose(); } catch { }
                it.Recorder = null;
                it.UserStopped = true;          // 关键：阻止自动再录
                it.Status = "已停止";
                it.SpeedText = "-";
                it.LastFileSize = 0;
            }
            UpdateAllRowStatus();
            lblTip.Text = "已停止选中项（若曾开启自动录制，需再点「开始录制」或「开启自动录制」才会继续）";
        }

        private void SetAutoRecord(bool on)
        {
            foreach (var it in GetSelectedItems())
            {
                it.AutoRecord = on;
                it.IntervalSec = (int)numInterval.Value;
                if (on)
                {
                    it.UserStopped = false;   // 重新允许自动
                    if (it.Status != "录制中") it.Status = "监控中";
                }
            }
            PersistRooms();
            UpdateAllRowStatus();
        }

        private void RemoveSelected()
        {
            var selected = GetSelectedItems();
            if (selected.Count == 0)
            {
                MessageBox.Show("请先选中要删除的行", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            foreach (var it in selected)
            {
                try { if (it.Recorder != null && !it.Recorder.HasExited) it.Recorder.Kill(true); } catch { }
            }
            var ids = selected.Select(x => x.Id).ToHashSet();
            var urls = selected.Select(x => x.Url).ToHashSet(StringComparer.OrdinalIgnoreCase);
            int before = _items.Count;
            _items.RemoveAll(x => ids.Contains(x.Id) || urls.Contains(x.Url));
            PersistRooms();
            RebuildList();
            lblTip.Text = "已删除 " + (before - _items.Count) + " 条";
        }

        private async Task MonitorTickAsync()
        {
            bool structureChanged = false; // 仅极端情况才整表重建
            var now = DateTime.Now;
            var cookie = AppSettings.Load().DouyinCookie;

            foreach (var it in _items.ToList())
            {
                // 1) 进程已退出
                if (it.Recorder != null && it.Recorder.HasExited)
                {
                    int code = -1;
                    try { code = it.Recorder.ExitCode; } catch { }
                    try { it.Recorder.Dispose(); } catch { }
                    it.Recorder = null;
                    it.SpeedText = "-";
                    it.LastFileSize = 0;
                    if (it.Status == "录制中")
                        it.Status = code == 0 ? "录制结束" : "录制中断";

                    // 用户手动停止过：绝不自动再录
                    if (it.UserStopped)
                        continue;

                    if (it.AutoRecord)
                    {
                        try
                        {
                            var info = await new LiveRoomService(cookie).ParseAsync(it.Url, it.Quality ?? "原画");
                            it.IsLive = info.IsLive;
                            it.StreamUrl = info.StreamUrl ?? "";
                            if (!string.IsNullOrEmpty(info.Anchor) && info.Anchor != "未知主播")
                                it.Anchor = info.Anchor;
                            if (info.IsLive && !string.IsNullOrEmpty(info.StreamUrl))
                                await StartRecordAsync(it);
                            else
                                it.Status = "监控中";
                        }
                        catch { it.Status = "监控中"; }
                    }
                    continue;
                }

                if (it.Recorder != null && !it.Recorder.HasExited)
                    continue;

                // 2) 自动录制巡检
                if (!it.AutoRecord) continue;
                if (it.UserStopped) continue;   // 关键
                if (now < it.NextCheckTime) continue;

                it.NextCheckTime = now.AddSeconds(Math.Max(10, it.IntervalSec));
                try
                {
                    var info = await new LiveRoomService(cookie).ParseAsync(it.Url, it.Quality ?? "原画");
                    it.IsLive = info.IsLive;
                    if (!string.IsNullOrEmpty(info.StreamUrl))
                        it.StreamUrl = info.StreamUrl;
                    if (info.IsLive && !string.IsNullOrEmpty(info.StreamUrl))
                        await StartRecordAsync(it);
                    else if (it.Status != "监控中" && it.Status != "未开播" && it.Status != "已停止")
                    {
                        it.Status = "监控中";
                        it.SpeedText = "-";
                    }
                }
                catch { }
            }

            // 只刷新列，不重建整表（避免跳动）
            try
            {
                if (IsHandleCreated && !IsDisposed)
                {
                    if (InvokeRequired) BeginInvoke(new Action(UpdateAllRowStatus));
                    else UpdateAllRowStatus();
                }
            }
            catch { }

            // structureChanged 预留；当前逻辑不需要 RebuildList
            _ = structureChanged;
        }

        private async Task ConvertDialogAsync()
        {
            var settings = AppSettings.Load();
            var ffmpeg = ResolveFFmpeg(settings.FFmpegPath);
            if (ffmpeg == null)
            {
                MessageBox.Show("未配置 ffmpeg", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var rootLive = Path.Combine(settings.DownloadPath, "Live");
            Directory.CreateDirectory(rootLive);

            using var dlg = new Form
            {
                Text = "格式转换",
                Size = new Size(820, 580),
                MinimumSize = new Size(720, 520),
                StartPosition = FormStartPosition.CenterParent,
                BackColor = Color.FromArgb(32, 34, 42),
                ForeColor = Color.WhiteSmoke,
                MinimizeBox = false,
                MaximizeBox = true,
                Font = new Font("Microsoft YaHei UI", 9F)
            };

            var list = new ListView
            {
                Location = new Point(16, 16),
                Size = new Size(772, 300),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                View = View.Details,
                CheckBoxes = true,
                FullRowSelect = true,
                MultiSelect = true,
                HideSelection = false,
                BackColor = Color.FromArgb(40, 42, 52),
                ForeColor = Color.WhiteSmoke,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Microsoft YaHei UI", 9F)
            };
            list.Columns.Add("", 30);
            list.Columns.Add("文件", 360);
            list.Columns.Add("大小", 90);
            list.Columns.Add("目录", 280);

            void AddPath(string path)
            {
                if (!File.Exists(path)) return;
                var ext = Path.GetExtension(path).ToLowerInvariant();
                if (ext is not (".flv" or ".mp4" or ".mkv" or ".ts")) return;
                foreach (ListViewItem x in list.Items)
                    if (string.Equals(x.Tag as string, path, StringComparison.OrdinalIgnoreCase))
                        return;
                var fi = new FileInfo(path);
                var it = new ListViewItem("") { Tag = path };
                it.SubItems.Add(fi.Name);
                it.SubItems.Add((fi.Length / 1024.0 / 1024.0).ToString("0.0") + " MB");
                it.SubItems.Add(fi.DirectoryName ?? "");
                list.Items.Add(it);
            }

            void ScanDir(string dir)
            {
                try
                {
                    foreach (var f in Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories))
                        AddPath(f);
                }
                catch { }
            }

            ScanDir(rootLive);

            // 浅色按钮，避免深色底上看不清
            Button Mk(string text, int x, int w = 88) => new Button
            {
                Text = text,
                Location = new Point(x, 328),
                Size = new Size(w, 32),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(70, 72, 88),
                ForeColor = Color.White,
                FlatAppearance = { BorderColor = Color.FromArgb(100, 100, 120) },
                Cursor = Cursors.Hand
            };

            var btnSelAll = Mk("全选", 16, 70);
            var btnSelNone = Mk("取消全选", 94, 88);
            var btnDelList = Mk("移出列表", 190, 88);
            var btnDelFile = Mk("删除本地文件", 286, 110);
            var btnAddFile = Mk("添加文件", 404, 88);
            var btnAddDir = Mk("添加目录", 500, 88);

            btnSelAll.Click += (s, e) => { foreach (ListViewItem it in list.Items) it.Checked = true; };
            btnSelNone.Click += (s, e) => { foreach (ListViewItem it in list.Items) it.Checked = false; };
            btnDelList.Click += (s, e) =>
            {
                var del = list.SelectedItems.Cast<ListViewItem>().ToList();
                if (del.Count == 0) del = list.CheckedItems.Cast<ListViewItem>().ToList();
                foreach (var it in del) list.Items.Remove(it);
            };
            btnDelFile.Click += (s, e) =>
            {
                var targets = list.SelectedItems.Cast<ListViewItem>().ToList();
                if (targets.Count == 0) targets = list.CheckedItems.Cast<ListViewItem>().ToList();
                if (targets.Count == 0)
                {
                    MessageBox.Show(dlg, "请先选中或勾选文件", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                if (MessageBox.Show(dlg, "将从磁盘删除 " + targets.Count + " 个文件，不可恢复。确定？", "删除本地文件",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                    return;
                foreach (var it in targets)
                {
                    try
                    {
                        var p = it.Tag as string;
                        if (!string.IsNullOrEmpty(p) && File.Exists(p)) File.Delete(p);
                    }
                    catch { }
                    list.Items.Remove(it);
                }
            };
            btnAddFile.Click += (s, e) =>
            {
                using var ofd = new OpenFileDialog
                {
                    Multiselect = true,
                    Filter = "视频|*.flv;*.mp4;*.mkv;*.ts|全部|*.*"
                };
                if (ofd.ShowDialog(dlg) == DialogResult.OK)
                    foreach (var f in ofd.FileNames) AddPath(f);
            };
            btnAddDir.Click += (s, e) =>
            {
                using var fbd = new FolderBrowserDialog();
                if (fbd.ShowDialog(dlg) == DialogResult.OK && !string.IsNullOrEmpty(fbd.SelectedPath))
                    ScanDir(fbd.SelectedPath);
            };

            var cms = new ContextMenuStrip();
            cms.Items.Add("全选", null, (s, e) => btnSelAll.PerformClick());
            cms.Items.Add("取消全选", null, (s, e) => btnSelNone.PerformClick());
            cms.Items.Add(new ToolStripSeparator());
            cms.Items.Add("移出列表", null, (s, e) => btnDelList.PerformClick());
            cms.Items.Add("删除本地文件", null, (s, e) => btnDelFile.PerformClick());
            list.ContextMenuStrip = cms;
            list.MouseDown += (s, e) =>
            {
                if (e.Button != MouseButtons.Right) return;
                var hit = list.GetItemAt(e.X, e.Y);
                if (hit != null && !hit.Selected)
                {
                    list.SelectedItems.Clear();
                    hit.Selected = true;
                }
            };

            var lblOut = new Label
            {
                Text = "输出格式：",
                Location = new Point(16, 375),
                AutoSize = true,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
                ForeColor = Color.WhiteSmoke
            };
            var cmbOut = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(90, 370),
                Size = new Size(220, 28),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
                BackColor = Color.FromArgb(50, 52, 64),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            // 只保留有意义的三项：flv→mp4/mkv，或压缩
            cmbOut.Items.AddRange(new object[]
            {
                "转 MP4（推荐，无损改封装）",
                "转 MKV（无损改封装）",
                "压缩为更小 MP4（重编码，较慢）"
            });
            cmbOut.SelectedIndex = 0;

            var tip = new Label
            {
                Text = "列表会加载 Live 目录下的 flv/mp4 等。分段录制多为 flv，选「转 MP4」可快速转成更好播放的格式（不重编码）。压缩会明显变慢。",
                Location = new Point(16, 405),
                Size = new Size(772, 36),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                ForeColor = Color.FromArgb(160, 165, 180)
            };

            var bar = new ProgressBar
            {
                Location = new Point(16, 450),
                Size = new Size(772, 20),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            var lbl = new Label
            {
                Text = "就绪",
                Location = new Point(16, 478),
                AutoSize = true,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
                ForeColor = Color.FromArgb(180, 185, 200)
            };

            var btnOk = new Button
            {
                Text = "开始转换",
                Location = new Point(580, 500),
                Size = new Size(100, 34),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(100, 80, 200),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            var btnCancel = new Button
            {
                Text = "关闭",
                Location = new Point(690, 500),
                Size = new Size(80, 34),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(70, 72, 88),
                ForeColor = Color.White,
                DialogResult = DialogResult.Cancel,
                Cursor = Cursors.Hand
            };

            btnOk.Click += async (s, e) =>
            {
                foreach (ListViewItem it in list.SelectedItems)
                    it.Checked = true;

                var jobs = list.CheckedItems.Cast<ListViewItem>()
                    .Select(x => x.Tag as string)
                    .Where(p => !string.IsNullOrEmpty(p) && File.Exists(p!))
                    .Cast<string>()
                    .ToList();

                if (jobs.Count == 0)
                {
                    MessageBox.Show(dlg, "请勾选或选中要转换的文件", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                btnOk.Enabled = false;
                bar.Maximum = Math.Max(1, jobs.Count);
                bar.Value = 0;
                int ok = 0, fail = 0, skip = 0;
                string mode = cmbOut.SelectedItem?.ToString() ?? "";

                for (int i = 0; i < jobs.Count; i++)
                {
                    var src = jobs[i];
                    lbl.Text = "转换中 " + (i + 1) + "/" + jobs.Count + "  " + Path.GetFileName(src);
                    bar.Value = i;
                    Application.DoEvents();

                    try
                    {
                        string dest;
                        string args;
                        bool compress = mode.Contains("压缩");

                        if (mode.Contains("MKV"))
                        {
                            dest = Path.ChangeExtension(src, ".mkv");
                            if (src.EndsWith(".mkv", StringComparison.OrdinalIgnoreCase)) { skip++; continue; }
                            args = "-y -i \"" + src + "\" -c copy \"" + dest + "\"";
                        }
                        else if (compress)
                        {
                            dest = Path.Combine(Path.GetDirectoryName(src) ?? "",
                                Path.GetFileNameWithoutExtension(src) + "_compact.mp4");
                            args = "-y -i \"" + src + "\" -c:v libx264 -preset fast -crf 28 -c:a aac -b:a 128k \"" + dest + "\"";
                        }
                        else // 转 MP4 无损
                        {
                            dest = Path.ChangeExtension(src, ".mp4");
                            if (src.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase)) { skip++; continue; }
                            args = "-y -i \"" + src + "\" -c copy \"" + dest + "\"";
                        }

                        if (File.Exists(dest) && !compress) { skip++; continue; }

                        await Task.Run(() =>
                        {
                            var psi = new ProcessStartInfo
                            {
                                FileName = ffmpeg,
                                Arguments = args,
                                UseShellExecute = false,
                                CreateNoWindow = true
                            };
                            using var proc = Process.Start(psi);
                            proc?.WaitForExit(compress ? 3_600_000 : 600_000);
                            if (proc != null && proc.ExitCode == 0 && File.Exists(dest)) ok++;
                            else fail++;
                        });
                    }
                    catch { fail++; }
                }

                bar.Value = bar.Maximum;
                lbl.Text = "完成：成功 " + ok + "，跳过 " + skip + "，失败 " + fail;
                MessageBox.Show(dlg, lbl.Text, "转换完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
                btnOk.Enabled = true;
            };

            dlg.Controls.AddRange(new Control[]
            {
                list, btnSelAll, btnSelNone, btnDelList, btnDelFile, btnAddFile, btnAddDir,
                lblOut, cmbOut, tip, bar, lbl, btnOk, btnCancel
            });
            dlg.ShowDialog(this.FindForm());
        }

        private static string MakeSafe(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "live";
            // 保留中文，只去 Windows 非法字符
            var s = Regex.Replace(name, @"[\\/:*?""<>|\r\n]", "_").Trim();
            if (s.Length > 40) s = s.Substring(0, 40);
            return string.IsNullOrEmpty(s) ? "live" : s;
        }

        private Panel CreateCard(int x, int y, int w, int h)
        {
            var card = new Panel { Location = new Point(x, y), Size = new Size(w, h), BackColor = AppTheme.CardBackground };
            card.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = new GraphicsPath();
                int d = 20; var b = card.ClientRectangle;
                path.AddArc(b.X, b.Y, d, d, 180, 90);
                path.AddArc(b.Right - d - 1, b.Y, d, d, 270, 90);
                path.AddArc(b.Right - d - 1, b.Bottom - d - 1, d, d, 0, 90);
                path.AddArc(b.X, b.Bottom - d - 1, d, d, 90, 90);
                path.CloseFigure();
                using var brush = new SolidBrush(AppTheme.CardBackground);
                e.Graphics.FillPath(brush, path);
                using var pen = new Pen(AppTheme.Border, 1);
                e.Graphics.DrawPath(pen, path);
            };
            return card;
        }
    }
}
