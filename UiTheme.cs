namespace ProcessGuard;

/// <summary>统一放大字体与控件尺寸，方便车间大屏/触摸操作。</summary>
public static class UiTheme
{
    public static readonly Font Ui = new("Microsoft YaHei UI", 12f, FontStyle.Regular);
    public static readonly Font UiBold = new("Microsoft YaHei UI", 12f, FontStyle.Bold);
    public static readonly Font Title = new("Microsoft YaHei UI", 13f, FontStyle.Bold);
    public static readonly Font Grid = new("Microsoft YaHei UI", 11.5f, FontStyle.Regular);

    public static void Apply(Control root)
    {
        root.Font = Ui;
        foreach (Control c in root.Controls)
            Apply(c);
    }
}
