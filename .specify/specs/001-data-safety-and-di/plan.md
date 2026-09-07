# PLAN-001: Implementación de Seguridad de Datos y Scopes de DI

> **Especificación asociada:** `[SPEC-001](./spec.md)`  
> **Estado:** Aprobado  
> **Fecha:** 2026-09-07  

---

## 1. Diseño Técnico

### 1.1 Respaldo Preventivo en `Program.cs`
En `EnsureDatabaseSchemaWithRecovery(IHost host)`:
1. Extraer la ruta física del archivo `.db` desde `NotesDbContext`.
2. Si el archivo existe y tiene tamaño mayor a 0 bytes, copiarlo a `[ruta].bak` antes de ejecutar `db.Database.Migrate()`.
3. Si la base de datos no existe en absoluto (primer arranque limpio), invocar `db.Database.EnsureCreated()` directamente.
4. En caso de excepción durante `Migrate()`, capturar el error, registrarlo detalladamente en `Debug.WriteLine` y propagar un error no destructivo, retirando completamente cualquier llamada a `EnsureDeleted()`.

### 1.2 Refactor de `GoogleDriveSyncService`
1. Reemplazar el campo `private readonly INoteRepository _noteRepository;` por `private readonly IServiceScopeFactory _scopeFactory;`.
2. En el constructor de `GoogleDriveSyncService`:
   ```csharp
   public GoogleDriveSyncService(IServiceScopeFactory scopeFactory, string clientId, string clientSecret)
   ```
3. En `SyncAllNotesAsync`:
   ```csharp
   using var scope = _scopeFactory.CreateScope();
   var noteRepository = scope.ServiceProvider.GetRequiredService<INoteRepository>();
   // Ejecutar sincronización utilizando noteRepository local
   ```
4. Actualizar el registro en `Program.cs`:
   ```csharp
   services.AddSingleton(sp => new GoogleDriveSyncService(
       sp.GetRequiredService<IServiceScopeFactory>(),
       clientId,
       clientSecret));
   ```
