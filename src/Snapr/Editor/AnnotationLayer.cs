using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using Snapr.Editor.Annotations;

namespace Snapr.Editor;

/// <summary>Draws the committed annotations plus the one currently being dragged.</summary>
public sealed class AnnotationLayer : FrameworkElement
{
    public IReadOnlyList<IAnnotation> Annotations { get; set; } = new List<IAnnotation>();

    public IAnnotation? Pending { get; set; }

    public AnnotationLayer()
    {
        IsHitTestVisible = false;
        // Keep pixelated blocks crisp when the editor shows the image scaled down.
        RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.NearestNeighbor);
    }

    protected override void OnRender(DrawingContext dc)
    {
        foreach (var annotation in Annotations)
            annotation.Render(dc);
        Pending?.Render(dc);
    }
}
