using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Snapr.Editor;

public static class Pixelator
{
    /// <summary>How far (per channel, 0-255) each block's colour is randomly nudged.</summary>
    private const int NoiseAmplitude = 18;

    /// <summary>
    /// Returns the given region of <paramref name="source"/> with every block of
    /// <paramref name="blockSize"/> pixels replaced by its average colour plus a little noise.
    /// </summary>
    /// <remarks>
    /// Plain block averages of text can be partly reversed by matching them against rendered
    /// candidate text (e.g. the Depix tool). The per-block noise breaks that match. Blocks sit on
    /// a fixed grid over the whole image and the noise depends only on <paramref name="seed"/> and
    /// the block's grid position, so the pattern stays stable while a region is being dragged.
    /// </remarks>
    public static BitmapSource Pixelate(BitmapSource source, Int32Rect region, int blockSize, int seed)
    {
        if (source.Format != PixelFormats.Bgr32 && source.Format != PixelFormats.Bgra32)
            source = new FormatConvertedBitmap(source, PixelFormats.Bgr32, null, 0);

        int width = region.Width, height = region.Height, stride = width * 4;
        var pixels = new byte[stride * height];
        source.CopyPixels(region, pixels, stride, 0);

        // Grid lines in region coordinates, aligned to multiples of blockSize in the image.
        int firstX = -(region.X % blockSize), firstY = -(region.Y % blockSize);

        for (int gy = firstY; gy < height; gy += blockSize)
        {
            int y0 = Math.Max(0, gy), y1 = Math.Min(height, gy + blockSize);
            for (int gx = firstX; gx < width; gx += blockSize)
            {
                int x0 = Math.Max(0, gx), x1 = Math.Min(width, gx + blockSize);

                long b = 0, g = 0, r = 0;
                for (int y = y0; y < y1; y++)
                {
                    int i = y * stride + x0 * 4;
                    for (int x = x0; x < x1; x++, i += 4)
                    {
                        b += pixels[i];
                        g += pixels[i + 1];
                        r += pixels[i + 2];
                    }
                }

                int count = (x1 - x0) * (y1 - y0);
                int noise = Noise(seed, (region.X + gx) / blockSize, (region.Y + gy) / blockSize);
                byte ab = Channel(b / count + noise), ag = Channel(g / count + noise), ar = Channel(r / count + noise);

                for (int y = y0; y < y1; y++)
                {
                    int i = y * stride + x0 * 4;
                    for (int x = x0; x < x1; x++, i += 4)
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

    /// <summary>Deterministic pseudo-random offset in [-NoiseAmplitude, NoiseAmplitude] for one block.</summary>
    private static int Noise(int seed, int column, int row)
    {
        unchecked
        {
            uint h = (uint)seed ^ ((uint)column * 0x9E3779B1u) ^ ((uint)row * 0x85EBCA77u);
            h ^= h >> 15;
            h *= 0x2C1B3C6Du;
            h ^= h >> 12;
            h *= 0x297A2D39u;
            h ^= h >> 15;
            return (int)(h % (2 * NoiseAmplitude + 1)) - NoiseAmplitude;
        }
    }

    private static byte Channel(long value) => (byte)Math.Clamp(value, 0, 255);
}
