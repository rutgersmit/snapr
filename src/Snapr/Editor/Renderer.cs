using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Snapr.Editor.Annotations;

namespace Snapr.Editor;

public static class Renderer
{
    /// <summary>Flattens the screenshot and its annotations at the original pixel size.</summary>
    public static BitmapSource Flatten(BitmapSource image, IEnumerable<IAnnotation> annotations)
    {
        int w = image.PixelWidth, h = image.PixelHeight;
        var visual = new DrawingVisual();
        RenderOptions.SetBitmapScalingMode(visual, BitmapScalingMode.NearestNeighbor);
        using (var dc = visual.RenderOpen())
        {
            dc.DrawImage(image, new Rect(0, 0, w, h));
            foreach (var annotation in InPaintOrder(annotations))
                annotation.Render(dc);
        }

        var target = new RenderTargetBitmap(w, h, 96, 96, PixelFormats.Pbgra32);
        target.Render(visual);
        target.Freeze();
        return target;
    }

    /// <summary>
    /// Highlights first, straight onto the screenshot like a marker on paper; everything else on top,
    /// each group in the order it was added. (A highlight is the screenshot's own pixels, tinted, so
    /// drawn later it would cover arrows, numbers and pixelated text under it.)
    /// </summary>
    public static IEnumerable<IAnnotation> InPaintOrder(IEnumerable<IAnnotation> annotations)
    {
        var all = annotations.ToList();
        return all.OfType<HighlightAnnotation>().Concat<IAnnotation>(all.Where(a => a is not HighlightAnnotation));
    }

    public static void SavePng(BitmapSource image, string path)
    {
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(image));
        using var stream = File.Create(path);
        encoder.Save(stream);
    }
}
