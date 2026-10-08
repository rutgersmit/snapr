using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using Microsoft.Win32;
using Snapr.Interop;
using Snapr.Services;

namespace Snapr.Settings;

public partial class SettingsWindow : Window
{
    private HotkeyGesture _hotkey;

    public SettingsWindow(AppSettings settings)
    {
        InitializeComponent();

        _hotkey = HotkeyGesture.Parse(settings.Hotkey);
        HotkeyBox.Text = _hotkey.ToString();
        FolderBox.Text = settings.SaveFolder;
        CopyOnCaptureBox.IsChecked = settings.CopyOnCapture;
        StartupBox.IsChecked = settings.StartWithWindows;

        SourceInitialized += (_, _) => NativeMethods.UseDarkTitleBar(new WindowInteropHelper(this).Handle);
        // The global hotkey would swallow the keys we want to record, so pause it while this window is open.
        Loaded += (_, _) => App.Current.SuspendHotkey();
        Closed += (_, _) => App.Current.RegisterHotkey();
    }

    // ---- Hotkey recorder -----------------------------------------------------------------

    private void OnHotkeyFocus(object sender, KeyboardFocusChangedEventArgs e) => HotkeyBox.Text = "Druk op een toetscombinatie…";

    private void OnHotkeyBlur(object sender, KeyboardFocusChangedEventArgs e) => HotkeyBox.Text = _hotkey.ToString();

    private void OnHotkeyKeyDown(object sender, KeyEventArgs e)
    {
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        e.Handled = true;

        if (key is Key.Tab or Key.Escape or Key.Enter && Keyboard.Modifiers == ModifierKeys.None)
        {
            e.Handled = false; // keep normal dialog navigation
            return;
        }
        Record(key);
    }

    // Windows only delivers a key-up for PrintScreen.
    private void OnHotkeyKeyUp(object sender, KeyEventArgs e)
    {
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key != Key.Snapshot) return;
        e.Handled = true;
        Record(key);
    }

    private void Record(Key key)
    {
        if (HotkeyGesture.IsModifierKey(key)) return;

        var modifiers = Keyboard.Modifiers;
        // Plain letters and digits would fire while typing; require a modifier for those.
        bool needsModifier = key is >= Key.A and <= Key.Z or >= Key.D0 and <= Key.D9 or Key.Space;
        if (needsModifier && modifiers is ModifierKeys.None or ModifierKeys.Shift) return;

        _hotkey = new HotkeyGesture(modifiers, key);
        HotkeyBox.Text = _hotkey.ToString();
    }

    private void OnResetHotkey(object sender, RoutedEventArgs e)
    {
        _hotkey = HotkeyGesture.Default;
        HotkeyBox.Text = _hotkey.ToString();
    }

    // ---- Buttons -------------------------------------------------------------------------

    private void OnBrowse(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog { Title = "Kies opslagmap" };
        if (System.IO.Directory.Exists(FolderBox.Text)) dialog.InitialDirectory = FolderBox.Text;
        if (dialog.ShowDialog(this) == true) FolderBox.Text = dialog.FolderName;
    }

    private void OnCancel(object sender, RoutedEventArgs e) => Close();

    private void OnSave(object sender, RoutedEventArgs e)
    {
        App.Current.ApplySettings(new AppSettings
        {
            Hotkey = _hotkey.ToString(),
            SaveFolder = string.IsNullOrWhiteSpace(FolderBox.Text) ? new AppSettings().SaveFolder : FolderBox.Text.Trim(),
            CopyOnCapture = CopyOnCaptureBox.IsChecked == true,
            StartWithWindows = StartupBox.IsChecked == true,
        });
        Close(); // Closed re-registers the (new) hotkey
    }
}
