using System.Diagnostics;
using System.Drawing.Imaging;
using System.Text.Json;
namespace VertiMask;
internal static class ZoneCapture
{
    public const string Photos = "Photos";
    public const string Gifs = "GIF";
    public const string Videos = "Videos";
    public static string RootDir
    {
        get
        {
            string dir = Path.Combine(AppContext.BaseDirectory, "Captures");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }
    public static string OutputDir(string sub)
    {
        string dir = Path.Combine(RootDir, sub);
        Directory.CreateDirectory(dir);
        return dir;
    }
    public static string OutputPath(string ext, string sub) =>
        Path.Combine(OutputDir(sub), $"VertiMask_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.{ext}");
    public static void OpenFolder(string? sub = null)
    {
        try
        {
            string dir = sub == null ? RootDir : OutputDir(sub);
            Process.Start(new ProcessStartInfo("explorer.exe", dir) { UseShellExecute = true });
        }
        catch {  }
    }
    public readonly record struct Item(string Path, string Kind);
    public static List<Item> Recent(int max = 60)
    {
        var items = new List<Item>();
        try
        {
            foreach (var (sub, kind) in new[] { (Photos, "Photo"), (Gifs, "GIF"), (Videos, "Video") })
            {
                string dir = Path.Combine(RootDir, sub);
                if (!Directory.Exists(dir)) continue;
                foreach (string f in Directory.GetFiles(dir))
                    items.Add(new Item(f, kind));
            }
            items.Sort((a, b) => File.GetLastWriteTimeUtc(b.Path).CompareTo(File.GetLastWriteTimeUtc(a.Path)));
            if (items.Count > max) items = items.GetRange(0, max);
        }
        catch {  }
        return items;
    }
    public static void OpenFile(string path)
    {
        try { Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }); } catch { }
    }
    public static void SelectInExplorer(string path)
    {
        try { Process.Start("explorer.exe", $"/select,\"{path}\""); } catch { }
    }
    private sealed class TrashEntry
    {
        public string Original { get; set; } = "";
        public string Trash { get; set; } = "";
        public DateTime DeletedAt { get; set; }
    }
    private static List<TrashEntry>? _trash;
    private static string TrashDir
    {
        get { string d = Path.Combine(RootDir, ".trash"); Directory.CreateDirectory(d); return d; }
    }
    private static string TrashIndexPath => Path.Combine(TrashDir, "trash.json");
    private static List<TrashEntry> Trash()
    {
        if (_trash != null) return _trash;
        try
        {
            _trash = File.Exists(TrashIndexPath)
                ? JsonSerializer.Deserialize<List<TrashEntry>>(File.ReadAllText(TrashIndexPath)) ?? new()
                : new();
        }
        catch { _trash = new(); }
        _trash.RemoveAll(e => !File.Exists(e.Trash));
        return _trash;
    }
    private static void SaveTrash()
    {
        try { File.WriteAllText(TrashIndexPath, JsonSerializer.Serialize(_trash)); } catch {  }
    }
    public static bool Delete(string path)
    {
        try
        {
            var list = Trash();
            string t = Path.Combine(TrashDir, Guid.NewGuid().ToString("N") + "_" + Path.GetFileName(path));
            File.Move(path, t);
            list.Add(new TrashEntry { Original = path, Trash = t, DeletedAt = DateTime.UtcNow });
            SaveTrash();
            return true;
        }
        catch { return false; }
    }
    public static bool CanUndo => Trash().Count > 0;
    public static string? UndoDelete()
    {
        var list = Trash();
        while (list.Count > 0)
        {
            TrashEntry e = list[^1];
            list.RemoveAt(list.Count - 1);
            try
            {
                if (!File.Exists(e.Trash)) { SaveTrash(); continue; }
                string dest = e.Original;
                if (File.Exists(dest))
                {
                    string dir = Path.GetDirectoryName(e.Original)!;
                    string name = Path.GetFileNameWithoutExtension(e.Original);
                    string ext = Path.GetExtension(e.Original);
                    int i = 1;
                    do { dest = Path.Combine(dir, $"{name}_restaure{(i == 1 ? "" : i.ToString())}{ext}"); i++; }
                    while (File.Exists(dest));
                }
                File.Move(e.Trash, dest);
                SaveTrash();
                return dest;
            }
            catch { SaveTrash();  }
        }
        return null;
    }
    public static void PurgeOldTrash(int days = 30)
    {
        try
        {
            var list = Trash();
            DateTime cutoff = DateTime.UtcNow.AddDays(-days);
            foreach (TrashEntry e in list.Where(e => e.DeletedAt < cutoff).ToList())
            {
                try { if (File.Exists(e.Trash)) File.Delete(e.Trash); } catch { }
                list.Remove(e);
            }
            SaveTrash();
        }
        catch {  }
    }
    public static string? Rename(string path, string newBaseName)
    {
        try
        {
            string dir = Path.GetDirectoryName(path)!;
            string ext = Path.GetExtension(path);
            string clean = string.Concat(newBaseName.Split(Path.GetInvalidFileNameChars())).Trim();
            if (clean.Length == 0) return null;
            string target = Path.Combine(dir, clean + ext);
            if (string.Equals(target, path, StringComparison.OrdinalIgnoreCase)) return path;
            if (File.Exists(target)) return null;
            File.Move(path, target);
            return target;
        }
        catch { return null; }
    }
    public static Bitmap GrabZone(Rectangle zone)
    {
        var bmp = new Bitmap(zone.Width, zone.Height, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
            g.CopyFromScreen(zone.Location, Point.Empty, zone.Size, CopyPixelOperation.SourceCopy);
        return bmp;
    }
    public static string Screenshot(Rectangle zone)
    {
        using Bitmap bmp = GrabZone(zone);
        string path = OutputPath("png", Photos);
        bmp.Save(path, ImageFormat.Png);
        try { Clipboard.SetImage(bmp); } catch {  }
        return path;
    }
}
internal sealed class GifRecorder
{
    private readonly System.Windows.Forms.Timer _timer = new();
    private readonly List<GifWriter.EncodedFrame> _frames = new();
    private Rectangle _zone;
    private readonly int _maxWidth;
    private int _w, _h;
    private const int MaxFrames = 600;
    public int Fps { get; }
    public bool Recording { get; private set; }
    public int FrameCount => _frames.Count;
    public GifRecorder(int fps = 12, int maxWidth = 480)
    {
        Fps = Math.Clamp(fps, 5, 30);
        _maxWidth = maxWidth;
        _timer.Interval = Math.Max(1, 1000 / Fps);
        _timer.Tick += (_, _) => Grab();
    }
    public void Start(Rectangle zone)
    {
        if (Recording) return;
        _zone = zone;
        _frames.Clear();
        Recording = true;
        _timer.Start();
    }
    public string? Stop()
    {
        if (!Recording) return null;
        _timer.Stop();
        Recording = false;
        if (_frames.Count == 0) return null;
        string path = ZoneCapture.OutputPath("gif", ZoneCapture.Gifs);
        GifWriter.WriteEncoded(_frames, _w, _h, Fps, path);
        _frames.Clear();
        return path;
    }
    private void Grab()
    {
        if (_frames.Count >= MaxFrames) { _timer.Stop(); return; }
        using Bitmap full = ZoneCapture.GrabZone(_zone);
        using Bitmap small = Downscale(full, _maxWidth);
        _w = small.Width;
        _h = small.Height;
        _frames.Add(GifWriter.Encode(small));
    }
    private static Bitmap Downscale(Bitmap src, int maxWidth)
    {
        if (src.Width <= maxWidth) return (Bitmap)src.Clone();
        int w = maxWidth;
        int h = Math.Max(1, (int)Math.Round(src.Height * (double)w / src.Width));
        var dst = new Bitmap(w, h, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(dst);
        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        g.DrawImage(src, 0, 0, w, h);
        return dst;
    }
}
internal sealed class CountdownForm : Form
{
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 1000 };
    private readonly Rectangle _zone;
    private readonly Action _onElapsed;
    private int _remaining;
    public CountdownForm(Rectangle zone, int seconds, Action onElapsed)
    {
        _zone = zone;
        _remaining = Math.Max(1, seconds);
        _onElapsed = onElapsed;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        AutoScaleMode = AutoScaleMode.None;
        BackColor = Color.Black;
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 72f, FontStyle.Bold);
        DoubleBuffered = true;
        Text = "VertiMask_Countdown";
        _timer.Tick += OnTick;
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
        const int size = 200;
        int x = _zone.X + (_zone.Width - size) / 2;
        int y = _zone.Y + (_zone.Height - size) / 2;
        Native.SetWindowPos(Handle, Native.HWND_TOPMOST, x, y, size, size,
            Native.SWP_SHOWWINDOW | Native.SWP_NOACTIVATE);
        _timer.Start();
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        TextRenderer.DrawText(e.Graphics, _remaining.ToString(), Font, ClientRectangle, ForeColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }
    private void OnTick(object? sender, EventArgs e)
    {
        _remaining--;
        if (_remaining <= 0)
        {
            _timer.Stop();
            Hide();
            BeginInvoke(new Action(() => { _onElapsed(); Close(); }));
            return;
        }
        Invalidate();
    }
}