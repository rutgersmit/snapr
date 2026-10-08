using System;
using System.Windows;
using System.Windows.Media;

namespace Snapr.Editor.Annotations;

/// <summary>A slightly tapered arrow with a broad, rounded head, white outline and soft shadow.</summary>
public sealed class ArrowAnnotation : IAnnotation
{
    private static readonly Brush ShadowBrush = Freeze(new SolidColorBrush(Color.FromArgb(70, 0, 0, 0)));

    private readonly Brush _fill;
    private readonly Pen _outline;
    private readonly double _thickness;

    public Point Start { get; }
    public Point End { get; set; }

    public ArrowAnnotation(Point start, Color color, double thickness)
    {
        Start = End = start;
        _thickness = thickness;
        _fill = Freeze(new SolidColorBrush(color));
        _outline = new Pen(Brushes.White, Math.Max(1.5, thickness * 0.3)) { LineJoin = PenLineJoin.Round };
        _outline.Freeze();
    }

    public bool IsTooShort => (End - Start).Length < _thickness * 2;

    public void Render(DrawingContext dc)
    {
        if ((End - Start).Length < 1) return;

        var geometry = BuildGeometry();
        double offset = Math.Max(1, _thickness * 0.35);
        dc.PushTransform(new TranslateTransform(offset, offset));
        dc.DrawGeometry(ShadowBrush, null, geometry);
        dc.Pop();
        dc.DrawGeometry(_fill, _outline, geometry);
    }

    private StreamGeometry BuildGeometry()
    {
        var vector = End - Start;
        double length = vector.Length;
        var dir = vector / length;
        var normal = new Vector(-dir.Y, dir.X);

        double t = _thickness;
        double headLength = Math.Min(t * 3.4, length * 0.6);
        double headHalfWidth = Math.Max(t * 2.5, headLength * 0.6);
        double shaftHalfWidth = t * 0.65;
        double tailHalfWidth = t * 0.4;
        var headBase = End - dir * headLength;

        // Outline from tail round to the tip and back, each corner with its own rounding radius.
        var corners = new (Point Point, double Radius)[]
        {
            (Start - normal * tailHalfWidth, tailHalfWidth),
            (headBase - normal * shaftHalfWidth, t * 0.3),
            (headBase - normal * headHalfWidth, t * 0.8),
            (End, t * 0.7),
            (headBase + normal * headHalfWidth, t * 0.8),
            (headBase + normal * shaftHalfWidth, t * 0.3),
            (Start + normal * tailHalfWidth, tailHalfWidth),
        };

        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            for (int i = 0; i < corners.Length; i++)
            {
                var prev = corners[(i + corners.Length - 1) % corners.Length].Point;
                var next = corners[(i + 1) % corners.Length].Point;
                var (corner, radius) = corners[i];

                // Never round more than half of either adjacent edge.
                var toPrev = prev - corner;
                var toNext = next - corner;
                double r = Math.Min(radius, Math.Min(toPrev.Length, toNext.Length) / 2);
                var entry = corner + Unit(toPrev) * r;
                var exit = corner + Unit(toNext) * r;

                if (i == 0) ctx.BeginFigure(entry, isFilled: true, isClosed: true);
                else ctx.LineTo(entry, isStroked: true, isSmoothJoin: true);
                ctx.QuadraticBezierTo(corner, exit, isStroked: true, isSmoothJoin: true);
            }
        }
        geometry.Freeze();
        return geometry;
    }

    private static Vector Unit(Vector v) => v.Length > 0 ? v / v.Length : v;

    private static T Freeze<T>(T freezable) where T : Freezable
    {
        freezable.Freeze();
        return freezable;
    }
}
