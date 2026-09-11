namespace ProcessGuard;

public static class AppIcon
{
    public static Icon Create()
    {
        using var bitmap = new Bitmap(64, 64);
        using var g = Graphics.FromImage(bitmap);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);
        using var glow = new SolidBrush(Color.FromArgb(42, 39, 120, 224));
        g.FillEllipse(glow, 4, 4, 56, 56);
        using var pen = new Pen(Color.FromArgb(190, 225, 245, 255), 4) { StartCap = System.Drawing.Drawing2D.LineCap.Round, EndCap = System.Drawing.Drawing2D.LineCap.Round };
        var center = new PointF(32, 32);
        for (var i = 0; i < 3; i++)
        {
            var angle = i * Math.PI / 3;
            var dx = (float)Math.Cos(angle) * 23;
            var dy = (float)Math.Sin(angle) * 23;
            g.DrawLine(pen, center.X - dx, center.Y - dy, center.X + dx, center.Y + dy);
            DrawBranches(g, pen, center, new PointF(center.X + dx, center.Y + dy), angle);
            DrawBranches(g, pen, center, new PointF(center.X - dx, center.Y - dy), angle + Math.PI);
        }
        using var dot = new SolidBrush(Color.FromArgb(225, 255, 255, 255));
        g.FillEllipse(dot, 27, 27, 10, 10);
        var handle = bitmap.GetHicon();
        return (Icon)Icon.FromHandle(handle).Clone();
    }

    private static void DrawBranches(Graphics g, Pen pen, PointF center, PointF tip, double angle)
    {
        var length = 7f;
        var branchAngle = Math.PI / 6;
        var x1 = tip.X - (float)Math.Cos(angle - branchAngle) * length;
        var y1 = tip.Y - (float)Math.Sin(angle - branchAngle) * length;
        var x2 = tip.X - (float)Math.Cos(angle + branchAngle) * length;
        var y2 = tip.Y - (float)Math.Sin(angle + branchAngle) * length;
        g.DrawLine(pen, tip.X, tip.Y, x1, y1);
        g.DrawLine(pen, tip.X, tip.Y, x2, y2);
    }
}
