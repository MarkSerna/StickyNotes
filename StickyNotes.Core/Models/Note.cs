using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using StickyNotes.Core.Enums;

namespace StickyNotes.Core.Models;

/// <summary>
/// Representa una nota adhesiva en el sistema con soporte de notificación reactiva.
/// </summary>
public class Note : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>Identificador único global (UUIDv4) de la nota.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    private string? _title;
    /// <summary>Título personalizado de la nota (opcional, editable en cabecera).</summary>
    public string? Title
    {
        get => _title;
        set
        {
            if (_title != value)
            {
                _title = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayTitle));
            }
        }
    }

    private string _content = string.Empty;
    /// <summary>Contenido en texto enriquecido (RTF) o Markdown según el editor.</summary>
    public string Content
    {
        get => _content;
        set
        {
            if (_content != value)
            {
                _content = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayTitle));
                OnPropertyChanged(nameof(PreviewText));
            }
        }
    }

    private NoteColor _color = NoteColor.Yellow;
    /// <summary>Color visual seleccionado para la nota.</summary>
    public NoteColor Color
    {
        get => _color;
        set
        {
            if (_color != value)
            {
                _color = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ColorHex));
            }
        }
    }

    /// <summary>Coordenada horizontal X en la pantalla (píxeles virtuales).</summary>
    public double PositionX { get; set; } = 100.0;

    /// <summary>Coordenada vertical Y en la pantalla (píxeles virtuales).</summary>
    public double PositionY { get; set; } = 100.0;

    /// <summary>Ancho de la ventana en modo flotante.</summary>
    public double Width { get; set; } = 300.0;

    private double _height = 260.0;
    /// <summary>Alto de la ventana en modo flotante o tarjeta de nota en panel lateral.</summary>
    public double Height
    {
        get => _height;
        set
        {
            if (Math.Abs(_height - value) > 0.5)
            {
                _height = value;
                OnPropertyChanged();
            }
        }
    }

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

    private SyncStatus _syncStatus = SyncStatus.PendingUpload;
    /// <summary>Estado de sincronización respecto a Google Drive.</summary>
    public SyncStatus SyncStatus
    {
        get => _syncStatus;
        set
        {
            if (_syncStatus != value)
            {
                _syncStatus = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Texto limpio para previsualizaciones en listas, filtrado de RTF para evitar mostrar código fuente.
    /// </summary>
    public string PreviewText
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Content))
                return string.Empty;

            var text = Content;
            if (text.StartsWith("{\\rtf", StringComparison.OrdinalIgnoreCase))
            {
                // Limpiar etiquetas RTF a texto plano legible
                text = Regex.Replace(text, @"{\\*?\\[^{}]+}|[{}]|\\\n?[A-Za-z]+-?\d* ?|\\'[0-9a-fA-F]{2}", " ");
            }

            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (!string.IsNullOrWhiteSpace(trimmed))
                {
                    return trimmed.Length > 40 ? trimmed[..40] + "..." : trimmed;
                }
            }

            return string.Empty;
        }
    }

    /// <summary>Devuelve el título visible o los primeros caracteres del contenido si el título es nulo.</summary>
    public string DisplayTitle
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(Title))
                return Title.Trim();

            var preview = PreviewText;
            return !string.IsNullOrWhiteSpace(preview) ? (preview.Length > 30 ? preview[..30] + "..." : preview) : "Nota sin título";
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