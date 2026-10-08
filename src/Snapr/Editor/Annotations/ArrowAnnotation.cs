using System;
using System.Windows;
using System.Windows.Media;

namespace Snapr.Editor.Annotations;

/// <summary>A tapered arrow with a filled head, white outline and soft shadow.</summary>
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
        double headLength = Math.Min(t * 3.6, length * 0.6);
        double headHalfWidth = Math.Max(t * 1.7, headLength * 0.5);
        double tailHalfWidth = t * 0.2;
        var headBase = End - dir * headLength;

        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(Start + normal * tailHalfWidth, isFilled: true, isClosed: true);
            ctx.PolyLineTo(new[]
            {
                headBase + normal * (t / 2),
                headBase + normal * headHalfWidth,
                End,
                headBase - normal * headHalfWidth,
                headBase - normal * (t / 2),
                Start - normal * tailHalfWidth,
            }, isStroked: true, isSmoothJoin: false);
        }
        geometry.Freeze();
        return geometry;
    }

    private static T Freeze<T>(T freezable) where T : Freezable
    {
        freezable.Freeze();
        return freezable;
    }
}
