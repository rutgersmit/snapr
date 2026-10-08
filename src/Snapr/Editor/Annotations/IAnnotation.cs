using System.Windows.Media;

namespace Snapr.Editor.Annotations;

/// <summary>
/// Something drawn on top of the screenshot. Coordinates are in image pixels, and the same
/// Render call is used for the on-screen preview and the exported image.
/// </summary>
public interface IAnnotation
{
    void Render(DrawingContext dc);
}
