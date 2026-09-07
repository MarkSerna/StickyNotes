import React, { useState } from 'react';
import { FileCode, Copy, Check, Terminal, Database, ShieldAlert } from 'lucide-react';

interface CodeFile {
  path: string;
  project: string;
  language: string;
  description: string;
  content: string;
}

export const GENERATED_STEP2_FILES: CodeFile[] = [
  {
    path: 'StickyNotes.Core/Enums/NoteColor.cs',
    project: 'StickyNotes.Core',
    language: 'csharp',
    description: 'Enum con los 6 colores oficiales de Notas Rápidas de Windows 11',
    content: `namespace StickyNotes.Core.Enums;

/// <summary>
/// Colores predefinidos que replican los colores nativos de Notas rápidas de Microsoft.
/// </summary>
public enum NoteColor
{
    Yellow = 0,
    Green = 1,
    Pink = 2,
    Purple = 3,
    Blue = 4,
    Gray = 5
}`
  },
  {
    path: 'StickyNotes.Core/Enums/SyncStatus.cs',
    project: 'StickyNotes.Core',
    language: 'csharp',
    description: 'Estado del ciclo de vida de sincronización con Google Drive',
    content: `namespace StickyNotes.Core.Enums;

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
}`
  },
  {
    path: 'StickyNotes.Core/Models/Note.cs',
    project: 'StickyNotes.Core',
    language: 'csharp',
    description: 'Entidad de dominio Note con título, soporte multi-monitor y borrado suave',
    content: `using System;
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

            var firstLine = Content.Split(new[] { '\\r', '\\n' }, StringSplitOptions.RemoveEmptyEntries);
            return firstLine.Length > 0 ? (firstLine[0].Length > 30 ? firstLine[0][..30] + "..." : firstLine[0]) : "Nota sin título";
        }
    }
}`
  },
  {
    path: 'StickyNotes.Data/Context/NotesDbContext.cs',
    project: 'StickyNotes.Data',
    language: 'csharp',
    description: 'DbContext de EF Core con filtro global de borrado suave y soporte de auditoría',
    content: `using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StickyNotes.Core.Models;
using StickyNotes.Data.Configurations;

namespace StickyNotes.Data.Context;

public class NotesDbContext : DbContext
{
    public DbSet<Note> Notes => Set<Note>();

    public NotesDbContext()
    {
    }

    public NotesDbContext(DbContextOptions<NotesDbContext> options) : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            // Ruta estándar en Windows: %LocalAppData%\\NotasRapidas\\notes.db
            var appDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "NotasRapidas"
            );

            if (!Directory.Exists(appDataFolder))
            {
                Directory.CreateDirectory(appDataFolder);
            }

            var dbPath = Path.Combine(appDataFolder, "notes.db");
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }
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
}`
  },
  {
    path: 'StickyNotes.Data/Configurations/NoteConfiguration.cs',
    project: 'StickyNotes.Data',
    language: 'csharp',
    description: 'Configuración Fluent API: Índices, filtro global DeletedAt y mapeo de enums',
    content: `using Microsoft.EntityFrameworkCore;
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
}`
  },
  {
    path: 'StickyNotes.Data/Migrations/20260906_InitialCreate.cs',
    project: 'StickyNotes.Data',
    language: 'csharp',
    description: 'Migración inicial de EF Core que crea la tabla Notes con índices',
    content: `using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StickyNotes.Data.Migrations;

/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Notes",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                Content = table.Column<string>(type: "TEXT", nullable: false),
                Color = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                PositionX = table.Column<double>(type: "REAL", nullable: false),
                PositionY = table.Column<double>(type: "REAL", nullable: false),
                Width = table.Column<double>(type: "REAL", nullable: false, defaultValue: 300.0),
                Height = table.Column<double>(type: "REAL", nullable: false, defaultValue: 260.0),
                Monitor = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                IsAlwaysOnTop = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                DeviceId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                SyncStatus = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Notes", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Notes_DeletedAt",
            table: "Notes",
            column: "DeletedAt");

        migrationBuilder.CreateIndex(
            name: "IX_Notes_SyncStatus",
            table: "Notes",
            column: "SyncStatus");

        migrationBuilder.CreateIndex(
            name: "IX_Notes_UpdatedAt",
            table: "Notes",
            column: "UpdatedAt");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Notes");
    }
}`
  },
  {
    path: 'StickyNotes.Data/Repositories/SqliteNoteRepository.cs',
    project: 'StickyNotes.Data',
    language: 'csharp',
    description: 'Repositorio con operaciones CRUD, papelera recuperable y consultas para sync',
    content: `using System;
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
    Task MarkAsSyncedAsync(Guid id, DateTime syncTime);
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

    public async Task MarkAsSyncedAsync(Guid id, DateTime syncTime)
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
}`
  }
];

export const GENERATED_STEP3_FILES: CodeFile[] = [
  {
    path: 'StickyNotes.App/Views/NoteWindow.xaml',
    project: 'StickyNotes.App',
    language: 'xml',
    description: 'XAML de la ventana de nota: sin bordes estándar, cabecera Fluent con título editable y RichEditBox',
    content: `<Window
    x:Class="StickyNotes.App.Views.NoteWindow"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
    xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
    xmlns:enums="using:StickyNotes.Core.Enums"
    mc:Ignorable="d"
    Title="Nota Rápida">

    <Grid x:Name="RootGrid" CornerRadius="8" BorderThickness="1">
        <Grid.RowDefinitions>
            <!-- Barra superior personalizada (arrastre, título y herramientas) -->
            <RowDefinition Height="38"/>
            <!-- Área de texto enriquecido (RichEditBox) -->
            <RowDefinition Height="*"/>
            <!-- Barra inferior de formato de texto y estado -->
            <RowDefinition Height="36"/>
        </Grid.RowDefinitions>

        <!-- CABECERA DE LA NOTA -->
        <Grid x:Name="AppTitleBar" Grid.Row="0" Padding="8,4,8,4">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="Auto"/>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>

            <!-- Botón Nueva Nota (+) -->
            <Button x:Name="BtnNewNote"
                    Grid.Column="0"
                    Click="BtnNewNote_Click"
                    ToolTipService.ToolTip="Nueva nota (Ctrl+N)"
                    Background="Transparent"
                    BorderThickness="0"
                    Padding="6,4">
                <FontIcon Glyph="&#xE710;" FontSize="13"/>
            </Button>

            <!-- Título Editable de la Nota -->
            <TextBox x:Name="TxtTitle"
                     Grid.Column="1"
                     PlaceholderText="Título de la nota..."
                     BorderThickness="0"
                     Background="Transparent"
                     FontWeight="SemiBold"
                     FontSize="12"
                     VerticalAlignment="Center"
                     Margin="4,0"
                     TextChanged="TxtTitle_TextChanged"/>

            <!-- Botones de Acción Superiores -->
            <StackPanel Grid.Column="2" Orientation="Horizontal" Spacing="2">
                <!-- Pin Siempre Encima (Always on Top) -->
                <ToggleButton x:Name="BtnPinTop"
                              Click="BtnPinTop_Click"
                              ToolTipService.ToolTip="Mantener siempre encima"
                              Background="Transparent"
                              BorderThickness="0"
                              Padding="6,4">
                    <FontIcon Glyph="&#xE718;" FontSize="12"/>
                </ToggleButton>

                <!-- Menú Selector de Color -->
                <Button x:Name="BtnColorPicker"
                        ToolTipService.ToolTip="Cambiar color de nota"
                        Background="Transparent"
                        BorderThickness="0"
                        Padding="6,4">
                    <FontIcon Glyph="&#xE790;" FontSize="12"/>
                    <Button.Flyout>
                        <MenuFlyout Placement="Bottom">
                            <MenuFlyoutItem Text="Amarillo" Click="ColorItem_Click" Tag="Yellow"/>
                            <MenuFlyoutItem Text="Verde" Click="ColorItem_Click" Tag="Green"/>
                            <MenuFlyoutItem Text="Rosa" Click="ColorItem_Click" Tag="Pink"/>
                            <MenuFlyoutItem Text="Púrpura" Click="ColorItem_Click" Tag="Purple"/>
                            <MenuFlyoutItem Text="Azul" Click="ColorItem_Click" Tag="Blue"/>
                            <MenuFlyoutItem Text="Carbón" Click="ColorItem_Click" Tag="Gray"/>
                        </MenuFlyout>
                    </Button.Flyout>
                </Button>

                <!-- Eliminar Nota -->
                <Button x:Name="BtnDelete"
                        Click="BtnDelete_Click"
                        ToolTipService.ToolTip="Eliminar nota"
                        Background="Transparent"
                        BorderThickness="0"
                        Padding="6,4">
                    <FontIcon Glyph="&#xE74D;" FontSize="12"/>
                </Button>

                <!-- Cerrar Ventana -->
                <Button x:Name="BtnClose"
                        Click="BtnClose_Click"
                        ToolTipService.ToolTip="Cerrar nota (Alt+F4)"
                        Background="Transparent"
                        BorderThickness="0"
                        Padding="6,4">
                    <FontIcon Glyph="&#xE8BB;" FontSize="12"/>
                </Button>
            </StackPanel>
        </Grid>

        <!-- CUERPO PRINCIPAL: EDITOR DE TEXTO ENRIQUECIDO -->
        <RichEditBox x:Name="EditorBox"
                     Grid.Row="1"
                     Margin="8,4,8,4"
                     BorderThickness="0"
                     Background="Transparent"
                     TextWrapping="Wrap"
                     AcceptsReturn="True"
                     FontSize="14"
                     FontFamily="Segoe UI Variable Text, Segoe UI"
                     TextChanged="EditorBox_TextChanged"/>

        <!-- BARRA INFERIOR: FORMATO Y ESTADO DE SYNC -->
        <Grid Grid.Row="2" Padding="8,2" BorderThickness="0,1,0,0" BorderBrush="#15000000">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>

            <!-- Herramientas de formato básico -->
            <StackPanel Grid.Column="0" Orientation="Horizontal" Spacing="2">
                <ToggleButton x:Name="BtnBold" Click="BtnBold_Click" ToolTipService.ToolTip="Negrita (Ctrl+B)" Background="Transparent" BorderThickness="0" Padding="5,3">
                    <FontIcon Glyph="&#xE8DD;" FontSize="12"/>
                </ToggleButton>
                <ToggleButton x:Name="BtnItalic" Click="BtnItalic_Click" ToolTipService.ToolTip="Cursiva (Ctrl+I)" Background="Transparent" BorderThickness="0" Padding="5,3">
                    <FontIcon Glyph="&#xE8DB;" FontSize="12"/>
                </ToggleButton>
                <ToggleButton x:Name="BtnUnderline" Click="BtnUnderline_Click" ToolTipService.ToolTip="Subrayado (Ctrl+U)" Background="Transparent" BorderThickness="0" Padding="5,3">
                    <FontIcon Glyph="&#xE8DC;" FontSize="12"/>
                </ToggleButton>
                <ToggleButton x:Name="BtnStrikethrough" Click="BtnStrikethrough_Click" ToolTipService.ToolTip="Tachado (Ctrl+T)" Background="Transparent" BorderThickness="0" Padding="5,3">
                    <FontIcon Glyph="&#xEDE0;" FontSize="12"/>
                </ToggleButton>
                <Button x:Name="BtnBullets" Click="BtnBullets_Click" ToolTipService.ToolTip="Lista con viñetas" Background="Transparent" BorderThickness="0" Padding="5,3">
                    <FontIcon Glyph="&#xE8FD;" FontSize="12"/>
                </Button>
            </StackPanel>

            <!-- Indicador de Sincronización -->
            <StackPanel Grid.Column="1" Orientation="Horizontal" VerticalAlignment="Center" Spacing="6">
                <FontIcon x:Name="IconSync" Glyph="&#xE753;" FontSize="11" Foreground="#88000000"/>
                <TextBlock x:Name="TxtSyncStatus" Text="Guardado local" FontSize="10" Foreground="#88000000" VerticalAlignment="Center"/>
            </StackPanel>
        </Grid>
    </Grid>
</Window>`
  },
  {
    path: 'StickyNotes.App/Views/NoteWindow.xaml.cs',
    project: 'StickyNotes.App',
    language: 'csharp',
    description: 'Code-behind: Win32 AppWindow, arrastre personalizado, TopMost, formato RTF y debounce de 500ms',
    content: `using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using StickyNotes.App.Helpers;
using StickyNotes.Core.Enums;
using StickyNotes.Core.Models;
using StickyNotes.Data.Repositories;
using Windows.Graphics;
using WinRT.Interop;

namespace StickyNotes.App.Views;

public sealed partial class NoteWindow : Window
{
    private readonly Note _note;
    private readonly INoteRepository _repository;
    private readonly AppWindow _appWindow;
    private readonly OverlappedPresenter _presenter;
    private readonly DispatcherTimer _debounceTimer;

    public Note Note => _note;

    public NoteWindow(Note note, INoteRepository repository)
    {
        InitializeComponent();

        _note = note;
        _repository = repository;

        // Configuración de AppWindow WinUI 3
        var hWnd = WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
        _appWindow = AppWindow.GetFromWindowId(windowId);
        _presenter = (_appWindow.Presenter as OverlappedPresenter)!;

        // Quitar la barra de título estándar de Windows para look idéntico a Notas Rápidas
        _appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
        _appWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
        _appWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

        // Restaurar posición, dimensiones y AlwaysOnTop
        RestoreWindowBounds();

        // Configurar timer de debounce de 500ms para auto-guardado en SQLite
        _debounceTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _debounceTimer.Tick += DebounceTimer_Tick;

        // Cargar datos en los controles
        LoadNoteData();

        // Aplicar paleta de color
        ApplyNoteColor(_note.Color);

        // Guardar coordenadas cuando se mueva o redimensione la ventana
        _appWindow.Changed += AppWindow_Changed;
    }

    private void RestoreWindowBounds()
    {
        _presenter.IsAlwaysOnTop = _note.IsAlwaysOnTop;
        BtnPinTop.IsChecked = _note.IsAlwaysOnTop;

        var x = (int)_note.PositionX;
        var y = (int)_note.PositionY;
        var width = (int)Math.Max(260, _note.Width);
        var height = (int)Math.Max(220, _note.Height);

        _appWindow.MoveAndResize(new RectInt32(x, y, width, height));
    }

    private void LoadNoteData()
    {
        TxtTitle.Text = _note.Title ?? string.Empty;

        // Carga contenido enriquecido RTF si existe, de lo contrario texto plano
        if (!string.IsNullOrEmpty(_note.Content))
        {
            try
            {
                EditorBox.Document.SetText(TextSetOptions.FormatRtf, _note.Content);
            }
            catch
            {
                EditorBox.Document.SetText(TextSetOptions.None, _note.Content);
            }
        }
    }

    private void ApplyNoteColor(NoteColor color)
    {
        var palette = ColorHelper.GetPalette(color);
        RootGrid.Background = palette.BodyBrush;
        RootGrid.BorderBrush = palette.BorderBrush;
        AppTitleBar.Background = palette.HeaderBrush;
    }

    #region Auto-guardado Debounce

    private void TxtTitle_TextChanged(object sender, TextChangedEventArgs e)
    {
        _debounceTimer.Stop();
        _debounceTimer.Start();
    }

    private void EditorBox_TextChanged(object sender, RoutedEventArgs e)
    {
        _debounceTimer.Stop();
        _debounceTimer.Start();
    }

    private async void DebounceTimer_Tick(object? sender, object e)
    {
        _debounceTimer.Stop();

        _note.Title = string.IsNullOrWhiteSpace(TxtTitle.Text) ? null : TxtTitle.Text.Trim();

        // Extrae el contenido en formato RTF para preservar negrita, cursiva, etc.
        EditorBox.Document.GetText(TextGetOptions.FormatRtf, out var rtfContent);
        _note.Content = rtfContent;

        TxtSyncStatus.Text = "Guardando...";
        await _repository.UpdateAsync(_note);

        TxtSyncStatus.Text = _note.SyncStatus == SyncStatus.Synced ? "Sincronizado" : "Guardado local";
    }

    private void AppWindow_Changed(AppWindow sender, AppWindowChangedEventArgs args)
    {
        if (args.DidPositionChange || args.DidSizeChange)
        {
            _note.PositionX = _appWindow.Position.X;
            _note.PositionY = _appWindow.Position.Y;
            _note.Width = _appWindow.Size.Width;
            _note.Height = _appWindow.Size.Height;

            _debounceTimer.Stop();
            _debounceTimer.Start();
        }
    }

    #endregion

    #region Formato de Texto Enriquecido

    private void BtnBold_Click(object sender, RoutedEventArgs e)
    {
        var selection = EditorBox.Document.Selection;
        selection.CharacterFormat.Bold = FormatEffect.Toggle;
    }

    private void BtnItalic_Click(object sender, RoutedEventArgs e)
    {
        var selection = EditorBox.Document.Selection;
        selection.CharacterFormat.Italic = FormatEffect.Toggle;
    }

    private void BtnUnderline_Click(object sender, RoutedEventArgs e)
    {
        var selection = EditorBox.Document.Selection;
        selection.CharacterFormat.Underline = selection.CharacterFormat.Underline == UnderlineType.None 
            ? UnderlineType.Single 
            : UnderlineType.None;
    }

    private void BtnStrikethrough_Click(object sender, RoutedEventArgs e)
    {
        var selection = EditorBox.Document.Selection;
        selection.CharacterFormat.Strikethrough = FormatEffect.Toggle;
    }

    private void BtnBullets_Click(object sender, RoutedEventArgs e)
    {
        var selection = EditorBox.Document.Selection;
        selection.ParagraphFormat.ListType = selection.ParagraphFormat.ListType == MarkerType.Bullet 
            ? MarkerType.None 
            : MarkerType.Bullet;
    }

    #endregion

    #region Acciones de Cabecera

    private async void ColorItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem item && Enum.TryParse<NoteColor>(item.Tag?.ToString(), out var newColor))
        {
            _note.Color = newColor;
            ApplyNoteColor(newColor);
            await _repository.UpdateAsync(_note);
        }
    }

    private async void BtnPinTop_Click(object sender, RoutedEventArgs e)
    {
        _note.IsAlwaysOnTop = BtnPinTop.IsChecked ?? false;
        _presenter.IsAlwaysOnTop = _note.IsAlwaysOnTop;
        await _repository.UpdateAsync(_note);
    }

    private async void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        await _repository.SoftDeleteAsync(_note.Id);
        this.Close();
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }

    private void BtnNewNote_Click(object sender, RoutedEventArgs e)
    {
        // Dispara evento para que AppManager cree y abra una nueva nota junto a esta
        AppManager.Instance.CreateAndOpenNewNote(_note.PositionX + 30, _note.PositionY + 30);
    }

    #endregion
}`
  },
  {
    path: 'StickyNotes.App/Helpers/ColorHelper.cs',
    project: 'StickyNotes.App',
    language: 'csharp',
    description: 'Paleta de colores oficial de Notas Rápidas de Windows 11 (Header, Body y Border Brushes)',
    content: `using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using StickyNotes.Core.Enums;
using Windows.UI;

namespace StickyNotes.App.Helpers;

public record NotePalette(SolidColorBrush HeaderBrush, SolidColorBrush BodyBrush, SolidColorBrush BorderBrush);

public static class ColorHelper
{
    public static NotePalette GetPalette(NoteColor color) => color switch
    {
        NoteColor.Yellow => new NotePalette(
            HeaderBrush: new SolidColorBrush(Color.FromArgb(255, 255, 244, 117)),
            BodyBrush:   new SolidColorBrush(Color.FromArgb(255, 255, 248, 153)),
            BorderBrush: new SolidColorBrush(Color.FromArgb(60, 0, 0, 0))),

        NoteColor.Green => new NotePalette(
            HeaderBrush: new SolidColorBrush(Color.FromArgb(255, 204, 255, 144)),
            BodyBrush:   new SolidColorBrush(Color.FromArgb(255, 226, 255, 179)),
            BorderBrush: new SolidColorBrush(Color.FromArgb(60, 0, 0, 0))),

        NoteColor.Pink => new NotePalette(
            HeaderBrush: new SolidColorBrush(Color.FromArgb(255, 253, 207, 232)),
            BodyBrush:   new SolidColorBrush(Color.FromArgb(255, 255, 223, 239)),
            BorderBrush: new SolidColorBrush(Color.FromArgb(60, 0, 0, 0))),

        NoteColor.Purple => new NotePalette(
            HeaderBrush: new SolidColorBrush(Color.FromArgb(255, 215, 174, 251)),
            BodyBrush:   new SolidColorBrush(Color.FromArgb(255, 228, 199, 255)),
            BorderBrush: new SolidColorBrush(Color.FromArgb(60, 0, 0, 0))),

        NoteColor.Blue => new NotePalette(
            HeaderBrush: new SolidColorBrush(Color.FromArgb(255, 203, 240, 248)),
            BodyBrush:   new SolidColorBrush(Color.FromArgb(255, 225, 247, 252)),
            BorderBrush: new SolidColorBrush(Color.FromArgb(60, 0, 0, 0))),

        NoteColor.Gray => new NotePalette(
            HeaderBrush: new SolidColorBrush(Color.FromArgb(255, 60, 64, 67)),
            BodyBrush:   new SolidColorBrush(Color.FromArgb(255, 40, 42, 45)),
            BorderBrush: new SolidColorBrush(Color.FromArgb(80, 255, 255, 255))),

        _ => GetPalette(NoteColor.Yellow)
    };
}
`
  }
];

export const GENERATED_STEP4_FILES: CodeFile[] = [
  {
    path: 'StickyNotes.App/Views/SideNotesWindow.xaml',
    project: 'StickyNotes.App',
    language: 'xml',
    description: 'XAML del panel lateral: pestaña retraída de 10px en reposo, panel de notas apiladas y editor activo',
    content: `<Window
    x:Class="StickyNotes.App.Views.SideNotesWindow"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
    xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
    xmlns:models="using:StickyNotes.Core.Models"
    mc:Ignorable="d"
    Title="Panel Lateral de Notas">

    <Grid x:Name="RootLayout" Background="Transparent">
        <!-- FRANJA DE REPOSO (PEEKING HANDLE) CUANDO ESTÁ COLAPSADO -->
        <Border x:Name="PeekingHandle"
                Width="12"
                HorizontalAlignment="Right"
                VerticalAlignment="Center"
                Height="120"
                CornerRadius="6,0,0,6"
                Background="#33000000"
                PointerEntered="PeekingHandle_PointerEntered"
                Cursor="Hand"
                ToolTipService.ToolTip="Pasa el cursor o haz clic para abrir Notas">
            <FontIcon Glyph="&#xE76C;" FontSize="10" Foreground="White" HorizontalAlignment="Center"/>
        </Border>

        <!-- CONTENEDOR PRINCIPAL DEL PANEL LATERAL EXPANDIDO -->
        <Grid x:Name="ExpandedPanel"
              Width="400"
              HorizontalAlignment="Right"
              Background="#F3F3F3"
              BorderThickness="1,0,0,0"
              BorderBrush="#D0D0D0"
              PointerEntered="ExpandedPanel_PointerEntered"
              PointerExited="ExpandedPanel_PointerExited">
            <Grid.RowDefinitions>
                <!-- Cabecera global del panel lateral -->
                <RowDefinition Height="46"/>
                <!-- Selector de pestañas apiladas de notas -->
                <RowDefinition Height="88"/>
                <!-- Editor de la nota seleccionada actualmente -->
                <RowDefinition Height="*"/>
            </Grid.RowDefinitions>

            <!-- CABECERA SUPERIOR DEL PANEL -->
            <Grid Grid.Row="0" Background="#E5E5E5" Padding="12,0" BorderThickness="0,0,0,1" BorderBrush="#DCDCDC">
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="Auto"/>
                </Grid.ColumnDefinitions>

                <StackPanel Orientation="Horizontal" VerticalAlignment="Center" Spacing="8">
                    <FontIcon Glyph="&#xE70F;" FontSize="14" Foreground="#005FB8"/>
                    <TextBlock Text="Notas Rápidas" FontWeight="SemiBold" FontSize="13" VerticalAlignment="Center"/>
                </StackPanel>

                <StackPanel Grid.Column="1" Orientation="Horizontal" Spacing="4" VerticalAlignment="Center">
                    <!-- Botón Nueva Nota -->
                    <Button Click="BtnNewNote_Click" ToolTipService.ToolTip="Nueva nota (+)" Background="Transparent" BorderThickness="0" Padding="6">
                        <FontIcon Glyph="&#xE710;" FontSize="12"/>
                    </Button>

                    <!-- Fijar / Desfijar (Pin) para auto-ocultar al retirar el mouse -->
                    <ToggleButton x:Name="BtnPin" Click="BtnPin_Click" ToolTipService.ToolTip="Fijar panel abierto" Background="Transparent" BorderThickness="0" Padding="6">
                        <FontIcon Glyph="&#xE718;" FontSize="12"/>
                    </ToggleButton>

                    <!-- Cambiar borde (Izquierda / Derecha) -->
                    <Button Click="BtnToggleEdge_Click" ToolTipService.ToolTip="Cambiar de lado de pantalla" Background="Transparent" BorderThickness="0" Padding="6">
                        <FontIcon Glyph="&#xE773;" FontSize="12"/>
                    </Button>

                    <!-- Desacoplar todo a ventanas flotantes individuales -->
                    <Button Click="BtnDetachAll_Click" ToolTipService.ToolTip="Cambiar a ventanas flotantes" Background="Transparent" BorderThickness="0" Padding="6">
                        <FontIcon Glyph="&#xE737;" FontSize="12"/>
                    </Button>

                    <!-- Colapsar panel -->
                    <Button Click="BtnCollapse_Click" ToolTipService.ToolTip="Ocultar panel lateral" Background="Transparent" BorderThickness="0" Padding="6">
                        <FontIcon Glyph="&#xE76C;" FontSize="12"/>
                    </Button>
                </StackPanel>
            </Grid>

            <!-- LISTA HORIZONTAL / VERTICAL DE PESTAÑAS APILADAS -->
            <ScrollViewer Grid.Row="1" HorizontalScrollBarVisibility="Auto" VerticalScrollBarVisibility="Disabled" Padding="8,6" Background="#ECECEC">
                <ListView x:Name="NotesTabList"
                          SelectionMode="Single"
                          SelectionChanged="NotesTabList_SelectionChanged"
                          ScrollViewer.HorizontalScrollBarVisibility="Auto">
                    <ListView.ItemsPanel>
                        <ItemsPanelTemplate>
                            <StackPanel Orientation="Horizontal" Spacing="6"/>
                        </ItemsPanelTemplate>
                    </ListView.ItemsPanel>
                    <ListView.ItemTemplate>
                        <DataTemplate x:DataType="models:Note">
                            <Grid Width="110" Height="72" CornerRadius="6" BorderThickness="1" BorderBrush="#20000000" Padding="6" Background="{Binding Color}">
                                <Grid.RowDefinitions>
                                    <RowDefinition Height="Auto"/>
                                    <RowDefinition Height="*"/>
                                </Grid.RowDefinitions>
                                <!-- Título de la pestaña -->
                                <TextBlock Grid.Row="0" Text="{Binding DisplayTitle}" FontWeight="Bold" FontSize="11" TextTrimming="CharacterEllipsis"/>
                                <!-- Extracto del contenido -->
                                <TextBlock Grid.Row="1" Text="{Binding Content}" FontSize="10" Opacity="0.75" TextTrimming="CharacterEllipsis" TextWrapping="Wrap" Margin="0,2,0,0"/>
                            </Grid>
                        </DataTemplate>
                    </ListView.ItemTemplate>
                </ListView>
            </ScrollViewer>

            <!-- ÁREA DE NOTA ACTIVA (CABECERA CON TÍTULO EDITABLE, MENÚ Y EDITOR) -->
            <Grid x:Name="ActiveNoteContainer" Grid.Row="2" Margin="8" CornerRadius="8" BorderThickness="1" BorderBrush="#25000000">
                <Grid.RowDefinitions>
                    <RowDefinition Height="38"/>
                    <RowDefinition Height="*"/>
                    <RowDefinition Height="36"/>
                </Grid.RowDefinitions>

                <!-- Barra de cabecera de la nota seleccionada -->
                <Grid x:Name="NoteHeaderBar" Grid.Row="0" Padding="8,4">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="*"/>
                        <ColumnDefinition Width="Auto"/>
                    </Grid.ColumnDefinitions>

                    <!-- TÍTULO EDITABLE DIRECTO -->
                    <TextBox x:Name="TxtActiveNoteTitle"
                             Grid.Column="0"
                             PlaceholderText="Título de la nota..."
                             Background="Transparent"
                             BorderThickness="0"
                             FontWeight="Bold"
                             FontSize="12"
                             VerticalAlignment="Center"
                             TextChanged="TxtActiveNoteTitle_TextChanged"/>

                    <StackPanel Grid.Column="1" Orientation="Horizontal" Spacing="2">
                        <!-- Menú de la nota (Colores, duplicar, desacoplar) -->
                        <Button x:Name="BtnNoteMenu" Background="Transparent" BorderThickness="0" Padding="5">
                            <FontIcon Glyph="&#xE712;" FontSize="12"/>
                            <Button.Flyout>
                                <MenuFlyout Placement="Bottom">
                                    <MenuFlyoutItem Text="Amarillo" Click="SetColor_Click" Tag="Yellow"/>
                                    <MenuFlyoutItem Text="Verde" Click="SetColor_Click" Tag="Green"/>
                                    <MenuFlyoutItem Text="Rosa" Click="SetColor_Click" Tag="Pink"/>
                                    <MenuFlyoutItem Text="Púrpura" Click="SetColor_Click" Tag="Purple"/>
                                    <MenuFlyoutItem Text="Azul" Click="SetColor_Click" Tag="Blue"/>
                                    <MenuFlyoutItem Text="Carbón" Click="SetColor_Click" Tag="Gray"/>
                                    <MenuFlyoutSeparator/>
                                    <MenuFlyoutItem Text="Abrir como ventana flotante" Click="BtnFloatSingleNote_Click"/>
                                    <MenuFlyoutItem Text="Duplicar nota" Click="BtnDuplicateSingleNote_Click"/>
                                    <MenuFlyoutSeparator/>
                                    <MenuFlyoutItem Text="Eliminar nota" Click="BtnDeleteActiveNote_Click"/>
                                </MenuFlyout>
                            </Button.Flyout>
                        </Button>
                    </StackPanel>
                </Grid>

                <!-- Editor de texto enriquecido -->
                <RichEditBox x:Name="ActiveNoteEditor"
                             Grid.Row="1"
                             Margin="8,4"
                             Background="Transparent"
                             BorderThickness="0"
                             TextWrapping="Wrap"
                             AcceptsReturn="True"
                             FontSize="13"
                             TextChanged="ActiveNoteEditor_TextChanged"/>

                <!-- Barra inferior de formato -->
                <Grid Grid.Row="2" Padding="8,2" BorderThickness="0,1,0,0" BorderBrush="#15000000">
                    <StackPanel Orientation="Horizontal" Spacing="2">
                        <ToggleButton Click="BtnBold_Click" ToolTipService.ToolTip="Negrita (Ctrl+B)" Background="Transparent" BorderThickness="0" Padding="5,3">
                            <FontIcon Glyph="&#xE8DD;" FontSize="11"/>
                        </ToggleButton>
                        <ToggleButton Click="BtnItalic_Click" ToolTipService.ToolTip="Cursiva (Ctrl+I)" Background="Transparent" BorderThickness="0" Padding="5,3">
                            <FontIcon Glyph="&#xE8DB;" FontSize="11"/>
                        </ToggleButton>
                        <ToggleButton Click="BtnUnderline_Click" ToolTipService.ToolTip="Subrayado (Ctrl+U)" Background="Transparent" BorderThickness="0" Padding="5,3">
                            <FontIcon Glyph="&#xE8DC;" FontSize="11"/>
                        </ToggleButton>
                        <Button Click="BtnBullets_Click" ToolTipService.ToolTip="Viñetas" Background="Transparent" BorderThickness="0" Padding="5,3">
                            <FontIcon Glyph="&#xE8FD;" FontSize="11"/>
                        </Button>
                    </StackPanel>
                </Grid>
            </Grid>
        </Grid>
    </Grid>
</Window>`
  },
  {
    path: 'StickyNotes.App/Views/SideNotesWindow.xaml.cs',
    project: 'StickyNotes.App',
    language: 'csharp',
    description: 'Code-behind: Anclaje de monitor Win32, detección de cursor (hover), auto-ocultado y desacople flotante',
    content: `using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using StickyNotes.App.Helpers;
using StickyNotes.Core.Enums;
using StickyNotes.Core.Models;
using StickyNotes.Data.Repositories;
using Windows.Graphics;
using WinRT.Interop;

namespace StickyNotes.App.Views;

public sealed partial class SideNotesWindow : Window
{
    private readonly INoteRepository _repository;
    private readonly AppWindow _appWindow;
    private readonly OverlappedPresenter _presenter;
    private readonly DispatcherTimer _autoHideTimer;
    private readonly DispatcherTimer _debounceTimer;

    private Note? _currentNote;
    private bool _isPinned = false;
    private bool _isRightEdge = true;
    private bool _isExpanded = true;
    private const int ExpandedWidth = 400;
    private const int CollapsedWidth = 14;

    public ObservableCollection<Note> Notes { get; } = new();

    public SideNotesWindow(INoteRepository repository)
    {
        InitializeComponent();
        _repository = repository;

        // Configuración de AppWindow WinUI 3 y Win32
        var hWnd = WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
        _appWindow = AppWindow.GetFromWindowId(windowId);
        _presenter = (_appWindow.Presenter as OverlappedPresenter)!;

        // Panel sin bordes ni barra estándar, siempre en primer plano
        _appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
        _presenter.IsAlwaysOnTop = true;
        _presenter.IsResizable = false;
        _presenter.SetBorderAndTitleBar(false, false);

        // Ajustar al área de trabajo del monitor principal
        PositionToMonitorEdge();

        // Timer de auto-ocultado al retirar el mouse (300ms de gracia)
        _autoHideTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _autoHideTimer.Tick += (s, e) =>
        {
            _autoHideTimer.Stop();
            if (!_isPinned && _isExpanded)
            {
                SetExpanded(false);
            }
        };

        // Timer de debounce de 500ms para persistencia en SQLite
        _debounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _debounceTimer.Tick += DebounceTimer_Tick;

        // Cargar notas desde SQLite
        _ = LoadNotesAsync();
    }

    private void PositionToMonitorEdge()
    {
        var workArea = MonitorHelper.GetPrimaryMonitorWorkArea();
        var width = _isExpanded ? ExpandedWidth : CollapsedWidth;
        var height = workArea.Height;
        var y = workArea.Y;
        var x = _isRightEdge ? (workArea.X + workArea.Width - width) : workArea.X;

        _appWindow.MoveAndResize(new RectInt32(x, y, width, height));
    }

    private void SetExpanded(bool expand)
    {
        _isExpanded = expand;
        ExpandedPanel.Visibility = expand ? Visibility.Visible : Visibility.Collapsed;
        PeekingHandle.Visibility = expand ? Visibility.Collapsed : Visibility.Visible;
        PositionToMonitorEdge();
    }

    private async Task LoadNotesAsync()
    {
        var activeNotes = await _repository.GetActiveNotesAsync();
        Notes.Clear();
        foreach (var n in activeNotes)
        {
            Notes.Add(n);
        }

        NotesTabList.ItemsSource = Notes;
        if (Notes.Any())
        {
            NotesTabList.SelectedIndex = 0;
        }
    }

    private void NotesTabList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (NotesTabList.SelectedItem is Note selectedNote)
        {
            _currentNote = selectedNote;
            TxtActiveNoteTitle.Text = selectedNote.Title ?? string.Empty;

            try
            {
                ActiveNoteEditor.Document.SetText(TextSetOptions.FormatRtf, selectedNote.Content);
            }
            catch
            {
                ActiveNoteEditor.Document.SetText(TextSetOptions.None, selectedNote.Content);
            }

            var palette = ColorHelper.GetPalette(selectedNote.Color);
            ActiveNoteContainer.Background = palette.BodyBrush;
            NoteHeaderBar.Background = palette.HeaderBrush;
        }
    }

    #region Auto-ocultado y Hover

    private void PeekingHandle_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        _autoHideTimer.Stop();
        if (!_isExpanded)
        {
            SetExpanded(true);
        }
    }

    private void ExpandedPanel_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        _autoHideTimer.Stop();
    }

    private void ExpandedPanel_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (!_isPinned)
        {
            _autoHideTimer.Start();
        }
    }

    private void BtnPin_Click(object sender, RoutedEventArgs e)
    {
        _isPinned = BtnPin.IsChecked ?? false;
    }

    private void BtnCollapse_Click(object sender, RoutedEventArgs e)
    {
        SetExpanded(false);
    }

    private void BtnToggleEdge_Click(object sender, RoutedEventArgs e)
    {
        _isRightEdge = !_isRightEdge;
        PositionToMonitorEdge();
    }

    #endregion

    #region Edición y Debounce de Nota

    private void TxtActiveNoteTitle_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_currentNote == null) return;
        _debounceTimer.Stop();
        _debounceTimer.Start();
    }

    private void ActiveNoteEditor_TextChanged(object sender, RoutedEventArgs e)
    {
        if (_currentNote == null) return;
        _debounceTimer.Stop();
        _debounceTimer.Start();
    }

    private async void DebounceTimer_Tick(object? sender, object e)
    {
        _debounceTimer.Stop();
        if (_currentNote == null) return;

        _currentNote.Title = string.IsNullOrWhiteSpace(TxtActiveNoteTitle.Text) ? null : TxtActiveNoteTitle.Text.Trim();
        ActiveNoteEditor.Document.GetText(TextGetOptions.FormatRtf, out var rtf);
        _currentNote.Content = rtf;

        await _repository.UpdateAsync(_currentNote);

        // Refresca la vista de la pestaña seleccionada
        var index = Notes.IndexOf(_currentNote);
        if (index >= 0)
        {
            Notes[index] = _currentNote;
            NotesTabList.SelectedIndex = index;
        }
    }

    #endregion

    #region Opciones de la Nota Activa (Menú)

    private async void SetColor_Click(object sender, RoutedEventArgs e)
    {
        if (_currentNote != null && sender is MenuFlyoutItem item && Enum.TryParse<NoteColor>(item.Tag?.ToString(), out var color))
        {
            _currentNote.Color = color;
            var palette = ColorHelper.GetPalette(color);
            ActiveNoteContainer.Background = palette.BodyBrush;
            NoteHeaderBar.Background = palette.HeaderBrush;
            await _repository.UpdateAsync(_currentNote);
        }
    }

    private async void BtnDeleteActiveNote_Click(object sender, RoutedEventArgs e)
    {
        if (_currentNote == null) return;
        await _repository.SoftDeleteAsync(_currentNote.Id);
        Notes.Remove(_currentNote);
        _currentNote = Notes.FirstOrDefault();
        if (_currentNote != null)
        {
            NotesTabList.SelectedItem = _currentNote;
        }
    }

    private async void BtnDuplicateSingleNote_Click(object sender, RoutedEventArgs e)
    {
        if (_currentNote == null) return;
        var duplicate = new Note
        {
            Title = _currentNote.Title != null ? $"{_currentNote.Title} (copia)" : "Copia de nota",
            Content = _currentNote.Content,
            Color = _currentNote.Color,
            PositionX = _currentNote.PositionX + 30,
            PositionY = _currentNote.PositionY + 30
        };

        var created = await _repository.CreateAsync(duplicate);
        Notes.Insert(0, created);
        NotesTabList.SelectedIndex = 0;
    }

    private void BtnFloatSingleNote_Click(object sender, RoutedEventArgs e)
    {
        if (_currentNote == null) return;
        AppManager.Instance.OpenNoteAsFloating(_currentNote);
    }

    private void BtnDetachAll_Click(object sender, RoutedEventArgs e)
    {
        AppManager.Instance.SwitchToFloatingMode();
        this.Close();
    }

    private async void BtnNewNote_Click(object sender, RoutedEventArgs e)
    {
        var newNote = new Note
        {
            Title = "Nueva nota",
            Content = string.Empty,
            Color = NoteColor.Yellow
        };

        var created = await _repository.CreateAsync(newNote);
        Notes.Insert(0, created);
        NotesTabList.SelectedIndex = 0;
    }

    #endregion

    #region Formato Rápido

    private void BtnBold_Click(object sender, RoutedEventArgs e) =>
        ActiveNoteEditor.Document.Selection.CharacterFormat.Bold = FormatEffect.Toggle;

    private void BtnItalic_Click(object sender, RoutedEventArgs e) =>
        ActiveNoteEditor.Document.Selection.CharacterFormat.Italic = FormatEffect.Toggle;

    private void BtnUnderline_Click(object sender, RoutedEventArgs e) =>
        ActiveNoteEditor.Document.Selection.CharacterFormat.Underline =
            ActiveNoteEditor.Document.Selection.CharacterFormat.Underline == UnderlineType.None ? UnderlineType.Single : UnderlineType.None;

    private void BtnBullets_Click(object sender, RoutedEventArgs e) =>
        ActiveNoteEditor.Document.Selection.ParagraphFormat.ListType =
            ActiveNoteEditor.Document.Selection.ParagraphFormat.ListType == MarkerType.Bullet ? MarkerType.None : MarkerType.Bullet;

    #endregion
}`
  },
  {
    path: 'StickyNotes.App/Helpers/MonitorHelper.cs',
    project: 'StickyNotes.App',
    language: 'csharp',
    description: 'P/Invoke Win32 para calcular el área de trabajo útil del monitor (excluyendo barra de tareas)',
    content: `using System;
using System.Runtime.InteropServices;
using Windows.Graphics;

namespace StickyNotes.App.Helpers;

public static class MonitorHelper
{
    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    private const uint MONITOR_DEFAULTTOPRIMARY = 1;

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    public static RectInt32 GetPrimaryMonitorWorkArea()
    {
        var mi = new MONITORINFO();
        mi.cbSize = Marshal.SizeOf(mi);

        var hMonitor = MonitorFromWindow(IntPtr.Zero, MONITOR_DEFAULTTOPRIMARY);
        if (GetMonitorInfo(hMonitor, ref mi))
        {
            return new RectInt32(
                mi.rcWork.Left,
                mi.rcWork.Top,
                mi.rcWork.Right - mi.rcWork.Left,
                mi.rcWork.Bottom - mi.rcWork.Top
            );
        }

        // Fallback estándar en caso de fallo Win32 (Full HD típico)
        return new RectInt32(0, 0, 1920, 1040);
    }
}
`
  }
];

export const GENERATED_STEP5_FILES: CodeFile[] = [
  {
    path: 'StickyNotes.Sync/Services/GoogleDriveSyncService.cs',
    project: 'StickyNotes.Sync',
    language: 'csharp',
    description: 'Servicio de sincronización bidireccional con Google Drive (appDataFolder, OAuth 2.0 y Last-Write-Wins)',
    content: `using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;
using StickyNotes.Core.Enums;
using StickyNotes.Core.Models;
using StickyNotes.Data.Repositories;
using StickyNotes.Sync.Models;
using StickyNotes.Sync.Security;

namespace StickyNotes.Sync.Services;

public record SyncReport(int Uploaded, int Downloaded, int Deleted, bool IsSuccess, string? ErrorMessage = null);

public class GoogleDriveSyncService
{
    private readonly INoteRepository _noteRepository;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private DriveService? _driveService;
    private readonly SemaphoreSlim _syncLock = new(1, 1);

    public bool IsAuthenticated => _driveService != null;

    public GoogleDriveSyncService(INoteRepository noteRepository, string clientId, string clientSecret)
    {
        _noteRepository = noteRepository;
        _clientId = clientId;
        _clientSecret = clientSecret;
    }

    /// <summary>
    /// Inicia el flujo OAuth 2.0 InstalledAppFlow y persiste tokens cifrados en DPAPI.
    /// Solo solicita acceso a drive.appdata (carpeta oculta sin acceso a archivos personales).
    /// </summary>
    public async Task<bool> AuthenticateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var secrets = new ClientSecrets
            {
                ClientId = _clientId,
                ClientSecret = _clientSecret
            };

            // Almacén cifrado con Windows DPAPI (CurrentUser)
            var dataStore = new WindowsCredentialDataStore("StickyNotesApp");

            var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                secrets,
                new[] { DriveService.ScopeConstants.DriveAppdata },
                "user",
                cancellationToken,
                dataStore);

            _driveService = new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "StickyNotes Windows 11"
            });

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Ejecuta la sincronización bidireccional completa resolviendo conflictos por Last-Write-Wins (UTC).
    /// </summary>
    public async Task<SyncReport> SyncAllAsync(CancellationToken cancellationToken = default)
    {
        if (_driveService == null)
        {
            var ok = await AuthenticateAsync(cancellationToken);
            if (!ok || _driveService == null)
            {
                return new SyncReport(0, 0, 0, false, "Usuario no autenticado en Google Drive.");
            }
        }

        await _syncLock.WaitAsync(cancellationToken);
        try
        {
            int uploaded = 0;
            int downloaded = 0;
            int deleted = 0;

            // 1. Obtener archivos remotos de la carpeta oculta appDataFolder
            var remoteFiles = await ListRemoteNotesAsync(cancellationToken);
            var localNotes = await _noteRepository.GetAllNotesIncludingDeletedAsync();
            var localNotesMap = localNotes.ToDictionary(n => n.Id);

            // 2. Procesar notas remotas hacia local
            foreach (var remoteFile in remoteFiles)
            {
                if (cancellationToken.IsCancellationRequested) break;

                // Extraer el NoteId guardado en appProperties
                if (!remoteFile.AppProperties.TryGetValue("noteId", out var noteIdStr) || !Guid.TryParse(noteIdStr, out var noteId))
                {
                    continue;
                }

                var remotePayload = await DownloadNotePayloadAsync(remoteFile.Id, cancellationToken);
                if (remotePayload == null) continue;

                if (!localNotesMap.TryGetValue(noteId, out var localNote))
                {
                    // Nota creada remotamente en otro equipo -> Descargar a SQLite local
                    if (!remotePayload.IsDeleted)
                    {
                        var newLocal = remotePayload.ToDomainModel();
                        newLocal.SyncStatus = SyncStatus.Synced;
                        await _noteRepository.CreateAsync(newLocal);
                        downloaded++;
                    }
                }
                else
                {
                    // Ambas existen: Resolver por Last-Write-Wins (UTC)
                    if (remotePayload.UpdatedAt > localNote.UpdatedAt)
                    {
                        if (remotePayload.IsDeleted && localNote.DeletedAt == null)
                        {
                            await _noteRepository.SoftDeleteAsync(localNote.Id);
                            deleted++;
                        }
                        else if (!remotePayload.IsDeleted)
                        {
                            remotePayload.ApplyTo(localNote);
                            localNote.SyncStatus = SyncStatus.Synced;
                            await _noteRepository.UpdateAsync(localNote);
                            downloaded++;
                        }
                    }
                }
            }

            // 3. Procesar notas locales pendientes de subida a Google Drive
            var pendingLocal = await _noteRepository.GetPendingSyncNotesAsync();
            foreach (var local in pendingLocal)
            {
                if (cancellationToken.IsCancellationRequested) break;

                var existingRemote = remoteFiles.FirstOrDefault(f => 
                    f.AppProperties.TryGetValue("noteId", out var idStr) && idStr == local.Id.ToString());

                var payload = DriveNotePayload.FromDomainModel(local);

                if (local.DeletedAt != null)
                {
                    // Si fue borrada localmente y existe en Drive, eliminar o marcar borrada
                    if (existingRemote != null)
                    {
                        await _driveService.Files.Delete(existingRemote.Id).ExecuteAsync(cancellationToken);
                        deleted++;
                    }
                }
                else
                {
                    // Subir o actualizar en appDataFolder
                    if (existingRemote != null)
                    {
                        await UpdateRemoteFileAsync(existingRemote.Id, payload, cancellationToken);
                    }
                    else
                    {
                        await CreateRemoteFileAsync(payload, cancellationToken);
                    }
                    uploaded++;
                }

                await _noteRepository.MarkAsSyncedAsync(local.Id);
            }

            return new SyncReport(uploaded, downloaded, deleted, true);
        }
        catch (Exception ex)
        {
            return new SyncReport(0, 0, 0, false, ex.Message);
        }
        finally
        {
            _syncLock.Release();
        }
    }

    #region Operaciones REST Drive API

    private async Task<List<Google.Apis.Drive.v3.Data.File>> ListRemoteNotesAsync(CancellationToken ct)
    {
        var request = _driveService!.Files.List();
        request.Spaces = "appDataFolder";
        request.Fields = "files(id, name, modifiedTime, appProperties)";
        request.Q = "'appDataFolder' in parents and trashed = false";

        var result = await request.ExecuteAsync(ct);
        return result.Files?.ToList() ?? new List<Google.Apis.Drive.v3.Data.File>();
    }

    private async Task<DriveNotePayload?> DownloadNotePayloadAsync(string fileId, CancellationToken ct)
    {
        var request = _driveService!.Files.Get(fileId);
        using var stream = new MemoryStream();
        await request.DownloadAsync(stream, ct);
        stream.Position = 0;

        return await JsonSerializer.DeserializeAsync<DriveNotePayload>(stream, cancellationToken: ct);
    }

    private async Task CreateRemoteFileAsync(DriveNotePayload payload, CancellationToken ct)
    {
        var fileMetadata = new Google.Apis.Drive.v3.Data.File
        {
            Name = $"note_{payload.Id}.json",
            Parents = new List<string> { "appDataFolder" },
            AppProperties = new Dictionary<string, string>
            {
                { "noteId", payload.Id.ToString() }
            }
        };

        var json = JsonSerializer.Serialize(payload);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        var request = _driveService!.Files.Create(fileMetadata, stream, "application/json");
        await request.UploadAsync(ct);
    }

    private async Task UpdateRemoteFileAsync(string fileId, DriveNotePayload payload, CancellationToken ct)
    {
        var fileMetadata = new Google.Apis.Drive.v3.Data.File();
        var json = JsonSerializer.Serialize(payload);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        var request = _driveService!.Files.Update(fileMetadata, fileId, stream, "application/json");
        await request.UploadAsync(ct);
    }

    #endregion
}`
  },
  {
    path: 'StickyNotes.Sync/Security/WindowsCredentialDataStore.cs',
    project: 'StickyNotes.Sync',
    language: 'csharp',
    description: 'Implementación de IDataStore con DPAPI de Windows (ProtectedData.Protect con DataProtectionScope.CurrentUser)',
    content: `using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Google.Apis.Util.Store;

namespace StickyNotes.Sync.Security;

/// <summary>
/// Almacén seguro para tokens OAuth 2.0 que utiliza la Data Protection API (DPAPI) de Windows.
/// Los tokens se cifran con la clave vinculada a la cuenta del usuario de Windows actual.
/// </summary>
public class WindowsCredentialDataStore : IDataStore
{
    private readonly string _storageFolder;

    public WindowsCredentialDataStore(string appFolderName)
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _storageFolder = Path.Combine(localAppData, appFolderName, "Tokens");
        Directory.CreateDirectory(_storageFolder);
    }

    public Task StoreAsync<T>(string key, T value)
    {
        if (string.IsNullOrEmpty(key)) throw new ArgumentException("Key cannot be null or empty", nameof(key));

        var filePath = GetFilePath(key);
        var json = JsonSerializer.Serialize(value);
        var plainBytes = Encoding.UTF8.GetBytes(json);

        // Cifrado simétrico transparente protegido por la cuenta de Windows
        var encryptedBytes = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(filePath, encryptedBytes);

        return Task.CompletedTask;
    }

    public Task<T?> GetAsync<T>(string key)
    {
        if (string.IsNullOrEmpty(key)) throw new ArgumentException("Key cannot be null or empty", nameof(key));

        var filePath = GetFilePath(key);
        if (!File.Exists(filePath))
        {
            return Task.FromResult<T?>(default);
        }

        try
        {
            var encryptedBytes = File.ReadAllBytes(filePath);
            var decryptedBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);
            var json = Encoding.UTF8.GetString(decryptedBytes);
            var value = JsonSerializer.Deserialize<T>(json);
            return Task.FromResult(value);
        }
        catch (CryptographicException)
        {
            // Clave cambiada o archivo corrupto -> invalidar credencial
            File.Delete(filePath);
            return Task.FromResult<T?>(default);
        }
    }

    public Task DeleteAsync<T>(string key)
    {
        if (string.IsNullOrEmpty(key)) throw new ArgumentException("Key cannot be null or empty", nameof(key));

        var filePath = GetFilePath(key);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        return Task.CompletedTask;
    }

    public Task ClearAsync()
    {
        if (Directory.Exists(_storageFolder))
        {
            var files = Directory.GetFiles(_storageFolder);
            foreach (var file in files)
            {
                File.Delete(file);
            }
        }

        return Task.CompletedTask;
    }

    private string GetFilePath(string key)
    {
        // Sanitizar el nombre de archivo
        var safeKey = Convert.ToBase64String(Encoding.UTF8.GetBytes(key))
            .Replace('/', '_')
            .Replace('+', '-');
        return Path.Combine(_storageFolder, $"{safeKey}.dat");
    }
}`
  },
  {
    path: 'StickyNotes.Sync/Models/DriveNotePayload.cs',
    project: 'StickyNotes.Sync',
    language: 'csharp',
    description: 'DTO serializable a JSON para almacenamiento en Google Drive appDataFolder',
    content: `using System;
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
}`
  },
  {
    path: 'StickyNotes.Sync/Services/SyncScheduler.cs',
    project: 'StickyNotes.Sync',
    language: 'csharp',
    description: 'Planificador de sincronización en segundo plano con timer de 5 min y disparo por eventos inmediatos',
    content: `using System;
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
`
  }
];

export const GENERATED_STEP6_FILES: CodeFile[] = [
  {
    path: 'StickyNotes.App/Services/TrayIconService.cs',
    project: 'StickyNotes.App',
    language: 'csharp',
    description: 'Icono en la bandeja del sistema (System Tray) con menú contextual WinUI 3 nativo y notificaciones',
    content: `using System;
using System.Threading.Tasks;
using H.NotifyIcon;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using StickyNotes.Sync.Services;

namespace StickyNotes.App.Services;

public class TrayIconService : IDisposable
{
    private TaskbarIcon? _trayIcon;
    private readonly SyncScheduler _syncScheduler;
    private readonly Action _onNewNoteRequested;
    private readonly Action _onToggleSidePanelRequested;
    private readonly Action _onShowAllFloatingRequested;

    public TrayIconService(
        SyncScheduler syncScheduler,
        Action onNewNoteRequested,
        Action onToggleSidePanelRequested,
        Action onShowAllFloatingRequested)
    {
        _syncScheduler = syncScheduler;
        _onNewNoteRequested = onNewNoteRequested;
        _onToggleSidePanelRequested = onToggleSidePanelRequested;
        _onShowAllFloatingRequested = onShowAllFloatingRequested;

        InitializeTrayIcon();
    }

    private void InitializeTrayIcon()
    {
        _trayIcon = new TaskbarIcon
        {
            ToolTipText = "Notas Rápidas (Win+Alt+N)"
        };

        // Asignar icono de la aplicación (Assets/TrayIcon.ico)
        _trayIcon.IconSource = new BitmapImage(new Uri("ms-appx:///Assets/TrayIcon.ico"));

        // Doble clic abre nueva nota
        _trayIcon.TrayMouseDoubleClick += (s, e) => _onNewNoteRequested();

        // Construir Menú Contextual WinUI 3
        var menu = new MenuFlyout();

        var itemNew = new MenuFlyoutItem { Text = "Nueva nota (Win+Alt+N)" };
        itemNew.Click += (s, e) => _onNewNoteRequested();
        menu.Items.Add(itemNew);

        var itemSide = new MenuFlyoutItem { Text = "Alternar panel lateral (SideNotes)" };
        itemSide.Click += (s, e) => _onToggleSidePanelRequested();
        menu.Items.Add(itemSide);

        var itemFloating = new MenuFlyoutItem { Text = "Mostrar todas las notas flotantes" };
        itemFloating.Click += (s, e) => _onShowAllFloatingRequested();
        menu.Items.Add(itemFloating);

        menu.Items.Add(new MenuFlyoutSeparator());

        var itemSync = new MenuFlyoutItem { Text = "Sincronizar ahora con Google Drive" };
        itemSync.Click += async (s, e) =>
        {
            _trayIcon.ShowNotification("Notas Rápidas", "Sincronizando notas con Google Drive...");
            await _syncScheduler.RequestImmediateSyncAsync();
        };
        menu.Items.Add(itemSync);

        var itemStartup = new ToggleMenuFlyoutItem
        {
            Text = "Iniciar con Windows",
            IsChecked = StartupService.IsRunAtStartupEnabled()
        };
        itemStartup.Click += (s, e) =>
        {
            StartupService.SetRunAtStartup(itemStartup.IsChecked);
        };
        menu.Items.Add(itemStartup);

        menu.Items.Add(new MenuFlyoutSeparator());

        var itemExit = new MenuFlyoutItem { Text = "Salir de Notas Rápidas" };
        itemExit.Click += (s, e) =>
        {
            _trayIcon.Dispose();
            Application.Current.Exit();
        };
        menu.Items.Add(itemExit);

        _trayIcon.ContextFlyout = menu;

        // Escuchar reportes de sincronización para mostrar notificaciones tipo Toast
        _syncScheduler.SyncCompleted += OnSyncCompleted;
    }

    private void OnSyncCompleted(SyncReport report)
    {
        if (_trayIcon == null) return;

        if (report.IsSuccess && (report.Uploaded > 0 || report.Downloaded > 0 || report.Deleted > 0))
        {
            var msg = $"Sincronización completa: {report.Uploaded} subidas, {report.Downloaded} descargadas.";
            _trayIcon.ShowNotification("Google Drive Sync", msg);
        }
        else if (!report.IsSuccess && !string.IsNullOrEmpty(report.ErrorMessage))
        {
            _trayIcon.ShowNotification("Error de Sincronización", report.ErrorMessage);
        }
    }

    public void Dispose()
    {
        _syncScheduler.SyncCompleted -= OnSyncCompleted;
        _trayIcon?.Dispose();
    }
}`
  },
  {
    path: 'StickyNotes.App/Services/GlobalHotkeyService.cs',
    project: 'StickyNotes.App',
    language: 'csharp',
    description: 'Hook global de teclado Win32 para Win+Alt+N sin necesidad de foco en la ventana',
    content: `using System;
using System.Runtime.InteropServices;
using Microsoft.UI.Dispatching;

namespace StickyNotes.App.Services;

public class GlobalHotkeyService : IDisposable
{
    private const int HOTKEY_ID = 9001;
    private const uint MOD_ALT = 0x0001;
    private const uint MOD_WIN = 0x0008;
    private const uint MOD_NOREPEAT = 0x4000;
    private const uint VK_N = 0x4E; // Tecla 'N'
    private const int WM_HOTKEY = 0x0312;

    private readonly DispatcherQueue _dispatcherQueue;
    private readonly Action _onHotkeyPressed;
    private IntPtr _messageHwnd;
    private WndProcDelegate? _wndProc;

    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr CreateWindowEx(
        uint dwExStyle, string lpClassName, string lpWindowName, uint dwStyle,
        int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr DefWindowProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern ushort RegisterClass(ref WNDCLASS lpWndClass);

    [StructLayout(LayoutKind.Sequential)]
    private struct WNDCLASS
    {
        public uint style;
        public IntPtr lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public IntPtr hInstance;
        public IntPtr hIcon;
        public IntPtr hCursor;
        public IntPtr hbrBackground;
        public string? lpszMenuName;
        public string lpszClassName;
    }

    public GlobalHotkeyService(DispatcherQueue dispatcherQueue, Action onHotkeyPressed)
    {
        _dispatcherQueue = dispatcherQueue;
        _onHotkeyPressed = onHotkeyPressed;

        InitializeMessageWindow();
        RegisterWinAltN();
    }

    private void InitializeMessageWindow()
    {
        _wndProc = CustomWndProc;
        var className = $"StickyNotes_HotkeyMsg_{Guid.NewGuid():N}";

        var wndClass = new WNDCLASS
        {
            lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wndProc),
            lpszClassName = className
        };

        RegisterClass(ref wndClass);

        // Ventana invisible exclusiva para bombeo de mensajes de Windows
        _messageHwnd = CreateWindowEx(
            0, className, "StickyNotesHotkeyListener", 0,
            0, 0, 0, 0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
    }

    private void RegisterWinAltN()
    {
        // Registrar combinación Win + Alt + N (con MOD_NOREPEAT para evitar ráfagas al mantener pulsado)
        var success = RegisterHotKey(_messageHwnd, HOTKEY_ID, MOD_WIN | MOD_ALT | MOD_NOREPEAT, VK_N);
        if (!success)
        {
            // Fallback secundario si otra app ocupa Win+Alt+N: Ctrl+Alt+N
            const uint MOD_CONTROL = 0x0002;
            RegisterHotKey(_messageHwnd, HOTKEY_ID, MOD_CONTROL | MOD_ALT | MOD_NOREPEAT, VK_N);
        }
    }

    private IntPtr CustomWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
        {
            // Enrutar la acción de forma segura al hilo principal de UI de WinUI 3
            _dispatcherQueue.TryEnqueue(() => _onHotkeyPressed());
            return IntPtr.Zero;
        }

        return DefWindowProc(hWnd, msg, wParam, lParam);
    }

    public void Dispose()
    {
        if (_messageHwnd != IntPtr.Zero)
        {
            UnregisterHotKey(_messageHwnd, HOTKEY_ID);
            DestroyWindow(_messageHwnd);
            _messageHwnd = IntPtr.Zero;
        }
    }
}`
  },
  {
    path: 'StickyNotes.App/Services/StartupService.cs',
    project: 'StickyNotes.App',
    language: 'csharp',
    description: 'Gestión del inicio automático de sesión en Windows mediante el Registro HKCU',
    content: `using System;
using System.Diagnostics;
using Microsoft.Win32;

namespace StickyNotes.App.Services;

public static class StartupService
{
    private const string RUN_KEY_PATH = @"Software\\Microsoft\\Windows\\CurrentVersion\\Run";
    private const string APP_NAME = "StickyNotesWindows11";

    /// <summary>
    /// Verifica si la aplicación está configurada para iniciar con Windows.
    /// </summary>
    public static bool IsRunAtStartupEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RUN_KEY_PATH, false);
            return key?.GetValue(APP_NAME) != null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Habilita o deshabilita el arranque automático con Windows 11.
    /// </summary>
    public static void SetRunAtStartup(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RUN_KEY_PATH, true);
            if (key == null) return;

            if (enable)
            {
                var exePath = Process.GetCurrentProcess().MainModule?.FileName;
                if (!string.IsNullOrEmpty(exePath))
                {
                    // Añadir argumento --minimized para iniciar silencioso en la bandeja
                    key.SetValue(APP_NAME, $"\"{exePath}\" --minimized");
                }
            }
            else
            {
                key.DeleteValue(APP_NAME, false);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error al modificar el registro de inicio: {ex.Message}");
        }
    }
}`
  }
];

export const GENERATED_STEP7_FILES: CodeFile[] = [
  {
    path: 'StickyNotes.App/appsettings.json',
    project: 'StickyNotes.App',
    language: 'json',
    description: 'Archivo de configuración con credenciales OAuth 2.0 y ajustes de sincronización',
    content: `{
  "Database": {
    "FileName": "stickynotes.db"
  },
  "GoogleDrive": {
    "ClientId": "TU_CLIENT_ID.apps.googleusercontent.com",
    "ClientSecret": "GOCSPX-TU_CLIENT_SECRET",
    "ApplicationName": "StickyNotes Windows 11",
    "Scopes": [
      "https://www.googleapis.com/auth/drive.appdata"
    ]
  },
  "SyncSettings": {
    "AutoSyncIntervalMinutes": 5,
    "DebounceSaveMilliseconds": 500,
    "SyncOnStartup": true
  },
  "Appearance": {
    "DefaultTheme": "System",
    "BackdropType": "MicaAlt",
    "DefaultNoteColor": "Yellow"
  }
}`
  },
  {
    path: 'StickyNotes.App/Program.cs',
    project: 'StickyNotes.App',
    language: 'csharp',
    description: 'Punto de entrada de la aplicación WinUI 3 con inyección de dependencias (DI) y ciclo de vida',
    content: `using System;
using System.IO;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using StickyNotes.App.Services;
using StickyNotes.App.ViewModels;
using StickyNotes.App.Views;
using StickyNotes.Data;
using StickyNotes.Data.Repositories;
using StickyNotes.Sync.Services;

namespace StickyNotes.App;

public static class Program
{
    private static Mutex? _singleInstanceMutex;

    [STAThread]
    public static void Main(string[] args)
    {
        // 1. Evitar múltiples instancias simultáneas mediante un Mutex con nombre
        const string mutexName = "Global\\\\StickyNotes_Win11_SingleInstanceMutex";
        _singleInstanceMutex = new Mutex(true, mutexName, out bool isOnlyInstance);

        if (!isOnlyInstance)
        {
            // Ya hay una instancia corriendo; salir para enfocar la existente
            return;
        }

        // 2. Inicializar arquitectura WinUI 3 y XamlApplication
        WinRT.ComWrappersSupport.InitializeComWrappers();

        Application.Start(p =>
        {
            var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);

            var host = CreateHostBuilder(args).Build();
            host.Start();

            // Arrancar la aplicación WinUI 3
            _ = new App(host.Services);
        });
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.SetBasePath(AppContext.BaseDirectory);
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            })
            .ConfigureServices((context, services) =>
            {
                var configuration = context.Configuration;

                // Directorio seguro en %LOCALAPPDATA%
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var dbFolder = Path.Combine(appData, "StickyNotesApp");
                Directory.CreateDirectory(dbFolder);
                var dbPath = Path.Combine(dbFolder, configuration["Database:FileName"] ?? "stickynotes.db");

                // Configurar DbContext SQLite con WAL
                services.AddDbContext<StickyNotesDbContext>(options =>
                {
                    options.UseSqlite($"Data Source={dbPath}");
                });

                // Repositorios
                services.AddScoped<INoteRepository, NoteRepository>();

                // Servicios de sincronización Google Drive
                var clientId = configuration["GoogleDrive:ClientId"] ?? string.Empty;
                var clientSecret = configuration["GoogleDrive:ClientSecret"] ?? string.Empty;

                services.AddSingleton(sp => new GoogleDriveSyncService(
                    sp.GetRequiredService<INoteRepository>(),
                    clientId,
                    clientSecret));

                services.AddSingleton<SyncScheduler>();

                // ViewModels
                services.AddTransient<SideNotesViewModel>();

                // Servicios del Sistema Operativo
                services.AddSingleton<WindowManager>();
            });
}`
  },
  {
    path: 'SETUP_AND_BUILD_GUIDE.md',
    project: 'Docs & Deployment',
    language: 'markdown',
    description: 'Guía detallada de configuración OAuth 2.0 en Google Cloud Console y compilación en Visual Studio',
    content: `# Guía de Configuración, Google Cloud y Compilación (.NET 8 + WinUI 3)

## 1. Configuración de Google Cloud Console (OAuth 2.0 para Drive)

Para que la aplicación sincronice tus notas en tu propia cuenta de Google Drive sin costes ni necesidad de servidores externos:

### Paso 1.1: Crear Proyecto
1. Entra a [Google Cloud Console](https://console.cloud.google.com/).
2. Haz clic en el selector de proyectos superior y presiona **"Nuevo proyecto"** (*New Project*).
3. Nómbralo \`StickyNotes-App\` y haz clic en **Crear**.

### Paso 1.2: Habilitar la API de Google Drive
1. En el menú lateral izquierdo, ve a **APIs y servicios > Biblioteca** (*APIs & Services > Library*).
2. Busca \`Google Drive API\`.
3. Entra y presiona el botón azul **Habilitar** (*Enable*).

### Paso 1.3: Pantalla de Consentimiento OAuth (OAuth Consent Screen)
1. Ve a **APIs y servicios > Pantalla de consentimiento de OAuth**.
2. Selecciona Tipo de usuario: **Externo** (*External*) y presiona **Crear**.
3. Rellena los datos básicos:
   - **Nombre de la aplicación**: \`Notas Rápidas Windows 11\`
   - **Correo de asistencia del usuario**: tu correo electrónico de Google.
   - **Datos de contacto del desarrollador**: tu correo electrónico.
4. En la pestaña **Permisos (Scopes)**:
   - Presiona **Agregar o quitar permisos**.
   - Añade manualmente el permiso: \`https://www.googleapis.com/auth/drive.appdata\`
   - *(Este permiso solo da acceso a la carpeta oculta de configuración de la app, NO a tus fotos o documentos)*.
5. En la pestaña **Usuarios de prueba (Test Users)**:
   - Añade tu propia dirección de correo de Google (ej: \`marcoesernal@gmail.com\`).
   - *(Al estar en modo "Pruebas", Google permite el inicio de sesión inmediato sin requerir el proceso largo de verificación pública)*.

### Paso 1.4: Crear Credenciales de Escritorio
1. Ve a **APIs y servicios > Credenciales**.
2. Haz clic en **+ Crear credenciales > ID de cliente de OAuth** (*OAuth client ID*).
3. En **Tipo de aplicación**, selecciona: **Aplicación de escritorio** (*Desktop app*).
4. Nombre: \`StickyNotes Desktop Client\`.
5. Presiona **Crear**.
6. Copia el **ID de cliente** (\`ClientId\`) y el **Secreto de cliente** (\`ClientSecret\`).
7. Pégalos en el archivo \`StickyNotes.App/appsettings.json\`.

---

## 2. Requisitos Previos en Windows 11

- **Sistema Operativo:** Windows 11 versión 22H2 (Build 22621) o superior (o Windows 10 20H2+).
- **Entorno:** Visual Studio 2022 (Community, Professional o Enterprise) versión 17.8 o superior.
- **Cargas de trabajo en Visual Studio Installer:**
  1. *Desarrollo para el escritorio con .NET* (.NET Desktop Development).
  2. *Windows App SDK C# Templates* (plantillas de Windows App SDK).
  3. SDK de .NET 8.0.

---

## 3. Comandos de Compilación y Ejecución por Terminal

Abre una terminal PowerShell o Windows Terminal en la raíz de la solución:

\`\`\`powershell
# 1. Restaurar paquetes NuGet
dotnet restore

# 2. Aplicar migraciones iniciales a SQLite (crea stickynotes.db con WAL y PRAGMAs)
dotnet ef database update --project src/StickyNotes.Data --startup-project src/StickyNotes.App

# 3. Compilar la solución en modo Debug
dotnet build

# 4. Ejecutar la aplicación
dotnet run --project src/StickyNotes.App
\`\`\`

---

## 4. Publicación para Distribución (Ejecutable Independiente .exe)

Para generar un ejecutable único sin dependencias que los usuarios puedan descargar y abrir directamente (Unpackaged WinUI 3):

\`\`\`powershell
dotnet publish src/StickyNotes.App/StickyNotes.App.csproj \`
  -c Release \`
  -r win-x64 \`
  --self-contained true \`
  /p:PublishSingleFile=true \`
  /p:WindowsPackageType=None \`
  /p:IncludeNativeLibrariesForSelfExtract=true \`
  -o ./dist
\`\`\`

El archivo \`./dist/StickyNotes.App.exe\` se ejecutará al instante con aceleración por GPU, efectos Mica Alt y soporte nativo para la bandeja de Windows 11.`
  }
];

export const CodeGeneratedView: React.FC = () => {
  const [activeStep, setActiveStep] = useState<2 | 3 | 4 | 5 | 6 | 7>(7);
  const [selectedFileIndex, setSelectedFileIndex] = useState(0);
  const [copied, setCopied] = useState(false);

  let fileList = GENERATED_STEP2_FILES;
  if (activeStep === 3) fileList = GENERATED_STEP3_FILES;
  if (activeStep === 4) fileList = GENERATED_STEP4_FILES;
  if (activeStep === 5) fileList = GENERATED_STEP5_FILES;
  if (activeStep === 6) fileList = GENERATED_STEP6_FILES;
  if (activeStep === 7) fileList = GENERATED_STEP7_FILES;

  const currentFile = fileList[selectedFileIndex] || fileList[0];

  const handleCopy = () => {
    navigator.clipboard.writeText(currentFile.content);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  return (
    <div className="bg-slate-900 border border-slate-800 rounded-xl overflow-hidden shadow-2xl">
      {/* Top Banner with Step Switcher */}
      <div className="bg-slate-800/80 px-4 py-3 border-b border-slate-700/80 flex flex-wrap items-center justify-between gap-3">
        <div className="flex items-center gap-2">
          <Database className="w-4 h-4 text-emerald-400" />
          <span className="text-xs font-bold text-slate-100 uppercase tracking-wider">
            Código C# y XAML Generado (.NET 8 + WinUI 3)
          </span>
        </div>

        {/* Step Selector Tabs */}
        <div className="flex flex-wrap items-center bg-slate-950 p-1 rounded-lg border border-slate-800 text-xs gap-1">
          <button
            onClick={() => {
              setActiveStep(2);
              setSelectedFileIndex(0);
            }}
            className={`px-2.5 py-1 rounded font-medium transition-all ${
              activeStep === 2 ? 'bg-blue-600 text-white' : 'text-slate-400 hover:text-slate-200'
            }`}
          >
            Paso 2: SQLite
          </button>
          <button
            onClick={() => {
              setActiveStep(3);
              setSelectedFileIndex(0);
            }}
            className={`px-2.5 py-1 rounded font-medium transition-all ${
              activeStep === 3 ? 'bg-blue-600 text-white' : 'text-slate-400 hover:text-slate-200'
            }`}
          >
            Paso 3: Ventana
          </button>
          <button
            onClick={() => {
              setActiveStep(4);
              setSelectedFileIndex(0);
            }}
            className={`px-2.5 py-1 rounded font-medium transition-all ${
              activeStep === 4 ? 'bg-blue-600 text-white' : 'text-slate-400 hover:text-slate-200'
            }`}
          >
            Paso 4: SideNotes
          </button>
          <button
            onClick={() => {
              setActiveStep(5);
              setSelectedFileIndex(0);
            }}
            className={`px-2.5 py-1 rounded font-medium transition-all ${
              activeStep === 5 ? 'bg-blue-600 text-white' : 'text-slate-400 hover:text-slate-200'
            }`}
          >
            Paso 5: Google Drive
          </button>
          <button
            onClick={() => {
              setActiveStep(6);
              setSelectedFileIndex(0);
            }}
            className={`px-2.5 py-1 rounded font-medium transition-all ${
              activeStep === 6 ? 'bg-blue-600 text-white' : 'text-slate-400 hover:text-slate-200'
            }`}
          >
            Paso 6: Tray & Hotkey
          </button>
          <button
            onClick={() => {
              setActiveStep(7);
              setSelectedFileIndex(0);
            }}
            className={`px-2.5 py-1 rounded font-medium transition-all ${
              activeStep === 7 ? 'bg-blue-600 text-white' : 'text-slate-400 hover:text-slate-200'
            }`}
          >
            Paso 7: Guía y Deploy (3 arch.)
          </button>
        </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-12 min-h-[520px]">
        {/* File tree sidebar */}
        <div className="md:col-span-4 bg-slate-950/80 border-r border-slate-800 p-3 flex flex-col gap-1.5">
          <span className="text-[10px] font-mono uppercase tracking-wider text-slate-500 px-2 py-1">
            Archivos del Paso {activeStep}
          </span>

          {fileList.map((file, idx) => {
            const isSelected = selectedFileIndex === idx;
            return (
              <button
                key={file.path}
                onClick={() => setSelectedFileIndex(idx)}
                className={`w-full text-left p-2 rounded-lg text-xs font-mono transition-all flex items-start gap-2 ${
                  isSelected
                    ? 'bg-blue-600/20 text-blue-300 border border-blue-500/40'
                    : 'text-slate-400 hover:bg-slate-900 hover:text-slate-200'
                }`}
              >
                <FileCode className={`w-3.5 h-3.5 mt-0.5 shrink-0 ${isSelected ? 'text-blue-400' : 'text-slate-500'}`} />
                <div className="flex-1 min-w-0">
                  <div className="truncate font-semibold">{file.path.split('/').pop()}</div>
                  <div className="text-[10px] text-slate-500 truncate">{file.project}</div>
                </div>
              </button>
            );
          })}

          <div className="mt-auto p-2.5 bg-slate-900/60 rounded-lg border border-slate-800 text-[11px] text-slate-400 space-y-1">
            <div className="font-semibold text-slate-300 flex items-center gap-1.5">
              <Terminal className="w-3.5 h-3.5 text-amber-400" />
              {activeStep === 2 && 'Comando EF Core CLI'}
              {activeStep === 3 && 'Ventana Desempaquetada'}
              {activeStep === 4 && 'Área de Trabajo Monitor'}
              {activeStep === 5 && 'Carpeta Oculta Drive'}
              {activeStep === 6 && 'Atajo Global del Sistema'}
              {activeStep === 7 && 'Publicación Single-File .exe'}
            </div>
            <p className="text-[10px] font-mono text-emerald-400 bg-slate-950 p-1.5 rounded overflow-x-auto">
              {activeStep === 2 && 'dotnet ef database update --project src/StickyNotes.Data'}
              {activeStep === 3 && 'AppWindow.TitleBar.ExtendsContentIntoTitleBar = true'}
              {activeStep === 4 && 'MonitorHelper.GetPrimaryMonitorWorkArea()'}
              {activeStep === 5 && "DriveService.ScopeConstants.DriveAppdata ('appDataFolder')"}
              {activeStep === 6 && 'RegisterHotKey(hWnd, 9001, MOD_WIN | MOD_ALT, VK_N)'}
              {activeStep === 7 && 'dotnet publish -c Release -r win-x64 --self-contained true'}
            </p>
          </div>
        </div>

        {/* Code Content */}
        <div className="md:col-span-8 bg-slate-950 flex flex-col">
          {/* File bar */}
          <div className="px-4 py-2.5 bg-slate-900/90 border-b border-slate-800 flex items-center justify-between text-xs font-mono">
            <div className="flex items-center gap-2 text-slate-300 truncate">
              <span className="text-blue-400">{currentFile.project}</span>
              <span className="text-slate-600">/</span>
              <span className="text-slate-100 font-semibold">{currentFile.path}</span>
            </div>

            <button
              onClick={handleCopy}
              className="bg-slate-800 hover:bg-slate-700 text-slate-200 text-xs px-2.5 py-1 rounded flex items-center gap-1.5 transition-colors border border-slate-700"
            >
              {copied ? (
                <>
                  <Check className="w-3.5 h-3.5 text-emerald-400" />
                  <span className="text-emerald-400 font-sans">Copiado</span>
                </>
              ) : (
                <>
                  <Copy className="w-3.5 h-3.5" />
                  <span className="font-sans">Copiar Código</span>
                </>
              )}
            </button>
          </div>

          <div className="px-4 py-1.5 bg-slate-900/40 border-b border-slate-800/60 text-[11px] text-slate-400">
            {currentFile.description}
          </div>

          {/* Code Viewer */}
          <div className="flex-1 p-4 overflow-x-auto font-mono text-xs text-slate-200 leading-relaxed bg-[#0d1117]">
            <pre>
              <code>{currentFile.content}</code>
            </pre>
          </div>
        </div>
      </div>
    </div>
  );
};
