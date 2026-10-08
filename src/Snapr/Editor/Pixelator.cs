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
    /// candidate text (e.g. the Depix tool). The per-block noise makes that matching much harder,
    /// though it is risk reduction rather than a guarantee. Blocks sit on
    /// a fixed grid over the whole image and the noise depends only on <paramref name="seed"/> and
    /// the block's grid position, so the pattern stays stable while a region is being dragged.
    /// </remarks>
    public static BitmapSource Pixelate(BitmapSource source, Int32Rect region, int blockSize, int seed)
    {
        if (source.Format != PixelFormats.Bgr32 && source.Format != PixelFormats.Bgra32)
            source = new FormatConvertedBitmap(source, PixelFormats.Bgr32, null, 0);

        // Averages come from whole grid blocks (clipped to the image), not just the part inside the
        // region, so an edge block keeps its colour while the region's edge moves through it.
        int left = region.X - region.X % blockSize;
        int top = region.Y - region.Y % blockSize;
        int right = Math.Min(source.PixelWidth, CeilToBlock(region.X + region.Width, blockSize));
        int bottom = Math.Min(source.PixelHeight, CeilToBlock(region.Y + region.Height, blockSize));

        int srcWidth = right - left, srcHeight = bottom - top, srcStride = srcWidth * 4;
        var src = new byte[srcStride * srcHeight];
        source.CopyPixels(new Int32Rect(left, top, srcWidth, srcHeight), src, srcStride, 0);

        int width = region.Width, height = region.Height, stride = width * 4;
        var pixels = new byte[stride * height];

        for (int by = 0; by < srcHeight; by += blockSize)
        {
            int bh = Math.Min(blockSize, srcHeight - by);
            for (int bx = 0; bx < srcWidth; bx += blockSize)
            {
                int bw = Math.Min(blockSize, srcWidth - bx);

                long b = 0, g = 0, r = 0;
                for (int y = by; y < by + bh; y++)
                {
                    int i = y * srcStride + bx * 4;
                    for (int x = 0; x < bw; x++, i += 4)
                    {
                        b += src[i];
                        g += src[i + 1];
                        r += src[i + 2];
                    }
                }

                int count = bw * bh;
                int noise = Noise(seed, (left + bx) / blockSize, (top + by) / blockSize);
                byte ab = Channel(b / count + noise), ag = Channel(g / count + noise), ar = Channel(r / count + noise);

                // Paint only the part of this block that lies inside the region.
                int x0 = Math.Max(0, left + bx - region.X), x1 = Math.Min(width, left + bx + bw - region.X);
                int y0 = Math.Max(0, top + by - region.Y), y1 = Math.Min(height, top + by + bh - region.Y);
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

    private static int CeilToBlock(int value, int blockSize) => (value + blockSize - 1) / blockSize * blockSize;

    private static byte Channel(long value) => (byte)Math.Clamp(value, 0, 255);
}
