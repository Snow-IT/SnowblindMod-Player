using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

// Loads tray_icon_generated.png, removes white background, and generates transparent ICO
var repoRoot = FindRepoRoot();
var pngPath = Path.Combine(repoRoot, "mockups", "tray_icon_generated.png");
var outIco = Path.Combine(repoRoot, "Assets", "snowflake_play_icon.ico");

if (!File.Exists(pngPath))
{
    Console.Error.WriteLine($"PNG not found: {pngPath}");
    return 1;
}

Directory.CreateDirectory(Path.GetDirectoryName(outIco)!);

using var srcBmp = (Bitmap)Image.FromFile(pngPath);
Console.WriteLine($"Loaded PNG: {srcBmp.Width}x{srcBmp.Height}, PixelFormat: {srcBmp.PixelFormat}");

// Convert to 32-bit ARGB and make white/near-white transparent
Bitmap transparentBmp = new Bitmap(srcBmp.Width, srcBmp.Height, PixelFormat.Format32bppArgb);
for (int y = 0; y < srcBmp.Height; y++)
{
    for (int x = 0; x < srcBmp.Width; x++)
    {
        Color pixel = srcBmp.GetPixel(x, y);
        
        // If pixel is white or near-white (R>240, G>240, B>240), make transparent
        if (pixel.R > 240 && pixel.G > 240 && pixel.B > 240)
        {
            transparentBmp.SetPixel(x, y, Color.Transparent);
        }
        else
        {
            // Keep pixel, ensure alpha = 255
            transparentBmp.SetPixel(x, y, Color.FromArgb(255, pixel.R, pixel.G, pixel.B));
        }
    }
}

Console.WriteLine("White background removed, transparency applied");

// Create multi-size ICO
var sizes = new[] { 16, 24, 32, 48, 64, 128, 256 };
CreateMultiIcon(outIco, transparentBmp, sizes);
transparentBmp.Dispose();

Console.WriteLine($"? Generated {outIco}");
return 0;

static string FindRepoRoot()
{
    var dir = AppContext.BaseDirectory;
    while (!string.IsNullOrEmpty(dir))
    {
        if (Directory.Exists(Path.Combine(dir, ".git")) || File.Exists(Path.Combine(dir, "SnowblindModPlayer.sln")))
            return dir;
        dir = Directory.GetParent(dir)?.FullName;
    }
    return Directory.GetCurrentDirectory();
}

static void CreateMultiIcon(string outPath, Bitmap source, int[] sizes)
{
    using var fs = new FileStream(outPath, FileMode.Create, FileAccess.Write);

    // ICONDIR: Reserved (2) = 0, Type (2)=1, Count (2)
    WriteU16(fs, 0);
    WriteU16(fs, 1);
    WriteU16(fs, (ushort)sizes.Length);

    var images = new byte[sizes.Length][];
    for (int i = 0; i < sizes.Length; i++)
    {
        using var resized = new Bitmap(sizes[i], sizes[i], PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(resized))
        {
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            g.Clear(Color.Transparent);
            g.DrawImage(source, 0, 0, sizes[i], sizes[i]);
        }

        using var ms = new MemoryStream();
        resized.Save(ms, ImageFormat.Png);
        images[i] = ms.ToArray();
    }

    int offset = 6 + (16 * sizes.Length);
    for (int i = 0; i < sizes.Length; i++)
    {
        int s = sizes[i];
        fs.WriteByte((byte)(s == 256 ? 0 : s)); // width
        fs.WriteByte((byte)(s == 256 ? 0 : s)); // height
        fs.WriteByte(0); // color count
        fs.WriteByte(0); // reserved
        WriteU16(fs, 1); // planes
        WriteU16(fs, 32); // bit count
        WriteU32(fs, (uint)images[i].Length);
        WriteU32(fs, (uint)offset);
        offset += images[i].Length;
    }

    for (int i = 0; i < images.Length; i++)
        fs.Write(images[i], 0, images[i].Length);
}

static void WriteU16(Stream s, ushort v)
{
    s.WriteByte((byte)(v & 0xFF));
    s.WriteByte((byte)((v >> 8) & 0xFF));
}

static void WriteU32(Stream s, uint v)
{
    s.WriteByte((byte)(v & 0xFF));
    s.WriteByte((byte)((v >> 8) & 0xFF));
    s.WriteByte((byte)((v >> 16) & 0xFF));
    s.WriteByte((byte)((v >> 24) & 0xFF));
}
