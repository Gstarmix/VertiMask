using System.Runtime.InteropServices;
namespace VertiMask;
internal sealed class WindowArranger
{
    private readonly List<(IntPtr Handle, Native.WINDOWPLACEMENT Placement)> _saved = new();
    private readonly HashSet<IntPtr> _stubborn = new();
    private Native.RECT _savedWorkArea;
    private bool _hasWorkArea;
    public bool Active { get; private set; }
    public void Arrange(Rectangle zone)
    {
        if (Active) return;
        _saved.Clear();
        _stubborn.Clear();
        Screen screen = Screen.FromRectangle(zone);
        _savedWorkArea = Native.RECT.From(screen.WorkingArea);
        Native.RECT zoneRect = Native.RECT.From(zone);
        _hasWorkArea = Native.SystemParametersInfo(
            Native.SPI_SETWORKAREA, 0, ref zoneRect, Native.SPIF_SENDCHANGE);
        uint myPid = (uint)Environment.ProcessId;
        string targetDevice = screen.DeviceName;
        var targets = new List<IntPtr>();
        Native.EnumWindows((h, _) =>
        {
            if (ShouldManage(h, myPid, targetDevice)) targets.Add(h);
            return true;
        }, IntPtr.Zero);
        foreach (IntPtr h in targets)
        {
            var wp = new Native.WINDOWPLACEMENT { length = Marshal.SizeOf<Native.WINDOWPLACEMENT>() };
            if (!Native.GetWindowPlacement(h, ref wp)) continue;
            _saved.Add((h, wp));
            if (wp.showCmd == Native.SW_SHOWMAXIMIZED)
                Native.ShowWindow(h, Native.SW_RESTORE);
            Native.MoveWindow(h, zone.X, zone.Y, zone.Width, zone.Height, true);
        }
        Active = true;
    }
    public void EnforceZone(Rectangle zone)
    {
        if (!Active) return;
        Screen screen = Screen.FromRectangle(zone);
        uint myPid = (uint)Environment.ProcessId;
        string targetDevice = screen.DeviceName;
        var targets = new List<IntPtr>();
        Native.EnumWindows((h, _) =>
        {
            if (ShouldManage(h, myPid, targetDevice)) targets.Add(h);
            return true;
        }, IntPtr.Zero);
        foreach (IntPtr h in targets)
        {
            if (_stubborn.Contains(h)) continue;
            bool known = false;
            foreach (var s in _saved) { if (s.Handle == h) { known = true; break; } }
            if (!known)
            {
                var wpNew = new Native.WINDOWPLACEMENT { length = Marshal.SizeOf<Native.WINDOWPLACEMENT>() };
                if (!Native.GetWindowPlacement(h, ref wpNew)) continue;
                _saved.Add((h, wpNew));
            }
            if (Native.IsZoomed(h)) Native.ShowWindow(h, Native.SW_RESTORE);
            if (!Native.GetWindowRect(h, out Native.RECT r)) continue;
            int w = r.Right - r.Left, hgt = r.Bottom - r.Top;
            if (Math.Abs(r.Left - zone.X) <= 2 && Math.Abs(r.Top - zone.Y) <= 2 &&
                Math.Abs(w - zone.Width) <= 2 && Math.Abs(hgt - zone.Height) <= 2)
                continue;
            Native.MoveWindow(h, zone.X, zone.Y, zone.Width, zone.Height, true);
            if (Native.GetWindowRect(h, out Native.RECT after))
            {
                int aw = after.Right - after.Left, ah = after.Bottom - after.Top;
                if (Math.Abs(aw - zone.Width) > 4 || Math.Abs(ah - zone.Height) > 4)
                    _stubborn.Add(h);
            }
        }
    }
    public void Restore()
    {
        if (!Active) return;
        foreach (var (h, wp) in _saved)
        {
            Native.WINDOWPLACEMENT placement = wp;
            Native.SetWindowPlacement(h, ref placement);
        }
        _saved.Clear();
        _stubborn.Clear();
        if (_hasWorkArea)
        {
            Native.RECT r = _savedWorkArea;
            Native.SystemParametersInfo(Native.SPI_SETWORKAREA, 0, ref r, Native.SPIF_SENDCHANGE);
            _hasWorkArea = false;
        }
        Active = false;
    }
    private static bool ShouldManage(IntPtr h, uint myPid, string targetDevice)
    {
        if (!Native.IsWindowVisible(h) || Native.IsIconic(h)) return false;
        if (Native.GetWindowTextLength(h) == 0) return false;
        Native.GetWindowThreadProcessId(h, out uint pid);
        if (pid == myPid) return false;
        long ex = Native.GetWindowLongPtr(h, Native.GWL_EXSTYLE).ToInt64();
        if ((ex & Native.WS_EX_TOOLWINDOW) != 0) return false;
        if (Native.GetWindow(h, Native.GW_OWNER) != IntPtr.Zero) return false;
        if (Native.DwmGetWindowAttribute(h, Native.DWMWA_CLOAKED, out int cloaked, sizeof(int)) == 0
            && cloaked != 0) return false;
        if (Screen.FromHandle(h).DeviceName != targetDevice) return false;
        return true;
    }
}