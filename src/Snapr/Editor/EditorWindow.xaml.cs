using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Snapr.Editor.Annotations;
using Snapr.Interop;
using Snapr.Services;

namespace Snapr.Editor;

public partial class EditorWindow : Window
{
    private enum Tool { Arrow, Pixelate }

    private readonly BitmapSource _image;
    private readonly List<IAnnotation> _annotations = new();
    private readonly Stack<IAnnotation> _redo = new();

    private Tool _tool = Tool.Arrow;
    private Color _color = Color.FromRgb(0xFF, 0x3B, 0x30);
    private double _dpiScale = 1;
    private IAnnotation? _pending;
    private bool _changedSinceCopy;

    public EditorWindow(BitmapSource image)
    {
        InitializeComponent();
        _image = image;

        BaseImage.Source = image;
        Surface.Width = image.PixelWidth;
        Surface.Height = image.PixelHeight;
        Layer.Annotations = _annotations;
        SizeText.Text = $"{image.PixelWidth} × {image.PixelHeight}";

        SourceInitialized += (_, _) =>
        {
            NativeMethods.UseDarkTitleBar(new WindowInteropHelper(this).Handle);
            // Size the content before the first layout so CenterScreen centres the final size.
            ApplyDpi(VisualTreeHelper.GetDpi(this));
        };
        PreviewKeyDown += OnKeyDown;
        UpdateButtons();
    }

    protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
    {
        base.OnDpiChanged(oldDpi, newDpi);
        ApplyDpi(newDpi);
    }

    /// <summary>Shows the image at 1:1 physical pixels (or smaller if it does not fit).</summary>
    private void ApplyDpi(DpiScale dpi)
    {
        _dpiScale = dpi.DpiScaleX;
        DpiScale.ScaleX = DpiScale.ScaleY = 1 / _dpiScale;

        var workArea = SystemParameters.WorkArea;
        MaxWidth = workArea.Width * 0.92;
        MaxHeight = workArea.Height * 0.92;
    }

    // ---- Drawing -------------------------------------------------------------------------

    private void OnSurfaceMouseDown(object sender, MouseButtonEventArgs e)
    {
        var p = e.GetPosition(Surface);
        _pending = _tool switch
        {
            Tool.Arrow => new ArrowAnnotation(p, _color, Math.Round(5 * _dpiScale)),
            _ => new PixelateAnnotation(_image, p, PixelBlockSize),
        };
        Layer.Pending = _pending;
        Surface.CaptureMouse();
        e.Handled = true;
    }

    private void OnSurfaceMouseMove(object sender, MouseEventArgs e)
    {
        if (_pending is null) return;

        var p = ClampToImage(e.GetPosition(Surface));
        switch (_pending)
        {
            case ArrowAnnotation arrow: arrow.End = p; break;
            case PixelateAnnotation pixelate: pixelate.Update(p); break;
        }
        Layer.InvalidateVisual();
    }

    private void OnSurfaceMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_pending is null) return;
        Surface.ReleaseMouseCapture();

        var finished = _pending;
        _pending = null;
        Layer.Pending = null;

        bool empty = finished switch
        {
            ArrowAnnotation arrow => arrow.IsTooShort,
            PixelateAnnotation pixelate => pixelate.IsEmpty,
            _ => false,
        };
        if (!empty)
        {
            _annotations.Add(finished);
            _redo.Clear();
            _changedSinceCopy = true;
        }
        Layer.InvalidateVisual();
        UpdateButtons();
    }

    /// <summary>Blocks large enough that text inside cannot be read, scaled with display DPI.</summary>
    private int PixelBlockSize => (int)Math.Max(10, Math.Round(12 * _dpiScale));

    private Point ClampToImage(Point p) => new(
        Math.Clamp(p.X, 0, _image.PixelWidth),
        Math.Clamp(p.Y, 0, _image.PixelHeight));

    // ---- Toolbar -------------------------------------------------------------------------

    private void OnToolClicked(object sender, RoutedEventArgs e)
        => SelectTool(sender == PixelateTool ? Tool.Pixelate : Tool.Arrow);

    private void SelectTool(Tool tool)
    {
        _tool = tool;
        ArrowTool.IsChecked = tool == Tool.Arrow;
        PixelateTool.IsChecked = tool == Tool.Pixelate;
    }

    private void OnColorChecked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton { Background: SolidColorBrush brush })
            _color = brush.Color;
        // Picking a colour implies drawing an arrow, the only coloured tool.
        if (ArrowTool is not null) SelectTool(Tool.Arrow);
    }

    private void SelectColor(int index)
    {
        if (Swatches.Children[index] is RadioButton swatch) swatch.IsChecked = true;
    }

    private void OnUndo(object sender, RoutedEventArgs e) => Undo();

    private void OnRedo(object sender, RoutedEventArgs e) => Redo();

    private void Undo()
    {
        if (_annotations.Count == 0) return;
        _redo.Push(_annotations[^1]);
        _annotations.RemoveAt(_annotations.Count - 1);
        _changedSinceCopy = true;
        Layer.InvalidateVisual();
        UpdateButtons();
    }

    private void Redo()
    {
        if (_redo.Count == 0) return;
        _annotations.Add(_redo.Pop());
        _changedSinceCopy = true;
        Layer.InvalidateVisual();
        UpdateButtons();
    }

    private void UpdateButtons()
    {
        UndoButton.IsEnabled = _annotations.Count > 0;
        RedoButton.IsEnabled = _redo.Count > 0;
    }

    private void OnCopy(object sender, RoutedEventArgs e) => Copy();

    private void OnSave(object sender, RoutedEventArgs e) => Save();

    private BitmapSource Flatten() => Renderer.Flatten(_image, _annotations);

    private void Copy()
    {
        if (ClipboardHelper.TrySetImage(Flatten()))
        {
            _changedSinceCopy = false;
            ShowStatus("Gekopieerd naar klembord");
        }
        else
        {
            ShowStatus("Klembord is bezet, probeer opnieuw");
        }
    }

    private void Save()
    {
        var settings = App.Current.Settings;
        var dialog = new SaveFileDialog
        {
            Filter = "PNG-afbeelding (*.png)|*.png",
            FileName = $"Snapr {DateTime.Now:yyyy-MM-dd HH.mm.ss}.png",
            DefaultExt = ".png",
        };
        try
        {
            Directory.CreateDirectory(settings.SaveFolder);
            dialog.InitialDirectory = settings.SaveFolder;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            // Fall back to the dialog's own default folder.
        }

        if (dialog.ShowDialog(this) != true) return;

        try
        {
            Renderer.SavePng(Flatten(), dialog.FileName);
            ShowStatus($"Opgeslagen: {Path.GetFileName(dialog.FileName)}");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, $"Opslaan mislukt:\n{ex.Message}", "Snapr", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ShowStatus(string text) => StatusText.Text = text;

    // ---- Keyboard & closing --------------------------------------------------------------

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        bool ctrl = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
        bool shift = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);

        switch (e.Key)
        {
            case Key.Escape: Close(); break;
            case Key.Z when ctrl && shift: Redo(); break;
            case Key.Z when ctrl: Undo(); break;
            case Key.Y when ctrl: Redo(); break;
            case Key.C when ctrl: Copy(); break;
            case Key.S when ctrl: Save(); break;
            case Key.A when !ctrl: SelectTool(Tool.Arrow); break;
            case Key.P or Key.B when !ctrl: SelectTool(Tool.Pixelate); break;
            case >= Key.D1 and <= Key.D5 when !ctrl: SelectColor(e.Key - Key.D1); break;
            default: return;
        }
        e.Handled = true;
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        // Leave the edited version on the clipboard, like Shottr does.
        if (_changedSinceCopy && _annotations.Any() && App.Current.Settings.CopyOnCapture)
            ClipboardHelper.TrySetImage(Flatten());
        base.OnClosing(e);
    }
}
