using System;
using System.Runtime.InteropServices;
using Windows.Graphics;

namespace StickyNotes.App.Helpers;

public static class MonitorHelper
{
    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    private const uint MONITOR_DEFAULTTOPRIMARY = 1;

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    public static RectInt32 GetPrimaryMonitorWorkArea()
    {
        var mi = new MONITORINFO();
        mi.cbSize = Marshal.SizeOf(mi);

        var hMonitor = MonitorFromWindow(IntPtr.Zero, MONITOR_DEFAULTTOPRIMARY);
        if (GetMonitorInfo(hMonitor, ref mi))
        {
            return new RectInt32(
                mi.rcWork.Left,
                mi.rcWork.Top,
                mi.rcWork.Right - mi.rcWork.Left,
                mi.rcWork.Bottom - mi.rcWork.Top
            );
        }

        // Fallback estándar en caso de fallo Win32 (Full HD típico)
        return new RectInt32(0, 0, 1920, 1040);
    }
}
