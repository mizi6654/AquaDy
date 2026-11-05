using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using AquaDy.Models;

namespace AquaDy.Controls
{
    public class DarkButton : Button
    {
        private bool _isHover;
        private bool _isPrimary;

        public bool IsPrimary
        {
            get => _isPrimary;
            set { _isPrimary = value; Invalidate(); }
        }

        public DarkButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Cursor = Cursors.Hand;
            Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular);
            ForeColor = AppTheme.TextPrimary;
            BackColor = AppTheme.CardBackground;
            Size = new Size(100, 36);
            Padding = new Padding(12, 6, 12, 6);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _isHover = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _isHover = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Parent?.BackColor ?? AppTheme.Background);

            Color bg = _isPrimary
                ? (_isHover ? AppTheme.PrimaryHover : AppTheme.Primary)
                : (_isHover ? AppTheme.CardBackgroundHover : AppTheme.CardBackground);

            using var path = RoundedRect(ClientRectangle, 8);
            using var brush = new SolidBrush(bg);
            g.FillPath(brush, path);

            // 文字
            TextRenderer.DrawText(g, Text, Font, ClientRectangle, ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
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
