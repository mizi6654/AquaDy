using System.Drawing;
using System.Windows.Forms;
using AquaDy.Controls;
using AquaDy.Models;
using AquaDy.Services;
using AquaDy.Utils;

namespace AquaDy.Forms
{
    partial class CompletedPage
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Label titleLabel;
        private System.Windows.Forms.Label subLabel;
        private AquaDy.Controls.DarkButton btnSelectAll;
        private AquaDy.Controls.DarkButton btnDeselect;
        private AquaDy.Controls.DarkButton btnRedownload;
        private AquaDy.Controls.DarkButton btnPause;
        private AquaDy.Controls.DarkButton btnCancelDone;
        private AquaDy.Controls.DarkButton btnRefresh;
        private AquaDy.Controls.DarkButton btnOpenFolder;
        private AquaDy.Controls.DarkButton btnClear;
        private System.Windows.Forms.ListView historyList;
        private System.Windows.Forms.ColumnHeader colIdx;
        private System.Windows.Forms.ColumnHeader colUrl;
        private System.Windows.Forms.ColumnHeader colTitle;
        private System.Windows.Forms.ColumnHeader colAuthor;
        private System.Windows.Forms.ColumnHeader colStatus;
        private System.Windows.Forms.ColumnHeader colSize;
        private System.Windows.Forms.ColumnHeader colTime;
        private System.Windows.Forms.ColumnHeader colPath;
        private System.Windows.Forms.ContextMenuStrip cms;
        private System.Windows.Forms.ToolStripMenuItem cmsRedownload;
        private System.Windows.Forms.ToolStripMenuItem cmsDelete;
        private System.Windows.Forms.ToolStripMenuItem cmsSelectAll;
        private System.Windows.Forms.ToolStripMenuItem cmsDeselectAll;

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
            this.components = new System.ComponentModel.Container();
            this.titleLabel = new System.Windows.Forms.Label();
            this.subLabel = new System.Windows.Forms.Label();
            this.btnSelectAll = new AquaDy.Controls.DarkButton();
            this.btnDeselect = new AquaDy.Controls.DarkButton();
            this.btnRedownload = new AquaDy.Controls.DarkButton();
            this.btnPause = new AquaDy.Controls.DarkButton();
            this.btnCancelDone = new AquaDy.Controls.DarkButton();
            this.btnRefresh = new AquaDy.Controls.DarkButton();
            this.btnOpenFolder = new AquaDy.Controls.DarkButton();
            this.btnClear = new AquaDy.Controls.DarkButton();
            this.historyList = new System.Windows.Forms.ListView();
            this.colIdx = new System.Windows.Forms.ColumnHeader();
            this.colUrl = new System.Windows.Forms.ColumnHeader();
            this.colTitle = new System.Windows.Forms.ColumnHeader();
            this.colAuthor = new System.Windows.Forms.ColumnHeader();
            this.colStatus = new System.Windows.Forms.ColumnHeader();
            this.colSize = new System.Windows.Forms.ColumnHeader();
            this.colTime = new System.Windows.Forms.ColumnHeader();
            this.colPath = new System.Windows.Forms.ColumnHeader();
            this.cms = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.cmsRedownload = new System.Windows.Forms.ToolStripMenuItem();
            this.cmsDelete = new System.Windows.Forms.ToolStripMenuItem();
            this.cmsSelectAll = new System.Windows.Forms.ToolStripMenuItem();
            this.cmsDeselectAll = new System.Windows.Forms.ToolStripMenuItem();
            this.SuspendLayout();
            // 
            // titleLabel
            // 
            this.titleLabel.AutoSize = true;
            this.titleLabel.Font = new System.Drawing.Font("Microsoft YaHei UI", 16F, System.Drawing.FontStyle.Bold);
            this.titleLabel.ForeColor = AppTheme.TextPrimary;
            this.titleLabel.Location = new System.Drawing.Point(24, 16);
            this.titleLabel.Name = "titleLabel";
            this.titleLabel.Text = "已完成 / 历史记录";
            // 
            // subLabel
            // 
            this.subLabel.AutoSize = true;
            this.subLabel.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.subLabel.ForeColor = AppTheme.TextSecondary;
            this.subLabel.Location = new System.Drawing.Point(24, 48);
            this.subLabel.Name = "subLabel";
            this.subLabel.Text = "记录「下载任务」与「账号批量」的成功/失败结果。可勾选后重新下载。";
            // 
            // btnSelectAll
            // 
            this.btnSelectAll.Location = new System.Drawing.Point(24, 90);
            this.btnSelectAll.Name = "btnSelectAll";
            this.btnSelectAll.Size = new System.Drawing.Size(70, 34);
            this.btnSelectAll.TabIndex = 2;
            this.btnSelectAll.Text = "全选";
            this.btnSelectAll.Click += new System.EventHandler(this.btnSelectAll_Click);
            // 
            // btnDeselect
            // 
            this.btnDeselect.Location = new System.Drawing.Point(104, 90);
            this.btnDeselect.Name = "btnDeselect";
            this.btnDeselect.Size = new System.Drawing.Size(90, 34);
            this.btnDeselect.TabIndex = 3;
            this.btnDeselect.Text = "取消全选";
            this.btnDeselect.Click += new System.EventHandler(this.btnDeselect_Click);
            // 
            // btnRedownload
            // 
            this.btnRedownload.IsPrimary = true;
            this.btnRedownload.Location = new System.Drawing.Point(204, 90);
            this.btnRedownload.Name = "btnRedownload";
            this.btnRedownload.Size = new System.Drawing.Size(100, 34);
            this.btnRedownload.TabIndex = 4;
            this.btnRedownload.Text = "重新下载";
            this.btnRedownload.Click += new System.EventHandler(this.btnRedownload_Click);
            // 
            // btnPause
            // 
            this.btnPause.Location = new System.Drawing.Point(314, 90);
            this.btnPause.Name = "btnPause";
            this.btnPause.Size = new System.Drawing.Size(100, 34);
            this.btnPause.TabIndex = 5;
            this.btnPause.Text = "暂停/继续";
            this.btnPause.Click += new System.EventHandler(this.btnPause_Click);
            // 
            // btnCancelDone
            // 
            this.btnCancelDone.Location = new System.Drawing.Point(424, 90);
            this.btnCancelDone.Name = "btnCancelDone";
            this.btnCancelDone.Size = new System.Drawing.Size(100, 34);
            this.btnCancelDone.TabIndex = 6;
            this.btnCancelDone.Text = "取消下载";
            this.btnCancelDone.Click += new System.EventHandler(this.btnCancelDone_Click);
            // 
            // btnRefresh
            // 
            this.btnRefresh.Location = new System.Drawing.Point(534, 90);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(80, 34);
            this.btnRefresh.TabIndex = 7;
            this.btnRefresh.Text = "刷新";
            this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);
            // 
            // btnOpenFolder
            // 
            this.btnOpenFolder.Location = new System.Drawing.Point(624, 90);
            this.btnOpenFolder.Name = "btnOpenFolder";
            this.btnOpenFolder.Size = new System.Drawing.Size(100, 34);
            this.btnOpenFolder.TabIndex = 8;
            this.btnOpenFolder.Text = "打开目录";
            this.btnOpenFolder.Click += new System.EventHandler(this.btnOpenFolder_Click);
            // 
            // btnClear
            // 
            this.btnClear.Location = new System.Drawing.Point(734, 90);
            this.btnClear.Name = "btnClear";
            this.btnClear.Size = new System.Drawing.Size(100, 34);
            this.btnClear.TabIndex = 9;
            this.btnClear.Text = "清空历史";
            this.btnClear.Click += new System.EventHandler(this.btnClear_Click);
            // 
            // cmsRedownload
            // 
            this.cmsRedownload.Name = "cmsRedownload";
            this.cmsRedownload.Size = new System.Drawing.Size(160, 22);
            this.cmsRedownload.Text = "重新下载选中";
            this.cmsRedownload.Click += new System.EventHandler(this.cmsRedownload_Click);
            // 
            // cmsDelete
            // 
            this.cmsDelete.Name = "cmsDelete";
            this.cmsDelete.Size = new System.Drawing.Size(160, 22);
            this.cmsDelete.Text = "删除选中";
            this.cmsDelete.Click += new System.EventHandler(this.cmsDelete_Click);
            // 
            // cmsSelectAll
            // 
            this.cmsSelectAll.Name = "cmsSelectAll";
            this.cmsSelectAll.Size = new System.Drawing.Size(160, 22);
            this.cmsSelectAll.Text = "全选";
            this.cmsSelectAll.Click += new System.EventHandler(this.cmsSelectAll_Click);
            // 
            // cmsDeselectAll
            // 
            this.cmsDeselectAll.Name = "cmsDeselectAll";
            this.cmsDeselectAll.Size = new System.Drawing.Size(160, 22);
            this.cmsDeselectAll.Text = "取消全选";
            this.cmsDeselectAll.Click += new System.EventHandler(this.cmsDeselectAll_Click);
            // 
            // cms
            // 
            this.cms.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.cmsRedownload,
                this.cmsDelete,
                this.cmsSelectAll,
                this.cmsDeselectAll});
            this.cms.Name = "cms";
            this.cms.Size = new System.Drawing.Size(164, 92);
            // 
            // colIdx
            // 
            this.colIdx.Text = "#";
            this.colIdx.Width = 40;
            // 
            // colUrl
            // 
            this.colUrl.Text = "链接";
            this.colUrl.Width = 280;
            // 
            // colTitle
            // 
            this.colTitle.Text = "标题";
            this.colTitle.Width = 140;
            // 
            // colAuthor
            // 
            this.colAuthor.Text = "作者";
            this.colAuthor.Width = 90;
            // 
            // colStatus
            // 
            this.colStatus.Text = "状态";
            this.colStatus.Width = 70;
            // 
            // colSize
            // 
            this.colSize.Text = "大小";
            this.colSize.Width = 80;
            // 
            // colTime
            // 
            this.colTime.Text = "时间";
            this.colTime.Width = 120;
            // 
            // colPath
            // 
            this.colPath.Text = "路径";
            this.colPath.Width = 200;
            // 
            // historyList
            // 
            this.historyList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.historyList.BackColor = AppTheme.CardBackground;
            this.historyList.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.historyList.CheckBoxes = true;
            this.historyList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
                this.colIdx,
                this.colUrl,
                this.colTitle,
                this.colAuthor,
                this.colStatus,
                this.colSize,
                this.colTime,
                this.colPath});
            this.historyList.ContextMenuStrip = this.cms;
            this.historyList.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.historyList.ForeColor = AppTheme.TextPrimary;
            this.historyList.FullRowSelect = true;
            this.historyList.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.historyList.Location = new System.Drawing.Point(24, 140);
            this.historyList.Name = "historyList";
            this.historyList.Size = new System.Drawing.Size(1012, 620);
            this.historyList.TabIndex = 10;
            this.historyList.UseCompatibleStateImageBehavior = false;
            this.historyList.View = System.Windows.Forms.View.Details;
            this.historyList.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.historyList_MouseDoubleClick);
            // 
            // CompletedPage
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = AppTheme.Background;
            this.Controls.Add(this.historyList);
            this.Controls.Add(this.btnClear);
            this.Controls.Add(this.btnOpenFolder);
            this.Controls.Add(this.btnRefresh);
            this.Controls.Add(this.btnCancelDone);
            this.Controls.Add(this.btnPause);
            this.Controls.Add(this.btnRedownload);
            this.Controls.Add(this.btnDeselect);
            this.Controls.Add(this.btnSelectAll);
            this.Controls.Add(this.subLabel);
            this.Controls.Add(this.titleLabel);
            this.Name = "CompletedPage";
            this.Padding = new System.Windows.Forms.Padding(24, 20, 24, 20);
            this.Size = new System.Drawing.Size(1060, 770);
            this.Load += new System.EventHandler(this.CompletedPage_Load);
            this.VisibleChanged += new System.EventHandler(this.CompletedPage_VisibleChanged);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion
    }
}