using System;
using System.Runtime.InteropServices;
using Microsoft.UI.Dispatching;

namespace StickyNotes.App.Services;

public class GlobalHotkeyService : IDisposable
{
    private const int HOTKEY_ID = 9001;
    private const uint MOD_ALT = 0x0001;
    private const uint MOD_WIN = 0x0008;
    private const uint MOD_NOREPEAT = 0x4000;
    private const uint VK_N = 0x4E; // Tecla 'N'
    private const int WM_HOTKEY = 0x0312;

    private readonly DispatcherQueue _dispatcherQueue;
    private readonly Action _onHotkeyPressed;
    private IntPtr _messageHwnd;
    private WndProcDelegate? _wndProc;

    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr CreateWindowEx(
        uint dwExStyle, string lpClassName, string lpWindowName, uint dwStyle,
        int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr DefWindowProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern ushort RegisterClass(ref WNDCLASS lpWndClass);

    [StructLayout(LayoutKind.Sequential)]
    private struct WNDCLASS
    {
        public uint style;
        public IntPtr lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public IntPtr hInstance;
        public IntPtr hIcon;
        public IntPtr hCursor;
        public IntPtr hbrBackground;
        public string? lpszMenuName;
        public string lpszClassName;
    }

    public GlobalHotkeyService(DispatcherQueue dispatcherQueue, Action onHotkeyPressed)
    {
        _dispatcherQueue = dispatcherQueue;
        _onHotkeyPressed = onHotkeyPressed;

        InitializeMessageWindow();
        RegisterWinAltN();
    }

    private void InitializeMessageWindow()
    {
        _wndProc = CustomWndProc;
        var className = $"StickyNotes_HotkeyMsg_{Guid.NewGuid():N}";

        var wndClass = new WNDCLASS
        {
            lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wndProc),
            lpszClassName = className
        };

        RegisterClass(ref wndClass);

        // Ventana invisible exclusiva para bombeo de mensajes de Windows
        _messageHwnd = CreateWindowEx(
            0, className, "StickyNotesHotkeyListener", 0,
            0, 0, 0, 0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
    }

    private void RegisterWinAltN()
    {
        // Registrar combinación Win + Alt + N (con MOD_NOREPEAT para evitar ráfagas al mantener pulsado)
        var success = RegisterHotKey(_messageHwnd, HOTKEY_ID, MOD_WIN | MOD_ALT | MOD_NOREPEAT, VK_N);
        if (!success)
        {
            // Fallback secundario si otra app ocupa Win+Alt+N: Ctrl+Alt+N
            const uint MOD_CONTROL = 0x0002;
            RegisterHotKey(_messageHwnd, HOTKEY_ID, MOD_CONTROL | MOD_ALT | MOD_NOREPEAT, VK_N);
        }
    }

    private IntPtr CustomWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
        {
            // Enrutar la acción de forma segura al hilo principal de UI de WinUI 3
            _dispatcherQueue.TryEnqueue(() => _onHotkeyPressed());
            return IntPtr.Zero;
        }

        return DefWindowProc(hWnd, msg, wParam, lParam);
    }

    public void Dispose()
    {
        if (_messageHwnd != IntPtr.Zero)
        {
            UnregisterHotKey(_messageHwnd, HOTKEY_ID);
            DestroyWindow(_messageHwnd);
            _messageHwnd = IntPtr.Zero;
        }
    }
}