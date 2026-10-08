using System;
using System.Windows.Interop;
using Snapr.Interop;

namespace Snapr.Services;

/// <summary>Registers one system-wide hotkey on a hidden message-only window.</summary>
public sealed class HotkeyService : IDisposable
{
    private const int HotkeyId = 0x5A1;

    private readonly HwndSource _source;
    private bool _registered;

    public event EventHandler? Pressed;

    public HotkeyService()
    {
        var parameters = new HwndSourceParameters("SnaprHotkeyWindow")
        {
            ParentWindow = NativeMethods.HWND_MESSAGE,
            WindowStyle = 0,
        };
        _source = new HwndSource(parameters);
        _source.AddHook(WndProc);
    }

    /// <summary>Registers the gesture, replacing any previous one. Returns false if Windows refused it.</summary>
    public bool Register(HotkeyGesture gesture)
    {
        Unregister();
        _registered = NativeMethods.RegisterHotKey(_source.Handle, HotkeyId,
            gesture.NativeModifiers | NativeMethods.MOD_NOREPEAT, gesture.VirtualKey);
        return _registered;
    }

    public void Unregister()
    {
        if (!_registered) return;
        NativeMethods.UnregisterHotKey(_source.Handle, HotkeyId);
        _registered = false;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_HOTKEY && wParam.ToInt32() == HotkeyId)
        {
            handled = true;
            Pressed?.Invoke(this, EventArgs.Empty);
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        Unregister();
        _source.RemoveHook(WndProc);
        _source.Dispose();
    }
}
