using System.Diagnostics;
using System.Drawing.Drawing2D;
namespace VertiMask;
internal static class Feedback
{
    public static void Flash(Rectangle zone) => new FlashOverlay(zone).Show();
    public static void Toast(Rectangle monitor, Rectangle zone, string message, Action? onClick = null)
        => new ToastForm(message, onClick).ShowAt(monitor, zone);
    public static Point CornerOutsideZone(Rectangle monitor, Rectangle zone, int w, int h)
    {
        int x = monitor.Left + 16;
        if (x + w > zone.Left - 8) x = Math.Max(monitor.Left + 8, zone.Left - w - 8);
        return new Point(x, monitor.Top + 16);
    }
}
internal sealed class FlashOverlay : Form
{
    private readonly System.Windows.Forms.Timer _t = new() { Interval = 25 };
    private readonly Rectangle _zone;
    public FlashOverlay(Rectangle zone)
    {
        _zone = zone;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        AutoScaleMode = AutoScaleMode.None;
        BackColor = Color.White;
        TopMost = true;
        Opacity = 0.75;
        Text = "VertiMask_Flash";
        _t.Tick += (_, _) =>
        {
            Opacity -= 0.13;
            if (Opacity <= 0.02) { _t.Stop(); Close(); }
        };
    }
    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams cp = base.CreateParams;
            cp.ExStyle |= Native.WS_EX_TOOLWINDOW | Native.WS_EX_TOPMOST | Native.WS_EX_NOACTIVATE;
            return cp;
        }
    }
    protected override bool ShowWithoutActivation => true;
    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        Native.SetWindowPos(Handle, Native.HWND_TOPMOST, _zone.X, _zone.Y, _zone.Width, _zone.Height,
            Native.SWP_SHOWWINDOW | Native.SWP_NOACTIVATE);
        _t.Start();
    }
}
internal sealed class ToastForm : Form
{
    private readonly System.Windows.Forms.Timer _life = new() { Interval = 3000 };
    private readonly Action? _onClick;
    private const int W = 340, H = 46;
    public ToastForm(string message, Action? onClick)
    {
        _onClick = onClick;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        AutoScaleMode = AutoScaleMode.None;
        BackColor = Color.FromArgb(30, 30, 34);
        TopMost = true;
        Text = "VertiMask_Toast";
        var lbl = new Label
        {
            Text = message,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.FromArgb(150, 230, 180),
            Font = new Font("Segoe UI Semibold", 10f),
            Cursor = Cursors.Hand,
        };
        lbl.Click += (_, _) => { _onClick?.Invoke(); Close(); };
        Click += (_, _) => { _onClick?.Invoke(); Close(); };
        Controls.Add(lbl);
        _life.Tick += (_, _) => { _life.Stop(); Close(); };
    }
    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams cp = base.CreateParams;
            cp.ExStyle |= Native.WS_EX_TOOLWINDOW | Native.WS_EX_TOPMOST | Native.WS_EX_NOACTIVATE;
            return cp;
        }
    }
    protected override bool ShowWithoutActivation => true;
    public void ShowAt(Rectangle monitor, Rectangle zone)
    {
        Show();
        Point p = Feedback.CornerOutsideZone(monitor, zone, W, H);
        Native.SetWindowPos(Handle, Native.HWND_TOPMOST, p.X, p.Y, W, H,
            Native.SWP_SHOWWINDOW | Native.SWP_NOACTIVATE);
        using (var path = Rounded(W, H, 10)) Region = new Region(path);
        _life.Start();
    }
    private static GraphicsPath Rounded(int w, int h, int r)
    {
        int d = r * 2;
        var p = new GraphicsPath();
        p.AddArc(0, 0, d, d, 180, 90);
        p.AddArc(w - d, 0, d, d, 270, 90);
        p.AddArc(w - d, h - d, d, d, 0, 90);
        p.AddArc(0, h - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }
}
internal sealed class RecBadge : Form
{
    private readonly System.Windows.Forms.Timer _t = new() { Interval = 500 };
    private readonly Stopwatch _sw = new();
    private readonly string _label;
    private bool _blink = true;
    private const int W = 168, H = 40;
    public RecBadge(string label)
    {
        _label = label;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        AutoScaleMode = AutoScaleMode.None;
        BackColor = Color.FromArgb(24, 24, 28);
        TopMost = true;
        DoubleBuffered = true;
        Text = "VertiMask_Rec";
        _t.Tick += (_, _) => { _blink = !_blink; Invalidate(); };
    }
    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams cp = base.CreateParams;
            cp.ExStyle |= Native.WS_EX_TOOLWINDOW | Native.WS_EX_TOPMOST | Native.WS_EX_NOACTIVATE;
            return cp;
        }
    }
    protected override bool ShowWithoutActivation => true;
    public void ShowAt(Rectangle monitor, Rectangle zone)
    {
        Show();
        Point p = Feedback.CornerOutsideZone(monitor, zone, W, H);
        Native.SetWindowPos(Handle, Native.HWND_TOPMOST, p.X, p.Y, W, H,
            Native.SWP_SHOWWINDOW | Native.SWP_NOACTIVATE);
        var path = RoundedRegion();
        Region = new Region(path);
        _sw.Start();
        _t.Start();
    }
    public void Stop()
    {
        _t.Stop();
        _sw.Stop();
        Close();
        Dispose();
    }
    private GraphicsPath RoundedRegion()
    {
        int d = 16;
        var p = new GraphicsPath();
        p.AddArc(0, 0, d, d, 180, 90);
        p.AddArc(W - d, 0, d, d, 270, 90);
        p.AddArc(W - d, H - d, d, d, 0, 90);
        p.AddArc(0, H - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        if (_blink)
            using (var b = new SolidBrush(Color.FromArgb(230, 60, 60)))
                g.FillEllipse(b, 14, H / 2 - 7, 14, 14);
        int total = (int)_sw.Elapsed.TotalSeconds;
        string txt = $"REC  {total / 60}:{total % 60:00}  {_label}";
        TextRenderer.DrawText(e.Graphics, txt, new Font("Segoe UI Semibold", 10.5f),
            new Rectangle(34, 0, W - 38, H), Color.White,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
    }
}