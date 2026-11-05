using System;
using System.Drawing;
using System.Windows.Forms;
using AquaDy.Models;

namespace AquaDy.Controls
{
    public class DarkTextBox : TextBox
    {
        public DarkTextBox()
        {
            BorderStyle = BorderStyle.None;
            BackColor = AppTheme.CardBackground;
            ForeColor = AppTheme.TextPrimary;
            Font = new Font("Microsoft YaHei UI", 9.5F);
            Padding = new Padding(10, 8, 10, 8);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
        }
    }

    /// <summary>
    /// 带边框和圆角的输入框容器
    /// </summary>
    public class DarkInputPanel : Panel
    {
        private DarkTextBox _textBox;
        private string _placeholder = "";
        private bool _isFocused;

        public string Placeholder
        {
            get => _placeholder;
            set { _placeholder = value; Invalidate(); }
        }

        public new string Text
        {
            get => _textBox?.Text ?? "";
            set { if (_textBox != null) _textBox.Text = value; }
        }

        public bool Multiline
        {
            get => _textBox?.Multiline ?? false;
            set
            {
                if (_textBox == null) return;
                _textBox.Multiline = value;
                _textBox.ScrollBars = value ? System.Windows.Forms.ScrollBars.Vertical : System.Windows.Forms.ScrollBars.None;
                _textBox.AcceptsReturn = value;
                _textBox.WordWrap = value;
            }
        }

        public new event EventHandler? TextChanged;

        public DarkInputPanel()
        {
            BackColor = AppTheme.CardBackground;
            Padding = new Padding(12, 10, 12, 10);
            Height = 42;

            _textBox = new DarkTextBox
            {
                Dock = DockStyle.Fill,
                BackColor = AppTheme.CardBackground
            };
            _textBox.GotFocus += (s, e) => { _isFocused = true; Invalidate(); };
            _textBox.LostFocus += (s, e) => { _isFocused = false; Invalidate(); };
            _textBox.TextChanged += (s, e) => TextChanged?.Invoke(this, e);
            Controls.Add(_textBox);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            using var path = CreateRoundedRect(ClientRectangle, 8);
            using var bgBrush = new SolidBrush(AppTheme.CardBackground);
            g.FillPath(bgBrush, path);

            Color borderColor = _isFocused ? AppTheme.BorderFocus : AppTheme.Border;
            using var pen = new Pen(borderColor, 1.5f);
            g.DrawPath(pen, path);

            // Placeholder
            if (string.IsNullOrEmpty(_textBox.Text) && !string.IsNullOrEmpty(_placeholder) && !_isFocused)
            {
                using var brush = new SolidBrush(AppTheme.TextMuted);
                var flags = TextFormatFlags.VerticalCenter | TextFormatFlags.Left;
                TextRenderer.DrawText(g, _placeholder, _textBox.Font,
                    new Rectangle(14, 0, Width - 28, Height), AppTheme.TextMuted, flags);
            }
        }

        private System.Drawing.Drawing2D.GraphicsPath CreateRoundedRect(Rectangle bounds, int radius)
        {
            int d = radius * 2;
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d - 1, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d - 1, bounds.Bottom - d - 1, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d - 1, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
