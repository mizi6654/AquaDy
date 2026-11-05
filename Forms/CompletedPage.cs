using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using AquaDy.Controls;
using AquaDy.Models;
using AquaDy.Services;

namespace AquaDy.Forms
{
    public class CompletedPage : UserControl
    {
        private ListView historyList;
        private DarkButton btnSelectAll, btnDeselect, btnRedownload, btnPause, btnOpenFolder, btnClear, btnRefresh;
        private DownloadService? _activeDownloader;
        private bool _batchPaused;

        public CompletedPage()
        {
            InitializeComponent();
            Load += (s, e) => Reload();
            VisibleChanged += (s, e) => { if (Visible) Reload(); };
        }

        private void InitializeComponent()
        {
            BackColor = AppTheme.Background;
            Padding = new Padding(24, 20, 24, 20);

            var title = new Label
            {
                Text = "已完成 / 历史记录",
                Font = new Font("Microsoft YaHei UI", 16F, FontStyle.Bold),
                ForeColor = AppTheme.TextPrimary,
                AutoSize = true,
                Location = new Point(24, 16)
            };
            var sub = new Label
            {
                Text = "记录「下载任务」与「账号批量」的成功/失败结果。可勾选后重新下载。",
                Font = new Font("Microsoft YaHei UI", 9F),
                ForeColor = AppTheme.TextSecondary,
                AutoSize = true,
                Location = new Point(24, 48)
            };

            btnSelectAll = MkBtn("全选", 24, 90, 70);
            btnSelectAll.Click += (s, e) => { foreach (ListViewItem i in historyList.Items) i.Checked = true; };
            btnDeselect = MkBtn("取消全选", 104, 90, 90);
            btnDeselect.Click += (s, e) => { foreach (ListViewItem i in historyList.Items) i.Checked = false; };
            btnRedownload = MkBtn("重新下载", 204, 90, 100);
            btnRedownload.IsPrimary = true;
            btnRedownload.Click += BtnRedownload_Click;
            btnPause = MkBtn("暂停/继续", 314, 90, 100);
            btnPause.Click += BtnPause_Click;
            var btnCancelDone = MkBtn("取消下载", 420, 90, 100);
            btnCancelDone.Click += (s, e) =>
            {
                var items = historyList.CheckedItems.Count > 0
                    ? historyList.CheckedItems.Cast<ListViewItem>().ToList()
                    : historyList.SelectedItems.Cast<ListViewItem>().ToList();
                foreach (var it in items)
                {
                    if (it.Tag is DownloadTask t)
                    {
                        t.Status = DownloadTaskStatus.Cancelled;
                        if (it.SubItems.Count > 4) it.SubItems[4].Text = "已取消";
                    }
                }
            };
            btnRefresh = MkBtn("刷新", 424, 90, 70);
            btnRefresh.Click += (s, e) => Reload();
            btnOpenFolder = MkBtn("打开目录", 504, 90, 100);
            btnOpenFolder.Click += (s, e) =>
            {
                var path = AppSettings.Load().DownloadPath;
                if (!System.IO.Directory.Exists(path)) System.IO.Directory.CreateDirectory(path);
                try { System.Diagnostics.Process.Start("explorer.exe", path); } catch { }
            };
            btnClear = MkBtn("清空历史", 614, 90, 100);
            btnClear.Click += (s, e) =>
            {
                if (MessageBox.Show("确定清空全部历史？", "确认", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    HistoryStore.Clear();
                    Reload();
                }
            };

            historyList = new ListView
            {
                Location = new Point(24, 140),
                Size = new Size(Width - 48, Height - 180),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                View = View.Details,
                FullRowSelect = true,
                BorderStyle = BorderStyle.None,
                BackColor = AppTheme.CardBackground,
                ForeColor = AppTheme.TextPrimary,
                Font = new Font("Microsoft YaHei UI", 9F),
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
                CheckBoxes = true
            };
            historyList.Columns.Add("#", 40);
            historyList.Columns.Add("链接", 280);
            historyList.Columns.Add("标题", 140);
            historyList.Columns.Add("作者", 90);
            historyList.Columns.Add("状态", 70);
            historyList.Columns.Add("大小", 80);
            historyList.Columns.Add("时间", 120);
            historyList.Columns.Add("路径", 200);
            var cms = new ContextMenuStrip();
            cms.Items.Add("重新下载选中", null, (s, e) =>
            {
                foreach (ListViewItem it in historyList.SelectedItems) it.Checked = true;
                BtnRedownload_Click(s, e);
            });
            cms.Items.Add("删除选中", null, (s, e) =>
            {
                var del = historyList.SelectedItems.Cast<ListViewItem>().ToList();
                if (del.Count == 0) del = historyList.CheckedItems.Cast<ListViewItem>().ToList();
                foreach (var it in del) historyList.Items.Remove(it);
                int n = 1;
                foreach (ListViewItem it in historyList.Items) { it.Text = n.ToString(); n++; }
            });
            cms.Items.Add("全选", null, (s, e) => { foreach (ListViewItem it in historyList.Items) it.Checked = true; });
            cms.Items.Add("取消全选", null, (s, e) => { foreach (ListViewItem it in historyList.Items) it.Checked = false; });
            historyList.ContextMenuStrip = cms;

            historyList.MouseDoubleClick += (s, e) =>
            {
                if (e.Button != MouseButtons.Left) return;
                if (historyList.SelectedItems.Count == 0) return;
                if (historyList.SelectedItems[0].Tag is DownloadTask task && !string.IsNullOrEmpty(task.Url))
                {
                    try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(task.Url) { UseShellExecute = true }); }
                    catch { }
                }
            };

            Controls.AddRange(new Control[] {
                title, sub, btnSelectAll, btnDeselect, btnRedownload, btnPause, btnCancelDone, btnRefresh, btnOpenFolder, btnClear, historyList
            });
        }

        private static DarkButton MkBtn(string text, int x, int y, int w) => new DarkButton
        {
            Text = text, Size = new Size(w, 34), Location = new Point(x, y)
        };

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

        private void BtnPause_Click(object? sender, EventArgs e)
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

        private async void BtnRedownload_Click(object? sender, EventArgs e)
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
            try
            {
                var settings = AppSettings.Load();
                var parser = new LinkParser(settings.DouyinCookie, settings.TikTokCookie);
                var downloader = new DownloadService(settings);
                _activeDownloader = downloader;
                _batchPaused = false;

                foreach (var task in selected)
                {
                    task.Status = DownloadTaskStatus.Parsing;
                    task.Progress = 0;
                    task.ErrorMessage = "";
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
                Reload();
                MessageBox.Show("重新下载流程已结束，请查看列表状态", "完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
    }
}
