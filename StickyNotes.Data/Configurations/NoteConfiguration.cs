using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StickyNotes.Core.Models;

namespace StickyNotes.Data.Configurations;

public class NoteConfiguration : IEntityTypeConfiguration<Note>
{
    public void Configure(EntityTypeBuilder<Note> builder)
    {
        builder.ToTable("Notes");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.Title)
               .HasMaxLength(200)
               .IsRequired(false);

        builder.Property(n => n.Content)
               .IsRequired();

        builder.Property(n => n.Color)
               .HasConversion<string>()
               .HasMaxLength(20)
               .IsRequired();

        builder.Property(n => n.PositionX)
               .IsRequired();

        builder.Property(n => n.PositionY)
               .IsRequired();

        builder.Property(n => n.Width)
               .HasDefaultValue(300.0);

        builder.Property(n => n.Height)
               .HasDefaultValue(260.0);

        builder.Property(n => n.Monitor)
               .HasDefaultValue(0);

        builder.Property(n => n.IsAlwaysOnTop)
               .HasDefaultValue(false);

        builder.Property(n => n.CreatedAt)
               .IsRequired();

        builder.Property(n => n.UpdatedAt)
               .IsRequired();

        builder.Property(n => n.DeletedAt)
               .IsRequired(false);

        builder.Property(n => n.DeviceId)
               .HasMaxLength(100)
               .IsRequired();

        builder.Property(n => n.SyncStatus)
               .HasConversion<int>()
               .IsRequired();

        // Índices para optimizar consultas de inicio y sincronización
        builder.HasIndex(n => n.DeletedAt);
        builder.HasIndex(n => n.SyncStatus);
        builder.HasIndex(n => n.UpdatedAt);

        // Filtro global: por defecto excluye notas eliminadas (soft-delete)
        builder.HasQueryFilter(n => n.DeletedAt == null);
    }
}