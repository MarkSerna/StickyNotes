namespace StickyNotes.Core.Enums;

/// <summary>
/// Estado de sincronización local respecto a la carpeta appDataFolder de Google Drive.
/// </summary>
public enum SyncStatus
{
    /// <summary>La nota está sincronizada y es idéntica en la nube.</summary>
    Synced = 0,

    /// <summary>Modificada localmente, pendiente de subida a Google Drive.</summary>
    PendingUpload = 1,

    /// <summary>Conflicto detectado (modificación concurrente en dos equipos).</summary>
    Conflict = 2
}