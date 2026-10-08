using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Snapr.Capture;
using Snapr.Editor;
using Snapr.Localization;
using Snapr.Services;
using Snapr.Settings;
using Forms = System.Windows.Forms;

namespace Snapr;

public partial class App : Application
{
    private Mutex? _singleInstance;
    private HotkeyService? _hotkeys;
    private Forms.NotifyIcon? _tray;
    private Icon? _trayIcon;
    private CaptureOverlayWindow? _overlay;
    private SettingsWindow? _settingsWindow;
    private bool _showingError;

    public AppSettings Settings { get; private set; } = new();

    public static new App Current => (App)Application.Current;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        HookUnhandledExceptions();

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
        _trayIcon?.Dispose();
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
                ShowBalloon(Strings.RunningTitle, Strings.RunningText(gesture.ToString()));
            return;
        }

        string message = gesture.Key == System.Windows.Input.Key.Snapshot
            ? Strings.PrintScreenInUse
            : Strings.HotkeyInUse(gesture.ToString());
        ShowBalloon(Strings.HotkeyUnavailableTitle, message, Forms.ToolTipIcon.Warning);
    }

    public void SuspendHotkey() => _hotkeys?.Unregister();

    public void StartCapture()
    {
        if (_overlay is not null) return;

        var shot = ScreenCapture.CaptureVirtualScreen();
        var overlay = new CaptureOverlayWindow(shot);
        overlay.Closed += (_, _) =>
        {
            _overlay = null;
            var result = overlay.Result;
            if (result is null) return;

            if (Settings.CopyOnCapture)
                ClipboardHelper.TrySetImage(result);

            var editor = new EditorWindow(result);
            editor.Show();
            editor.Activate();
        };

        _overlay = overlay;
        try
        {
            overlay.Show();
            overlay.Activate();
        }
        catch
        {
            _overlay = null; // otherwise every later capture would be ignored
            throw;
        }
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
            ShowBalloon("Snapr", Strings.StartupFailed, Forms.ToolTipIcon.Warning);
        }
    }

    // ---- Error handling ------------------------------------------------------------------

    /// <summary>
    /// A tray app that dies silently also takes the hotkey with it, so log every unexpected
    /// error, tell the user once, and keep running wherever .NET allows it.
    /// </summary>
    private void HookUnhandledExceptions()
    {
        DispatcherUnhandledException += (_, e) =>
        {
            ReportError(e.Exception, "UI");
            e.Handled = true;
        };
        // Tray menu clicks run through WinForms, which has its own handler.
        Forms.Application.ThreadException += (_, e) => ReportError(e.Exception, "Tray");
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            ErrorLog.Write(e.Exception, "Task");
            e.SetObserved();
        };
        // Not recoverable, but at least leave a trace.
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception ex) ErrorLog.Write(ex, "Fatal");
        };
    }

    private void ReportError(Exception exception, string source)
    {
        ErrorLog.Write(exception, source);
        if (_showingError) return; // one dialog at a time, even if errors repeat

        _showingError = true;
        try
        {
            MessageBox.Show(Strings.UnexpectedError(exception.Message, ErrorLog.FilePath), "Snapr",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            _showingError = false;
        }
    }

    // ---- Tray ----------------------------------------------------------------------------

    private void CreateTrayIcon()
    {
        _trayIcon = CreateAppIcon();

        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add(Strings.TrayCapture, null, (_, _) => StartCapture());
        menu.Items.Add(Strings.TraySettings, null, (_, _) => ShowSettings());
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add(Strings.TrayExit, null, (_, _) => Shutdown());

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

    /// <summary>Loads the app icon (Assets/snapr.ico) at the tray's DPI-aware small icon size.</summary>
    private static Icon CreateAppIcon()
    {
        var resource = GetResourceStream(new Uri("pack://application:,,,/Assets/snapr.ico"))!;
        using var stream = resource.Stream;
        return new Icon(stream, Forms.SystemInformation.SmallIconSize);
    }
}
