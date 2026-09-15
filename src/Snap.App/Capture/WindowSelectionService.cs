using System.Runtime.InteropServices;
using Snap.Core.Capture;

namespace Snap.App.Capture;

public sealed partial class WindowSelectionService
{
    public PixelRect? FindTopLevelWindowAt(PixelPoint screenPoint, uint excludedProcessId)
    {
        PixelRect? result = null;
        EnumWindows((window, _) =>
        {
            if (!IsWindowVisible(window))
            {
                return true;
            }

            GetWindowThreadProcessId(window, out var processId);
            if (processId == excludedProcessId || !GetWindowRect(window, out var bounds))
            {
                return true;
            }

            if (screenPoint.X < bounds.Left || screenPoint.X >= bounds.Right ||
                screenPoint.Y < bounds.Top || screenPoint.Y >= bounds.Bottom)
            {
                return true;
            }

            result = new PixelRect(bounds.Left, bounds.Top, bounds.Right - bounds.Left, bounds.Bottom - bounds.Top);
            return false;
        }, nint.Zero);

        return result;
    }

    private delegate bool EnumWindowsCallback(nint window, nint state);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumWindows(EnumWindowsCallback callback, nint state);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool IsWindowVisible(nint window);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetWindowRect(nint window, out NativeRectangle bounds);

    [LibraryImport("user32.dll")]
    private static partial uint GetWindowThreadProcessId(nint window, out uint processId);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRectangle
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}
