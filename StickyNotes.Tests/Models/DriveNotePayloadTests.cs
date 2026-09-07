using System;
using System.Text.Json;
using FluentAssertions;
using StickyNotes.Core.Enums;
using StickyNotes.Core.Models;
using StickyNotes.Sync.Models;
using Xunit;

namespace StickyNotes.Tests.Models;

public class DriveNotePayloadTests
{
    [Fact]
    public void FromDomainModel_And_ToDomainModel_ShouldPreserveAllAttributes()
    {
        var original = new Note
        {
            Id = Guid.NewGuid(),
            Title = "Nota Cloud",
            Content = "Contenido sincronizado",
            Color = NoteColor.Purple,
            PositionX = 250,
            PositionY = 180,
            Width = 400,
            Height = 350,
            Monitor = 1,
            IsAlwaysOnTop = true,
            CreatedAt = DateTime.UtcNow.AddHours(-1),
            UpdatedAt = DateTime.UtcNow,
            DeletedAt = null
        };

        // Act
        var payload = DriveNotePayload.FromDomainModel(original);
        var reconstituted = payload.ToDomainModel();

        // Assert
        payload.Id.Should().Be(original.Id);
        payload.Title.Should().Be(original.Title);
        payload.Content.Should().Be(original.Content);
        payload.Color.Should().Be(original.Color);
        payload.PositionX.Should().Be(original.PositionX);
        payload.PositionY.Should().Be(original.PositionY);
        payload.Width.Should().Be(original.Width);
        payload.Height.Should().Be(original.Height);
        payload.Monitor.Should().Be(original.Monitor);
        payload.IsAlwaysOnTop.Should().BeTrue();
        payload.IsDeleted.Should().BeFalse();

        reconstituted.Id.Should().Be(original.Id);
        reconstituted.Title.Should().Be(original.Title);
        reconstituted.Content.Should().Be(original.Content);
        reconstituted.Color.Should().Be(original.Color);
        reconstituted.PositionX.Should().Be(original.PositionX);
        reconstituted.PositionY.Should().Be(original.PositionY);
        reconstituted.Width.Should().Be(original.Width);
        reconstituted.Height.Should().Be(original.Height);
        reconstituted.IsAlwaysOnTop.Should().BeTrue();
    }

    [Fact]
    public void ApplyTo_ShouldUpdateTargetNoteProperties()
    {
        var localNote = new Note
        {
            Id = Guid.NewGuid(),
            Title = "Local Antiguo",
            Content = "Texto viejo",
            Color = NoteColor.Green,
            PositionX = 100,
            PositionY = 100
        };

        var remotePayload = new DriveNotePayload
        {
            Id = localNote.Id,
            Title = "Remoto Actualizado",
            Content = "Texto nuevo desde la nube",
            Color = NoteColor.Pink,
            PositionX = 300,
            PositionY = 200,
            Width = 500,
            Height = 400,
            Monitor = 0,
            IsAlwaysOnTop = true,
            UpdatedAt = DateTime.UtcNow,
            DeletedAt = null
        };

        // Act
        remotePayload.ApplyTo(localNote);

        // Assert
        localNote.Title.Should().Be("Remoto Actualizado");
        localNote.Content.Should().Be("Texto nuevo desde la nube");
        localNote.Color.Should().Be(NoteColor.Pink);
        localNote.PositionX.Should().Be(300);
        localNote.PositionY.Should().Be(200);
        localNote.Width.Should().Be(500);
        localNote.Height.Should().Be(400);
        localNote.IsAlwaysOnTop.Should().BeTrue();
    }

    [Fact]
    public void JsonSerialization_ShouldRoundtripSuccessfully()
    {
        var payload = new DriveNotePayload
        {
            Id = Guid.NewGuid(),
            Title = "Prueba JSON",
            Content = "Sincronización JSON",
            Color = NoteColor.Gray,
            PositionX = 150.5,
            PositionY = 220.0,
            Width = 320,
            Height = 280,
            Monitor = 0,
            IsAlwaysOnTop = false,
            CreatedAt = DateTime.UtcNow.AddMinutes(-10),
            UpdatedAt = DateTime.UtcNow,
            DeletedAt = DateTime.UtcNow.AddMinutes(-2)
        };

        // Act
        var json = JsonSerializer.Serialize(payload);
        var deserialized = JsonSerializer.Deserialize<DriveNotePayload>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Id.Should().Be(payload.Id);
        deserialized.Title.Should().Be(payload.Title);
        deserialized.Content.Should().Be(payload.Content);
        deserialized.Color.Should().Be(payload.Color);
        deserialized.IsDeleted.Should().BeTrue();
        deserialized.DeletedAt.Should().NotBeNull();
    }
}
