using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Snapr.Editor.Annotations;

namespace Snapr.Editor;

/// <summary>
/// Draws the committed annotations, the one currently being dragged, and the selection outline.
/// The selection is editor-only chrome and never ends up in the exported image.
/// </summary>
public sealed class AnnotationLayer : FrameworkElement
{
    private static readonly Brush AccentBrush = Freeze(new SolidColorBrush(Color.FromRgb(0x0A, 0x84, 0xFF)));

    public IReadOnlyList<IAnnotation> Annotations { get; set; } = new List<IAnnotation>();

    public IAnnotation? Pending { get; set; }

    public IEditableAnnotation? Selected { get; set; }

    /// <summary>Radius of the drag handles, in image pixels.</summary>
    public double HandleRadius { get; set; } = 5;

    public AnnotationLayer()
    {
        IsHitTestVisible = false;
        // Keep pixelated blocks crisp when the editor shows the image scaled down.
        RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.NearestNeighbor);
    }

    protected override void OnRender(DrawingContext dc)
    {
        var annotations = Pending is null ? Annotations : Annotations.Append(Pending);
        foreach (var annotation in Renderer.InPaintOrder(annotations))
            annotation.Render(dc);

        if (Selected is not null) RenderSelection(dc, Selected);
    }

    private void RenderSelection(DrawingContext dc, IEditableAnnotation selected)
    {
        double r = HandleRadius;
        var bounds = selected.Bounds;
        bounds.Inflate(r, r);

        var outline = new Pen(AccentBrush, r * 0.35) { DashStyle = new DashStyle(new double[] { 3, 2 }, 0) };
        dc.DrawRoundedRectangle(null, outline, bounds, r, r);

        // Only two-point shapes (arrows, straight highlights) get draggable end points; a number has
        // just its centre, a curved highlight is only moved as a whole.
        if (selected.Handles.Count != 2) return;
        var handlePen = new Pen(AccentBrush, r * 0.45);
        foreach (var handle in selected.Handles)
            dc.DrawEllipse(Brushes.White, handlePen, handle, r, r);
    }

    private static T Freeze<T>(T freezable) where T : Freezable
    {
        freezable.Freeze();
        return freezable;
    }
}
