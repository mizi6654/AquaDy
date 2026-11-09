using System.Drawing;
using System.Windows.Forms;
using AquaDy.Controls;
using AquaDy.Models;
using AquaDy.Services;

namespace AquaDy.Forms
{
    partial class SettingsPage
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Label titleLabel;
        private System.Windows.Forms.Label subLabel;
        private AquaDy.Controls.DarkButton btnSave;
        private AquaDy.Controls.DarkButton btnReset;
        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.TabPage tabDownload;
        private System.Windows.Forms.TabPage tabCookie;
        private System.Windows.Forms.TabPage tabTools;

        // ---------- 下载与处理 Tab ----------
        private System.Windows.Forms.Label lblDownloadPath;
        private System.Windows.Forms.TextBox txtDownloadPath;
        private AquaDy.Controls.DarkButton btnBrowseDownload;
        private System.Windows.Forms.Label lblNameTemplate;
        private System.Windows.Forms.TextBox txtNameTemplate;
        private System.Windows.Forms.Label lblNameTip;
        private System.Windows.Forms.Label lblConcurrent;
        private System.Windows.Forms.NumericUpDown numConcurrent;
        private System.Windows.Forms.Label lblRetry;
        private System.Windows.Forms.NumericUpDown numRetry;
        private System.Windows.Forms.CheckBox chkSkipExisting;
        private System.Windows.Forms.CheckBox chkDownloadCover;
        private System.Windows.Forms.CheckBox chkEnableLogging;
        private System.Windows.Forms.CheckBox chkFolderByAuthor;

        // ---------- 网络与 Cookie Tab ----------
        private System.Windows.Forms.Label lblDouyinCookie;
        private System.Windows.Forms.TextBox txtDouyinCookie;
        private AquaDy.Controls.DarkButton btnPasteCookie;
        private AquaDy.Controls.DarkButton btnCheckCookie;
        private System.Windows.Forms.Label lblTikTokCookie;
        private System.Windows.Forms.TextBox txtTikTokCookie;
        private System.Windows.Forms.Label lblDeviceId;
        private System.Windows.Forms.TextBox txtDeviceId;

        // ---------- 工具与更新 Tab ----------
        private System.Windows.Forms.CheckBox chkUseProxy;
        private System.Windows.Forms.Label lblProxy;
        private System.Windows.Forms.TextBox txtProxy;
        private System.Windows.Forms.Label lblFfmpeg;
        private System.Windows.Forms.TextBox txtFFmpegPath;
        private AquaDy.Controls.DarkButton btnFf;
        private System.Windows.Forms.Label lblToolsTip;

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
            this.subLabel = new System.Windows.Forms.Label();
            this.btnSave = new AquaDy.Controls.DarkButton();
            this.btnReset = new AquaDy.Controls.DarkButton();
            this.tabControl = new System.Windows.Forms.TabControl();
            this.tabDownload = new System.Windows.Forms.TabPage();
            this.tabCookie = new System.Windows.Forms.TabPage();
            this.tabTools = new System.Windows.Forms.TabPage();

            this.lblDownloadPath = new System.Windows.Forms.Label();
            this.txtDownloadPath = new System.Windows.Forms.TextBox();
            this.btnBrowseDownload = new AquaDy.Controls.DarkButton();
            this.lblNameTemplate = new System.Windows.Forms.Label();
            this.txtNameTemplate = new System.Windows.Forms.TextBox();
            this.lblNameTip = new System.Windows.Forms.Label();
            this.lblConcurrent = new System.Windows.Forms.Label();
            this.numConcurrent = new System.Windows.Forms.NumericUpDown();
            this.lblRetry = new System.Windows.Forms.Label();
            this.numRetry = new System.Windows.Forms.NumericUpDown();
            this.chkSkipExisting = new System.Windows.Forms.CheckBox();
            this.chkDownloadCover = new System.Windows.Forms.CheckBox();
            this.chkEnableLogging = new System.Windows.Forms.CheckBox();
            this.chkFolderByAuthor = new System.Windows.Forms.CheckBox();

            this.lblDouyinCookie = new System.Windows.Forms.Label();
            this.txtDouyinCookie = new System.Windows.Forms.TextBox();
            this.btnPasteCookie = new AquaDy.Controls.DarkButton();
            this.btnCheckCookie = new AquaDy.Controls.DarkButton();
            this.lblTikTokCookie = new System.Windows.Forms.Label();
            this.txtTikTokCookie = new System.Windows.Forms.TextBox();
            this.lblDeviceId = new System.Windows.Forms.Label();
            this.txtDeviceId = new System.Windows.Forms.TextBox();

            this.chkUseProxy = new System.Windows.Forms.CheckBox();
            this.lblProxy = new System.Windows.Forms.Label();
            this.txtProxy = new System.Windows.Forms.TextBox();
            this.lblFfmpeg = new System.Windows.Forms.Label();
            this.txtFFmpegPath = new System.Windows.Forms.TextBox();
            this.btnFf = new AquaDy.Controls.DarkButton();
            this.lblToolsTip = new System.Windows.Forms.Label();

            this.tabDownload.SuspendLayout();
            this.tabCookie.SuspendLayout();
            this.tabTools.SuspendLayout();
            this.tabControl.SuspendLayout();
            this.SuspendLayout();

            // titleLabel
            this.titleLabel.AutoSize = true;
            this.titleLabel.Font = new System.Drawing.Font("Microsoft YaHei UI", 16F, System.Drawing.FontStyle.Bold);
            this.titleLabel.ForeColor = AppTheme.TextPrimary;
            this.titleLabel.Location = new System.Drawing.Point(24, 12);
            this.titleLabel.Name = "titleLabel";
            this.titleLabel.Text = "设置";

            // subLabel
            this.subLabel.AutoSize = true;
            this.subLabel.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.subLabel.ForeColor = AppTheme.TextSecondary;
            this.subLabel.Location = new System.Drawing.Point(24, 44);
            this.subLabel.Name = "subLabel";
            this.subLabel.Text = "下载、网络、工具和外观。";

            // btnSave
            this.btnSave.Anchor = (AnchorStyles)(AnchorStyles.Top | AnchorStyles.Right);
            this.btnSave.IsPrimary = true;
            this.btnSave.Location = new System.Drawing.Point(920, 16);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(110, 36);
            this.btnSave.TabIndex = 2;
            this.btnSave.Text = "保存设置";
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);

            // btnReset
            this.btnReset.Anchor = (AnchorStyles)(AnchorStyles.Top | AnchorStyles.Right);
            this.btnReset.Location = new System.Drawing.Point(800, 16);
            this.btnReset.Name = "btnReset";
            this.btnReset.Size = new System.Drawing.Size(100, 36);
            this.btnReset.TabIndex = 3;
            this.btnReset.Text = "恢复默认";
            this.btnReset.Click += new System.EventHandler(this.btnReset_Click);

            // tabControl
            this.tabControl.Anchor = (AnchorStyles)(((AnchorStyles.Top | AnchorStyles.Bottom) | AnchorStyles.Left) | AnchorStyles.Right);
            this.tabControl.Controls.Add(this.tabDownload);
            this.tabControl.Controls.Add(this.tabCookie);
            this.tabControl.Controls.Add(this.tabTools);
            this.tabControl.Font = new System.Drawing.Font("Microsoft YaHei UI", 9.5F);
            this.tabControl.Location = new System.Drawing.Point(24, 80);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(1012, 650);
            this.tabControl.TabIndex = 4;

            // tabDownload
            this.tabDownload.BackColor = AppTheme.Background;
            this.tabDownload.Controls.Add(this.chkFolderByAuthor);
            this.tabDownload.Controls.Add(this.chkEnableLogging);
            this.tabDownload.Controls.Add(this.chkDownloadCover);
            this.tabDownload.Controls.Add(this.chkSkipExisting);
            this.tabDownload.Controls.Add(this.numRetry);
            this.tabDownload.Controls.Add(this.lblRetry);
            this.tabDownload.Controls.Add(this.numConcurrent);
            this.tabDownload.Controls.Add(this.lblConcurrent);
            this.tabDownload.Controls.Add(this.lblNameTip);
            this.tabDownload.Controls.Add(this.txtNameTemplate);
            this.tabDownload.Controls.Add(this.lblNameTemplate);
            this.tabDownload.Controls.Add(this.btnBrowseDownload);
            this.tabDownload.Controls.Add(this.txtDownloadPath);
            this.tabDownload.Controls.Add(this.lblDownloadPath);
            this.tabDownload.Location = new System.Drawing.Point(4, 26);
            this.tabDownload.Name = "tabDownload";
            this.tabDownload.Padding = new System.Windows.Forms.Padding(16);
            this.tabDownload.Size = new System.Drawing.Size(1004, 620);
            this.tabDownload.TabIndex = 0;
            this.tabDownload.Text = "下载与处理";

            // lblDownloadPath
            this.lblDownloadPath.AutoSize = true;
            this.lblDownloadPath.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.lblDownloadPath.ForeColor = AppTheme.TextSecondary;
            this.lblDownloadPath.Location = new System.Drawing.Point(16, 10);
            this.lblDownloadPath.Name = "lblDownloadPath";
            this.lblDownloadPath.Text = "下载保存路径";

            // txtDownloadPath
            this.txtDownloadPath.BackColor = AppTheme.CardBackground;
            this.txtDownloadPath.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtDownloadPath.Font = new System.Drawing.Font("Microsoft YaHei UI", 9.5F);
            this.txtDownloadPath.ForeColor = AppTheme.TextPrimary;
            this.txtDownloadPath.Location = new System.Drawing.Point(16, 34);
            this.txtDownloadPath.Name = "txtDownloadPath";
            this.txtDownloadPath.Size = new System.Drawing.Size(400, 28);
            this.txtDownloadPath.TabIndex = 1;

            // btnBrowseDownload
            this.btnBrowseDownload.Location = new System.Drawing.Point(420, 34);
            this.btnBrowseDownload.Name = "btnBrowseDownload";
            this.btnBrowseDownload.Size = new System.Drawing.Size(80, 32);
            this.btnBrowseDownload.TabIndex = 2;
            this.btnBrowseDownload.Text = "浏览...";
            this.btnBrowseDownload.Click += new System.EventHandler(this.btnBrowseDownload_Click);

            // lblNameTemplate
            this.lblNameTemplate.AutoSize = true;
            this.lblNameTemplate.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.lblNameTemplate.ForeColor = AppTheme.TextSecondary;
            this.lblNameTemplate.Location = new System.Drawing.Point(16, 84);
            this.lblNameTemplate.Name = "lblNameTemplate";
            this.lblNameTemplate.Text = "作品文件命名（下载任务 / 账号批量，可改）";

            // txtNameTemplate
            this.txtNameTemplate.BackColor = AppTheme.CardBackground;
            this.txtNameTemplate.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtNameTemplate.Font = new System.Drawing.Font("Microsoft YaHei UI", 9.5F);
            this.txtNameTemplate.ForeColor = AppTheme.TextPrimary;
            this.txtNameTemplate.Location = new System.Drawing.Point(16, 108);
            this.txtNameTemplate.Name = "txtNameTemplate";
            this.txtNameTemplate.Size = new System.Drawing.Size(500, 28);
            this.txtNameTemplate.TabIndex = 3;

            // lblNameTip
            this.lblNameTip.ForeColor = AppTheme.TextSecondary;
            this.lblNameTip.Location = new System.Drawing.Point(16, 144);
            this.lblNameTip.Name = "lblNameTip";
            this.lblNameTip.Size = new System.Drawing.Size(560, 40);
            this.lblNameTip.Text = "变量：{create_time} {nickname} {desc} {id}。改错会导致文件名异常，可点「恢复默认」。\n直播录制不走此模板。下方「封面」「作者子目录」对下载任务与账号批量全局生效。";

            // lblConcurrent
            this.lblConcurrent.AutoSize = true;
            this.lblConcurrent.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.lblConcurrent.ForeColor = AppTheme.TextSecondary;
            this.lblConcurrent.Location = new System.Drawing.Point(16, 194);
            this.lblConcurrent.Name = "lblConcurrent";
            this.lblConcurrent.Text = "最大并发下载数，建议3-5即可（超过这个数值下载时容易出现失败！）";

            // numConcurrent
            this.numConcurrent.BackColor = AppTheme.CardBackground;
            this.numConcurrent.ForeColor = AppTheme.TextPrimary;
            this.numConcurrent.Location = new System.Drawing.Point(16, 218);
            this.numConcurrent.Maximum = new decimal(new int[] { 10, 0, 0, 0 });
            this.numConcurrent.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.numConcurrent.Name = "numConcurrent";
            this.numConcurrent.Size = new System.Drawing.Size(80, 28);
            this.numConcurrent.TabIndex = 4;
            this.numConcurrent.Value = new decimal(new int[] { 3, 0, 0, 0 });

            // lblRetry
            this.lblRetry.AutoSize = true;
            this.lblRetry.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.lblRetry.ForeColor = AppTheme.TextSecondary;
            this.lblRetry.Location = new System.Drawing.Point(16, 268);
            this.lblRetry.Name = "lblRetry";
            this.lblRetry.Text = "失败重试次数";

            // numRetry
            this.numRetry.BackColor = AppTheme.CardBackground;
            this.numRetry.ForeColor = AppTheme.TextPrimary;
            this.numRetry.Location = new System.Drawing.Point(16, 292);
            this.numRetry.Maximum = new decimal(new int[] { 10, 0, 0, 0 });
            this.numRetry.Name = "numRetry";
            this.numRetry.Size = new System.Drawing.Size(80, 28);
            this.numRetry.TabIndex = 5;
            this.numRetry.Value = new decimal(new int[] { 3, 0, 0, 0 });

            // chkSkipExisting
            this.chkSkipExisting.AutoSize = true;
            this.chkSkipExisting.Checked = true;
            this.chkSkipExisting.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkSkipExisting.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.chkSkipExisting.ForeColor = AppTheme.TextPrimary;
            this.chkSkipExisting.Location = new System.Drawing.Point(16, 342);
            this.chkSkipExisting.Name = "chkSkipExisting";
            this.chkSkipExisting.TabIndex = 6;
            this.chkSkipExisting.Text = "自动跳过已存在的文件";

            // chkDownloadCover
            this.chkDownloadCover.AutoSize = true;
            this.chkDownloadCover.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.chkDownloadCover.ForeColor = AppTheme.TextPrimary;
            this.chkDownloadCover.Location = new System.Drawing.Point(16, 372);
            this.chkDownloadCover.Name = "chkDownloadCover";
            this.chkDownloadCover.TabIndex = 7;
            this.chkDownloadCover.Text = "默认下载封面图";

            // chkEnableLogging
            this.chkEnableLogging.AutoSize = true;
            this.chkEnableLogging.Checked = true;
            this.chkEnableLogging.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkEnableLogging.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.chkEnableLogging.ForeColor = AppTheme.TextPrimary;
            this.chkEnableLogging.Location = new System.Drawing.Point(16, 402);
            this.chkEnableLogging.Name = "chkEnableLogging";
            this.chkEnableLogging.TabIndex = 8;
            this.chkEnableLogging.Text = "记录操作日志（解析/下载/暂停，写入程序目录 log/）";

            // chkFolderByAuthor
            this.chkFolderByAuthor.AutoSize = true;
            this.chkFolderByAuthor.Checked = true;
            this.chkFolderByAuthor.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkFolderByAuthor.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.chkFolderByAuthor.ForeColor = AppTheme.TextPrimary;
            this.chkFolderByAuthor.Location = new System.Drawing.Point(16, 442);
            this.chkFolderByAuthor.Name = "chkFolderByAuthor";
            this.chkFolderByAuthor.TabIndex = 9;
            this.chkFolderByAuthor.Text = "按作者名创建子目录保存";

            // tabCookie
            this.tabCookie.BackColor = AppTheme.Background;
            this.tabCookie.Controls.Add(this.txtDeviceId);
            this.tabCookie.Controls.Add(this.lblDeviceId);
            this.tabCookie.Controls.Add(this.txtTikTokCookie);
            this.tabCookie.Controls.Add(this.lblTikTokCookie);
            this.tabCookie.Controls.Add(this.btnCheckCookie);
            this.tabCookie.Controls.Add(this.btnPasteCookie);
            this.tabCookie.Controls.Add(this.txtDouyinCookie);
            this.tabCookie.Controls.Add(this.lblDouyinCookie);
            this.tabCookie.Location = new System.Drawing.Point(4, 26);
            this.tabCookie.Name = "tabCookie";
            this.tabCookie.Padding = new System.Windows.Forms.Padding(16);
            this.tabCookie.Size = new System.Drawing.Size(1004, 620);
            this.tabCookie.TabIndex = 1;
            this.tabCookie.Text = "网络与 Cookie";

            // lblDouyinCookie
            this.lblDouyinCookie.AutoSize = true;
            this.lblDouyinCookie.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.lblDouyinCookie.ForeColor = AppTheme.TextSecondary;
            this.lblDouyinCookie.Location = new System.Drawing.Point(16, 10);
            this.lblDouyinCookie.Name = "lblDouyinCookie";
            this.lblDouyinCookie.Text = "抖音 Cookie（完整字符串，格式 key1=value1; key2=value2; ... 与项目【TikTokDownloader】一致，不清楚请看项目主页）";

            // txtDouyinCookie
            this.txtDouyinCookie.Anchor = (AnchorStyles)((AnchorStyles.Top | AnchorStyles.Left) | AnchorStyles.Right);
            this.txtDouyinCookie.BackColor = AppTheme.CardBackground;
            this.txtDouyinCookie.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtDouyinCookie.ForeColor = AppTheme.TextPrimary;
            this.txtDouyinCookie.Location = new System.Drawing.Point(16, 34);
            this.txtDouyinCookie.Multiline = true;
            this.txtDouyinCookie.Name = "txtDouyinCookie";
            this.txtDouyinCookie.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtDouyinCookie.Size = new System.Drawing.Size(968, 80);
            this.txtDouyinCookie.TabIndex = 1;

            // btnPasteCookie
            this.btnPasteCookie.Location = new System.Drawing.Point(16, 129);
            this.btnPasteCookie.Name = "btnPasteCookie";
            this.btnPasteCookie.Size = new System.Drawing.Size(120, 32);
            this.btnPasteCookie.TabIndex = 2;
            this.btnPasteCookie.Text = "从剪贴板粘贴";
            this.btnPasteCookie.Click += new System.EventHandler(this.btnPasteCookie_Click);

            // btnCheckCookie
            this.btnCheckCookie.Location = new System.Drawing.Point(150, 129);
            this.btnCheckCookie.Name = "btnCheckCookie";
            this.btnCheckCookie.Size = new System.Drawing.Size(110, 32);
            this.btnCheckCookie.TabIndex = 3;
            this.btnCheckCookie.Text = "主页校验";
            this.btnCheckCookie.Click += new System.EventHandler(this.btnCheckCookie_Click);

            // lblTikTokCookie
            this.lblTikTokCookie.AutoSize = true;
            this.lblTikTokCookie.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.lblTikTokCookie.ForeColor = AppTheme.TextSecondary;
            this.lblTikTokCookie.Location = new System.Drawing.Point(16, 179);
            this.lblTikTokCookie.Name = "lblTikTokCookie";
            this.lblTikTokCookie.Text = "TikTok Cookie";

            // txtTikTokCookie
            this.txtTikTokCookie.Anchor = (AnchorStyles)((AnchorStyles.Top | AnchorStyles.Left) | AnchorStyles.Right);
            this.txtTikTokCookie.BackColor = AppTheme.CardBackground;
            this.txtTikTokCookie.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtTikTokCookie.ForeColor = AppTheme.TextPrimary;
            this.txtTikTokCookie.Location = new System.Drawing.Point(16, 203);
            this.txtTikTokCookie.Multiline = true;
            this.txtTikTokCookie.Name = "txtTikTokCookie";
            this.txtTikTokCookie.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtTikTokCookie.Size = new System.Drawing.Size(968, 60);
            this.txtTikTokCookie.TabIndex = 4;

            // lblDeviceId
            this.lblDeviceId.AutoSize = true;
            this.lblDeviceId.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.lblDeviceId.ForeColor = AppTheme.TextSecondary;
            this.lblDeviceId.Location = new System.Drawing.Point(16, 278);
            this.lblDeviceId.Name = "lblDeviceId";
            this.lblDeviceId.Text = "TikTok device_id（部分接口需要）";

            // txtDeviceId
            this.txtDeviceId.BackColor = AppTheme.CardBackground;
            this.txtDeviceId.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtDeviceId.Font = new System.Drawing.Font("Microsoft YaHei UI", 9.5F);
            this.txtDeviceId.ForeColor = AppTheme.TextPrimary;
            this.txtDeviceId.Location = new System.Drawing.Point(16, 302);
            this.txtDeviceId.Name = "txtDeviceId";
            this.txtDeviceId.Size = new System.Drawing.Size(400, 28);
            this.txtDeviceId.TabIndex = 5;

            // tabTools
            this.tabTools.BackColor = AppTheme.Background;
            this.tabTools.Controls.Add(this.lblToolsTip);
            this.tabTools.Controls.Add(this.btnFf);
            this.tabTools.Controls.Add(this.txtFFmpegPath);
            this.tabTools.Controls.Add(this.lblFfmpeg);
            this.tabTools.Controls.Add(this.txtProxy);
            this.tabTools.Controls.Add(this.lblProxy);
            this.tabTools.Controls.Add(this.chkUseProxy);
            this.tabTools.Location = new System.Drawing.Point(4, 26);
            this.tabTools.Name = "tabTools";
            this.tabTools.Padding = new System.Windows.Forms.Padding(16);
            this.tabTools.Size = new System.Drawing.Size(1004, 620);
            this.tabTools.TabIndex = 2;
            this.tabTools.Text = "工具与更新";

            // chkUseProxy
            this.chkUseProxy.AutoSize = true;
            this.chkUseProxy.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.chkUseProxy.ForeColor = AppTheme.TextPrimary;
            this.chkUseProxy.Location = new System.Drawing.Point(16, 10);
            this.chkUseProxy.Name = "chkUseProxy";
            this.chkUseProxy.TabIndex = 0;
            this.chkUseProxy.Text = "启用代理";

            // lblProxy
            this.lblProxy.AutoSize = true;
            this.lblProxy.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.lblProxy.ForeColor = AppTheme.TextSecondary;
            this.lblProxy.Location = new System.Drawing.Point(16, 40);
            this.lblProxy.Name = "lblProxy";
            this.lblProxy.Text = "代理地址（例：http://127.0.0.1:7890）";

            // txtProxy
            this.txtProxy.BackColor = AppTheme.CardBackground;
            this.txtProxy.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtProxy.Font = new System.Drawing.Font("Microsoft YaHei UI", 9.5F);
            this.txtProxy.ForeColor = AppTheme.TextPrimary;
            this.txtProxy.Location = new System.Drawing.Point(16, 64);
            this.txtProxy.Name = "txtProxy";
            this.txtProxy.Size = new System.Drawing.Size(400, 28);
            this.txtProxy.TabIndex = 1;

            // lblFfmpeg
            this.lblFfmpeg.AutoSize = true;
            this.lblFfmpeg.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.lblFfmpeg.ForeColor = AppTheme.TextSecondary;
            this.lblFfmpeg.Location = new System.Drawing.Point(16, 114);
            this.lblFfmpeg.Name = "lblFfmpeg";
            this.lblFfmpeg.Text = "ffmpeg 路径（直播录制，指向 ffmpeg.exe 或 bin 目录）";

            // txtFFmpegPath
            this.txtFFmpegPath.BackColor = AppTheme.CardBackground;
            this.txtFFmpegPath.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtFFmpegPath.Font = new System.Drawing.Font("Microsoft YaHei UI", 9.5F);
            this.txtFFmpegPath.ForeColor = AppTheme.TextPrimary;
            this.txtFFmpegPath.Location = new System.Drawing.Point(16, 138);
            this.txtFFmpegPath.Name = "txtFFmpegPath";
            this.txtFFmpegPath.Size = new System.Drawing.Size(360, 28);
            this.txtFFmpegPath.TabIndex = 2;

            // btnFf
            this.btnFf.Location = new System.Drawing.Point(390, 138);
            this.btnFf.Name = "btnFf";
            this.btnFf.Size = new System.Drawing.Size(70, 28);
            this.btnFf.TabIndex = 3;
            this.btnFf.Text = "浏览";
            this.btnFf.Click += new System.EventHandler(this.btnFf_Click);

            // lblToolsTip
            this.lblToolsTip.ForeColor = AppTheme.TextMuted;
            this.lblToolsTip.Location = new System.Drawing.Point(16, 178);
            this.lblToolsTip.Name = "lblToolsTip";
            this.lblToolsTip.Size = new System.Drawing.Size(520, 50);
            this.lblToolsTip.Text = "提示：你本机若已有 ffmpeg 解压包，请在设置中选择 bin\\ffmpeg.exe。\n直播录制只使用 ffmpeg：选择解压包内 bin\\ffmpeg.exe 后保存即可。";

            // SettingsPage
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = AppTheme.Background;
            this.Controls.Add(this.titleLabel);
            this.Controls.Add(this.subLabel);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.btnReset);
            this.Controls.Add(this.tabControl);
            this.Name = "SettingsPage";
            this.Padding = new System.Windows.Forms.Padding(24, 16, 24, 16);
            this.Size = new System.Drawing.Size(1060, 770);

            this.tabDownload.ResumeLayout(false);
            this.tabDownload.PerformLayout();
            this.tabCookie.ResumeLayout(false);
            this.tabCookie.PerformLayout();
            this.tabTools.ResumeLayout(false);
            this.tabTools.PerformLayout();
            this.tabControl.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion
    }
}