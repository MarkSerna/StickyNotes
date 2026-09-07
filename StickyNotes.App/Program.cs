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
        var appDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "StickyNotes");
        Directory.CreateDirectory(appDataDir);
        var logPath = Path.Combine(appDataDir, "startup.log");
        try
        {
            File.WriteAllText(logPath, $"[{DateTime.UtcNow:O}] Iniciando StickyNotes.App...\n");

            // 1. Evitar múltiples instancias simultáneas mediante un Mutex con nombre
            const string mutexName = "Local\\StickyNotes_Win11_SingleInstanceMutex";
            _singleInstanceMutex = new Mutex(true, mutexName, out bool isOnlyInstance);

            File.AppendAllText(logPath, $"[{DateTime.UtcNow:O}] Mutex adquirido: {isOnlyInstance}\n");
            if (!isOnlyInstance)
            {
                File.AppendAllText(logPath, $"[{DateTime.UtcNow:O}] Otra instancia ya está en ejecución. Saliendo.\n");
                return;
            }

            // 2. Inicializar arquitectura WinUI 3 y XamlApplication
            File.AppendAllText(logPath, $"[{DateTime.UtcNow:O}] Inicializando ComWrappers...\n");
            WinRT.ComWrappersSupport.InitializeComWrappers();

            File.AppendAllText(logPath, $"[{DateTime.UtcNow:O}] Ejecutando Application.Start...\n");
            Application.Start(p =>
            {
                try
                {
                    File.AppendAllText(logPath, $"[{DateTime.UtcNow:O}] En Application.Start callback...\n");
                    var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
                    SynchronizationContext.SetSynchronizationContext(context);

                    File.AppendAllText(logPath, $"[{DateTime.UtcNow:O}] Creando host...\n");
                    var host = CreateHostBuilder(args).Build();

                    File.AppendAllText(logPath, $"[{DateTime.UtcNow:O}] Verificando base de datos...\n");
                    EnsureDatabaseSchemaWithRecovery(host);

                    File.AppendAllText(logPath, $"[{DateTime.UtcNow:O}] Iniciando host...\n");
                    host.Start();

                    File.AppendAllText(logPath, $"[{DateTime.UtcNow:O}] Instanciando App...\n");
                    _ = new App(host.Services);
                    File.AppendAllText(logPath, $"[{DateTime.UtcNow:O}] App instanciada con éxito.\n");
                }
                catch (Exception ex)
                {
                    File.AppendAllText(logPath, $"[{DateTime.UtcNow:O}] ERROR en Application.Start: {ex}\n");
                    throw;
                }
            });
        }
        catch (Exception ex)
        {
            File.AppendAllText(logPath, $"[{DateTime.UtcNow:O}] ERROR FATAL en Main: {ex}\n");
        }
    }

    private static void EnsureDatabaseSchemaWithRecovery(IHost host)
    {
        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NotesDbContext>();
            var dbPath = db.Database.GetDbConnection().DataSource;

            System.Diagnostics.Debug.WriteLine($"📊 Database path: {dbPath}");

            // 1. Respaldo preventivo automático si el archivo ya existe y tiene contenido
            if (!string.IsNullOrWhiteSpace(dbPath) && File.Exists(dbPath))
            {
                try
                {
                    var fileInfo = new FileInfo(dbPath);
                    if (fileInfo.Length > 0)
                    {
                        var backupPath = dbPath + ".bak";
                        File.Copy(dbPath, backupPath, overwrite: true);
                        System.Diagnostics.Debug.WriteLine($"🛡️ Backup preventivo creado: {backupPath}");
                    }
                }
                catch (Exception backupEx)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ No se pudo crear backup preventivo: {backupEx.Message}");
                }
            }

            try
            {
                // 2. Si la base de datos es nueva (primer arranque), inicializar con Migrate()
                if (!string.IsNullOrWhiteSpace(dbPath) && !File.Exists(dbPath))
                {
                    System.Diagnostics.Debug.WriteLine("🌱 Base de datos nueva, inicializando esquema...");
                    db.Database.Migrate();
                    System.Diagnostics.Debug.WriteLine("✅ Esquema inicial creado con éxito!");
                    return;
                }

                // 3. Aplicar migraciones pendientes
                System.Diagnostics.Debug.WriteLine("🔄 Ejecutando migraciones pendientes...");
                db.Database.Migrate();

                // 4. Validar que la tabla Notes existe
                System.Diagnostics.Debug.WriteLine("✅ Validando tabla Notes...");
                db.Database.ExecuteSqlRaw("SELECT 1 FROM Notes LIMIT 0");
                System.Diagnostics.Debug.WriteLine("✅ Tabla Notes verificada con éxito!");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error durante inicialización/migración: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"   Inner exception: {ex.InnerException?.Message}");

                // Si la tabla no existe en absoluto (base de datos vacía o sin migración aplicada)
                if (ex.Message.Contains("no such table: Notes", StringComparison.OrdinalIgnoreCase) || 
                    ex.InnerException?.Message.Contains("no such table: Notes", StringComparison.OrdinalIgnoreCase) == true)
                {
                    System.Diagnostics.Debug.WriteLine("🔨 Tabla Notes no detectada, inicializando esquema con EnsureCreated...");
                    db.Database.CloseConnection();
                    // NUNCA borrar datos existentes: solo EnsureCreated para crear tablas faltantes
                    db.Database.EnsureCreated();
                    System.Diagnostics.Debug.WriteLine("✅ Esquema inicializado de forma segura (sin borrado destructivo)!");
                }
                else
                {
                    // Relanzar la excepción para diagnóstico sin destruir la base de datos
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
                config.AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true);
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

                // Servicios de sincronización Google Drive (usando IServiceScopeFactory para scopes aislados)
                var clientId = configuration["GoogleDrive:ClientId"] ?? string.Empty;
                var clientSecret = configuration["GoogleDrive:ClientSecret"] ?? string.Empty;

                services.AddSingleton(sp => new GoogleDriveSyncService(
                    sp.GetRequiredService<IServiceScopeFactory>(),
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