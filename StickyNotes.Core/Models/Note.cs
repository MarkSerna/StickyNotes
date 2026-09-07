using System;
using StickyNotes.Core.Enums;

namespace StickyNotes.Core.Models;

/// <summary>
/// Representa una nota adhesiva en el sistema.
/// </summary>
public class Note
{
    /// <summary>Identificador único global (UUIDv4) de la nota.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Título personalizado de la nota (opcional, editable en cabecera).</summary>
    public string? Title { get; set; }

    /// <summary>Contenido en texto enriquecido (RTF) o Markdown según el editor.</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>Color visual seleccionado para la nota.</summary>
    public NoteColor Color { get; set; } = NoteColor.Yellow;

    /// <summary>Coordenada horizontal X en la pantalla (píxeles virtuales).</summary>
    public double PositionX { get; set; } = 100.0;

    /// <summary>Coordenada vertical Y en la pantalla (píxeles virtuales).</summary>
    public double PositionY { get; set; } = 100.0;

    /// <summary>Ancho de la ventana en modo flotante.</summary>
    public double Width { get; set; } = 300.0;

    /// <summary>Alto de la ventana en modo flotante.</summary>
    public double Height { get; set; } = 260.0;

    /// <summary>Índice del monitor donde se ubicó la nota por última vez.</summary>
    public int Monitor { get; set; } = 0;

    /// <summary>Indica si la nota debe permanecer siempre por encima (TopMost).</summary>
    public bool IsAlwaysOnTop { get; set; } = false;

    /// <summary>Fecha y hora de creación (UTC).</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Fecha y hora de la última modificación local o remota (UTC).</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Fecha de eliminación lógica. Si es null, la nota está activa. 
    /// Si tiene valor, está en la papelera de reciclaje.
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>Identificador del dispositivo que realizó la última edición.</summary>
    public string DeviceId { get; set; } = Environment.MachineName;

    /// <summary>Estado de sincronización respecto a Google Drive.</summary>
    public SyncStatus SyncStatus { get; set; } = SyncStatus.PendingUpload;

    /// <summary>Devuelve el título visible o los primeros caracteres del contenido si el título es nulo.</summary>
    public string DisplayTitle
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(Title))
                return Title.Trim();

            if (string.IsNullOrWhiteSpace(Content))
                return "Nota sin título";

            var firstLine = Content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            return firstLine.Length > 0 ? (firstLine[0].Length > 30 ? firstLine[0][..30] + "..." : firstLine[0]) : "Nota sin título";
        }
    }

    /// <summary>Color hexadecimal de la nota para plantillas e interfaces.</summary>
    public string ColorHex => Color switch
    {
        NoteColor.Yellow => "#FFF385",
        NoteColor.Green => "#D2F8B8",
        NoteColor.Pink => "#FFCEE8",
        NoteColor.Purple => "#E7DCFF",
        NoteColor.Blue => "#CEECFE",
        NoteColor.Gray => "#E9ECEF",
        _ => "#FFF385"
    };
}