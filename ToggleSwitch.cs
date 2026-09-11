namespace ProcessGuard;

public sealed class ToggleSwitch : CheckBox
{
    public ToggleSwitch()
    {
        AutoSize = false;
        Width = 54;
        Height = 30;
        Appearance = Appearance.Normal;
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        Text = "";
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        SetStyle(ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
        Cursor = Cursors.Hand;
        TabStop = true;
        CheckedChanged += (_, _) => Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(Parent?.BackColor ?? SystemColors.Control);
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        var track = new Rectangle(1, 5, Width - 2, Height - 10);
        using var trackBrush = new SolidBrush(Checked ? Color.FromArgb(39, 120, 224) : Color.FromArgb(190, 201, 214));
        e.Graphics.FillRoundedRectangle(trackBrush, track, track.Height / 2);
        var diameter = Height - 14;
        var x = Checked ? Width - diameter - 5 : 5;
        using var thumbBrush = new SolidBrush(Color.White);
        e.Graphics.FillEllipse(thumbBrush, x, 7, diameter, diameter);
        using var shadow = new Pen(Color.FromArgb(45, 0, 0, 0), 1);
        e.Graphics.DrawEllipse(shadow, x, 7, diameter, diameter);
    }
}

internal static class GraphicsExtensions
{
    public static void FillRoundedRectangle(this Graphics graphics, Brush brush, Rectangle bounds, int radius)
    {
        using var path = new System.Drawing.Drawing2D.GraphicsPath();
        var d = radius * 2;
        path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
        path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
        path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        graphics.FillPath(brush, path);
    }
}
