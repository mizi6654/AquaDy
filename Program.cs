using AquaDy.Forms;

namespace AquaDy
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            AquaDy.Utils.AppPaths.MigrateFromAppDataIfNeeded();
            AquaDy.Services.ParseLogger.ReloadSetting();
            AquaDy.Services.ParseLogger.CleanupOnStartup();
            AquaDy.Services.ParseLogger.Op("启动", "AquaDy config=" + AquaDy.Utils.AppPaths.ConfigDir);

            AquaDy.Utils.AppInfo.EnsureVersionFile();

            Application.Run(new MainForm());
        }
    }
}
