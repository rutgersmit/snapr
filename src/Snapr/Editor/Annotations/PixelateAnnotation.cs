using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Snapr.Editor.Annotations;

/// <summary>A rectangle of the original screenshot replaced by coarse blocks, making text unreadable.</summary>
public sealed class PixelateAnnotation : IAnnotation
{
    private readonly BitmapSource _source;
    private readonly int _blockSize;
    private readonly int _seed = Random.Shared.Next(); // fresh noise pattern per region
    private readonly Point _start;
    private Int32Rect _region = Int32Rect.Empty;
    private BitmapSource? _pixelated;

    public PixelateAnnotation(BitmapSource source, Point start, int blockSize)
    {
        _source = source;
        _start = start;
        _blockSize = blockSize;
    }

    public bool IsEmpty => _region.Width < 2 || _region.Height < 2;

    public void Update(Point current)
    {
        int x1 = Clamp(Math.Min(_start.X, current.X), _source.PixelWidth);
        int y1 = Clamp(Math.Min(_start.Y, current.Y), _source.PixelHeight);
        int x2 = Clamp(Math.Max(_start.X, current.X), _source.PixelWidth);
        int y2 = Clamp(Math.Max(_start.Y, current.Y), _source.PixelHeight);

        var region = new Int32Rect(x1, y1, x2 - x1, y2 - y1);
        if (region == _region) return;

        _region = region;
        _pixelated = IsEmpty ? null : Pixelator.Pixelate(_source, _region, _blockSize, _seed);
    }

    public void Render(DrawingContext dc)
    {
        if (_pixelated is null) return;
        dc.DrawImage(_pixelated, new Rect(_region.X, _region.Y, _region.Width, _region.Height));
    }

    private static int Clamp(double value, int max) => (int)Math.Clamp(Math.Round(value), 0, max);
}
