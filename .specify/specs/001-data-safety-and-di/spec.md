# SPEC-001: Seguridad Crítica de Datos y Corrección de Ámbitos de DI

> **Estado:** Aprobado  
> **Autor:** Antigravity / MarkSerna  
> **Fecha:** 2026-09-07  

---

## 1. Visión General

### 1.1 Declaración del Problema
1. En `StickyNotes.App/Program.cs`, el método `EnsureDatabaseSchemaWithRecovery` invoca `db.Database.EnsureDeleted()` si la migración de EF Core falla. Esto puede ocurrir si SQLite está bloqueado temporalmente o si hay un cambio de versión, causando la eliminación silenciosa e irrecuperable de todas las notas del usuario.
2. `GoogleDriveSyncService` está registrado como `Singleton` pero depende directamente de `INoteRepository`, el cual tiene ciclo de vida `Scoped` (y utiliza `NotesDbContext`, que no es thread-safe). Cuando el servicio en segundo plano `SyncScheduler` sincroniza mientras el usuario edita una nota, se produce una colisión multihilo en Entity Framework Core.

### 1.2 Propuesta de Solución
- Eliminar la llamada a `EnsureDeleted()`. Implementar un mecanismo de respaldo automático preventivo (`stickynotes.db.bak`) antes de intentar cualquier migración.
- Refactorizar `GoogleDriveSyncService` para que reciba `IServiceScopeFactory` en lugar de una instancia fija de `INoteRepository`. Cada ciclo de sincronización creará su propio `IServiceScope` efímero, aislando completamente las operaciones de base de datos.

---

## 2. Requisitos del Sistema

### 2.1 Requisitos Funcionales
- **RF-01:** Antes de invocar `db.Database.Migrate()`, el sistema debe verificar si el archivo `.db` existe; de ser así, debe generar o actualizar una copia de seguridad segura `.db.bak`.
- **RF-02:** Si la migración falla, el sistema NUNCA debe borrar la base de datos existente. Debe registrar el error en diagnóstico y, si es necesario, lanzar una excepción controlada sin destruir los datos.
- **RF-03:** `GoogleDriveSyncService.SyncAllNotesAsync` debe crear un scope mediante `_scopeFactory.CreateScope()`, resolver un `INoteRepository` local y desechar el scope al finalizar la sincronización.

### 2.2 Requisitos No Funcionales
- **RNF-01 (Principio I Constitución):** Cero pérdida de datos.
- **RNF-02 (Principio II Constitución):** Resolución limpia de dependencias sin mezclar Scoped dentro de Singleton.
