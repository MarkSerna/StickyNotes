using System;
using System.Text.Json.Serialization;
using StickyNotes.Core.Enums;
using StickyNotes.Core.Models;

namespace StickyNotes.Sync.Models;

public class DriveNotePayload
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("color")]
    public NoteColor Color { get; set; }

    [JsonPropertyName("positionX")]
    public double PositionX { get; set; }

    [JsonPropertyName("positionY")]
    public double PositionY { get; set; }

    [JsonPropertyName("width")]
    public double Width { get; set; }

    [JsonPropertyName("height")]
    public double Height { get; set; }

    [JsonPropertyName("monitor")]
    public int Monitor { get; set; }

    [JsonPropertyName("isAlwaysOnTop")]
    public bool IsAlwaysOnTop { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("deletedAt")]
    public DateTime? DeletedAt { get; set; }

    [JsonIgnore]
    public bool IsDeleted => DeletedAt != null;

    public static DriveNotePayload FromDomainModel(Note note) => new()
    {
        Id = note.Id,
        Title = note.Title,
        Content = note.Content,
        Color = note.Color,
        PositionX = note.PositionX,
        PositionY = note.PositionY,
        Width = note.Width,
        Height = note.Height,
        Monitor = note.Monitor,
        IsAlwaysOnTop = note.IsAlwaysOnTop,
        CreatedAt = note.CreatedAt,
        UpdatedAt = note.UpdatedAt,
        DeletedAt = note.DeletedAt
    };

    public Note ToDomainModel() => new()
    {
        Id = this.Id,
        Title = this.Title,
        Content = this.Content,
        Color = this.Color,
        PositionX = this.PositionX,
        PositionY = this.PositionY,
        Width = this.Width,
        Height = this.Height,
        Monitor = this.Monitor,
        IsAlwaysOnTop = this.IsAlwaysOnTop,
        CreatedAt = this.CreatedAt,
        UpdatedAt = this.UpdatedAt,
        DeletedAt = this.DeletedAt
    };

    public void ApplyTo(Note target)
    {
        target.Title = this.Title;
        target.Content = this.Content;
        target.Color = this.Color;
        target.PositionX = this.PositionX;
        target.PositionY = this.PositionY;
        target.Width = this.Width;
        target.Height = this.Height;
        target.Monitor = this.Monitor;
        target.IsAlwaysOnTop = this.IsAlwaysOnTop;
        target.UpdatedAt = this.UpdatedAt;
        target.DeletedAt = this.DeletedAt;
    }
}