using System.Collections.Generic;
using System.Windows;

namespace Snapr.Editor.Annotations;

/// <summary>
/// An annotation that can be selected and moved after it was placed. Its shape is fully described
/// by a few handle points (both ends of an arrow, the centre of a number), which makes moving,
/// reshaping and undoing those changes uniform.
/// </summary>
public interface IEditableAnnotation : IAnnotation
{
    IReadOnlyList<Point> Handles { get; set; }

    /// <summary>Whether <paramref name="p"/> touches the drawn shape, with some slack for easier clicking.</summary>
    bool HitTest(Point p, double tolerance);

    /// <summary>The area the selection outline is drawn around.</summary>
    Rect Bounds { get; }
}
