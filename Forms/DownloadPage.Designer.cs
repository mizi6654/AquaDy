using System.Drawing;
using System.Windows.Forms;
using AquaDy.Controls;
using AquaDy.Models;
using AquaDy.Services;

namespace AquaDy.Forms
{
    partial class DownloadPage
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Label titleLabel;
        private System.Windows.Forms.Label subTitle;
        private System.Windows.Forms.Panel cardNew;
        private System.Windows.Forms.Label cardTitle;
        private System.Windows.Forms.TextBox txtLinks;
        private System.Windows.Forms.ComboBox cmbQuality;
        private System.Windows.Forms.CheckBox chkCover;
        private AquaDy.Controls.DarkButton btnAdd;
        private System.Windows.Forms.Label listHeader;
        private System.Windows.Forms.Panel toolPanel;
        private AquaDy.Controls.DarkButton btnSelectAll;
        private AquaDy.Controls.DarkButton btnDeselect;
        private AquaDy.Controls.DarkButton btnDownloadSelected;
        private AquaDy.Controls.DarkButton btnDownloadAll;
        private AquaDy.Controls.DarkButton btnPause;
        private AquaDy.Controls.DarkButton btnCancel;
        private AquaDy.Controls.DarkButton btnClear;
        private System.Windows.Forms.ListView taskList;
        private System.Windows.Forms.ColumnHeader colIdx;
        private System.Windows.Forms.ColumnHeader colUrl;
        private System.Windows.Forms.ColumnHeader colTitle;
        private System.Windows.Forms.ColumnHeader colAuthor;
        private System.Windows.Forms.ColumnHeader colDescription;
        private System.Windows.Forms.ColumnHeader colPublishTime;
        private System.Windows.Forms.ColumnHeader colStatus;
        private System.Windows.Forms.ColumnHeader colProgress;
        private System.Windows.Forms.ColumnHeader colSize;
        private System.Windows.Forms.Panel detailPanel;
        private System.Windows.Forms.Label detailTitle;
        private System.Windows.Forms.Label lblDetailTitle;
        private System.Windows.Forms.Label lblDetailInfo;
        private System.Windows.Forms.ProgressBar detailProgress;
        private AquaDy.Controls.DarkButton btnOpenLink;
        private AquaDy.Controls.DarkButton btnOpenFolder;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        private void InitializeComponent()
        {
            this.titleLabel = new System.Windows.Forms.Label();
            this.subTitle = new System.Windows.Forms.Label();
            this.cardNew = new System.Windows.Forms.Panel();
            this.cardTitle = new System.Windows.Forms.Label();
            this.txtLinks = new System.Windows.Forms.TextBox();
            this.cmbQuality = new System.Windows.Forms.ComboBox();
            this.chkCover = new System.Windows.Forms.CheckBox();
            this.btnAdd = new AquaDy.Controls.DarkButton();
            this.listHeader = new System.Windows.Forms.Label();
            this.toolPanel = new System.Windows.Forms.Panel();
            this.btnSelectAll = new AquaDy.Controls.DarkButton();
            this.btnDeselect = new AquaDy.Controls.DarkButton();
            this.btnDownloadSelected = new AquaDy.Controls.DarkButton();
            this.btnDownloadAll = new AquaDy.Controls.DarkButton();
            this.btnPause = new AquaDy.Controls.DarkButton();
            this.btnCancel = new AquaDy.Controls.DarkButton();
            this.btnClear = new AquaDy.Controls.DarkButton();
            this.taskList = new System.Windows.Forms.ListView();
            this.colIdx = new System.Windows.Forms.ColumnHeader();
            this.colUrl = new System.Windows.Forms.ColumnHeader();
            this.colTitle = new System.Windows.Forms.ColumnHeader();
            this.colAuthor = new System.Windows.Forms.ColumnHeader();
            this.colDescription = new System.Windows.Forms.ColumnHeader();
            this.colPublishTime = new System.Windows.Forms.ColumnHeader();
            this.colStatus = new System.Windows.Forms.ColumnHeader();
            this.colProgress = new System.Windows.Forms.ColumnHeader();
            this.colSize = new System.Windows.Forms.ColumnHeader();
            this.detailPanel = new System.Windows.Forms.Panel();
            this.detailTitle = new System.Windows.Forms.Label();
            this.lblDetailTitle = new System.Windows.Forms.Label();
            this.lblDetailInfo = new System.Windows.Forms.Label();
            this.detailProgress = new System.Windows.Forms.ProgressBar();
            this.btnOpenLink = new AquaDy.Controls.DarkButton();
            this.btnOpenFolder = new AquaDy.Controls.DarkButton();

            this.cardNew.SuspendLayout();
            this.toolPanel.SuspendLayout();
            this.detailPanel.SuspendLayout();
            this.SuspendLayout();

            // titleLabel
            this.titleLabel.AutoSize = true;
            this.titleLabel.Font = new System.Drawing.Font("Microsoft YaHei UI", 16F, System.Drawing.FontStyle.Bold);
            this.titleLabel.ForeColor = AppTheme.TextPrimary;
            this.titleLabel.Location = new System.Drawing.Point(24, 16);
            this.titleLabel.Name = "titleLabel";
            this.titleLabel.Text = "下载任务";

            // subTitle
            this.subTitle.AutoSize = true;
            this.subTitle.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.subTitle.ForeColor = AppTheme.TextSecondary;
            this.subTitle.Location = new System.Drawing.Point(24, 48);
            this.subTitle.Name = "subTitle";
            this.subTitle.Text = "仅支持单个作品链接（www.douyin.com/video/… 或 v.douyin.com/…）。主页/合集请到「账号批量」。";

            // cardNew
            this.cardNew.BackColor = AppTheme.CardBackground;
            this.cardNew.Controls.Add(this.cardTitle);
            this.cardNew.Controls.Add(this.txtLinks);
            this.cardNew.Controls.Add(this.cmbQuality);
            this.cardNew.Controls.Add(this.chkCover);
            this.cardNew.Controls.Add(this.btnAdd);
            this.cardNew.Location = new System.Drawing.Point(24, 80);
            this.cardNew.Name = "cardNew";
            this.cardNew.Size = new System.Drawing.Size(764, 200);
            this.cardNew.TabIndex = 2;

            // cardTitle
            this.cardTitle.AutoSize = true;
            this.cardTitle.Font = new System.Drawing.Font("Microsoft YaHei UI", 10F, System.Drawing.FontStyle.Bold);
            this.cardTitle.ForeColor = AppTheme.TextPrimary;
            this.cardTitle.Location = new System.Drawing.Point(16, 12);
            this.cardTitle.Name = "cardTitle";
            this.cardTitle.Text = "新建下载 · 支持单条作品 / 批量链接（每行一条）";

            // txtLinks
            this.txtLinks.AcceptsReturn = true;
            this.txtLinks.BackColor = System.Drawing.Color.FromArgb(28, 28, 36);
            this.txtLinks.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtLinks.Font = new System.Drawing.Font("Consolas", 9.5F);
            this.txtLinks.ForeColor = AppTheme.TextPrimary;
            this.txtLinks.Location = new System.Drawing.Point(16, 42);
            this.txtLinks.Multiline = true;
            this.txtLinks.Name = "txtLinks";
            this.txtLinks.PlaceholderText = "每行粘贴一条作品链接，例如：\r\nhttps://www.douyin.com/video/7687902923120532723\r\nhttps://v.douyin.com/xxxxx/";
            this.txtLinks.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtLinks.Size = new System.Drawing.Size(614, 100);
            this.txtLinks.TabIndex = 1;
            this.txtLinks.WordWrap = false;

            // cmbQuality
            this.cmbQuality.BackColor = AppTheme.CardBackground;
            this.cmbQuality.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbQuality.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cmbQuality.ForeColor = AppTheme.TextPrimary;
            this.cmbQuality.Items.AddRange(new object[] { "最高画质 (自动)", "1080P", "720P", "原画" });
            this.cmbQuality.Location = new System.Drawing.Point(16, 155);
            this.cmbQuality.Name = "cmbQuality";
            this.cmbQuality.SelectedIndex = 0;
            this.cmbQuality.Size = new System.Drawing.Size(160, 28);
            this.cmbQuality.TabIndex = 2;

            // chkCover
            this.chkCover.AutoSize = true;
            this.chkCover.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.chkCover.ForeColor = AppTheme.TextSecondary;
            this.chkCover.Location = new System.Drawing.Point(200, 157);
            this.chkCover.Name = "chkCover";
            this.chkCover.TabIndex = 3;
            this.chkCover.Text = "下载封面";

            // btnAdd
            this.btnAdd.IsPrimary = true;
            this.btnAdd.Location = new System.Drawing.Point(634, 155);
            this.btnAdd.Name = "btnAdd";
            this.btnAdd.Size = new System.Drawing.Size(110, 32);
            this.btnAdd.TabIndex = 4;
            this.btnAdd.Text = "+ 添加下载";
            this.btnAdd.Click += new System.EventHandler(this.btnAdd_Click);

            // listHeader
            this.listHeader.AutoSize = true;
            this.listHeader.Font = new System.Drawing.Font("Microsoft YaHei UI", 11F, System.Drawing.FontStyle.Bold);
            this.listHeader.ForeColor = AppTheme.TextPrimary;
            this.listHeader.Location = new System.Drawing.Point(24, 295);
            this.listHeader.Name = "listHeader";
            this.listHeader.Text = "任务列表";

            // toolPanel
            this.toolPanel.BackColor = AppTheme.Background;
            this.toolPanel.Controls.Add(this.btnSelectAll);
            this.toolPanel.Controls.Add(this.btnDeselect);
            this.toolPanel.Controls.Add(this.btnDownloadSelected);
            this.toolPanel.Controls.Add(this.btnDownloadAll);
            this.toolPanel.Controls.Add(this.btnPause);
            this.toolPanel.Controls.Add(this.btnCancel);
            this.toolPanel.Controls.Add(this.btnClear);
            this.toolPanel.Location = new System.Drawing.Point(24, 325);
            this.toolPanel.Name = "toolPanel";
            this.toolPanel.Size = new System.Drawing.Size(764, 40);
            this.toolPanel.TabIndex = 4;

            // btnSelectAll
            this.btnSelectAll.Location = new System.Drawing.Point(0, 4);
            this.btnSelectAll.Name = "btnSelectAll";
            this.btnSelectAll.Size = new System.Drawing.Size(70, 32);
            this.btnSelectAll.TabIndex = 0;
            this.btnSelectAll.Text = "全选";
            this.btnSelectAll.Click += new System.EventHandler(this.btnSelectAll_Click);

            // btnDeselect
            this.btnDeselect.Location = new System.Drawing.Point(80, 4);
            this.btnDeselect.Name = "btnDeselect";
            this.btnDeselect.Size = new System.Drawing.Size(90, 32);
            this.btnDeselect.TabIndex = 1;
            this.btnDeselect.Text = "取消全选";
            this.btnDeselect.Click += new System.EventHandler(this.btnDeselect_Click);

            // btnDownloadSelected
            this.btnDownloadSelected.Location = new System.Drawing.Point(180, 4);
            this.btnDownloadSelected.Name = "btnDownloadSelected";
            this.btnDownloadSelected.Size = new System.Drawing.Size(100, 32);
            this.btnDownloadSelected.TabIndex = 2;
            this.btnDownloadSelected.Text = "下载选中";
            this.btnDownloadSelected.Click += new System.EventHandler(this.btnDownloadSelected_Click);

            // btnDownloadAll
            this.btnDownloadAll.IsPrimary = true;
            this.btnDownloadAll.Location = new System.Drawing.Point(290, 4);
            this.btnDownloadAll.Name = "btnDownloadAll";
            this.btnDownloadAll.Size = new System.Drawing.Size(100, 32);
            this.btnDownloadAll.TabIndex = 3;
            this.btnDownloadAll.Text = "全部下载";
            this.btnDownloadAll.Click += new System.EventHandler(this.btnDownloadAll_Click);

            // btnPause
            this.btnPause.Location = new System.Drawing.Point(400, 4);
            this.btnPause.Name = "btnPause";
            this.btnPause.Size = new System.Drawing.Size(100, 32);
            this.btnPause.TabIndex = 4;
            this.btnPause.Text = "暂停/继续";
            this.btnPause.Click += new System.EventHandler(this.btnPause_Click);

            // btnCancel
            this.btnCancel.Location = new System.Drawing.Point(510, 4);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(100, 32);
            this.btnCancel.TabIndex = 5;
            this.btnCancel.Text = "取消下载";
            this.btnCancel.Click += new System.EventHandler(this.btnCancelDl_Click);

            // btnClear
            this.btnClear.Location = new System.Drawing.Point(620, 4);
            this.btnClear.Name = "btnClear";
            this.btnClear.Size = new System.Drawing.Size(90, 32);
            this.btnClear.TabIndex = 6;
            this.btnClear.Text = "清空列表";
            this.btnClear.Click += new System.EventHandler(this.btnClear_Click);

            // colIdx
            this.colIdx.Text = "#";
            this.colIdx.Width = 40;
            // colUrl
            this.colUrl.Text = "链接";
            this.colUrl.Width = 220;
            // colTitle
            this.colTitle.Text = "标题";
            this.colTitle.Width = 120;
            // colAuthor
            this.colAuthor.Text = "作者";
            this.colAuthor.Width = 80;
            // colDescription
            this.colDescription.Text = "描述";
            this.colDescription.Width = 120;
            // colPublishTime
            this.colPublishTime.Text = "发布时间";
            this.colPublishTime.Width = 120;
            // colStatus
            this.colStatus.Text = "状态";
            this.colStatus.Width = 70;
            // colProgress
            this.colProgress.Text = "进度";
            this.colProgress.Width = 55;
            // colSize
            this.colSize.Text = "大小";
            this.colSize.Width = 70;

            // taskList
            this.taskList.BackColor = AppTheme.CardBackground;
            this.taskList.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.taskList.CheckBoxes = true;
            this.taskList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
                this.colIdx, this.colUrl, this.colTitle, this.colAuthor, this.colDescription,
                this.colPublishTime, this.colStatus, this.colProgress, this.colSize });
            this.taskList.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.taskList.ForeColor = AppTheme.TextPrimary;
            this.taskList.FullRowSelect = true;
            this.taskList.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.taskList.Location = new System.Drawing.Point(24, 370);
            this.taskList.MultiSelect = true;
            this.taskList.Name = "taskList";
            this.taskList.Size = new System.Drawing.Size(764, 380);
            this.taskList.TabIndex = 5;
            this.taskList.UseCompatibleStateImageBehavior = false;
            this.taskList.View = System.Windows.Forms.View.Details;
            this.taskList.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.TaskList_ColumnClick);
            this.taskList.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.TaskList_MouseDoubleClick);
            this.taskList.MouseUp += new System.Windows.Forms.MouseEventHandler(this.TaskList_MouseUp);
            this.taskList.SelectedIndexChanged += new System.EventHandler(this.TaskList_SelectedIndexChanged);

            // detailPanel
            this.detailPanel.BackColor = AppTheme.CardBackground;
            this.detailPanel.Controls.Add(this.detailTitle);
            this.detailPanel.Controls.Add(this.lblDetailTitle);
            this.detailPanel.Controls.Add(this.lblDetailInfo);
            this.detailPanel.Controls.Add(this.detailProgress);
            this.detailPanel.Controls.Add(this.btnOpenLink);
            this.detailPanel.Controls.Add(this.btnOpenFolder);
            this.detailPanel.Location = new System.Drawing.Point(808, 80);
            this.detailPanel.Name = "detailPanel";
            this.detailPanel.Padding = new System.Windows.Forms.Padding(12);
            this.detailPanel.Size = new System.Drawing.Size(240, 670);
            this.detailPanel.TabIndex = 6;

            // detailTitle
            this.detailTitle.AutoSize = true;
            this.detailTitle.Font = new System.Drawing.Font("Microsoft YaHei UI", 10F, System.Drawing.FontStyle.Bold);
            this.detailTitle.ForeColor = AppTheme.TextPrimary;
            this.detailTitle.Location = new System.Drawing.Point(12, 12);
            this.detailTitle.Name = "detailTitle";
            this.detailTitle.Text = "已选任务";

            // lblDetailTitle
            this.lblDetailTitle.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblDetailTitle.ForeColor = AppTheme.TextPrimary;
            this.lblDetailTitle.Location = new System.Drawing.Point(12, 45);
            this.lblDetailTitle.Name = "lblDetailTitle";
            this.lblDetailTitle.Size = new System.Drawing.Size(216, 50);
            this.lblDetailTitle.Text = "选中左侧任务后显示：标题、作者、描述、链接、错误信息";

            // lblDetailInfo
            this.lblDetailInfo.Font = new System.Drawing.Font("Microsoft YaHei UI", 8.5F);
            this.lblDetailInfo.ForeColor = AppTheme.TextSecondary;
            this.lblDetailInfo.Location = new System.Drawing.Point(12, 100);
            this.lblDetailInfo.Name = "lblDetailInfo";
            this.lblDetailInfo.Size = new System.Drawing.Size(216, 180);
            this.lblDetailInfo.Text = "";

            // detailProgress
            this.detailProgress.Location = new System.Drawing.Point(12, 290);
            this.detailProgress.Maximum = 100;
            this.detailProgress.Name = "detailProgress";
            this.detailProgress.Size = new System.Drawing.Size(216, 8);
            this.detailProgress.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.detailProgress.TabIndex = 4;

            // btnOpenLink
            this.btnOpenLink.Location = new System.Drawing.Point(12, 300);
            this.btnOpenLink.Name = "btnOpenLink";
            this.btnOpenLink.Size = new System.Drawing.Size(216, 32);
            this.btnOpenLink.TabIndex = 5;
            this.btnOpenLink.Text = "打开作品链接";
            this.btnOpenLink.Click += new System.EventHandler(this.btnOpenLink_Click);

            // btnOpenFolder
            this.btnOpenFolder.IsPrimary = true;
            this.btnOpenFolder.Location = new System.Drawing.Point(12, 340);
            this.btnOpenFolder.Name = "btnOpenFolder";
            this.btnOpenFolder.Size = new System.Drawing.Size(216, 32);
            this.btnOpenFolder.TabIndex = 6;
            this.btnOpenFolder.Text = "打开保存目录";
            this.btnOpenFolder.Click += new System.EventHandler(this.btnOpenFolder_Click);

            // DownloadPage
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = AppTheme.Background;
            this.Controls.Add(this.titleLabel);
            this.Controls.Add(this.subTitle);
            this.Controls.Add(this.cardNew);
            this.Controls.Add(this.listHeader);
            this.Controls.Add(this.toolPanel);
            this.Controls.Add(this.taskList);
            this.Controls.Add(this.detailPanel);
            this.DoubleBuffered = true;
            this.Name = "DownloadPage";
            this.Padding = new System.Windows.Forms.Padding(0);
            this.Size = new System.Drawing.Size(1060, 770);
            this.Load += new System.EventHandler(this.DownloadPage_Load);
            this.Resize += new System.EventHandler(this.DownloadPage_Resize);

            this.cardNew.ResumeLayout(false);
            this.cardNew.PerformLayout();
            this.toolPanel.ResumeLayout(false);
            this.detailPanel.ResumeLayout(false);
            this.detailPanel.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion
    }
}