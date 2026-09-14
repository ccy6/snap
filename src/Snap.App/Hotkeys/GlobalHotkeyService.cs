using System.Runtime.InteropServices;
using System.Windows.Interop;
using Snap.Core.Hotkeys;

namespace Snap.App.Hotkeys;

public sealed partial class GlobalHotkeyService : IDisposable
{
    private const int HotkeyId = 0x534E;
    private const int WmHotkey = 0x0312;
    private readonly HwndSource _messageWindow;
    private bool _isRegistered;

    public GlobalHotkeyService()
    {
        _messageWindow = new HwndSource(new HwndSourceParameters("Snap.HotkeyWindow")
        {
            ParentWindow = new nint(-3),
            WindowStyle = 0,
        });
        _messageWindow.AddHook(WindowProcedure);
    }

    public event EventHandler? Pressed;

    public bool Register(HotkeyGesture hotkey)
    {
        ArgumentNullException.ThrowIfNull(hotkey);
        Unregister();
        _isRegistered = RegisterHotKey(
            _messageWindow.Handle,
            HotkeyId,
            (uint)hotkey.Modifiers,
            hotkey.VirtualKey);
        return _isRegistered;
    }

    public void Dispose()
    {
        Unregister();
        _messageWindow.RemoveHook(WindowProcedure);
        _messageWindow.Dispose();
    }

    private void Unregister()
    {
        if (!_isRegistered)
        {
            return;
        }

        UnregisterHotKey(_messageWindow.Handle, HotkeyId);
        _isRegistered = false;
    }

    private nint WindowProcedure(nint hwnd, int message, nint wParam, nint lParam, ref bool handled)
    {
        if (message == WmHotkey && wParam == HotkeyId)
        {
            handled = true;
            Pressed?.Invoke(this, EventArgs.Empty);
        }

        return nint.Zero;
    }

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool RegisterHotKey(nint hwnd, int id, uint modifiers, uint virtualKey);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool UnregisterHotKey(nint hwnd, int id);
}
