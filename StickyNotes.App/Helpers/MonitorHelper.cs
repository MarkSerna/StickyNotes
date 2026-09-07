using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Windows.Graphics;

namespace StickyNotes.App.Helpers;

public record MonitorInfo(int Index, RectInt32 Bounds, RectInt32 WorkArea, bool IsPrimary);

public static class MonitorHelper
{
    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    private delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

    [DllImport("user32.dll")]
    private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    private const uint MONITOR_DEFAULTTOPRIMARY = 1;
    private const uint MONITOR_DEFAULTTONEAREST = 2;

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;
    }

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

    public static (int X, int Y) GetCursorPosition()
    {
        if (GetCursorPos(out var pt))
        {
            return (pt.X, pt.Y);
        }
        return (0, 0);
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

        return new RectInt32(0, 0, 1920, 1040);
    }

    public static RectInt32 GetMonitorWorkAreaFromPoint(int x, int y)
    {
        var mi = new MONITORINFO();
        mi.cbSize = Marshal.SizeOf(mi);

        var pt = new POINT { X = x, Y = y };
        var hMonitor = MonitorFromPoint(pt, MONITOR_DEFAULTTONEAREST);
        if (hMonitor != IntPtr.Zero && GetMonitorInfo(hMonitor, ref mi))
        {
            return new RectInt32(
                mi.rcWork.Left,
                mi.rcWork.Top,
                mi.rcWork.Right - mi.rcWork.Left,
                mi.rcWork.Bottom - mi.rcWork.Top
            );
        }

        return GetPrimaryMonitorWorkArea();
    }

    public static RectInt32 GetMonitorWorkAreaFromWindow(IntPtr hWnd)
    {
        var mi = new MONITORINFO();
        mi.cbSize = Marshal.SizeOf(mi);

        var hMonitor = MonitorFromWindow(hWnd, MONITOR_DEFAULTTONEAREST);
        if (hMonitor != IntPtr.Zero && GetMonitorInfo(hMonitor, ref mi))
        {
            return new RectInt32(
                mi.rcWork.Left,
                mi.rcWork.Top,
                mi.rcWork.Right - mi.rcWork.Left,
                mi.rcWork.Bottom - mi.rcWork.Top
            );
        }

        return GetPrimaryMonitorWorkArea();
    }

    public static List<MonitorInfo> GetAllMonitors()
    {
        var list = new List<MonitorInfo>();
        int index = 1;

        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr hMon, IntPtr hdc, ref RECT r, IntPtr data) =>
        {
            var mi = new MONITORINFO();
            mi.cbSize = Marshal.SizeOf(mi);
            if (GetMonitorInfo(hMon, ref mi))
            {
                var bounds = new RectInt32(mi.rcMonitor.Left, mi.rcMonitor.Top, mi.rcMonitor.Right - mi.rcMonitor.Left, mi.rcMonitor.Bottom - mi.rcMonitor.Top);
                var work = new RectInt32(mi.rcWork.Left, mi.rcWork.Top, mi.rcWork.Right - mi.rcWork.Left, mi.rcWork.Bottom - mi.rcWork.Top);
                bool isPrimary = (mi.dwFlags & 1) != 0;
                list.Add(new MonitorInfo(index++, bounds, work, isPrimary));
            }
            return true;
        }, IntPtr.Zero);

        if (list.Count == 0)
        {
            var primary = GetPrimaryMonitorWorkArea();
            list.Add(new MonitorInfo(1, primary, primary, true));
        }

        return list;
    }
}
