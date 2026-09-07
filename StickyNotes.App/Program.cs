using System;
using System.IO;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using StickyNotes.App.Services;
using StickyNotes.App.ViewModels;
using StickyNotes.App.Views;
using StickyNotes.Data;
using StickyNotes.Data.Context;
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
        const string mutexName = "Global\\StickyNotes_Win11_SingleInstanceMutex";
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

            // Inicializar y actualizar el esquema antes de crear ventanas o arrancar servicios.
            EnsureDatabaseSchemaWithRecovery(host);

            host.Start();

            // Arrancar la aplicación WinUI 3
            _ = new App(host.Services);
        });
    }

    private static void EnsureDatabaseSchemaWithRecovery(IHost host)
    {
        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NotesDbContext>();
            var dbPath = db.Database.GetDbConnection().DataSource;

            System.Diagnostics.Debug.WriteLine($"📊 Database path: {dbPath}");

            try
            {
                // Intenta aplicar migraciones primero
                System.Diagnostics.Debug.WriteLine("🔄 Executing migrations...");
                db.Database.Migrate();

                // Valida que la tabla Notes existe
                System.Diagnostics.Debug.WriteLine("✅ Validating Notes table...");
                db.Database.ExecuteSqlRaw("SELECT 1 FROM Notes LIMIT 0");
                System.Diagnostics.Debug.WriteLine("✅ Notes table exists!");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error during migration: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"   Inner exception: {ex.InnerException?.Message}");

                if (ex.Message.Contains("no such table: Notes", StringComparison.OrdinalIgnoreCase) || 
                    ex.InnerException?.Message.Contains("no such table: Notes", StringComparison.OrdinalIgnoreCase) == true)
                {
                    System.Diagnostics.Debug.WriteLine("🔨 Notes table missing, creating from model...");

                    // Si Migrate() falló al crear la tabla, usa EnsureCreated
                    // Primero cierra cualquier conexión abierta
                    db.Database.CloseConnection();

                    // Luego destruye y recrea completamente
                    db.Database.EnsureDeleted();
                    db.Database.EnsureCreated();

                    System.Diagnostics.Debug.WriteLine("✅ Database schema created successfully!");
                }
                else
                {
                    throw;
                }
            }
        }
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
                services.AddDbContext<StickyNotes.Data.Context.NotesDbContext>(options =>
                {
                    options.UseSqlite(
                        $"Data Source={dbPath}",
                        sqliteOptions => sqliteOptions.MigrationsAssembly(typeof(NotesDbContext).Assembly.FullName));
                });

                // Repositorios
                services.AddScoped<INoteRepository, StickyNotes.Data.Repositories.SqliteNoteRepository>();

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
                services.AddTransient<SideNotesWindow>();

                // Servicios del Sistema Operativo
                services.AddSingleton<WindowManager>();
            });
}