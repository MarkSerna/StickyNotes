using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using StickyNotes.App.Views;
using StickyNotes.Core.Enums;
using StickyNotes.Core.Models;
using StickyNotes.Data.Repositories;
using StickyNotes.Sync.Services;

namespace StickyNotes.App.Services;

public class WindowManager
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly SyncScheduler _syncScheduler;
    private IServiceScope? _sideNotesScope;
    private SideNotesWindow? _sideNotesWindow;
    public SideNotesWindow? SideNotesWindow => _sideNotesWindow;
    private readonly Dictionary<Guid, NoteWindow> _floatingWindows = new();
    private TrayIconService? _trayIconService;
    private GlobalHotkeyService? _hotkeyService;

    public WindowManager(IServiceScopeFactory scopeFactory, SyncScheduler syncScheduler)
    {
        _scopeFactory = scopeFactory;
        _syncScheduler = syncScheduler;
    }

    public void Initialize(DispatcherQueue dispatcherQueue)
    {
        // 1. Registrar fachada en AppManager
        AppManager.Instance.RegisterWindowManager(this);

        // 2. Inicializar bandeja del sistema (Tray Icon)
        try
        {
            _trayIconService = new TrayIconService(
                _syncScheduler,
                dispatcherQueue,
                onNewNoteRequested: () => dispatcherQueue.TryEnqueue(() => _ = CreateAndOpenNewNoteFloatingAsync()),
                onToggleSidePanelRequested: () => dispatcherQueue.TryEnqueue(ToggleSideNotes),
                onShowAllFloatingRequested: () => dispatcherQueue.TryEnqueue(() => _ = ShowAllFloatingNotesAsync()),
                onOpenSettingsRequested: () => dispatcherQueue.TryEnqueue(OpenSettingsDialog)
            );
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"⚠️ No se pudo inicializar TrayIconService: {ex.Message}");
        }

        // 3. Inicializar atajo global de teclado (Win + Alt + N)
        try
        {
            _hotkeyService = new GlobalHotkeyService(
                dispatcherQueue,
                onHotkeyPressed: () => dispatcherQueue.TryEnqueue(() => _ = CreateAndOpenNewNoteFloatingAsync())
            );
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"⚠️ No se pudo inicializar GlobalHotkeyService: {ex.Message}");
        }

        // 4. Abrir panel lateral SideNotes inicial
        EnsureSideNotesWindow();
        _sideNotesWindow?.Activate();
    }

    private void EnsureSideNotesWindow()
    {
        if (_sideNotesWindow == null)
        {
            _sideNotesScope = _scopeFactory.CreateScope();
            _sideNotesWindow = _sideNotesScope.ServiceProvider.GetRequiredService<SideNotesWindow>();
            _sideNotesWindow.Closed += SideNotesWindow_Closed;
        }
    }

    public void ToggleSideNotes()
    {
        EnsureSideNotesWindow();
        if (_sideNotesWindow != null)
        {
            _sideNotesWindow.Activate();
        }
    }

    public async Task<NoteWindow?> OpenNoteAsFloatingAsync(Guid noteId)
    {
        // 1. Anti-duplicación: si ya está abierta, traerla al frente
        if (_floatingWindows.TryGetValue(noteId, out var existingWindow))
        {
            existingWindow.Activate();
            return existingWindow;
        }

        // 2. Obtener la nota desde SQLite
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<INoteRepository>();
        var note = await repository.GetByIdAsync(noteId);

        if (note == null)
        {
            return null;
        }

        // 3. Instanciar y registrar NoteWindow
        var noteWindow = new NoteWindow(note, repository);
        _floatingWindows[noteId] = noteWindow;

        noteWindow.Closed += (sender, args) =>
        {
            _floatingWindows.Remove(noteId);
        };

        noteWindow.Activate();
        return noteWindow;
    }

    public async Task<NoteWindow> CreateAndOpenNewNoteFloatingAsync(double? x = null, double? y = null)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<INoteRepository>();

        // Calcular posición escalonada si no se especifican coordenadas
        var offsetX = x ?? (100 + (_floatingWindows.Count * 30));
        var offsetY = y ?? (120 + (_floatingWindows.Count * 30));

        var newNote = new Note
        {
            Title = null,
            Content = string.Empty,
            Color = NoteColor.Yellow,
            PositionX = offsetX,
            PositionY = offsetY,
            Width = 280,
            Height = 260,
            IsAlwaysOnTop = false
        };

        var created = await repository.CreateAsync(newNote);

        var noteWindow = new NoteWindow(created, repository);
        _floatingWindows[created.Id] = noteWindow;

        noteWindow.Closed += (sender, args) =>
        {
            _floatingWindows.Remove(created.Id);
        };

        noteWindow.Activate();
        return noteWindow;
    }

    public async Task ShowAllFloatingNotesAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<INoteRepository>();
        var activeNotes = await repository.GetActiveNotesAsync();

        foreach (var note in activeNotes)
        {
            await OpenNoteAsFloatingAsync(note.Id);
        }
    }

    private void SideNotesWindow_Closed(object sender, Microsoft.UI.Xaml.WindowEventArgs args)
    {
        if (_sideNotesWindow != null)
        {
            _sideNotesWindow.Closed -= SideNotesWindow_Closed;
            _sideNotesWindow = null;
        }

        _sideNotesScope?.Dispose();
        _sideNotesScope = null;
    }

    public void OpenSettingsDialog()
    {
        EnsureSideNotesWindow();
        if (_sideNotesWindow != null)
        {
            _sideNotesWindow.Activate();
            var syncService = App.Services.GetRequiredService<GoogleDriveSyncService>();
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(_sideNotesWindow);
            var dialog = new SettingsDialog(syncService, _syncScheduler, hWnd)
            {
                XamlRoot = _sideNotesWindow.Content.XamlRoot
            };
            _ = dialog.ShowAsync();
        }
    }

    public void Shutdown()
    {
        _trayIconService?.Dispose();
        _trayIconService = null;

        _hotkeyService?.Dispose();
        _hotkeyService = null;

        foreach (var window in _floatingWindows.Values.ToList())
        {
            window.Close();
        }
        _floatingWindows.Clear();

        if (_sideNotesWindow != null)
        {
            _sideNotesWindow.Close();
        }
    }
}
