using System;
using System.Diagnostics;
using Microsoft.Win32;

namespace StickyNotes.App.Services;

public static class StartupService
{
    private const string RUN_KEY_PATH = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string APP_NAME = "StickyNotesWindows11";

    /// <summary>
    /// Verifica si la aplicación está configurada para iniciar con Windows.
    /// </summary>
    public static bool IsRunAtStartupEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RUN_KEY_PATH, false);
            return key?.GetValue(APP_NAME) != null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Habilita o deshabilita el arranque automático con Windows 11.
    /// </summary>
    public static void SetRunAtStartup(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RUN_KEY_PATH, true);
            if (key == null) return;

            if (enable)
            {
                var exePath = Process.GetCurrentProcess().MainModule?.FileName;
                if (!string.IsNullOrEmpty(exePath))
                {
                    // Añadir argumento --minimized para iniciar silencioso en la bandeja
                    key.SetValue(APP_NAME, $"\"{exePath}\" --minimized");
                }
            }
            else
            {
                key.DeleteValue(APP_NAME, false);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error al modificar el registro de inicio: {ex.Message}");
        }
    }
}