using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace Snapr.Editor.Annotations;

/// <summary>A numbered marker: the number in white (or dark on light colours) on a solid rounded square.</summary>
public sealed class NumberAnnotation : IAnnotation
{
    private static readonly Typeface Font = new(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
    private static readonly Brush ShadowBrush = Freeze(new SolidColorBrush(Color.FromArgb(70, 0, 0, 0)));

    private readonly Brush _fill;
    private readonly Brush _textBrush;
    private readonly Pen _outline;
    private readonly double _size;

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
    }

    public void Render(DrawingContext dc)
    {
        var text = new FormattedText(Number.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight, Font, _size * 0.58, _textBrush, pixelsPerDip: 1);

        // Grow horizontally for two or more digits so the square becomes a pill-ish rounded rectangle.
        double width = Math.Max(_size, text.Width + _size * 0.5);
        var box = new Rect(Center.X - width / 2, Center.Y - _size / 2, width, _size);
        double radius = _size * 0.28;

        double offset = Math.Max(1, _size * 0.06);
        dc.DrawRoundedRectangle(ShadowBrush, null, Rect.Offset(box, offset, offset), radius, radius);
        dc.DrawRoundedRectangle(_fill, _outline, box, radius, radius);
        dc.DrawText(text, new Point(Center.X - text.Width / 2, Center.Y - text.Height / 2));
    }

    private static bool IsLight(Color c) => 0.299 * c.R + 0.587 * c.G + 0.114 * c.B > 186;

    private static T Freeze<T>(T freezable) where T : Freezable
    {
        freezable.Freeze();
        return freezable;
    }
}
