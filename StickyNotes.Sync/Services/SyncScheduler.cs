using System;
using System.Threading;
using System.Threading.Tasks;

namespace StickyNotes.Sync.Services;

public class SyncScheduler : IDisposable
{
    private readonly GoogleDriveSyncService _syncService;
    private readonly Timer _periodicTimer;
    private readonly CancellationTokenSource _cts = new();

    public event Action<SyncReport>? SyncCompleted;

    public SyncScheduler(GoogleDriveSyncService syncService)
    {
        _syncService = syncService;

        // Intervalo periódico: cada 5 minutos
        _periodicTimer = new Timer(OnPeriodicSync, null, TimeSpan.FromSeconds(10), TimeSpan.FromMinutes(5));
    }

    private async void OnPeriodicSync(object? state)
    {
        await ExecuteSyncAsync();
    }

    /// <summary>
    /// Dispara una sincronización inmediata (por ejemplo tras guardar una nota o al reanudar la app).
    /// </summary>
    public async Task RequestImmediateSyncAsync()
    {
        await ExecuteSyncAsync();
    }

    private async Task ExecuteSyncAsync()
    {
        if (_cts.IsCancellationRequested) return;

        try
        {
            var report = await _syncService.SyncAllAsync(_cts.Token);
            SyncCompleted?.Invoke(report);
        }
        catch (Exception ex)
        {
            SyncCompleted?.Invoke(new SyncReport(0, 0, 0, false, ex.Message));
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _periodicTimer.Dispose();
        _cts.Dispose();
    }
}
