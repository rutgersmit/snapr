using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading;
using System.Windows;
using Snapr.Capture;
using Snapr.Editor;
using Snapr.Interop;
using Snapr.Services;
using Snapr.Settings;
using Forms = System.Windows.Forms;
using Point = System.Drawing.Point;

namespace Snapr;

public partial class App : Application
{
    private Mutex? _singleInstance;
    private HotkeyService? _hotkeys;
    private Forms.NotifyIcon? _tray;
    private Icon? _trayIcon;
    private CaptureOverlayWindow? _overlay;
    private SettingsWindow? _settingsWindow;

    public AppSettings Settings { get; private set; } = new();

    public static new App Current => (App)Application.Current;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _singleInstance = new Mutex(true, @"Local\Snapr.SingleInstance", out bool isFirst);
        if (!isFirst)
        {
            Shutdown();
            return;
        }

        Settings = SettingsService.Load();

        CreateTrayIcon();

        _hotkeys = new HotkeyService();
        _hotkeys.Pressed += (_, _) => StartCapture();
        RegisterHotkey(showSuccess: true);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _hotkeys?.Dispose();
        if (_tray is not null)
        {
            _tray.Visible = false;
            _tray.Dispose();
        }
        if (_trayIcon is not null) NativeMethods.DestroyIcon(_trayIcon.Handle);
        _singleInstance?.Dispose();
        base.OnExit(e);
    }

    /// <summary>(Re)registers the configured hotkey and tells the user when Windows refuses it.</summary>
    public void RegisterHotkey(bool showSuccess = false)
    {
        if (_hotkeys is null) return;

        var gesture = HotkeyGesture.Parse(Settings.Hotkey);
        if (_hotkeys.Register(gesture))
        {
            if (showSuccess)
                ShowBalloon("Snapr draait", $"Druk op {gesture} om een screenshot te maken.");
            return;
        }

        string message = gesture.Key == System.Windows.Input.Key.Snapshot
            ? "PrintScreen is al in gebruik. Zet in Windows-instellingen > Toegankelijkheid > Toetsenbord " +
              "\"Knipprogramma openen met PrtSc\" uit, of kies een andere sneltoets."
            : $"{gesture} is al in gebruik door een ander programma. Kies een andere sneltoets.";
        ShowBalloon("Sneltoets niet beschikbaar", message, Forms.ToolTipIcon.Warning);
    }

    public void SuspendHotkey() => _hotkeys?.Unregister();

    public void StartCapture()
    {
        if (_overlay is not null) return;

        var shot = ScreenCapture.CaptureVirtualScreen();
        _overlay = new CaptureOverlayWindow(shot);
        _overlay.Closed += (_, _) =>
        {
            var result = _overlay.Result;
            _overlay = null;
            if (result is null) return;

            if (Settings.CopyOnCapture)
                ClipboardHelper.TrySetImage(result);

            var editor = new EditorWindow(result);
            editor.Show();
            editor.Activate();
        };
        _overlay.Show();
        _overlay.Activate();
    }

    public void ShowSettings()
    {
        if (_settingsWindow is not null)
        {
            _settingsWindow.Activate();
            return;
        }

        _settingsWindow = new SettingsWindow(Settings);
        _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        _settingsWindow.Show();
        _settingsWindow.Activate();
    }

    public void ApplySettings(AppSettings settings)
    {
        Settings = settings;
        SettingsService.Save(settings);
        try
        {
            StartupService.Apply(settings.StartWithWindows);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException)
        {
            ShowBalloon("Snapr", "Kon 'start met Windows' niet instellen.", Forms.ToolTipIcon.Warning);
        }
    }

    private void CreateTrayIcon()
    {
        _trayIcon = CreateAppIcon();

        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Screenshot maken", null, (_, _) => StartCapture());
        menu.Items.Add("Instellingen…", null, (_, _) => ShowSettings());
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Afsluiten", null, (_, _) => Shutdown());

        _tray = new Forms.NotifyIcon
        {
            Icon = _trayIcon,
            Text = "Snapr",
            ContextMenuStrip = menu,
            Visible = true,
        };
        _tray.MouseClick += (_, e) =>
        {
            if (e.Button == Forms.MouseButtons.Left) StartCapture();
        };
    }

    private void ShowBalloon(string title, string text, Forms.ToolTipIcon icon = Forms.ToolTipIcon.Info)
        => _tray?.ShowBalloonTip(4000, title, text, icon);

    /// <summary>Draws the tray icon: a blue rounded square with white viewfinder corners.</summary>
    private static Icon CreateAppIcon()
    {
        const int size = 32;
        using var bitmap = new Bitmap(size, size);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var background = new GraphicsPath();
            const int r = 8;
            background.AddArc(1, 1, r * 2, r * 2, 180, 90);
            background.AddArc(size - 1 - r * 2, 1, r * 2, r * 2, 270, 90);
            background.AddArc(size - 1 - r * 2, size - 1 - r * 2, r * 2, r * 2, 0, 90);
            background.AddArc(1, size - 1 - r * 2, r * 2, r * 2, 90, 90);
            background.CloseFigure();
            using var fill = new SolidBrush(Color.FromArgb(10, 132, 255));
            g.FillPath(fill, background);

            using var pen = new Pen(Color.White, 3) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            const int a = 8, b = 24, l = 6;
            g.DrawLines(pen, new[] { new Point(a, a + l), new Point(a, a), new Point(a + l, a) });
            g.DrawLines(pen, new[] { new Point(b - l, a), new Point(b, a), new Point(b, a + l) });
            g.DrawLines(pen, new[] { new Point(b, b - l), new Point(b, b), new Point(b - l, b) });
            g.DrawLines(pen, new[] { new Point(a + l, b), new Point(a, b), new Point(a, b - l) });
        }
        return Icon.FromHandle(bitmap.GetHicon());
    }
}
