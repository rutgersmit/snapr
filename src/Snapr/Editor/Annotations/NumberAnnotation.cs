using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace Snapr.Editor.Annotations;

/// <summary>A numbered marker: the number in white (or dark on light colours) on a solid rounded square.</summary>
public sealed class NumberAnnotation : IEditableAnnotation
{
    private static readonly Typeface Font = new(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
    private static readonly Brush ShadowBrush = Freeze(new SolidColorBrush(Color.FromArgb(70, 0, 0, 0)));

    private readonly Brush _fill;
    private readonly Brush _textBrush;
    private readonly Pen _outline;
    private readonly double _size;
    private readonly FormattedText _text;

    public int Number { get; }
    public Point Center { get; set; }

    public NumberAnnotation(Point center, int number, Color color, double size)
    {
        Center = center;
        Number = number;
        _size = size;
        _fill = Freeze(new SolidColorBrush(color));
        _textBrush = IsLight(color) ? Freeze(new SolidColorBrush(Color.FromRgb(0x1C, 0x1C, 0x1E))) : Brushes.White;
        _outline = new Pen(Brushes.White, Math.Max(1.5, size * 0.07));
        _outline.Freeze();
        _text = new FormattedText(number.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight, Font, size * 0.58, _textBrush, pixelsPerDip: 1);
    }

    public IReadOnlyList<Point> Handles
    {
        get => new[] { Center };
        set => Center = value[0];
    }

    public bool HitTest(Point p, double tolerance)
    {
        var box = Box;
        box.Inflate(tolerance, tolerance);
        return box.Contains(p);
    }

    public Rect Bounds => Box;

    // Grows horizontally for two or more digits so the square becomes a pill-ish rounded rectangle.
    private Rect Box
    {
        get
        {
            double width = Math.Max(_size, _text.Width + _size * 0.5);
            return new Rect(Center.X - width / 2, Center.Y - _size / 2, width, _size);
        }
    }

    public void Render(DrawingContext dc)
    {
        var box = Box;
        double radius = _size * 0.28;

        double offset = Math.Max(1, _size * 0.06);
        dc.DrawRoundedRectangle(ShadowBrush, null, Rect.Offset(box, offset, offset), radius, radius);
        dc.DrawRoundedRectangle(_fill, _outline, box, radius, radius);
        dc.DrawText(_text, new Point(Center.X - _text.Width / 2, Center.Y - _text.Height / 2));
    }

    private static bool IsLight(Color c) => 0.299 * c.R + 0.587 * c.G + 0.114 * c.B > 186;

    private static T Freeze<T>(T freezable) where T : Freezable
    {
        freezable.Freeze();
        return freezable;
    }
}
