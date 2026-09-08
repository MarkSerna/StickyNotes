using System;
using System.Threading.Tasks;
using H.NotifyIcon;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using StickyNotes.Sync.Services;

namespace StickyNotes.App.Services;

public class TrayIconService : IDisposable
{
    private TaskbarIcon? _trayIcon;
    private readonly SyncScheduler _syncScheduler;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly Action _onNewNoteRequested;
    private readonly Action _onToggleSidePanelRequested;
    private readonly Action _onShowAllFloatingRequested;
    private readonly Action? _onOpenSettingsRequested;

    public TrayIconService(
        SyncScheduler syncScheduler,
        DispatcherQueue dispatcherQueue,
        Action onNewNoteRequested,
        Action onToggleSidePanelRequested,
        Action onShowAllFloatingRequested,
        Action? onOpenSettingsRequested = null)
    {
        _syncScheduler = syncScheduler;
        _dispatcherQueue = dispatcherQueue;
        _onNewNoteRequested = onNewNoteRequested;
        _onToggleSidePanelRequested = onToggleSidePanelRequested;
        _onShowAllFloatingRequested = onShowAllFloatingRequested;
        _onOpenSettingsRequested = onOpenSettingsRequested;

        InitializeTrayIcon();
    }

    private void InitializeTrayIcon()
    {
        _trayIcon = new TaskbarIcon
        {
            ToolTipText = "Notas Rápidas (Win+Alt+N)"
        };

        // Asignar icono de la aplicación (físico en unpackaged o ms-appx)
        try
        {
            var baseDir = AppContext.BaseDirectory;
            var physicalIcoPath = System.IO.Path.Combine(baseDir, "Assets", "TrayIcon.ico");
            if (System.IO.File.Exists(physicalIcoPath))
            {
                _trayIcon.IconSource = new BitmapImage(new Uri(physicalIcoPath));
            }
            else
            {
                _trayIcon.IconSource = new BitmapImage(new Uri("ms-appx:///Assets/TrayIcon.ico"));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TrayIcon] Advertencia al cargar IconSource: {ex.Message}");
        }

        // Doble clic abre nueva nota
        // La API de H.NotifyIcon puede variar; en esta implementación mínima omitimos el evento
        // y dejamos la opción de Nueva nota en el menú contextual.

        // Construir Menú Contextual WinUI 3
        var menu = new MenuFlyout();

        var itemNew = new MenuFlyoutItem { Text = "Nueva nota (Win+Alt+N)" };
        itemNew.Click += (s, e) => _onNewNoteRequested();
        menu.Items.Add(itemNew);

        var itemSide = new MenuFlyoutItem { Text = "Alternar panel lateral (SideNotes)" };
        itemSide.Click += (s, e) => _onToggleSidePanelRequested();
        menu.Items.Add(itemSide);

        var itemFloating = new MenuFlyoutItem { Text = "Mostrar todas las notas flotantes" };
        itemFloating.Click += (s, e) => _onShowAllFloatingRequested();
        menu.Items.Add(itemFloating);

        menu.Items.Add(new MenuFlyoutSeparator());

        var itemSync = new MenuFlyoutItem { Text = "Sincronizar ahora con Google Drive" };
        itemSync.Click += async (s, e) =>
        {
            ShowNotificationSafe("Notas Rápidas", "Sincronizando notas con Google Drive...");
            await _syncScheduler.RequestImmediateSyncAsync();
        };
        menu.Items.Add(itemSync);

        var itemStartup = new ToggleMenuFlyoutItem
        {
            Text = "Iniciar con Windows",
            IsChecked = StartupService.IsRunAtStartupEnabled()
        };
        itemStartup.Click += (s, e) =>
        {
            StartupService.SetRunAtStartup(itemStartup.IsChecked);
        };
        menu.Items.Add(itemStartup);

        if (_onOpenSettingsRequested != null)
        {
            var itemSettings = new MenuFlyoutItem { Text = "Configuración de Google Drive..." };
            itemSettings.Click += (s, e) => _onOpenSettingsRequested();
            menu.Items.Add(itemSettings);
        }

        menu.Items.Add(new MenuFlyoutSeparator());

        var itemExit = new MenuFlyoutItem { Text = "Salir de Notas Rápidas" };
        itemExit.Click += (s, e) =>
        {
            _trayIcon.Dispose();
            Application.Current.Exit();
        };
        menu.Items.Add(itemExit);

        _trayIcon.ContextFlyout = menu;

        // Registrar explícitamente el icono en el System Tray de Windows (imprescindible cuando se crea por código fuera de XAML)
        try
        {
            _trayIcon.ForceCreate();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TrayIcon] Advertencia al forzar creación en la bandeja: {ex.Message}");
        }

        // Escuchar reportes de sincronización para mostrar notificaciones tipo Toast
        _syncScheduler.SyncCompleted += OnSyncCompleted;
    }

    public void ShowNotificationSafe(string title, string message)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            try
            {
                if (_trayIcon == null) return;

                if (!_trayIcon.IsCreated)
                {
                    _trayIcon.ForceCreate();
                }

                if (_trayIcon.IsCreated)
                {
                    _trayIcon.ShowNotification(title, message);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TrayIcon] Error al mostrar notificación: {ex.Message}");
            }
        });
    }

    private void OnSyncCompleted(SyncReport report)
    {
        if (report.IsSuccess && (report.Uploaded > 0 || report.Downloaded > 0 || report.Deleted > 0))
        {
            var msg = $"Sincronización completa: {report.Uploaded} subidas, {report.Downloaded} descargadas.";
            ShowNotificationSafe("Google Drive Sync", msg);
        }
        else if (!report.IsSuccess && !string.IsNullOrEmpty(report.ErrorMessage))
        {
            // Evitar spam de notificaciones toast si el usuario todavía no ha completado el inicio de sesión OAuth en el navegador
            if (report.ErrorMessage.Contains("no autenticado", StringComparison.OrdinalIgnoreCase) ||
                report.ErrorMessage.Contains("unauthenticated", StringComparison.OrdinalIgnoreCase))
            {
                System.Diagnostics.Debug.WriteLine($"[Sync] Estado de sincronización: {report.ErrorMessage}");
                return;
            }

            ShowNotificationSafe("Error de Sincronización", report.ErrorMessage);
        }
    }

    public void Dispose()
    {
        _syncScheduler.SyncCompleted -= OnSyncCompleted;
        _trayIcon?.Dispose();
    }
}