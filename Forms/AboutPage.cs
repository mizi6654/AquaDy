using System.Diagnostics;
using AquaDy.Services;
using AquaDy.Utils;

namespace AquaDy.Forms
{
    public partial class AboutPage : UserControl
    {
        public AboutPage()
        {
            InitializeComponent();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            // 原代码在 InitializeComponent 末尾执行这两行；
            // 因依赖运行时值（版本号、更新日志文本），移到 OnLoad，行为对用户无差异，
            // 但可以让 VS 设计器正常打开而不抛异常。
            versionLabel.Text = AppInfo.VersionText + "  ·  " + AppInfo.Subtitle;
            desc.Text = Changelog.BuildAboutBody();
        }

        // 与原 InitializeComponent 里的 Resize lambda 逐字等价
        private void AboutPage_Resize(object? sender, EventArgs e)
        {
            if (desc == null || btnMine == null || btnUpdate == null) return;

            int bottomReserve = 70;
            desc.Height = Math.Max(180, this.ClientSize.Height - desc.Top - bottomReserve);
            desc.Width = Math.Max(400, this.ClientSize.Width - 80);
            btnMine.Top = this.ClientSize.Height - 56;
            btnUpdate.Top = this.ClientSize.Height - 56;
        }

        private void btnMine_Click(object? sender, EventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = AppInfo.GitHubRepo,
                    UseShellExecute = true
                });
            }
            catch { }
        }

        private async void btnUpdate_Click(object? sender, EventArgs e)
        {
            btnUpdate.Enabled = false;
            try
            {
                await UpdateChecker.CheckAsync(silentIfLatest: false, this.FindForm());
            }
            finally
            {
                btnUpdate.Enabled = true;
            }
        }
    }
}