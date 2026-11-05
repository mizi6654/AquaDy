using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
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
                try { info = await new LiveRoomService(AppSettings.Load().DouyinCookie).ParseAsync(url); }
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
                lblSavePath.Text = "保存：" + Path.Combine(AppSettings.Load().DownloadPath, "Live");
            }
            finally { _refreshing = false; }
        }

        private void UpdateAllRowStatus()
        {
            foreach (ListViewItem row in liveList.Items)
            {
                if (row.Tag is not LiveItem it || row.SubItems.Count < 11) continue;
                row.SubItems[1].Text = string.IsNullOrEmpty(it.Anchor) ? "-" : it.Anchor;
                row.SubItems[2].Text = string.IsNullOrEmpty(it.RoomName) ? "-" : it.RoomName;
                row.SubItems[3].Text = it.AutoRecord ? ("是/" + it.IntervalSec + "秒") : "否";
                row.SubItems[4].Text = it.Quality;
                row.SubItems[5].Text = it.Status;
                row.SubItems[9].Text = it.Format;
                row.SubItems[10].Text = it.SpeedText;
            }
        }

        private static void SyncProcessState(LiveItem it)
        {
            if (it.Recorder == null) return;
            if (!it.Recorder.HasExited) return;
            int code = -1;
            try { code = it.Recorder.ExitCode; } catch { }
            it.Recorder = null;
            if (it.Status == "录制中")
                it.Status = code == 0 ? "录制结束" : "录制中断";
            it.SpeedText = "-";
        }

        private void UpdateSpeedsOnly()
        {
            foreach (ListViewItem row in liveList.Items)
            {
                if (row.Tag is not LiveItem it) continue;
                SyncProcessState(it);
                if (it.Recorder == null || it.Recorder.HasExited)
                {
                    if (row.SubItems.Count >= 11 && row.SubItems[5].Text == "录制中")
                        row.SubItems[5].Text = it.Status;
                    continue;
                }
                try
                {
                    // 分段时可能是 pattern，取目录最新文件
                    var path = ResolveCurrentOutput(it);
                    if (string.IsNullOrEmpty(path) || !File.Exists(path)) continue;
                    var fi = new FileInfo(path);
                    // 强制刷新
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
                    if (row.SubItems.Count >= 11)
                        row.SubItems[10].Text = it.SpeedText;
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
            RebuildList();
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
                var info = await new LiveRoomService(AppSettings.Load().DouyinCookie).ParseAsync(it.Url);
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
                argList.Append("-rw_timeout 15000000 ");
                argList.Append("-user_agent \"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36\" ");
                argList.Append("-referer \"https://live.douyin.com/\" ");
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
                    var info = await new LiveRoomService(AppSettings.Load().DouyinCookie).ParseAsync(it.Url);
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
                it.Recorder = null;
                it.Status = "录制取消";
                it.SpeedText = "-";
            }
            RebuildList();
        }

        private void SetAutoRecord(bool on)
        {
            foreach (var it in GetSelectedItems())
            {
                it.AutoRecord = on;
                it.IntervalSec = (int)numInterval.Value;
                if (on && it.Status != "录制中") it.Status = "监控中";
            }
            PersistRooms();
            RebuildList();
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
            bool changed = false;
            var now = DateTime.Now;
            foreach (var it in _items.ToList())
            {
                if (it.Recorder != null && it.Recorder.HasExited)
                {
                    int code = -1;
                    try { code = it.Recorder.ExitCode; } catch { }
                    it.Recorder = null;
                    it.SpeedText = "-";
                    if (it.Status == "录制中")
                        it.Status = code == 0 ? "录制结束" : "录制中断";
                    changed = true;

                    if (it.AutoRecord)
                    {
                        try
                        {
                            var info = await new LiveRoomService(AppSettings.Load().DouyinCookie).ParseAsync(it.Url);
                            it.IsLive = info.IsLive;
                            it.StreamUrl = info.StreamUrl ?? "";
                            if (info.IsLive && !string.IsNullOrEmpty(info.StreamUrl))
                            {
                                await StartRecordAsync(it);
                                changed = true;
                            }
                            else it.Status = "监控中";
                        }
                        catch { it.Status = "监控中"; }
                    }
                    continue;
                }

                if (it.Recorder != null && !it.Recorder.HasExited) continue;
                if (!it.AutoRecord) continue;
                if (now < it.NextCheckTime) continue;

                it.NextCheckTime = now.AddSeconds(Math.Max(10, it.IntervalSec));
                try
                {
                    var info = await new LiveRoomService(AppSettings.Load().DouyinCookie).ParseAsync(it.Url);
                    it.IsLive = info.IsLive;
                    if (!string.IsNullOrEmpty(info.StreamUrl)) it.StreamUrl = info.StreamUrl;
                    if (info.IsLive && !string.IsNullOrEmpty(info.StreamUrl))
                    {
                        await StartRecordAsync(it);
                        changed = true;
                    }
                    else if (it.Status != "未开播" && it.Status != "监控中")
                    {
                        it.Status = "监控中";
                        changed = true;
                    }
                }
                catch { }
            }
            if (changed && IsHandleCreated && !IsDisposed)
                BeginInvoke(new Action(UpdateAllRowStatus));
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

            using var dlg = new Form
            {
                Text = "视频格式转换",
                Size = new Size(640, 460),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.FromArgb(32, 32, 40),
                ForeColor = Color.White
            };

            var list = new CheckedListBox
            {
                Location = new Point(12, 12),
                Size = new Size(600, 240),
                CheckOnClick = true,
                BackColor = Color.FromArgb(45, 45, 55),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            void AddFiles(IEnumerable<string> files)
            {
                foreach (var f in files)
                {
                    bool exists = false;
                    foreach (var item in list.Items)
                        if (string.Equals(item.ToString(), f, StringComparison.OrdinalIgnoreCase)) { exists = true; break; }
                    if (!exists) list.Items.Add(f, true);
                }
            }

            // 预填 Live 目录 flv/mp4
            var root = Path.Combine(settings.DownloadPath, "Live");
            if (Directory.Exists(root))
            {
                AddFiles(Directory.GetFiles(root, "*.flv", SearchOption.AllDirectories));
                AddFiles(Directory.GetFiles(root, "*.mp4", SearchOption.AllDirectories));
            }

            var btnAddFile = new Button
            {
                Text = "添加文件", Location = new Point(12, 260), Size = new Size(100, 32),
                BackColor = Color.FromArgb(90, 90, 120), ForeColor = Color.White, FlatStyle = FlatStyle.Flat
            };
            btnAddFile.Click += (s, e) =>
            {
                using var ofd = new OpenFileDialog
                {
                    Multiselect = true,
                    Filter = "视频|*.flv;*.mp4;*.ts;*.mkv;*.avi;*.wmv|所有|*.*"
                };
                if (ofd.ShowDialog() == DialogResult.OK) AddFiles(ofd.FileNames);
            };

            var btnAddDir = new Button
            {
                Text = "添加目录", Location = new Point(120, 260), Size = new Size(100, 32),
                BackColor = Color.FromArgb(90, 90, 120), ForeColor = Color.White, FlatStyle = FlatStyle.Flat
            };
            btnAddDir.Click += (s, e) =>
            {
                using var fbd = new FolderBrowserDialog();
                if (fbd.ShowDialog() == DialogResult.OK && Directory.Exists(fbd.SelectedPath))
                {
                    AddFiles(Directory.GetFiles(fbd.SelectedPath, "*.*", SearchOption.AllDirectories)
                        .Where(f => Regex.IsMatch(f, @"\.(flv|mp4|ts|mkv|avi|wmv)$", RegexOptions.IgnoreCase)));
                }
            };

            var lblOut = new Label
            {
                Text = "输出格式", Location = new Point(240, 266), AutoSize = true, ForeColor = Color.White
            };
            var cmbOut = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(310, 262), Size = new Size(120, 28),
                BackColor = Color.FromArgb(45, 45, 55), ForeColor = Color.White, FlatStyle = FlatStyle.Flat
            };
            // copy 无损 / 重编码更小
            cmbOut.Items.AddRange(new object[]
            {
                "MP4(复制)",
                "MKV(复制)"
            });
            cmbOut.SelectedIndex = 0;

            var bar = new ProgressBar
            {
                Location = new Point(12, 305), Size = new Size(600, 22),
                Maximum = 1
            };
            var lbl = new Label
            {
                Location = new Point(12, 335), Size = new Size(600, 24),
                Text = "可从 Live 目录自动载入，也可手动添加文件/目录。复制=快且无损；压缩=更小但慢。",
                ForeColor = Color.LightGray
            };

            var btnOk = new Button
            {
                Text = "开始转换", Location = new Point(400, 370), Size = new Size(120, 36),
                BackColor = Color.FromArgb(120, 80, 200), ForeColor = Color.White, FlatStyle = FlatStyle.Flat
            };
            var btnCancel = new Button
            {
                Text = "关闭", Location = new Point(530, 370), Size = new Size(80, 36),
                BackColor = Color.FromArgb(70, 70, 80), ForeColor = Color.White, FlatStyle = FlatStyle.Flat
            };
            btnCancel.Click += (s, e) => dlg.Close();

            btnOk.Click += async (s, e) =>
            {
                var selected = list.CheckedItems.Cast<string>().ToList();
                if (selected.Count == 0)
                {
                    MessageBox.Show("请勾选文件", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                btnOk.Enabled = false;
                int ok = 0, fail = 0, i = 0;
                bar.Maximum = Math.Max(1, selected.Count);
                bar.Value = 0;
                string mode = cmbOut.SelectedItem?.ToString() ?? "MP4(复制)";

                foreach (var src in selected)
                {
                    i++;
                    bar.Value = Math.Min(i, bar.Maximum);
                    lbl.Text = "(" + i + "/" + selected.Count + ") " + Path.GetFileName(src);
                    Application.DoEvents();

                    string args;
                    string dest;
                    try
                    {
                        if (mode.StartsWith("MP4(复制)"))
                        {
                            dest = Path.ChangeExtension(src, ".mp4");
                            if (src.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase)) { ok++; continue; }
                            args = "-y -i \"" + src + "\" -c copy \"" + dest + "\"";
                        }
                        else if (mode.StartsWith("FLV(复制)"))
                        {
                            dest = Path.ChangeExtension(src, ".flv");
                            if (src.EndsWith(".flv", StringComparison.OrdinalIgnoreCase)) { ok++; continue; }
                            args = "-y -i \"" + src + "\" -c copy \"" + dest + "\"";
                        }
                        else if (mode.StartsWith("MKV(复制)"))
                        {
                            dest = Path.ChangeExtension(src, ".mkv");
                            args = "-y -i \"" + src + "\" -c copy \"" + dest + "\"";
                        }
                        else if (mode.StartsWith("MP4(压缩)"))
                        {
                            dest = Path.Combine(Path.GetDirectoryName(src) ?? "", Path.GetFileNameWithoutExtension(src) + "_compact.mp4");
                            args = "-y -i \"" + src + "\" -c:v libx264 -preset fast -crf 28 -c:a aac -b:a 128k \"" + dest + "\"";
                        }
                        else if (mode.StartsWith("AVI(压缩)"))
                        {
                            dest = Path.ChangeExtension(src, ".avi");
                            args = "-y -i \"" + src + "\" -c:v libx264 -preset fast -crf 28 -c:a aac \"" + dest + "\"";
                        }
                        else // WMV
                        {
                            dest = Path.ChangeExtension(src, ".wmv");
                            args = "-y -i \"" + src + "\" -c:v wmv2 -b:v 1M -c:a wmav2 -b:a 128k \"" + dest + "\"";
                        }

                        if (File.Exists(dest) && mode.Contains("复制")) { ok++; continue; }

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
                            proc?.WaitForExit(mode.Contains("压缩") ? 3600000 : 600000);
                            if (proc != null && proc.ExitCode == 0 && File.Exists(dest)) ok++;
                            else fail++;
                        });
                    }
                    catch { fail++; }
                }

                lbl.Text = "完成：成功 " + ok + "，失败 " + fail;
                MessageBox.Show(lbl.Text, "转换完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
                btnOk.Enabled = true;
            };

            dlg.Controls.AddRange(new Control[] { list, btnAddFile, btnAddDir, lblOut, cmbOut, bar, lbl, btnOk, btnCancel });
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
