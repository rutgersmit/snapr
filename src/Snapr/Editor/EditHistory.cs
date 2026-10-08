using System.Collections.Generic;
using System.Windows;
using Snapr.Editor.Annotations;

namespace Snapr.Editor;

/// <summary>Undo/redo over the annotation list: adding, removing and reshaping annotations.</summary>
public sealed class EditHistory
{
    private abstract record Edit
    {
        public abstract void Undo(List<IAnnotation> annotations);
        public abstract void Redo(List<IAnnotation> annotations);
    }

    private sealed record Added(IAnnotation Annotation) : Edit
    {
        public override void Undo(List<IAnnotation> annotations) => annotations.Remove(Annotation);
        public override void Redo(List<IAnnotation> annotations) => annotations.Add(Annotation);
    }

    private sealed record Removed(IAnnotation Annotation, int Index) : Edit
    {
        public override void Undo(List<IAnnotation> annotations) => annotations.Insert(Index, Annotation);
        public override void Redo(List<IAnnotation> annotations) => annotations.Remove(Annotation);
    }

    private sealed record Reshaped(IEditableAnnotation Annotation, IReadOnlyList<Point> Before, IReadOnlyList<Point> After) : Edit
    {
        public override void Undo(List<IAnnotation> annotations) => Annotation.Handles = Before;
        public override void Redo(List<IAnnotation> annotations) => Annotation.Handles = After;
    }

    private readonly List<IAnnotation> _annotations;
    private readonly Stack<Edit> _undo = new();
    private readonly Stack<Edit> _redo = new();

    public EditHistory(List<IAnnotation> annotations) => _annotations = annotations;

    public bool CanUndo => _undo.Count > 0;
    public bool CanRedo => _redo.Count > 0;

    public void Add(IAnnotation annotation)
    {
        _annotations.Add(annotation);
        Push(new Added(annotation));
    }

    public void Remove(IAnnotation annotation)
    {
        int index = _annotations.IndexOf(annotation);
        if (index < 0) return;
        _annotations.RemoveAt(index);
        Push(new Removed(annotation, index));
    }

    /// <summary>Records a move or reshape that was already applied while dragging.</summary>
    public void RecordReshape(IEditableAnnotation annotation, IReadOnlyList<Point> before)
        => Push(new Reshaped(annotation, before, annotation.Handles));

    public void Undo()
    {
        if (!CanUndo) return;
        var edit = _undo.Pop();
        edit.Undo(_annotations);
        _redo.Push(edit);
    }

    public void Redo()
    {
        if (!CanRedo) return;
        var edit = _redo.Pop();
        edit.Redo(_annotations);
        _undo.Push(edit);
    }

    private void Push(Edit edit)
    {
        _undo.Push(edit);
        _redo.Clear();
    }
}
