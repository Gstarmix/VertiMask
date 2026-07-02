using System.Runtime.InteropServices;
namespace VertiMask;
internal sealed class Taskbar
{
    private readonly List<IntPtr> _hidden = new();
    private int _savedState = -1;
    [StructLayout(LayoutKind.Sequential)]
    private struct APPBARDATA
    {
        public uint cbSize;
        public IntPtr hWnd;
        public uint uCallbackMessage;
        public uint uEdge;
        public Native.RECT rc;
        public int lParam;
    }
    [DllImport("shell32.dll")]
    private static extern UIntPtr SHAppBarMessage(uint dwMessage, ref APPBARDATA pData);
    private const uint ABM_GETSTATE = 0x00000004;
    private const uint ABM_SETSTATE = 0x0000000A;
    private const int ABS_AUTOHIDE = 0x00000001;
    private static int GetState()
    {
        var d = new APPBARDATA { cbSize = (uint)Marshal.SizeOf<APPBARDATA>() };
        return (int)SHAppBarMessage(ABM_GETSTATE, ref d).ToUInt32();
    }
    private static void SetState(int state)
    {
        var d = new APPBARDATA { cbSize = (uint)Marshal.SizeOf<APPBARDATA>(), lParam = state };
        SHAppBarMessage(ABM_SETSTATE, ref d);
    }
    public void SetAutoHide()
    {
        if (_savedState >= 0) return;
        _savedState = GetState();
        SetState(_savedState | ABS_AUTOHIDE);
    }
    public void RestoreAutoHide()
    {
        if (_savedState < 0) return;
        SetState(_savedState);
        _savedState = -1;
    }
    public void Hide()
    {
        if (_hidden.Count > 0) return;
        IntPtr main = Native.FindWindow("Shell_TrayWnd", null);
        if (main != IntPtr.Zero)
        {
            Native.ShowWindow(main, Native.SW_HIDE);
            _hidden.Add(main);
        }
        IntPtr secondary = IntPtr.Zero;
        while ((secondary = Native.FindWindowEx(IntPtr.Zero, secondary, "Shell_SecondaryTrayWnd", null)) != IntPtr.Zero)
        {
            Native.ShowWindow(secondary, Native.SW_HIDE);
            _hidden.Add(secondary);
        }
    }
    public void Show()
    {
        foreach (IntPtr h in _hidden)
            Native.ShowWindow(h, Native.SW_SHOW);
        _hidden.Clear();
    }
}