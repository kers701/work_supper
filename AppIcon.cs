namespace ProcessGuard;

public static class AppIcon
{
    public static Icon Create()
    {
        using var bitmap = new Bitmap(64, 64);
        using var g = Graphics.FromImage(bitmap);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);
        using var badge = new SolidBrush(Color.FromArgb(30, 111, 211));
        g.FillRoundedRectangle(badge, new Rectangle(1, 1, 62, 62), 14);
        using var dark = new SolidBrush(Color.FromArgb(10, 65, 145));
        g.FillEllipse(dark, 6, 17, 19, 32);
        g.FillEllipse(dark, 39, 17, 19, 32);
        using var face = new SolidBrush(Color.FromArgb(227, 243, 255));
        g.FillEllipse(face, 13, 11, 38, 42);
        using var muzzle = new SolidBrush(Color.FromArgb(194, 225, 250));
        g.FillEllipse(muzzle, 20, 32, 24, 19);
        using var ink = new SolidBrush(Color.FromArgb(18, 42, 75));
        g.FillEllipse(ink, 20, 25, 6, 6);
        g.FillEllipse(ink, 38, 25, 6, 6);
        g.FillEllipse(ink, 28, 36, 8, 6);
        using var pen = new Pen(ink, 2);
        g.DrawArc(pen, 26, 37, 12, 12, 0, 180);
        var handle = bitmap.GetHicon();
        return (Icon)Icon.FromHandle(handle).Clone();
    }
}
