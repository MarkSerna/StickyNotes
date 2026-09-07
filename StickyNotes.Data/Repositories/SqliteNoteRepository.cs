using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StickyNotes.Core.Enums;
using StickyNotes.Core.Models;
using StickyNotes.Data.Context;

namespace StickyNotes.Data.Repositories;

public interface INoteRepository
{
    Task<List<Note>> GetActiveNotesAsync();
    Task<List<Note>> GetTrashNotesAsync();
    Task<Note?> GetByIdAsync(Guid id);
    Task<Note> CreateAsync(Note note);
    Task UpdateAsync(Note note);
    Task SoftDeleteAsync(Guid id);
    Task RestoreFromTrashAsync(Guid id);
    Task PermanentDeleteAsync(Guid id);
    Task<List<Note>> GetPendingSyncNotesAsync();
    Task<List<Note>> GetAllNotesIncludingDeletedAsync();
    Task MarkAsSyncedAsync(Guid id, DateTime? syncTime = null);
}

public class SqliteNoteRepository : INoteRepository
{
    private readonly NotesDbContext _context;

    public SqliteNoteRepository(NotesDbContext context)
    {
        _context = context;
    }

    public async Task<List<Note>> GetActiveNotesAsync()
    {
        // Aplica el filtro de DeletedAt == null automáticamente
        return await _context.Notes
            .OrderByDescending(n => n.UpdatedAt)
            .ToListAsync();
    }

    public async Task<List<Note>> GetTrashNotesAsync()
    {
        // Ignora el filtro global para consultar notas en la papelera
        return await _context.Notes
            .IgnoreQueryFilters()
            .Where(n => n.DeletedAt != null)
            .OrderByDescending(n => n.DeletedAt)
            .ToListAsync();
    }

    public async Task<Note?> GetByIdAsync(Guid id)
    {
        return await _context.Notes.FindAsync(id);
    }

    public async Task<Note> CreateAsync(Note note)
    {
        note.CreatedAt = DateTime.UtcNow;
        note.UpdatedAt = DateTime.UtcNow;
        note.SyncStatus = SyncStatus.PendingUpload;

        _context.Notes.Add(note);
        await _context.SaveChangesAsync();
        return note;
    }

    public async Task UpdateAsync(Note note)
    {
        note.UpdatedAt = DateTime.UtcNow;
        note.SyncStatus = SyncStatus.PendingUpload;

        _context.Entry(note).State = EntityState.Modified;
        await _context.SaveChangesAsync();
    }

    public async Task SoftDeleteAsync(Guid id)
    {
        var note = await _context.Notes.FindAsync(id);
        if (note != null)
        {
            note.DeletedAt = DateTime.UtcNow;
            note.UpdatedAt = DateTime.UtcNow;
            note.SyncStatus = SyncStatus.PendingUpload;
            await _context.SaveChangesAsync();
        }
    }

    public async Task RestoreFromTrashAsync(Guid id)
    {
        var note = await _context.Notes
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(n => n.Id == id);

        if (note != null)
        {
            note.DeletedAt = null;
            note.UpdatedAt = DateTime.UtcNow;
            note.SyncStatus = SyncStatus.PendingUpload;
            await _context.SaveChangesAsync();
        }
    }

    public async Task PermanentDeleteAsync(Guid id)
    {
        var note = await _context.Notes
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(n => n.Id == id);

        if (note != null)
        {
            _context.Notes.Remove(note);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<Note>> GetPendingSyncNotesAsync()
    {
        // Incluye tanto activas como en papelera pendientes de subida a Drive
        return await _context.Notes
            .IgnoreQueryFilters()
            .Where(n => n.SyncStatus == SyncStatus.PendingUpload)
            .ToListAsync();
    }

    public async Task<List<Note>> GetAllNotesIncludingDeletedAsync()
    {
        return await _context.Notes
            .IgnoreQueryFilters()
            .ToListAsync();
    }

    public async Task MarkAsSyncedAsync(Guid id, DateTime? syncTime = null)
    {
        var note = await _context.Notes
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(n => n.Id == id);

        if (note != null)
        {
            note.SyncStatus = SyncStatus.Synced;
            await _context.SaveChangesAsync();
        }
    }
}