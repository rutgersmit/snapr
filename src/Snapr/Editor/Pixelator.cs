using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Snapr.Editor;

public static class Pixelator
{
    /// <summary>
    /// Returns the given region of <paramref name="source"/> with every block of
    /// <paramref name="blockSize"/> pixels replaced by its average colour.
    /// </summary>
    public static BitmapSource Pixelate(BitmapSource source, Int32Rect region, int blockSize)
    {
        if (source.Format != PixelFormats.Bgr32 && source.Format != PixelFormats.Bgra32)
            source = new FormatConvertedBitmap(source, PixelFormats.Bgr32, null, 0);

        int width = region.Width, height = region.Height, stride = width * 4;
        var pixels = new byte[stride * height];
        source.CopyPixels(region, pixels, stride, 0);

        for (int by = 0; by < height; by += blockSize)
        {
            int bh = Math.Min(blockSize, height - by);
            for (int bx = 0; bx < width; bx += blockSize)
            {
                int bw = Math.Min(blockSize, width - bx);
                long b = 0, g = 0, r = 0;
                for (int y = by; y < by + bh; y++)
                {
                    int i = y * stride + bx * 4;
                    for (int x = 0; x < bw; x++, i += 4)
                    {
                        b += pixels[i];
                        g += pixels[i + 1];
                        r += pixels[i + 2];
                    }
                }

                int count = bw * bh;
                byte ab = (byte)(b / count), ag = (byte)(g / count), ar = (byte)(r / count);
                for (int y = by; y < by + bh; y++)
                {
                    int i = y * stride + bx * 4;
                    for (int x = 0; x < bw; x++, i += 4)
                    {
                        pixels[i] = ab;
                        pixels[i + 1] = ag;
                        pixels[i + 2] = ar;
                        pixels[i + 3] = 255;
                    }
                }
            }
        }

        var result = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgr32, null, pixels, stride);
        result.Freeze();
        return result;
    }
}
