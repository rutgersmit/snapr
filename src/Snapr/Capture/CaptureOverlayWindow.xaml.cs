using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Snapr.Interop;
using Snapr.Services;
using Forms = System.Windows.Forms;

namespace Snapr.Capture;

/// <summary>
/// Full-desktop overlay showing the frozen screenshot. The user drags a region, clicks for the
/// current monitor, or presses Enter for everything.
/// </summary>
public partial class CaptureOverlayWindow : Window
{
    private const double MinDragSize = 4;

    private readonly ScreenShot _shot;
    private Point? _dragStart;

    /// <summary>The selected region, or null when cancelled.</summary>
    public BitmapSource? Result { get; private set; }

    public CaptureOverlayWindow(ScreenShot shot)
    {
        InitializeComponent();
        _shot = shot;
        Screenshot.Source = shot.Image;

        SourceInitialized += (_, _) => CoverVirtualScreen();
        Loaded += (_, _) => CoverVirtualScreen();
        Root.SizeChanged += (_, _) => LayoutChrome();
        MouseLeftButtonDown += OnMouseDown;
        MouseMove += OnMouseMove;
        MouseLeftButtonUp += OnMouseUp;
        MouseRightButtonUp += (_, _) => Close();
        KeyDown += OnKeyDown;
        Deactivated += (_, _) => { if (IsVisible && Result is null && _dragStart is null) Close(); };
    }

    // Position in physical pixels; WPF's own Left/Top are DPI-relative and unreliable across mixed-DPI monitors.
    private void CoverVirtualScreen()
    {
        var b = _shot.Bounds;
        var hwnd = new WindowInteropHelper(this).Handle;
        NativeMethods.SetWindowPos(hwnd, NativeMethods.HWND_TOPMOST, b.Left, b.Top, b.Width, b.Height,
            NativeMethods.SWP_SHOWWINDOW);
    }

    protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
    {
        base.OnDpiChanged(oldDpi, newDpi);
        // Windows resizes the window to the "suggested" rect on a DPI change; put it back afterwards.
        Dispatcher.BeginInvoke(CoverVirtualScreen, DispatcherPriority.Background);
    }

    /// <summary>Physical pixels per WPF unit for this window.</summary>
    private double PixelsPerDip => Root.ActualWidth > 0 ? _shot.Image.PixelWidth / Root.ActualWidth : 1;

    private void LayoutChrome()
    {
        DimFull.Rect = new Rect(0, 0, Root.ActualWidth, Root.ActualHeight);
        DimHole.Rect = Rect.Empty;

        // Show the hint centred near the top of the monitor the cursor is on.
        var monitor = MonitorRectInDips(Forms.Cursor.Position);
        Hint.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        System.Windows.Controls.Canvas.SetLeft(Hint, monitor.Left + (monitor.Width - Hint.DesiredSize.Width) / 2);
        System.Windows.Controls.Canvas.SetTop(Hint, monitor.Top + 24);
    }

    private Rect MonitorRectInDips(System.Drawing.Point physicalPoint)
    {
        var screen = Forms.Screen.FromPoint(physicalPoint).Bounds;
        double s = PixelsPerDip;
        return new Rect((screen.Left - _shot.Bounds.Left) / s, (screen.Top - _shot.Bounds.Top) / s,
            screen.Width / s, screen.Height / s);
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        _dragStart = e.GetPosition(Root);
        Hint.Visibility = Visibility.Collapsed;
        CaptureMouse();
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        var p = e.GetPosition(Root);
        CrossH.X1 = 0; CrossH.X2 = Root.ActualWidth; CrossH.Y1 = CrossH.Y2 = p.Y;
        CrossV.Y1 = 0; CrossV.Y2 = Root.ActualHeight; CrossV.X1 = CrossV.X2 = p.X;

        if (_dragStart is not { } start) return;

        var rect = new Rect(start, p);
        DimHole.Rect = rect;
        System.Windows.Controls.Canvas.SetLeft(SelectionBorder, rect.X);
        System.Windows.Controls.Canvas.SetTop(SelectionBorder, rect.Y);
        SelectionBorder.Width = rect.Width;
        SelectionBorder.Height = rect.Height;
        SelectionBorder.Visibility = Visibility.Visible;

        var px = ToPixels(rect);
        SizeText.Text = $"{px.Width} × {px.Height}";
        SizeLabel.Visibility = Visibility.Visible;
        System.Windows.Controls.Canvas.SetLeft(SizeLabel, rect.X);
        System.Windows.Controls.Canvas.SetTop(SizeLabel, rect.Y >= 28 ? rect.Y - 26 : rect.Bottom + 4);
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_dragStart is not { } start) return;
        ReleaseMouseCapture();
        _dragStart = null;

        var rect = new Rect(start, e.GetPosition(Root));
        if (rect.Width < MinDragSize || rect.Height < MinDragSize)
        {
            // A plain click captures the monitor under the cursor.
            var screen = Forms.Screen.FromPoint(Forms.Cursor.Position).Bounds;
            Finish(new Int32Rect(screen.Left - _shot.Bounds.Left, screen.Top - _shot.Bounds.Top,
                screen.Width, screen.Height));
            return;
        }
        Finish(ToPixels(rect));
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Escape:
                Close();
                break;
            case Key.Enter:
                Finish(new Int32Rect(0, 0, _shot.Image.PixelWidth, _shot.Image.PixelHeight));
                break;
        }
    }

    private Int32Rect ToPixels(Rect dipRect)
    {
        double s = PixelsPerDip;
        int x = (int)Math.Round(dipRect.X * s);
        int y = (int)Math.Round(dipRect.Y * s);
        int right = (int)Math.Round(dipRect.Right * s);
        int bottom = (int)Math.Round(dipRect.Bottom * s);
        return new Int32Rect(x, y, Math.Max(1, right - x), Math.Max(1, bottom - y));
    }

    private void Finish(Int32Rect rect)
    {
        // Clamp to the captured image in case of rounding at the edges.
        int x = Math.Clamp(rect.X, 0, _shot.Image.PixelWidth - 1);
        int y = Math.Clamp(rect.Y, 0, _shot.Image.PixelHeight - 1);
        int w = Math.Clamp(rect.Width, 1, _shot.Image.PixelWidth - x);
        int h = Math.Clamp(rect.Height, 1, _shot.Image.PixelHeight - y);

        Result = ScreenCapture.Crop(_shot.Image, new Int32Rect(x, y, w, h));
        Close();
    }
}
