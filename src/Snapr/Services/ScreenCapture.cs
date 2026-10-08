using System.Drawing;
using System.Drawing.Imaging;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Snapr.Interop;

namespace Snapr.Services;

/// <summary>A frozen copy of the whole virtual desktop, in physical pixels.</summary>
public sealed record ScreenShot(BitmapSource Image, Rectangle Bounds);

public static class ScreenCapture
{
    public static Rectangle VirtualScreenBounds => new(
        NativeMethods.GetSystemMetrics(NativeMethods.SM_XVIRTUALSCREEN),
        NativeMethods.GetSystemMetrics(NativeMethods.SM_YVIRTUALSCREEN),
        NativeMethods.GetSystemMetrics(NativeMethods.SM_CXVIRTUALSCREEN),
        NativeMethods.GetSystemMetrics(NativeMethods.SM_CYVIRTUALSCREEN));

    public static ScreenShot CaptureVirtualScreen()
    {
        var bounds = VirtualScreenBounds;
        using var bitmap = new Bitmap(bounds.Width, bounds.Height, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);
        }
        return new ScreenShot(ToBitmapSource(bitmap), bounds);
    }

    private static BitmapSource ToBitmapSource(Bitmap bitmap)
    {
        var data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
            ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
        try
        {
            var source = BitmapSource.Create(bitmap.Width, bitmap.Height, 96, 96, PixelFormats.Bgr32, null,
                data.Scan0, data.Stride * data.Height, data.Stride);
            source.Freeze();
            return source;
        }
        finally
        {
            bitmap.UnlockBits(data);
        }
    }

    /// <summary>Copies a region into a new, standalone bitmap so the full desktop capture can be released.</summary>
    public static BitmapSource Crop(BitmapSource source, Int32Rect rect)
    {
        int stride = rect.Width * 4;
        var pixels = new byte[stride * rect.Height];
        source.CopyPixels(rect, pixels, stride, 0);
        var cropped = BitmapSource.Create(rect.Width, rect.Height, 96, 96, PixelFormats.Bgr32, null, pixels, stride);
        cropped.Freeze();
        return cropped;
    }
}
