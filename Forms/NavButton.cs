using AquaDy.Models;
using AquaDy.Utils;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AquaDy.Forms
{
    /// <summary>
    /// 自定义导航按钮
    /// </summary>
    public class NavButton : Control
    {
        private bool _isSelected;
        private bool _isHover;

        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; Invalidate(); }
        }

        public NavButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Cursor = Cursors.Hand;
            Font = new Font("Microsoft YaHei UI", 9.5F);
            Height = 42;
        }

        protected override void OnMouseEnter(EventArgs e) { _isHover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _isHover = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(AppTheme.SidebarBackground);

            Color bg = _isSelected ? Color.FromArgb(45, 35, 70) :
                       _isHover ? AppTheme.NavHover : Color.Transparent;

            if (bg != Color.Transparent)
            {
                using var path = RoundedRect(new Rectangle(0, 2, Width - 1, Height - 4), 8);
                using var brush = new SolidBrush(bg);
                g.FillPath(brush, path);

                if (_isSelected)
                {
                    using var indicator = new SolidBrush(AppTheme.Primary);
                    g.FillRectangle(indicator, 0, 8, 3, Height - 16);
                }
            }

            Color textColor = _isSelected ? AppTheme.Primary : AppTheme.TextSecondary;
            TextRenderer.DrawText(g, Text, Font, new Rectangle(12, 0, Width - 24, Height),
                textColor, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
        }

        private GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}