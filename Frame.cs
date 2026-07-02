namespace VertiMask;
internal static class Frame
{
    public static Rectangle Hole(Rectangle monitor, int ratioW, int ratioH)
    {
        int holeH = monitor.Height;
        int holeW = (int)Math.Round(holeH * (double)ratioW / ratioH);
        if (holeW > monitor.Width)
        {
            holeW = monitor.Width;
            holeH = (int)Math.Round(holeW * (double)ratioH / ratioW);
        }
        int x = (monitor.Width - holeW) / 2;
        int y = (monitor.Height - holeH) / 2;
        return new Rectangle(x, y, holeW, holeH);
    }
}