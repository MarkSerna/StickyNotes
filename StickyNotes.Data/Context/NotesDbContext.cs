using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StickyNotes.Core.Models;
using StickyNotes.Data.Configurations;

namespace StickyNotes.Data.Context;

public class NotesDbContext : DbContext
{
    public DbSet<Note> Notes => Set<Note>();

    public NotesDbContext(DbContextOptions<NotesDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Aplica configuraciones Fluent API
        modelBuilder.ApplyConfiguration(new NoteConfiguration());
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Actualiza automáticamente UpdatedAt antes de persistir
        foreach (var entry in ChangeTracker.Entries<Note>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}