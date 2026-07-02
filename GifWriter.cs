using System.Drawing.Imaging;
using System.Text;
namespace VertiMask;
internal static class GifWriter
{
    public readonly struct EncodedFrame
    {
        public readonly byte[] ColorTable;
        public readonly byte[] ImageData;
        public EncodedFrame(byte[] colorTable, byte[] imageData)
        {
            ColorTable = colorTable;
            ImageData = imageData;
        }
    }
    public static EncodedFrame Encode(Bitmap frame)
    {
        using var ms = new MemoryStream();
        frame.Save(ms, ImageFormat.Gif);
        byte[] g = ms.ToArray();
        int pos = 6;
        byte packed = g[pos + 4];
        pos += 7;
        var colorTable = new byte[768];
        if ((packed & 0x80) != 0)
        {
            int gctBytes = (2 << (packed & 0x07)) * 3;
            Array.Copy(g, pos, colorTable, 0, Math.Min(gctBytes, 768));
            pos += gctBytes;
        }
        while (true)
        {
            byte b = g[pos];
            if (b == 0x2C)
            {
                int lctPacked = g[pos + 9];
                pos += 10;
                if ((lctPacked & 0x80) != 0)
                {
                    int lctBytes = (2 << (lctPacked & 0x07)) * 3;
                    Array.Copy(g, pos, colorTable, 0, Math.Min(lctBytes, 768));
                    pos += lctBytes;
                }
                int start = pos;
                pos++;
                while (g[pos] != 0x00) pos += g[pos] + 1;
                pos++;
                var imageData = new byte[pos - start];
                Array.Copy(g, start, imageData, 0, imageData.Length);
                return new EncodedFrame(colorTable, imageData);
            }
            if (b == 0x21)
            {
                pos += 2;
                while (g[pos] != 0x00) pos += g[pos] + 1;
                pos++;
            }
            else pos++;
        }
    }
    public static void WriteEncoded(IReadOnlyList<EncodedFrame> frames, int width, int height, int fps, string path)
    {
        if (frames.Count == 0) return;
        int delayCs = Math.Max(2, (int)Math.Round(100.0 / Math.Max(1, fps)));
        using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
        fs.Write(Encoding.ASCII.GetBytes("GIF89a"));
        WriteUShort(fs, (ushort)width);
        WriteUShort(fs, (ushort)height);
        fs.WriteByte(0x70);
        fs.WriteByte(0x00);
        fs.WriteByte(0x00);
        fs.WriteByte(0x21); fs.WriteByte(0xFF); fs.WriteByte(0x0B);
        fs.Write(Encoding.ASCII.GetBytes("NETSCAPE2.0"));
        fs.WriteByte(0x03); fs.WriteByte(0x01);
        WriteUShort(fs, 0);
        fs.WriteByte(0x00);
        foreach (EncodedFrame f in frames)
        {
            fs.WriteByte(0x21); fs.WriteByte(0xF9); fs.WriteByte(0x04);
            fs.WriteByte(0x00);
            WriteUShort(fs, (ushort)delayCs);
            fs.WriteByte(0x00);
            fs.WriteByte(0x00);
            fs.WriteByte(0x2C);
            WriteUShort(fs, 0); WriteUShort(fs, 0);
            WriteUShort(fs, (ushort)width); WriteUShort(fs, (ushort)height);
            fs.WriteByte(0x87);
            fs.Write(f.ColorTable, 0, 768);
            fs.Write(f.ImageData, 0, f.ImageData.Length);
        }
        fs.WriteByte(0x3B);
    }
    public static void Write(IReadOnlyList<Bitmap> frames, int fps, string path)
    {
        if (frames.Count == 0) return;
        var encoded = new List<EncodedFrame>(frames.Count);
        foreach (Bitmap f in frames) encoded.Add(Encode(f));
        WriteEncoded(encoded, frames[0].Width, frames[0].Height, fps, path);
    }
    private static void WriteUShort(Stream s, ushort v)
    {
        s.WriteByte((byte)(v & 0xFF));
        s.WriteByte((byte)((v >> 8) & 0xFF));
    }
}