using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StickyNotes.Core.Enums;
using StickyNotes.Core.Models;
using StickyNotes.Data.Repositories;
using StickyNotes.Tests.TestHelpers;
using Xunit;

namespace StickyNotes.Tests.Repositories;

public class SqliteNoteRepositoryTests : IDisposable
{
    private readonly SqliteTestContext _testContext;
    private readonly SqliteNoteRepository _repository;

    public SqliteNoteRepositoryTests()
    {
        _testContext = new SqliteTestContext();
        _repository = new SqliteNoteRepository(_testContext.DbContext);
    }

    public void Dispose()
    {
        _testContext.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task CreateAsync_ShouldPersistNote_AndSetDefaults()
    {
        // Arrange
        var note = new Note
        {
            Title = "Nota de prueba",
            Content = "Contenido de prueba",
            Color = NoteColor.Blue
        };

        // Act
        var created = await _repository.CreateAsync(note);

        // Assert
        created.Should().NotBeNull();
        created.Id.Should().NotBeEmpty();
        created.Title.Should().Be("Nota de prueba");
        created.SyncStatus.Should().Be(SyncStatus.PendingUpload);
        created.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        created.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        created.DeletedAt.Should().BeNull();

        var inDb = await _repository.GetByIdAsync(created.Id);
        inDb.Should().NotBeNull();
        inDb!.Title.Should().Be("Nota de prueba");
    }

    [Fact]
    public async Task GetActiveNotesAsync_ShouldExcludeSoftDeletedNotes()
    {
        // Arrange
        var active1 = await _repository.CreateAsync(new Note { Title = "Activa 1", Content = "C1" });
        var active2 = await _repository.CreateAsync(new Note { Title = "Activa 2", Content = "C2" });
        var toDelete = await _repository.CreateAsync(new Note { Title = "Para borrar", Content = "C3" });

        await _repository.SoftDeleteAsync(toDelete.Id);

        // Act
        var activeNotes = await _repository.GetActiveNotesAsync();

        // Assert
        activeNotes.Should().HaveCount(2);
        activeNotes.Should().Contain(n => n.Id == active1.Id);
        activeNotes.Should().Contain(n => n.Id == active2.Id);
        activeNotes.Should().NotContain(n => n.Id == toDelete.Id);
    }

    [Fact]
    public async Task GetTrashNotesAsync_ShouldReturnOnlyDeletedNotes()
    {
        // Arrange
        var active = await _repository.CreateAsync(new Note { Title = "Nota activa", Content = "C1" });
        var deleted = await _repository.CreateAsync(new Note { Title = "Nota eliminada", Content = "C2" });

        await _repository.SoftDeleteAsync(deleted.Id);

        // Act
        var trashNotes = await _repository.GetTrashNotesAsync();

        // Assert
        trashNotes.Should().HaveCount(1);
        trashNotes[0].Id.Should().Be(deleted.Id);
        trashNotes[0].DeletedAt.Should().NotBeNull();
        trashNotes[0].DeletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task RestoreFromTrashAsync_ShouldClearDeletedAt_AndReturnToActive()
    {
        // Arrange
        var note = await _repository.CreateAsync(new Note { Title = "Nota temporal", Content = "C1" });
        await _repository.SoftDeleteAsync(note.Id);

        // Act
        await _repository.RestoreFromTrashAsync(note.Id);

        // Assert
        var activeNotes = await _repository.GetActiveNotesAsync();
        activeNotes.Should().Contain(n => n.Id == note.Id);

        var restored = activeNotes.First(n => n.Id == note.Id);
        restored.DeletedAt.Should().BeNull();
        restored.SyncStatus.Should().Be(SyncStatus.PendingUpload);

        var trashNotes = await _repository.GetTrashNotesAsync();
        trashNotes.Should().NotContain(n => n.Id == note.Id);
    }

    [Fact]
    public async Task PermanentDeleteAsync_ShouldPhysicallyRemoveNoteFromDatabase()
    {
        // Arrange
        var note = await _repository.CreateAsync(new Note { Title = "Nota a erradicar", Content = "C1" });
        await _repository.SoftDeleteAsync(note.Id);

        // Act
        await _repository.PermanentDeleteAsync(note.Id);

        // Assert
        var activeNotes = await _repository.GetActiveNotesAsync();
        activeNotes.Should().NotContain(n => n.Id == note.Id);

        var trashNotes = await _repository.GetTrashNotesAsync();
        trashNotes.Should().NotContain(n => n.Id == note.Id);

        var allNotes = await _repository.GetAllNotesIncludingDeletedAsync();
        allNotes.Should().NotContain(n => n.Id == note.Id);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateFields_AndMarkPendingUpload()
    {
        // Arrange
        var note = await _repository.CreateAsync(new Note { Title = "Original", Content = "Contenido original" });
        await _repository.MarkAsSyncedAsync(note.Id);

        var syncedNote = await _repository.GetByIdAsync(note.Id);
        syncedNote!.SyncStatus.Should().Be(SyncStatus.Synced);

        // Act
        syncedNote.Title = "Modificado";
        syncedNote.Content = "Nuevo contenido";
        await _repository.UpdateAsync(syncedNote);

        // Assert
        var updated = await _repository.GetByIdAsync(note.Id);
        updated!.Title.Should().Be("Modificado");
        updated.Content.Should().Be("Nuevo contenido");
        updated.SyncStatus.Should().Be(SyncStatus.PendingUpload);
    }

    [Fact]
    public async Task MarkAsSyncedAsync_ShouldUpdateStatusToSynced()
    {
        // Arrange
        var note = await _repository.CreateAsync(new Note { Title = "Sync Test", Content = "..." });
        note.SyncStatus.Should().Be(SyncStatus.PendingUpload);

        // Act
        await _repository.MarkAsSyncedAsync(note.Id);

        // Assert
        var updated = await _repository.GetByIdAsync(note.Id);
        updated!.SyncStatus.Should().Be(SyncStatus.Synced);
    }

    [Fact]
    public async Task GetPendingSyncNotesAsync_ShouldIncludeBothActiveAndTrashPendingNotes()
    {
        // Arrange
        var activePending = await _repository.CreateAsync(new Note { Title = "Activa Pendiente", Content = "..." });
        var activeSynced = await _repository.CreateAsync(new Note { Title = "Activa Sincronizada", Content = "..." });
        await _repository.MarkAsSyncedAsync(activeSynced.Id);

        var trashPending = await _repository.CreateAsync(new Note { Title = "Papelera Pendiente", Content = "..." });
        await _repository.SoftDeleteAsync(trashPending.Id); // Esto vuelve a poner SyncStatus = PendingUpload

        // Act
        var pendingList = await _repository.GetPendingSyncNotesAsync();

        // Assert
        pendingList.Should().HaveCount(2);
        pendingList.Should().Contain(n => n.Id == activePending.Id);
        pendingList.Should().Contain(n => n.Id == trashPending.Id);
        pendingList.Should().NotContain(n => n.Id == activeSynced.Id);
    }
}
