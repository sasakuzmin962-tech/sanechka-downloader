using System.Drawing.Drawing2D;

namespace SanechkaHub;

public static class Theme
{
    public static readonly Color Background = Color.FromArgb(7, 9, 14);
    public static readonly Color Surface = Color.FromArgb(15, 18, 26);
    public static readonly Color Surface2 = Color.FromArgb(21, 25, 35);
    public static readonly Color Surface3 = Color.FromArgb(29, 34, 47);
    public static readonly Color Border = Color.FromArgb(42, 48, 63);
    public static readonly Color Text = Color.FromArgb(244, 246, 251);
    public static readonly Color Muted = Color.FromArgb(139, 148, 166);
    public static readonly Color Accent = Color.FromArgb(126, 91, 255);
    public static readonly Color Accent2 = Color.FromArgb(99, 70, 224);
    public static readonly Color Success = Color.FromArgb(58, 211, 135);
    public static readonly Color Warning = Color.FromArgb(244, 184, 72);
    public static readonly Color Danger = Color.FromArgb(235, 80, 91);
    public static readonly Color Cyan = Color.FromArgb(65, 196, 231);

    public static Label Label(string text, float size = 10, bool bold = false)
        => new()
        {
            Text = text,
            AutoSize = true,
            ForeColor = bold ? Text : Muted,
            Font = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular),
            Margin = Padding.Empty
        };

    public static TextBox Input()
        => new()
        {
            Dock = DockStyle.Fill,
            AutoSize = false,
            BackColor = Surface2,
            ForeColor = Text,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 10.5F),
            Margin = Padding.Empty,
            Padding = new Padding(10, 7, 10, 7)
        };

    public static Button Button(string text, bool primary = false)
    {
        var b = new Button
        {
            Text = text,
            Dock = DockStyle.Fill,
            FlatStyle = FlatStyle.Flat,
            BackColor = primary ? Accent : Surface2,
            ForeColor = Text,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Margin = Padding.Empty,
            UseVisualStyleBackColor = false
        };
        b.FlatAppearance.BorderSize = primary ? 0 : 1;
        b.FlatAppearance.BorderColor = Border;

        b.MouseEnter += (_, _) => b.BackColor = primary ? Accent2 : Surface3;
        b.MouseLeave += (_, _) => b.BackColor = primary ? Accent : Surface2;
        return b;
    }

    public static Panel Card()
        => new()
        {
            BackColor = Surface,
            Padding = new Padding(22),
            Margin = new Padding(0, 0, 0, 16),
            Dock = DockStyle.Top
        };

    public static void PaintRounded(object? sender, PaintEventArgs e, int radius = 14)
    {
        if (sender is not Control c || c.Width < radius * 2 || c.Height < radius * 2)
            return;

        using var path = new GraphicsPath();
        var r = new Rectangle(0, 0, c.Width - 1, c.Height - 1);
        int d = radius * 2;

        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();

        using var pen = new Pen(Border);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.DrawPath(pen, path);
        c.Region = new Region(path);
    }
}
