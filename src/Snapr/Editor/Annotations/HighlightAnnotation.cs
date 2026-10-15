using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Snapr.Editor.Annotations;

/// <summary>
/// A highlighter stroke: a broad band that colours the screenshot under it like a marker does
/// (multiplied, so dark text stays dark).
/// </summary>
/// <remarks>
/// People mostly highlight a line of text, dragging roughly from left to right with a shaky hand.
/// The band doesn't trace every wobble of the mouse: a stroke that is more or less straight becomes
/// one straight band along the best-fitting line, and snaps to exactly horizontal (or vertical) when
/// it is close. Only a clearly curved stroke is followed, and then smoothed.
///
/// WPF has no multiply blend mode, so the band is drawn as the screenshot's own pixels, tinted,
/// clipped to the band's shape. That is why highlights are drawn below the other annotations
/// (see <see cref="Renderer.InPaintOrder"/>).
/// </remarks>
public sealed class HighlightAnnotation : IEditableAnnotation
{
    /// <summary>How strongly the colour shows (0–1).</summary>
    private const double Strength = 150 / 255.0;

    /// <summary>Strokes within this angle of horizontal or vertical are snapped to it.</summary>
    private const double SnapDegrees = 7;

    /// <summary>
    /// At most this many samples are kept while drawing, so each mouse move costs the same however
    /// long the drag goes on: past it, every other sample is dropped and the spacing doubles.
    /// </summary>
    private const int MaxSamples = 512;

    private readonly BitmapSource _source;
    private readonly Color _color;
    private readonly double _thickness;

    /// <summary>The band's centre line: two points when straight, more when it follows a curve.</summary>
    private Point[] _points;

    // The creating drag, resampled as it comes in; null once the stroke is finished.
    private List<Point>? _samples;
    private double _step;
    private Point _tip;

    // What Render draws, rebuilt when the points change.
    private Drawing? _drawing;

    public HighlightAnnotation(BitmapSource source, Point start, Color color, double thickness)
    {
        _source = source.Format == PixelFormats.Bgr32 || source.Format == PixelFormats.Bgra32 || source.Format == PixelFormats.Pbgra32
            ? source
            : new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);
        _color = color;
        _thickness = thickness;
        _points = new[] { start };
        _samples = new List<Point> { start };
        _step = Math.Max(1, thickness / 4);
        _tip = start;
    }

    public IReadOnlyList<Point> Handles
    {
        get => _points;
        set
        {
            _points = value.ToArray();
            Invalidate();
        }
    }

    public bool IsTooShort => Length < _thickness * 0.5;

    private double Length
    {
        get
        {
            double length = 0;
            for (int i = 1; i < _points.Length; i++) length += (_points[i] - _points[i - 1]).Length;
            return length;
        }
    }

    /// <summary>Adds a mouse position to the stroke being drawn and reshapes it.</summary>
    public void Extend(Point p)
    {
        if (_samples is null || p == _tip) return;

        var last = _samples[^1];
        var v = p - last;
        while (v.Length >= _step)
        {
            last += v / v.Length * _step;
            _samples.Add(last);
            v = p - last;
        }
        _tip = p;

        if (_samples.Count > MaxSamples)
        {
            _samples = _samples.Where((_, i) => i % 2 == 0).ToList();
            _step *= 2;
        }

        var path = new List<Point>(_samples);
        if (path[^1] != _tip) path.Add(_tip);
        _points = Shape(path, _thickness, _step);
        Invalidate();
    }

    /// <summary>Ends the creating drag; the shape is fixed from now on.</summary>
    public void Finish() => _samples = null;

    public bool HitTest(Point p, double tolerance)
    {
        double reach = _thickness / 2 + tolerance;
        if (_points.Length == 1) return (p - _points[0]).Length <= reach;
        for (int i = 1; i < _points.Length; i++)
        {
            if (DistanceToSegment(p, _points[i - 1], _points[i]) <= reach) return true;
        }
        return false;
    }

    public Rect Bounds
    {
        get
        {
            var rect = new Rect(_points[0], _points[0]);
            foreach (var p in _points) rect.Union(p);
            rect.Inflate(_thickness / 2, _thickness / 2);
            return rect;
        }
    }

    public void Render(DrawingContext dc)
    {
        if (_points.Length < 2) return;
        _drawing ??= Build();
        if (_drawing is not null) dc.DrawDrawing(_drawing);
    }

    private void Invalidate() => _drawing = null;

    /// <summary>The band's outline, and the screenshot under it with the colour multiplied in.</summary>
    private Drawing? Build()
    {
        var line = new StreamGeometry();
        using (var ctx = line.Open())
        {
            ctx.BeginFigure(_points[0], isFilled: false, isClosed: false);
            ctx.PolyLineTo(_points.Skip(1).ToArray(), isStroked: true, isSmoothJoin: true);
        }
        // Flat ends like a chisel tip; rounded where a curved stroke bends.
        var pen = new Pen(Brushes.Black, _thickness)
        {
            StartLineCap = PenLineCap.Flat,
            EndLineCap = PenLineCap.Flat,
            LineJoin = PenLineJoin.Round,
        };
        var band = line.GetWidenedPathGeometry(pen);
        band.Freeze();

        var bounds = band.Bounds;
        int left = Math.Max(0, (int)Math.Floor(bounds.Left));
        int top = Math.Max(0, (int)Math.Floor(bounds.Top));
        int right = Math.Min(_source.PixelWidth, (int)Math.Ceiling(bounds.Right));
        int bottom = Math.Min(_source.PixelHeight, (int)Math.Ceiling(bounds.Bottom));
        if (right <= left || bottom <= top) return null;

        int width = right - left, height = bottom - top, stride = width * 4;
        var pixels = new byte[stride * height];
        _source.CopyPixels(new Int32Rect(left, top, width, height), pixels, stride, 0);

        // Multiply: white paper takes the colour, black text stays black. Scaling all channels by at
        // most 1 also keeps premultiplied pixels valid.
        double fb = 1 - Strength + Strength * _color.B / 255.0;
        double fg = 1 - Strength + Strength * _color.G / 255.0;
        double fr = 1 - Strength + Strength * _color.R / 255.0;
        for (int i = 0; i < pixels.Length; i += 4)
        {
            pixels[i] = (byte)Math.Round(pixels[i] * fb);
            pixels[i + 1] = (byte)Math.Round(pixels[i + 1] * fg);
            pixels[i + 2] = (byte)Math.Round(pixels[i + 2] * fr);
        }

        var tinted = BitmapSource.Create(width, height, 96, 96, _source.Format, null, pixels, stride);
        var drawing = new DrawingGroup { ClipGeometry = band };
        drawing.Children.Add(new ImageDrawing(tinted, new Rect(left, top, width, height)));
        // Scaled like the screenshot under it, not with the layer's crisp pixel-block scaling.
        RenderOptions.SetBitmapScalingMode(drawing, BitmapScalingMode.HighQuality);
        drawing.Freeze();
        return drawing;
    }

    /// <summary>The band's centre line for a stroke drawn through <paramref name="points"/>, about <paramref name="step"/> apart.</summary>
    private static Point[] Shape(List<Point> points, double thickness, double step)
    {
        if (points.Count < 2) return points.ToArray();

        var center = new Point(points.Average(p => p.X), points.Average(p => p.Y));
        var direction = FittedDirection(points, center);

        // How far the stroke strays from that line. A long stroke may sag a little more: the arm
        // pivots at the elbow, so long "straight" lines come out as slight arcs.
        double length = 0;
        for (int i = 1; i < points.Count; i++) length += (points[i] - points[i - 1]).Length;
        double tolerance = thickness * 0.6 + length * 0.03;
        if (points.All(p => Math.Abs(Vector.CrossProduct(direction, p - center)) <= tolerance))
        {
            double from = points.Min(p => (p - center) * direction);
            double to = points.Max(p => (p - center) * direction);
            return new[] { center + direction * from, center + direction * to };
        }

        return Smooth(points, (int)Math.Round(thickness * 1.5 / step));
    }

    /// <summary>
    /// The direction of the line that best fits the points (least squares, perpendicular distance),
    /// pointing the way the stroke was drawn and snapped to horizontal or vertical when close.
    /// </summary>
    private static Vector FittedDirection(List<Point> points, Point center)
    {
        double sxx = 0, syy = 0, sxy = 0;
        foreach (var p in points)
        {
            var v = p - center;
            sxx += v.X * v.X;
            syy += v.Y * v.Y;
            sxy += v.X * v.Y;
        }
        double angle = 0.5 * Math.Atan2(2 * sxy, sxx - syy);
        var direction = new Vector(Math.Cos(angle), Math.Sin(angle));

        double snap = Math.Sin(SnapDegrees * Math.PI / 180);
        if (Math.Abs(direction.Y) <= snap) direction = new Vector(1, 0);
        else if (Math.Abs(direction.X) <= snap) direction = new Vector(0, 1);

        return (points[^1] - points[0]) * direction < 0 ? -direction : direction;
    }

    /// <summary>
    /// A moving average over up to <paramref name="radius"/> neighbours on each side; the window
    /// narrows towards the ends so the stroke keeps its start and end points.
    /// </summary>
    private static Point[] Smooth(List<Point> points, int radius)
    {
        int last = points.Count - 1;
        var result = new Point[points.Count];
        for (int i = 0; i <= last; i++)
        {
            int r = Math.Min(radius, Math.Min(i, last - i));
            double x = 0, y = 0;
            for (int j = i - r; j <= i + r; j++)
            {
                x += points[j].X;
                y += points[j].Y;
            }
            result[i] = new Point(x / (2 * r + 1), y / (2 * r + 1));
        }
        return result;
    }

    private static double DistanceToSegment(Point p, Point a, Point b)
    {
        var ab = b - a;
        double lengthSquared = ab.LengthSquared;
        if (lengthSquared == 0) return (p - a).Length;
        double t = Math.Clamp(Vector.Multiply(p - a, ab) / lengthSquared, 0, 1);
        return (p - (a + t * ab)).Length;
    }
}
