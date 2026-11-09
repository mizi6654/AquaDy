using System.Drawing;
using System.Windows.Forms;
using AquaDy.Controls;
using AquaDy.Models;
using AquaDy.Services;

namespace AquaDy.Forms
{
    partial class LivePage
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Label titleLabel;
        private System.Windows.Forms.Label subLabel;
        private System.Windows.Forms.Panel card;
        private System.Windows.Forms.Label lblUrlCaption;
        private AquaDy.Controls.DarkInputPanel inputUrl;
        private AquaDy.Controls.DarkButton btnAdd;
        private System.Windows.Forms.Label lblQ;
        private System.Windows.Forms.ComboBox cmbQuality;
        private System.Windows.Forms.Label lblFmt;
        private System.Windows.Forms.ComboBox cmbFormat;
        private System.Windows.Forms.Label lblIv;
        private System.Windows.Forms.NumericUpDown numInterval;
        private System.Windows.Forms.CheckBox chkAutoDefault;
        private System.Windows.Forms.Label lblSeg;
        private System.Windows.Forms.NumericUpDown numSegment;
        private System.Windows.Forms.Label lblSegTip;
        private System.Windows.Forms.Label lblSavePath;
        private System.Windows.Forms.Label lblTip;
        private System.Windows.Forms.ListView liveList;
        private System.Windows.Forms.ColumnHeader colIdx;
        private System.Windows.Forms.ColumnHeader colAnchor;
        private System.Windows.Forms.ColumnHeader colRoom;
        private System.Windows.Forms.ColumnHeader colAuto;
        private System.Windows.Forms.ColumnHeader colQuality;
        private System.Windows.Forms.ColumnHeader colStatus;
        private System.Windows.Forms.ColumnHeader colStart;
        private System.Windows.Forms.ColumnHeader colCreate;
        private System.Windows.Forms.ColumnHeader colUrl;
        private System.Windows.Forms.ColumnHeader colFormat;
        private System.Windows.Forms.ColumnHeader colSpeed;
        private AquaDy.Controls.DarkButton btnStart;
        private AquaDy.Controls.DarkButton btnStop;
        private AquaDy.Controls.DarkButton btnDel;
        private AquaDy.Controls.DarkButton btnOpen;
        private AquaDy.Controls.DarkButton btnConvert;
        private System.Windows.Forms.ContextMenuStrip cms;
        private System.Windows.Forms.ToolStripMenuItem cmsStart;
        private System.Windows.Forms.ToolStripMenuItem cmsStop;
        private System.Windows.Forms.ToolStripMenuItem cmsReparse;
        private System.Windows.Forms.ToolStripMenuItem cmsOpenRoom;
        private System.Windows.Forms.ToolStripSeparator cmsSep1;
        private System.Windows.Forms.ToolStripMenuItem cmsAutoOn;
        private System.Windows.Forms.ToolStripMenuItem cmsAutoOff;
        private System.Windows.Forms.ToolStripSeparator cmsSep2;
        private System.Windows.Forms.ToolStripMenuItem cmsDelete;

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
            this.lblUrlCaption = new System.Windows.Forms.Label();
            this.inputUrl = new AquaDy.Controls.DarkInputPanel();
            this.btnAdd = new AquaDy.Controls.DarkButton();
            this.lblQ = new System.Windows.Forms.Label();
            this.cmbQuality = new System.Windows.Forms.ComboBox();
            this.lblFmt = new System.Windows.Forms.Label();
            this.cmbFormat = new System.Windows.Forms.ComboBox();
            this.lblIv = new System.Windows.Forms.Label();
            this.numInterval = new System.Windows.Forms.NumericUpDown();
            this.chkAutoDefault = new System.Windows.Forms.CheckBox();
            this.lblSeg = new System.Windows.Forms.Label();
            this.numSegment = new System.Windows.Forms.NumericUpDown();
            this.lblSegTip = new System.Windows.Forms.Label();
            this.lblSavePath = new System.Windows.Forms.Label();
            this.lblTip = new System.Windows.Forms.Label();
            this.liveList = new System.Windows.Forms.ListView();
            this.colIdx = new System.Windows.Forms.ColumnHeader();
            this.colAnchor = new System.Windows.Forms.ColumnHeader();
            this.colRoom = new System.Windows.Forms.ColumnHeader();
            this.colAuto = new System.Windows.Forms.ColumnHeader();
            this.colQuality = new System.Windows.Forms.ColumnHeader();
            this.colStatus = new System.Windows.Forms.ColumnHeader();
            this.colStart = new System.Windows.Forms.ColumnHeader();
            this.colCreate = new System.Windows.Forms.ColumnHeader();
            this.colUrl = new System.Windows.Forms.ColumnHeader();
            this.colFormat = new System.Windows.Forms.ColumnHeader();
            this.colSpeed = new System.Windows.Forms.ColumnHeader();
            this.btnStart = new AquaDy.Controls.DarkButton();
            this.btnStop = new AquaDy.Controls.DarkButton();
            this.btnDel = new AquaDy.Controls.DarkButton();
            this.btnOpen = new AquaDy.Controls.DarkButton();
            this.btnConvert = new AquaDy.Controls.DarkButton();
            this.cms = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.cmsStart = new System.Windows.Forms.ToolStripMenuItem();
            this.cmsStop = new System.Windows.Forms.ToolStripMenuItem();
            this.cmsReparse = new System.Windows.Forms.ToolStripMenuItem();
            this.cmsOpenRoom = new System.Windows.Forms.ToolStripMenuItem();
            this.cmsSep1 = new System.Windows.Forms.ToolStripSeparator();
            this.cmsAutoOn = new System.Windows.Forms.ToolStripMenuItem();
            this.cmsAutoOff = new System.Windows.Forms.ToolStripMenuItem();
            this.cmsSep2 = new System.Windows.Forms.ToolStripSeparator();
            this.cmsDelete = new System.Windows.Forms.ToolStripMenuItem();

            this.card.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numInterval)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numSegment)).BeginInit();
            this.cms.SuspendLayout();
            this.SuspendLayout();

            // titleLabel
            this.titleLabel.AutoSize = true;
            this.titleLabel.Font = new System.Drawing.Font("Microsoft YaHei UI", 16F, System.Drawing.FontStyle.Bold);
            this.titleLabel.ForeColor = AppTheme.TextPrimary;
            this.titleLabel.Location = new System.Drawing.Point(24, 16);
            this.titleLabel.Name = "titleLabel";
            this.titleLabel.Text = "直播下载";

            // subLabel
            this.subLabel.AutoSize = true;
            this.subLabel.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.subLabel.ForeColor = AppTheme.TextSecondary;
            this.subLabel.Location = new System.Drawing.Point(24, 48);
            this.subLabel.Name = "subLabel";
            this.subLabel.Text = "监控间隔单位=秒（≥10）。分段=分钟（0=不分段）。选中行可改封装/清晰度，下次开录生效。";

            // card
            this.card.BackColor = AppTheme.CardBackground;
            this.card.Controls.Add(this.lblSegTip);
            this.card.Controls.Add(this.numSegment);
            this.card.Controls.Add(this.lblSeg);
            this.card.Controls.Add(this.chkAutoDefault);
            this.card.Controls.Add(this.numInterval);
            this.card.Controls.Add(this.lblIv);
            this.card.Controls.Add(this.cmbFormat);
            this.card.Controls.Add(this.lblFmt);
            this.card.Controls.Add(this.cmbQuality);
            this.card.Controls.Add(this.lblQ);
            this.card.Controls.Add(this.btnAdd);
            this.card.Controls.Add(this.inputUrl);
            this.card.Controls.Add(this.lblUrlCaption);
            this.card.Location = new System.Drawing.Point(24, 80);
            this.card.Name = "card";
            this.card.Size = new System.Drawing.Size(1012, 118);
            this.card.TabIndex = 2;

            // lblUrlCaption
            this.lblUrlCaption.AutoSize = true;
            this.lblUrlCaption.ForeColor = AppTheme.TextSecondary;
            this.lblUrlCaption.Location = new System.Drawing.Point(20, 10);
            this.lblUrlCaption.Name = "lblUrlCaption";
            this.lblUrlCaption.Text = "直播间链接";

            // inputUrl
            this.inputUrl.Anchor = (AnchorStyles)(((AnchorStyles.Top | AnchorStyles.Left) | AnchorStyles.Right));
            this.inputUrl.Location = new System.Drawing.Point(20, 32);
            this.inputUrl.Name = "inputUrl";
            this.inputUrl.Placeholder = "https://live.douyin.com/房间号";
            this.inputUrl.Size = new System.Drawing.Size(862, 34);
            this.inputUrl.TabIndex = 1;

            // btnAdd
            this.btnAdd.Anchor = (AnchorStyles)((AnchorStyles.Top | AnchorStyles.Right));
            this.btnAdd.IsPrimary = true;
            this.btnAdd.Location = new System.Drawing.Point(892, 32);
            this.btnAdd.Name = "btnAdd";
            this.btnAdd.Size = new System.Drawing.Size(100, 34);
            this.btnAdd.TabIndex = 2;
            this.btnAdd.Text = "添加监控";
            this.btnAdd.Click += new System.EventHandler(this.btnAdd_Click);

            // lblQ
            this.lblQ.AutoSize = true;
            this.lblQ.ForeColor = AppTheme.TextSecondary;
            this.lblQ.Location = new System.Drawing.Point(20, 78);
            this.lblQ.Name = "lblQ";
            this.lblQ.Text = "清晰度";

            // cmbQuality
            this.cmbQuality.BackColor = AppTheme.CardBackground;
            this.cmbQuality.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbQuality.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cmbQuality.ForeColor = AppTheme.TextPrimary;
            this.cmbQuality.Items.AddRange(new object[] { "原画", "超清", "高清", "标清" });
            this.cmbQuality.Location = new System.Drawing.Point(70, 74);
            this.cmbQuality.Name = "cmbQuality";
            this.cmbQuality.Size = new System.Drawing.Size(72, 25);
            this.cmbQuality.TabIndex = 3;
            this.cmbQuality.SelectedIndexChanged += new System.EventHandler(this.cmbQuality_SelectedIndexChanged);

            // lblFmt
            this.lblFmt.AutoSize = true;
            this.lblFmt.ForeColor = AppTheme.TextSecondary;
            this.lblFmt.Location = new System.Drawing.Point(150, 78);
            this.lblFmt.Name = "lblFmt";
            this.lblFmt.Text = "封装";

            // cmbFormat
            this.cmbFormat.BackColor = AppTheme.CardBackground;
            this.cmbFormat.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbFormat.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cmbFormat.ForeColor = AppTheme.TextPrimary;
            this.cmbFormat.Items.AddRange(new object[] { "FLV", "MP4" });
            this.cmbFormat.Location = new System.Drawing.Point(190, 74);
            this.cmbFormat.Name = "cmbFormat";
            this.cmbFormat.Size = new System.Drawing.Size(64, 25);
            this.cmbFormat.TabIndex = 4;
            this.cmbFormat.SelectedIndexChanged += new System.EventHandler(this.cmbFormat_SelectedIndexChanged);

            // lblIv
            this.lblIv.AutoSize = true;
            this.lblIv.ForeColor = AppTheme.TextSecondary;
            this.lblIv.Location = new System.Drawing.Point(265, 78);
            this.lblIv.Name = "lblIv";
            this.lblIv.Text = "监控(秒)";

            // numInterval
            this.numInterval.BackColor = AppTheme.CardBackground;
            this.numInterval.ForeColor = AppTheme.TextPrimary;
            this.numInterval.Increment = new decimal(new int[] { 5, 0, 0, 0 });
            this.numInterval.Location = new System.Drawing.Point(330, 74);
            this.numInterval.Maximum = new decimal(new int[] { 3600, 0, 0, 0 });
            this.numInterval.Minimum = new decimal(new int[] { 10, 0, 0, 0 });
            this.numInterval.Name = "numInterval";
            this.numInterval.Size = new System.Drawing.Size(58, 25);
            this.numInterval.TabIndex = 5;
            this.numInterval.Value = new decimal(new int[] { 10, 0, 0, 0 });
            this.numInterval.ValueChanged += new System.EventHandler(this.numInterval_ValueChanged);

            // chkAutoDefault
            this.chkAutoDefault.AutoSize = true;
            this.chkAutoDefault.ForeColor = AppTheme.TextPrimary;
            this.chkAutoDefault.Location = new System.Drawing.Point(400, 76);
            this.chkAutoDefault.Name = "chkAutoDefault";
            this.chkAutoDefault.TabIndex = 6;
            this.chkAutoDefault.Text = "添加后自动录制";

            // lblSeg
            this.lblSeg.AutoSize = true;
            this.lblSeg.ForeColor = AppTheme.TextSecondary;
            this.lblSeg.Location = new System.Drawing.Point(540, 78);
            this.lblSeg.Name = "lblSeg";
            this.lblSeg.Text = "分段(分)";

            // numSegment
            this.numSegment.BackColor = AppTheme.CardBackground;
            this.numSegment.ForeColor = AppTheme.TextPrimary;
            this.numSegment.Increment = new decimal(new int[] { 5, 0, 0, 0 });
            this.numSegment.Location = new System.Drawing.Point(600, 74);
            this.numSegment.Maximum = new decimal(new int[] { 240, 0, 0, 0 });
            this.numSegment.Name = "numSegment";
            this.numSegment.Size = new System.Drawing.Size(50, 25);
            this.numSegment.TabIndex = 7;
            this.numSegment.Value = new decimal(new int[] { 30, 0, 0, 0 });
            this.numSegment.ValueChanged += new System.EventHandler(this.numSegment_ValueChanged);

            // lblSegTip
            this.lblSegTip.AutoSize = true;
            this.lblSegTip.ForeColor = AppTheme.TextMuted;
            this.lblSegTip.Location = new System.Drawing.Point(655, 78);
            this.lblSegTip.Name = "lblSegTip";
            this.lblSegTip.Text = "0=不分段；>0强制FLV";

            // lblSavePath
            this.lblSavePath.AutoSize = true;
            this.lblSavePath.ForeColor = AppTheme.TextMuted;
            this.lblSavePath.Location = new System.Drawing.Point(24, 206);
            this.lblSavePath.Name = "lblSavePath";
            this.lblSavePath.Text = "保存：";

            // lblTip
            this.lblTip.AutoSize = true;
            this.lblTip.ForeColor = AppTheme.TextMuted;
            this.lblTip.Location = new System.Drawing.Point(24, 226);
            this.lblTip.Name = "lblTip";
            this.lblTip.Text = "就绪。修改清晰度/封装/分段后，对选中行下次「开始录制」生效（不会自动重录）。";

            // 列头
            this.colIdx.Text = "#"; this.colIdx.Width = 36;
            this.colAnchor.Text = "主播信息"; this.colAnchor.Width = 110;
            this.colRoom.Text = "直播间名称"; this.colRoom.Width = 120;
            this.colAuto.Text = "自动录制"; this.colAuto.Width = 90;
            this.colQuality.Text = "清晰度"; this.colQuality.Width = 55;
            this.colStatus.Text = "录制状态"; this.colStatus.Width = 90;
            this.colStart.Text = "开始时间"; this.colStart.Width = 115;
            this.colCreate.Text = "创建时间"; this.colCreate.Width = 115;
            this.colUrl.Text = "链接"; this.colUrl.Width = 140;
            this.colFormat.Text = "封装"; this.colFormat.Width = 50;
            this.colSpeed.Text = "下载速度"; this.colSpeed.Width = 90;

            // liveList
            this.liveList.Anchor = (AnchorStyles)(((AnchorStyles.Top | AnchorStyles.Bottom) | AnchorStyles.Left) | AnchorStyles.Right);
            this.liveList.BackColor = AppTheme.CardBackground;
            this.liveList.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.liveList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
                this.colIdx, this.colAnchor, this.colRoom, this.colAuto, this.colQuality, this.colStatus,
                this.colStart, this.colCreate, this.colUrl, this.colFormat, this.colSpeed });
            this.liveList.ContextMenuStrip = this.cms;
            this.liveList.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.liveList.ForeColor = AppTheme.TextPrimary;
            this.liveList.FullRowSelect = true;
            this.liveList.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.liveList.HideSelection = false;
            this.liveList.Location = new System.Drawing.Point(24, 250);
            this.liveList.MultiSelect = true;
            this.liveList.Name = "liveList";
            this.liveList.Size = new System.Drawing.Size(1012, 435);
            this.liveList.TabIndex = 5;
            this.liveList.UseCompatibleStateImageBehavior = false;
            this.liveList.View = System.Windows.Forms.View.Details;
            this.liveList.ItemSelectionChanged += new System.Windows.Forms.ListViewItemSelectionChangedEventHandler(this.liveList_ItemSelectionChanged);
            this.liveList.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.liveList_MouseDoubleClick);
            this.liveList.MouseDown += new System.Windows.Forms.MouseEventHandler(this.liveList_MouseDown);

            // 5 个底部按钮（MkBtn 展开）
            this.btnStart.Anchor = (AnchorStyles)((AnchorStyles.Bottom | AnchorStyles.Left));
            this.btnStart.IsPrimary = true;
            this.btnStart.Location = new System.Drawing.Point(24, 710);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(110, 36);
            this.btnStart.TabIndex = 6;
            this.btnStart.Text = "开始录制";
            this.btnStart.Click += new System.EventHandler(this.btnStart_Click);

            this.btnStop.Anchor = (AnchorStyles)((AnchorStyles.Bottom | AnchorStyles.Left));
            this.btnStop.Location = new System.Drawing.Point(146, 710);
            this.btnStop.Name = "btnStop";
            this.btnStop.Size = new System.Drawing.Size(110, 36);
            this.btnStop.TabIndex = 7;
            this.btnStop.Text = "停止录制";
            this.btnStop.Click += new System.EventHandler(this.btnStop_Click);

            this.btnDel.Anchor = (AnchorStyles)((AnchorStyles.Bottom | AnchorStyles.Left));
            this.btnDel.Location = new System.Drawing.Point(268, 710);
            this.btnDel.Name = "btnDel";
            this.btnDel.Size = new System.Drawing.Size(110, 36);
            this.btnDel.TabIndex = 8;
            this.btnDel.Text = "删除选中";
            this.btnDel.Click += new System.EventHandler(this.btnDel_Click);

            this.btnOpen.Anchor = (AnchorStyles)((AnchorStyles.Bottom | AnchorStyles.Left));
            this.btnOpen.Location = new System.Drawing.Point(390, 710);
            this.btnOpen.Name = "btnOpen";
            this.btnOpen.Size = new System.Drawing.Size(120, 36);
            this.btnOpen.TabIndex = 9;
            this.btnOpen.Text = "打开保存目录";
            this.btnOpen.Click += new System.EventHandler(this.btnOpen_Click);

            this.btnConvert.Anchor = (AnchorStyles)((AnchorStyles.Bottom | AnchorStyles.Left));
            this.btnConvert.Location = new System.Drawing.Point(522, 710);
            this.btnConvert.Name = "btnConvert";
            this.btnConvert.Size = new System.Drawing.Size(110, 36);
            this.btnConvert.TabIndex = 10;
            this.btnConvert.Text = "格式转换";
            this.btnConvert.Click += new System.EventHandler(this.btnConvert_Click);

            // cms
            this.cmsStart.Text = "开始录制"; this.cmsStart.Name = "cmsStart"; this.cmsStart.Size = new System.Drawing.Size(160, 22);
            this.cmsStart.Click += new System.EventHandler(this.cmsStart_Click);
            this.cmsStop.Text = "停止录制"; this.cmsStop.Name = "cmsStop"; this.cmsStop.Size = new System.Drawing.Size(160, 22);
            this.cmsStop.Click += new System.EventHandler(this.cmsStop_Click);
            this.cmsReparse.Text = "重新解析"; this.cmsReparse.Name = "cmsReparse"; this.cmsReparse.Size = new System.Drawing.Size(160, 22);
            this.cmsReparse.Click += new System.EventHandler(this.cmsReparse_Click);
            this.cmsOpenRoom.Text = "打开直播间"; this.cmsOpenRoom.Name = "cmsOpenRoom"; this.cmsOpenRoom.Size = new System.Drawing.Size(160, 22);
            this.cmsOpenRoom.Click += new System.EventHandler(this.cmsOpenRoom_Click);
            this.cmsSep1.Name = "cmsSep1"; this.cmsSep1.Size = new System.Drawing.Size(157, 6);
            this.cmsAutoOn.Text = "开启自动录制"; this.cmsAutoOn.Name = "cmsAutoOn"; this.cmsAutoOn.Size = new System.Drawing.Size(160, 22);
            this.cmsAutoOn.Click += new System.EventHandler(this.cmsAutoOn_Click);
            this.cmsAutoOff.Text = "关闭自动录制"; this.cmsAutoOff.Name = "cmsAutoOff"; this.cmsAutoOff.Size = new System.Drawing.Size(160, 22);
            this.cmsAutoOff.Click += new System.EventHandler(this.cmsAutoOff_Click);
            this.cmsSep2.Name = "cmsSep2"; this.cmsSep2.Size = new System.Drawing.Size(157, 6);
            this.cmsDelete.Text = "删除选中"; this.cmsDelete.Name = "cmsDelete"; this.cmsDelete.Size = new System.Drawing.Size(160, 22);
            this.cmsDelete.Click += new System.EventHandler(this.cmsDelete_Click);

            this.cms.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.cmsStart, this.cmsStop, this.cmsReparse, this.cmsOpenRoom,
                this.cmsSep1, this.cmsAutoOn, this.cmsAutoOff, this.cmsSep2, this.cmsDelete });
            this.cms.Name = "cms";
            this.cms.Size = new System.Drawing.Size(164, 170);

            // LivePage
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = AppTheme.Background;
            this.Controls.Add(this.titleLabel);
            this.Controls.Add(this.subLabel);
            this.Controls.Add(this.card);
            this.Controls.Add(this.lblSavePath);
            this.Controls.Add(this.lblTip);
            this.Controls.Add(this.liveList);
            this.Controls.Add(this.btnStart);
            this.Controls.Add(this.btnStop);
            this.Controls.Add(this.btnDel);
            this.Controls.Add(this.btnOpen);
            this.Controls.Add(this.btnConvert);
            this.Name = "LivePage";
            this.Padding = new System.Windows.Forms.Padding(24, 20, 24, 20);
            this.Size = new System.Drawing.Size(1060, 770);
            this.Load += new System.EventHandler(this.LivePage_Load);
            this.Resize += new System.EventHandler(this.LivePage_Resize);
            this.VisibleChanged += new System.EventHandler(this.LivePage_VisibleChanged);

            this.card.ResumeLayout(false);
            this.card.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numInterval)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numSegment)).EndInit();
            this.cms.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion
    }
}