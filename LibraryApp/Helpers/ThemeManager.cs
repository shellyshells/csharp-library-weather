namespace LibraryApp.Helpers;

public static class ThemeManager
{
    public static readonly Color LightBg         = Color.FromArgb(247, 248, 252);
    public static readonly Color LightSurface    = Color.White;
    public static readonly Color LightSidebar    = Color.FromArgb(44, 32, 80);
    public static readonly Color LightSideHover  = Color.FromArgb(62, 48, 105);
    public static readonly Color LightSideActive = Color.FromArgb(75, 58, 120);
    public static readonly Color LightText       = Color.FromArgb(33, 37, 41);
    public static readonly Color LightTextMuted  = Color.FromArgb(108, 117, 125);
    public static readonly Color LightAccent     = Color.FromArgb(99, 69, 180);
    public static readonly Color LightBorder     = Color.FromArgb(222, 226, 230);

    public static readonly Color DarkBg          = Color.FromArgb(24, 24, 36);
    public static readonly Color DarkSurface     = Color.FromArgb(36, 36, 52);
    public static readonly Color DarkSidebar     = Color.FromArgb(20, 18, 32);
    public static readonly Color DarkSideHover   = Color.FromArgb(45, 40, 70);
    public static readonly Color DarkSideActive  = Color.FromArgb(60, 52, 95);
    public static readonly Color DarkText        = Color.FromArgb(230, 232, 240);
    public static readonly Color DarkTextMuted   = Color.FromArgb(148, 155, 170);
    public static readonly Color DarkAccent      = Color.FromArgb(130, 100, 220);
    public static readonly Color DarkBorder      = Color.FromArgb(55, 55, 75);

    public static readonly Color Success = Color.FromArgb(40, 167, 69);
    public static readonly Color Danger  = Color.FromArgb(220, 53, 69);
    public static readonly Color Warning = Color.FromArgb(255, 165, 2);
    public static readonly Color Info    = Color.FromArgb(23, 162, 184);

    public static bool IsDark { get; private set; } = false;

    public static Color Bg        => IsDark ? DarkBg        : LightBg;
    public static Color Surface   => IsDark ? DarkSurface   : LightSurface;
    public static Color Sidebar   => IsDark ? DarkSidebar   : LightSidebar;
    public static Color SideHover => IsDark ? DarkSideHover : LightSideHover;
    public static Color SideActive=> IsDark ? DarkSideActive: LightSideActive;
    public static Color Text      => IsDark ? DarkText      : LightText;
    public static Color TextMuted => IsDark ? DarkTextMuted : LightTextMuted;
    public static Color Accent    => IsDark ? DarkAccent    : LightAccent;
    public static Color Border    => IsDark ? DarkBorder    : LightBorder;

    public static void Toggle() => IsDark = !IsDark;
    public static void Apply(Control root) => Walk(root);

    private static void Walk(Control c)
    {
        switch (c)
        {
            case DataGridView dgv: StyleGrid(dgv); break;
            case Button b when b.Tag?.ToString() == "side":
                b.BackColor = Sidebar; b.ForeColor = Color.FromArgb(200, 200, 220);
                b.FlatAppearance.BorderSize = 0;
                b.FlatAppearance.MouseOverBackColor = SideHover; break;
            case Button b when b.Tag?.ToString() == "accent":
                b.BackColor = Accent; b.ForeColor = Color.White;
                b.FlatAppearance.BorderSize = 0; break;
            case Button b when b.Tag?.ToString() == "danger":
                b.BackColor = Danger; b.ForeColor = Color.White;
                b.FlatAppearance.BorderSize = 0; break;
            case Button b when b.Tag?.ToString() == "success":
                b.BackColor = Success; b.ForeColor = Color.White;
                b.FlatAppearance.BorderSize = 0; break;
            case Button b:
                b.BackColor = Surface; b.ForeColor = Text;
                b.FlatAppearance.BorderColor = Border; break;
            case TextBox tb:  tb.BackColor = IsDark ? Color.FromArgb(42, 42, 60) : Color.White; tb.ForeColor = Text; break;
            case ComboBox cb: cb.BackColor = IsDark ? Color.FromArgb(42, 42, 60) : Color.White; cb.ForeColor = Text; break;
            case Label l when l.Tag?.ToString() == "heading": l.ForeColor = Text; break;
            case Label l when l.Tag?.ToString() == "muted":   l.ForeColor = TextMuted; break;
            case Label l when l.Tag?.ToString() == "accent":  l.ForeColor = Accent; break;
            case Label l when l.Tag?.ToString() == "side":    break;
            case Label l:     l.ForeColor = Text; break;
            case TabPage tp:  tp.BackColor = Bg; break;
            case TableLayoutPanel tl when tl.Tag?.ToString() == "card": tl.BackColor = Surface; break;
            case TableLayoutPanel tl: tl.BackColor = Bg; break;
            case Panel p when p.Tag?.ToString() == "side": p.BackColor = Sidebar; break;
            case Panel p when p.Tag?.ToString() == "card": p.BackColor = Surface; break;
            case Panel p:     p.BackColor = Bg; break;
            case GroupBox g:  g.ForeColor = TextMuted; g.BackColor = Bg; break;
            case TabControl t: t.BackColor = Bg; break;
            case StatusStrip s: s.BackColor = Surface; break;
            default: c.BackColor = Bg; c.ForeColor = Text; break;
        }
        foreach (Control child in c.Controls) Walk(child);
    }

    public static void StyleGrid(DataGridView g)
    {
        g.BackgroundColor = Bg;
        g.GridColor = Border;
        g.BorderStyle = BorderStyle.None;
        g.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        g.EnableHeadersVisualStyles = false;
        g.ColumnHeadersHeight = 36;
        g.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        g.ColumnHeadersDefaultCellStyle.BackColor = Accent;
        g.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        g.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9.5F);
        g.ColumnHeadersDefaultCellStyle.Padding = new Padding(6, 0, 0, 0);
        g.DefaultCellStyle.BackColor = Surface;
        g.DefaultCellStyle.ForeColor = Text;
        g.DefaultCellStyle.SelectionBackColor = IsDark ? Color.FromArgb(60, 52, 100) : Color.FromArgb(230, 224, 245);
        g.DefaultCellStyle.SelectionForeColor = Text;
        g.DefaultCellStyle.Padding = new Padding(6, 0, 0, 0);
        g.AlternatingRowsDefaultCellStyle.BackColor = IsDark ? Color.FromArgb(30, 30, 44) : Color.FromArgb(250, 250, 255);
        g.RowTemplate.Height = 32;
    }
}
