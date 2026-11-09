using System.Drawing;
using System.Windows.Forms;
using AquaDy.Controls;
using AquaDy.Models;
using AquaDy.Services;

namespace AquaDy.Forms
{
    partial class AccountPage
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Label titleLabel;
        private System.Windows.Forms.Label subLabel;
        private System.Windows.Forms.Panel card;
        private System.Windows.Forms.Label lblUrl;
        private AquaDy.Controls.DarkInputPanel inputUrl;
        private AquaDy.Controls.DarkButton btnParse;
        private System.Windows.Forms.Label lblMode;
        private System.Windows.Forms.RadioButton radioBrowser;
        private System.Windows.Forms.RadioButton radioApi;
        private System.Windows.Forms.Label lblType;
        private System.Windows.Forms.CheckBox chkWantPost;
        private System.Windows.Forms.CheckBox chkWantMix;
        private System.Windows.Forms.CheckBox chkFullMode;
        private System.Windows.Forms.CheckBox chkIncremental;
        private System.Windows.Forms.Label lblMax;
        private System.Windows.Forms.NumericUpDown numMaxPages;
        private System.Windows.Forms.Label lblParseResult;
        private System.Windows.Forms.ProgressBar parseProgress;
        private System.Windows.Forms.ListView resultList;
        private System.Windows.Forms.ColumnHeader colIdx;
        private System.Windows.Forms.ColumnHeader colUrl;
        private System.Windows.Forms.ColumnHeader colTitle;
        private System.Windows.Forms.ColumnHeader colAuthor;
        private System.Windows.Forms.ColumnHeader colDesc;
        private System.Windows.Forms.ColumnHeader colPublishTime;
        private System.Windows.Forms.ColumnHeader colType;
        private System.Windows.Forms.ColumnHeader colStatus;
        private System.Windows.Forms.ColumnHeader colProgress;
        private AquaDy.Controls.DarkButton btnDownload;
        private AquaDy.Controls.DarkButton btnSelectAll;
        private AquaDy.Controls.DarkButton btnDeselect;
        private AquaDy.Controls.DarkButton btnPause;
        private AquaDy.Controls.DarkButton btnCancel;
        private AquaDy.Controls.DarkButton btnClearList;
        private System.Windows.Forms.ContextMenuStrip cms;
        private System.Windows.Forms.ToolStripMenuItem cmsDownload;
        private System.Windows.Forms.ToolStripMenuItem cmsDelete;
        private System.Windows.Forms.ToolStripMenuItem cmsSelectAll;
        private System.Windows.Forms.ToolStripMenuItem cmsDeselectAll;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.titleLabel = new System.Windows.Forms.Label();
            this.subLabel = new System.Windows.Forms.Label();
            this.card = new System.Windows.Forms.Panel();
            this.lblUrl = new System.Windows.Forms.Label();
            this.inputUrl = new AquaDy.Controls.DarkInputPanel();
            this.btnParse = new AquaDy.Controls.DarkButton();
            this.lblMode = new System.Windows.Forms.Label();
            this.radioBrowser = new System.Windows.Forms.RadioButton();
            this.radioApi = new System.Windows.Forms.RadioButton();
            this.lblType = new System.Windows.Forms.Label();
            this.chkWantPost = new System.Windows.Forms.CheckBox();
            this.chkWantMix = new System.Windows.Forms.CheckBox();
            this.chkFullMode = new System.Windows.Forms.CheckBox();
            this.chkIncremental = new System.Windows.Forms.CheckBox();
            this.lblMax = new System.Windows.Forms.Label();
            this.numMaxPages = new System.Windows.Forms.NumericUpDown();
            this.lblParseResult = new System.Windows.Forms.Label();
            this.parseProgress = new System.Windows.Forms.ProgressBar();
            this.resultList = new System.Windows.Forms.ListView();
            this.colIdx = new System.Windows.Forms.ColumnHeader();
            this.colUrl = new System.Windows.Forms.ColumnHeader();
            this.colTitle = new System.Windows.Forms.ColumnHeader();
            this.colAuthor = new System.Windows.Forms.ColumnHeader();
            this.colDesc = new System.Windows.Forms.ColumnHeader();
            this.colPublishTime = new System.Windows.Forms.ColumnHeader();
            this.colType = new System.Windows.Forms.ColumnHeader();
            this.colStatus = new System.Windows.Forms.ColumnHeader();
            this.colProgress = new System.Windows.Forms.ColumnHeader();
            this.btnDownload = new AquaDy.Controls.DarkButton();
            this.btnSelectAll = new AquaDy.Controls.DarkButton();
            this.btnDeselect = new AquaDy.Controls.DarkButton();
            this.btnPause = new AquaDy.Controls.DarkButton();
            this.btnCancel = new AquaDy.Controls.DarkButton();
            this.btnClearList = new AquaDy.Controls.DarkButton();
            this.cms = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.cmsDownload = new System.Windows.Forms.ToolStripMenuItem();
            this.cmsDelete = new System.Windows.Forms.ToolStripMenuItem();
            this.cmsSelectAll = new System.Windows.Forms.ToolStripMenuItem();
            this.cmsDeselectAll = new System.Windows.Forms.ToolStripMenuItem();

            this.card.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numMaxPages)).BeginInit();
            this.cms.SuspendLayout();
            this.SuspendLayout();

            // titleLabel
            this.titleLabel.AutoSize = true;
            this.titleLabel.Font = new System.Drawing.Font("Microsoft YaHei UI", 16F, System.Drawing.FontStyle.Bold);
            this.titleLabel.ForeColor = AppTheme.TextPrimary;
            this.titleLabel.Location = new System.Drawing.Point(24, 16);
            this.titleLabel.Name = "titleLabel";
            this.titleLabel.Text = "账号批量下载";

            // subLabel
            this.subLabel.AutoSize = true;
            this.subLabel.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.subLabel.ForeColor = AppTheme.TextSecondary;
            this.subLabel.Location = new System.Drawing.Point(24, 48);
            this.subLabel.Name = "subLabel";
            this.subLabel.Text = "支持：① 主页链接 → 发布作品；② 合集链接（douyin.com/collection/数字）→ 合集内作品。可多行粘贴多个合集。";

            // card
            this.card.BackColor = AppTheme.CardBackground;
            this.card.Controls.Add(this.numMaxPages);
            this.card.Controls.Add(this.lblMax);
            this.card.Controls.Add(this.chkIncremental);
            this.card.Controls.Add(this.chkFullMode);
            this.card.Controls.Add(this.chkWantMix);
            this.card.Controls.Add(this.chkWantPost);
            this.card.Controls.Add(this.lblType);
            this.card.Controls.Add(this.radioApi);
            this.card.Controls.Add(this.radioBrowser);
            this.card.Controls.Add(this.lblMode);
            this.card.Controls.Add(this.btnParse);
            this.card.Controls.Add(this.inputUrl);
            this.card.Controls.Add(this.lblUrl);
            this.card.Location = new System.Drawing.Point(24, 80);
            this.card.Name = "card";
            this.card.Size = new System.Drawing.Size(1012, 320);
            this.card.TabIndex = 2;

            // lblUrl
            this.lblUrl.AutoSize = true;
            this.lblUrl.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.lblUrl.ForeColor = AppTheme.TextSecondary;
            this.lblUrl.Location = new System.Drawing.Point(20, 16);
            this.lblUrl.Name = "lblUrl";
            this.lblUrl.Text = "主页链接 或 合集链接（可多行）";

            // inputUrl —— 关键：Multiline 必须在 InitializeComponent 内与 Placeholder/Size 一同设置，
            // 与原始代码顺序一致；否则 DarkInputPanel 在 Load 时才切换为多行，Placeholder/Text 行为异常。
            this.inputUrl.Anchor = (AnchorStyles)((AnchorStyles.Top | AnchorStyles.Left) | AnchorStyles.Right);
            this.inputUrl.Location = new System.Drawing.Point(20, 42);
            this.inputUrl.Multiline = true;
            this.inputUrl.Name = "inputUrl";
            this.inputUrl.Placeholder = "粘贴抖音或 TikTok 账号主页链接...";
            this.inputUrl.Size = new System.Drawing.Size(972, 100);
            this.inputUrl.TabIndex = 1;

            // btnParse —— 位于"页内接口翻页"单选按钮右侧（Y=148，与 radio 行同一视觉区域）
            this.btnParse.Anchor = (AnchorStyles)(AnchorStyles.Top | AnchorStyles.Left);
            this.btnParse.IsPrimary = true;
            this.btnParse.Location = new System.Drawing.Point(400, 148);
            this.btnParse.Name = "btnParse";
            this.btnParse.Size = new System.Drawing.Size(110, 40);
            this.btnParse.TabIndex = 2;
            this.btnParse.Text = "开始解析";
            this.btnParse.Click += new System.EventHandler(this.btnParse_Click);

            // lblMode
            this.lblMode.AutoSize = true;
            this.lblMode.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.lblMode.ForeColor = AppTheme.TextSecondary;
            this.lblMode.Location = new System.Drawing.Point(20, 155);
            this.lblMode.Name = "lblMode";
            this.lblMode.Text = "解析方式";

            // radioBrowser
            this.radioBrowser.AutoSize = true;
            this.radioBrowser.BackColor = System.Drawing.Color.Transparent;
            this.radioBrowser.Checked = true;
            this.radioBrowser.ForeColor = AppTheme.TextPrimary;
            this.radioBrowser.Location = new System.Drawing.Point(90, 153);
            this.radioBrowser.Name = "radioBrowser";
            this.radioBrowser.TabIndex = 3;
            this.radioBrowser.TabStop = true;
            this.radioBrowser.Text = "浏览器拦截（推荐）";

            // radioApi
            this.radioApi.AutoSize = true;
            this.radioApi.BackColor = System.Drawing.Color.Transparent;
            this.radioApi.ForeColor = AppTheme.TextPrimary;
            this.radioApi.Location = new System.Drawing.Point(250, 153);
            this.radioApi.Name = "radioApi";
            this.radioApi.TabIndex = 4;
            this.radioApi.Text = "页内接口翻页";

            // lblType
            this.lblType.AutoSize = true;
            this.lblType.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.lblType.ForeColor = AppTheme.TextSecondary;
            this.lblType.Location = new System.Drawing.Point(20, 185);
            this.lblType.Name = "lblType";
            this.lblType.Text = "作品类型";

            // chkWantPost —— Y=207，与 chkFullMode 完全对齐
            this.chkWantPost.AutoSize = true;
            this.chkWantPost.Checked = true;
            this.chkWantPost.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkWantPost.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.chkWantPost.ForeColor = AppTheme.TextPrimary;
            this.chkWantPost.Location = new System.Drawing.Point(20, 207);
            this.chkWantPost.Name = "chkWantPost";
            this.chkWantPost.TabIndex = 5;
            this.chkWantPost.Text = "发布作品";

            // chkWantMix —— Y=233，与 chkIncremental 完全对齐（中心距 26px）
            this.chkWantMix.AutoSize = true;
            this.chkWantMix.Checked = true;
            this.chkWantMix.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkWantMix.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.chkWantMix.ForeColor = AppTheme.TextPrimary;
            this.chkWantMix.Location = new System.Drawing.Point(20, 233);
            this.chkWantMix.Name = "chkWantMix";
            this.chkWantMix.TabIndex = 6;
            this.chkWantMix.Text = "合集作品";

            // chkFullMode
            this.chkFullMode.AutoSize = true;
            this.chkFullMode.Checked = true;
            this.chkFullMode.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkFullMode.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.chkFullMode.ForeColor = AppTheme.TextPrimary;
            this.chkFullMode.Location = new System.Drawing.Point(220, 207);
            this.chkFullMode.Name = "chkFullMode";
            this.chkFullMode.TabIndex = 7;
            this.chkFullMode.Text = "完整模式（直到无更多数据）";

            // chkIncremental
            this.chkIncremental.AutoSize = true;
            this.chkIncremental.Checked = true;
            this.chkIncremental.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkIncremental.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.chkIncremental.ForeColor = AppTheme.TextPrimary;
            this.chkIncremental.Location = new System.Drawing.Point(220, 233);
            this.chkIncremental.Name = "chkIncremental";
            this.chkIncremental.TabIndex = 8;
            this.chkIncremental.Text = "增量下载（跳过已下载）";

            // lblMax
            this.lblMax.AutoSize = true;
            this.lblMax.ForeColor = AppTheme.TextSecondary;
            this.lblMax.Location = new System.Drawing.Point(400, 235);
            this.lblMax.Name = "lblMax";
            this.lblMax.Text = "最大页数(0=不限)";

            // numMaxPages
            this.numMaxPages.BackColor = AppTheme.CardBackground;
            this.numMaxPages.ForeColor = AppTheme.TextPrimary;
            this.numMaxPages.Location = new System.Drawing.Point(520, 232);
            this.numMaxPages.Maximum = new decimal(new int[] { 500, 0, 0, 0 });
            this.numMaxPages.Name = "numMaxPages";
            this.numMaxPages.Size = new System.Drawing.Size(56, 25);
            this.numMaxPages.TabIndex = 9;

            // lblParseResult
            this.lblParseResult.AutoSize = true;
            this.lblParseResult.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.lblParseResult.ForeColor = AppTheme.TextMuted;
            this.lblParseResult.Location = new System.Drawing.Point(24, 410);
            this.lblParseResult.Name = "lblParseResult";
            this.lblParseResult.Text = "解析完成后将显示：浏览器验证数量 vs 实际获取数量";

            // parseProgress
            this.parseProgress.Anchor = (AnchorStyles)((AnchorStyles.Top | AnchorStyles.Left) | AnchorStyles.Right);
            this.parseProgress.Location = new System.Drawing.Point(24, 435);
            this.parseProgress.Maximum = 100;
            this.parseProgress.Name = "parseProgress";
            this.parseProgress.Size = new System.Drawing.Size(1012, 8);
            this.parseProgress.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.parseProgress.TabIndex = 4;
            this.parseProgress.Visible = false;

            // colIdx / 各列
            this.colIdx.Text = "#"; this.colIdx.Width = 40;
            this.colUrl.Text = "作品链接"; this.colUrl.Width = 220;
            this.colTitle.Text = "标题"; this.colTitle.Width = 120;
            this.colAuthor.Text = "作者"; this.colAuthor.Width = 80;
            this.colDesc.Text = "描述"; this.colDesc.Width = 140;
            this.colPublishTime.Text = "发布时间"; this.colPublishTime.Width = 120;
            this.colType.Text = "类型"; this.colType.Width = 60;
            this.colStatus.Text = "状态"; this.colStatus.Width = 70;
            this.colProgress.Text = "进度"; this.colProgress.Width = 70;

            // resultList
            this.resultList.Anchor = (AnchorStyles)(((AnchorStyles.Top | AnchorStyles.Bottom) | AnchorStyles.Left) | AnchorStyles.Right);
            this.resultList.BackColor = AppTheme.CardBackground;
            this.resultList.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.resultList.CheckBoxes = true;
            this.resultList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
                this.colIdx, this.colUrl, this.colTitle, this.colAuthor, this.colDesc,
                this.colPublishTime, this.colType, this.colStatus, this.colProgress });
            this.resultList.ContextMenuStrip = this.cms;
            this.resultList.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.resultList.ForeColor = AppTheme.TextPrimary;
            this.resultList.FullRowSelect = true;
            this.resultList.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.resultList.Location = new System.Drawing.Point(24, 455);
            this.resultList.MultiSelect = true;
            this.resultList.Name = "resultList";
            this.resultList.Size = new System.Drawing.Size(1012, 210);
            this.resultList.TabIndex = 5;
            this.resultList.UseCompatibleStateImageBehavior = false;
            this.resultList.View = System.Windows.Forms.View.Details;
            this.resultList.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.resultList_MouseDoubleClick);

            // btnDownload
            this.btnDownload.Anchor = (AnchorStyles)((AnchorStyles.Bottom | AnchorStyles.Left));
            this.btnDownload.IsPrimary = true;
            this.btnDownload.Location = new System.Drawing.Point(24, 700);
            this.btnDownload.Name = "btnDownload";
            this.btnDownload.Size = new System.Drawing.Size(140, 40);
            this.btnDownload.TabIndex = 6;
            this.btnDownload.Text = "下载选中作品";
            this.btnDownload.Click += new System.EventHandler(this.btnDownload_Click);

            // btnSelectAll
            this.btnSelectAll.Anchor = (AnchorStyles)((AnchorStyles.Bottom | AnchorStyles.Left));
            this.btnSelectAll.Location = new System.Drawing.Point(180, 700);
            this.btnSelectAll.Name = "btnSelectAll";
            this.btnSelectAll.Size = new System.Drawing.Size(80, 40);
            this.btnSelectAll.TabIndex = 7;
            this.btnSelectAll.Text = "全选";
            this.btnSelectAll.Click += new System.EventHandler(this.btnSelectAll_Click);

            // btnDeselect
            this.btnDeselect.Anchor = (AnchorStyles)((AnchorStyles.Bottom | AnchorStyles.Left));
            this.btnDeselect.Location = new System.Drawing.Point(270, 700);
            this.btnDeselect.Name = "btnDeselect";
            this.btnDeselect.Size = new System.Drawing.Size(90, 40);
            this.btnDeselect.TabIndex = 8;
            this.btnDeselect.Text = "取消全选";
            this.btnDeselect.Click += new System.EventHandler(this.btnDeselect_Click);

            // btnPause
            this.btnPause.Anchor = (AnchorStyles)((AnchorStyles.Bottom | AnchorStyles.Left));
            this.btnPause.Location = new System.Drawing.Point(370, 700);
            this.btnPause.Name = "btnPause";
            this.btnPause.Size = new System.Drawing.Size(100, 40);
            this.btnPause.TabIndex = 9;
            this.btnPause.Text = "暂停/继续";
            this.btnPause.Click += new System.EventHandler(this.btnPause_Click);

            // btnCancel
            this.btnCancel.Anchor = (AnchorStyles)((AnchorStyles.Bottom | AnchorStyles.Left));
            this.btnCancel.Location = new System.Drawing.Point(478, 700);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(100, 40);
            this.btnCancel.TabIndex = 10;
            this.btnCancel.Text = "取消下载";
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);

            // btnClearList
            this.btnClearList.Anchor = (AnchorStyles)((AnchorStyles.Bottom | AnchorStyles.Left));
            this.btnClearList.Location = new System.Drawing.Point(586, 700);
            this.btnClearList.Name = "btnClearList";
            this.btnClearList.Size = new System.Drawing.Size(90, 40);
            this.btnClearList.TabIndex = 11;
            this.btnClearList.Text = "清空列表";
            this.btnClearList.Click += new System.EventHandler(this.btnClearList_Click);

            // cmsDownload
            this.cmsDownload.Name = "cmsDownload";
            this.cmsDownload.Size = new System.Drawing.Size(124, 22);
            this.cmsDownload.Text = "下载选中";
            this.cmsDownload.Click += new System.EventHandler(this.cmsDownload_Click);

            // cmsDelete
            this.cmsDelete.Name = "cmsDelete";
            this.cmsDelete.Size = new System.Drawing.Size(124, 22);
            this.cmsDelete.Text = "删除选中";
            this.cmsDelete.Click += new System.EventHandler(this.cmsDelete_Click);

            // cmsSelectAll
            this.cmsSelectAll.Name = "cmsSelectAll";
            this.cmsSelectAll.Size = new System.Drawing.Size(124, 22);
            this.cmsSelectAll.Text = "全选";
            this.cmsSelectAll.Click += new System.EventHandler(this.cmsSelectAll_Click);

            // cmsDeselectAll
            this.cmsDeselectAll.Name = "cmsDeselectAll";
            this.cmsDeselectAll.Size = new System.Drawing.Size(124, 22);
            this.cmsDeselectAll.Text = "取消全选";
            this.cmsDeselectAll.Click += new System.EventHandler(this.cmsDeselectAll_Click);

            // cms
            this.cms.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.cmsDownload, this.cmsDelete, this.cmsSelectAll, this.cmsDeselectAll });
            this.cms.Name = "cms";
            this.cms.Size = new System.Drawing.Size(128, 92);

            // AccountPage
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = AppTheme.Background;
            this.Controls.Add(this.titleLabel);
            this.Controls.Add(this.subLabel);
            this.Controls.Add(this.card);
            this.Controls.Add(this.lblParseResult);
            this.Controls.Add(this.parseProgress);
            this.Controls.Add(this.resultList);
            this.Controls.Add(this.btnDownload);
            this.Controls.Add(this.btnSelectAll);
            this.Controls.Add(this.btnDeselect);
            this.Controls.Add(this.btnPause);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnClearList);
            this.DoubleBuffered = true;
            this.Name = "AccountPage";
            this.Padding = new System.Windows.Forms.Padding(24, 20, 24, 20);
            this.Size = new System.Drawing.Size(1060, 770);
            this.Load += new System.EventHandler(this.AccountPage_Load);
            this.Resize += new System.EventHandler(this.AccountPage_Resize);

            this.card.ResumeLayout(false);
            this.card.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numMaxPages)).EndInit();
            this.cms.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion
    }
}