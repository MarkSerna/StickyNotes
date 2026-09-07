# TASKS-001: Tareas de Seguridad de Datos y DI

> **Especificación:** `[SPEC-001](./spec.md)`  
> **Plan:** `[PLAN-001](./plan.md)`  
> **Progreso:** 0% [ 0 / 4 ]  

---

## 📋 Checklist

- [ ] **TASK-001-1:** Refactorizar `EnsureDatabaseSchemaWithRecovery` en `StickyNotes.App/Program.cs` para crear backup `.bak` y suprimir `EnsureDeleted()`.
- [ ] **TASK-001-2:** Modificar `StickyNotes.Sync/Services/GoogleDriveSyncService.cs` para usar `IServiceScopeFactory`.
- [ ] **TASK-001-3:** Ajustar registro del servicio `GoogleDriveSyncService` en `StickyNotes.App/Program.cs`.
- [ ] **TASK-001-4:** Validar compilación con `dotnet build` y probar el arranque seguro.
