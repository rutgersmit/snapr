using System.Globalization;

namespace Snapr.Localization;

/// <summary>
/// All user-facing text. English by default; Dutch when the Windows display language is Dutch.
/// Used from XAML via <c>{x:Static loc:Strings.Name}</c>.
/// </summary>
public static class Strings
{
    public static bool IsDutch { get; } = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "nl";

    private static string T(string en, string nl) => IsDutch ? nl : en;

    // ---- Tray & notifications ------------------------------------------------------------

    public static string TrayCapture => T("Take screenshot", "Screenshot maken");
    public static string TraySettings => T("Settings…", "Instellingen…");
    public static string TrayExit => T("Exit", "Afsluiten");

    public static string RunningTitle => T("Snapr is running", "Snapr draait");
    public static string RunningText(string hotkey) =>
        T($"Press {hotkey} to take a screenshot.", $"Druk op {hotkey} om een screenshot te maken.");

    public static string HotkeyUnavailableTitle => T("Shortcut not available", "Sneltoets niet beschikbaar");
    public static string PrintScreenInUse => T(
        "PrintScreen is already in use. Turn off \"Use the Print screen key to open Snipping Tool\" in " +
        "Windows Settings > Accessibility > Keyboard, or choose a different shortcut.",
        "PrintScreen is al in gebruik. Zet in Windows-instellingen > Toegankelijkheid > Toetsenbord " +
        "\"Knipprogramma openen met PrtSc\" uit, of kies een andere sneltoets.");
    public static string HotkeyInUse(string hotkey) => T(
        $"{hotkey} is already in use by another program. Choose a different shortcut.",
        $"{hotkey} is al in gebruik door een ander programma. Kies een andere sneltoets.");

    public static string UnexpectedError(string message, string logPath) => T(
        $"Something went wrong, but Snapr keeps running.\n\n{message}\n\nDetails were written to:\n{logPath}",
        $"Er ging iets mis, maar Snapr blijft draaien.\n\n{message}\n\nDetails staan in:\n{logPath}");

    public static string StartupFailed => T("Could not set 'Start with Windows'.", "Kon 'start met Windows' niet instellen.");

    // ---- Capture overlay -----------------------------------------------------------------

    public static string CaptureHint => T(
        "Drag to select an area  ·  Click for this screen  ·  Enter for all screens  ·  Esc to cancel",
        "Sleep om een gebied te kiezen  ·  Klik voor dit scherm  ·  Enter voor alle schermen  ·  Esc annuleert");

    // ---- Editor --------------------------------------------------------------------------

    public static string ToolArrow => T("Arrow (A)", "Pijl (A)");
    public static string ToolPixelate => T("Pixelate (P)", "Pixeleren (P)");
    public static string ToolNumber => T("Numbers (N)", "Nummers (N)");

    public static string ColorRed => T("Red (1)", "Rood (1)");
    public static string ColorOrange => T("Orange (2)", "Oranje (2)");
    public static string ColorYellow => T("Yellow (3)", "Geel (3)");
    public static string ColorGreen => T("Green (4)", "Groen (4)");
    public static string ColorBlue => T("Blue (5)", "Blauw (5)");

    public static string Undo => T("Undo (Ctrl+Z)", "Ongedaan maken (Ctrl+Z)");
    public static string Redo => T("Redo (Ctrl+Y)", "Opnieuw (Ctrl+Y)");
    public static string Copy => T("Copy (Ctrl+C)", "Kopiëren (Ctrl+C)");
    public static string Save => T("Save (Ctrl+S)", "Opslaan (Ctrl+S)");

    public static string EditorHint => T(
        "A arrow  ·  P pixelate  ·  N numbers  ·  Ctrl+C copy  ·  Ctrl+S save  ·  Esc close",
        "A pijl  ·  P pixeleren  ·  N nummers  ·  Ctrl+C kopiëren  ·  Ctrl+S opslaan  ·  Esc sluiten");

    public static string Copied => T("Copied to clipboard", "Gekopieerd naar klembord");
    public static string ClipboardBusy => T("Clipboard is busy, please try again", "Klembord is bezet, probeer opnieuw");
    public static string PngFilter => T("PNG image (*.png)|*.png", "PNG-afbeelding (*.png)|*.png");
    public static string Saved(string fileName) => T($"Saved: {fileName}", $"Opgeslagen: {fileName}");
    public static string SaveFailed(string error) => T($"Saving failed:\n{error}", $"Opslaan mislukt:\n{error}");

    // ---- Settings ------------------------------------------------------------------------

    public static string SettingsTitle => T("Snapr – Settings", "Snapr – Instellingen");
    public static string Shortcut => T("Shortcut", "Sneltoets");
    public static string ShortcutHelp => T(
        "Click the field and press the key combination you want. If PrintScreen doesn't work, turn off " +
        "\"Use the Print screen key to open Snipping Tool\" under Accessibility › Keyboard in Windows.",
        "Klik in het veld en druk de gewenste toetscombinatie. Werkt PrintScreen niet, zet dan in Windows bij " +
        "Toegankelijkheid › Toetsenbord \"Knipprogramma openen met PrtSc\" uit.");
    public static string PressShortcut => T("Press a key combination…", "Druk op een toetscombinatie…");
    public static string SaveFolder => T("Save folder", "Opslagmap");
    public static string Browse => T("Browse…", "Bladeren…");
    public static string ChooseSaveFolder => T("Choose save folder", "Kies opslagmap");
    public static string CopyOnCapture => T("Copy screenshot to the clipboard right away", "Screenshot direct naar het klembord kopiëren");
    public static string StartWithWindows => T("Start with Windows", "Starten met Windows");
    public static string Cancel => T("Cancel", "Annuleren");
    public static string SaveButton => T("Save", "Opslaan");
}
