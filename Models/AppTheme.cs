using System.Drawing;

namespace AquaDy.Models
{
    /// <summary>
    /// 全局主题颜色（参考 Huifa 深色紫色风格）
    /// </summary>
    public static class AppTheme
    {
        // 主背景
        public static readonly Color Background = Color.FromArgb(18, 18, 22);
        public static readonly Color SidebarBackground = Color.FromArgb(24, 24, 30);
        public static readonly Color CardBackground = Color.FromArgb(32, 32, 40);
        public static readonly Color CardBackgroundHover = Color.FromArgb(40, 40, 50);

        // 强调色（紫色系）
        public static readonly Color Primary = Color.FromArgb(139, 92, 246);       // 主紫色
        public static readonly Color PrimaryHover = Color.FromArgb(167, 139, 250);
        public static readonly Color PrimaryDark = Color.FromArgb(109, 40, 217);

        // 文字
        public static readonly Color TextPrimary = Color.FromArgb(240, 240, 245);
        public static readonly Color TextSecondary = Color.FromArgb(160, 160, 175);
        public static readonly Color TextMuted = Color.FromArgb(120, 120, 135);

        // 边框
        public static readonly Color Border = Color.FromArgb(50, 50, 60);
        public static readonly Color BorderFocus = Color.FromArgb(139, 92, 246);

        // 状态色
        public static readonly Color Success = Color.FromArgb(34, 197, 94);
        public static readonly Color Warning = Color.FromArgb(234, 179, 8);
        public static readonly Color Error = Color.FromArgb(239, 68, 68);
        public static readonly Color Info = Color.FromArgb(59, 130, 246);

        // 进度条
        public static readonly Color ProgressBackground = Color.FromArgb(45, 45, 55);
        public static readonly Color ProgressFill = Color.FromArgb(139, 92, 246);

        // 导航选中
        public static readonly Color NavSelected = Color.FromArgb(139, 92, 246);
        public static readonly Color NavHover = Color.FromArgb(45, 45, 55);
    }
}
