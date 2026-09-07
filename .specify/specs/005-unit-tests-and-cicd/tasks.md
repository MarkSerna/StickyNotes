# TASKS-005: Tareas de Implementación para Pruebas Unitarias y CI/CD

> **Spec:** [SPEC-005](spec.md) | **Plan:** [PLAN-005](plan.md)  
> **Fecha:** 2026-09-07  

---

## Tareas

- [x] **T-501.1**: Crear proyecto `StickyNotes.Tests.csproj` configurando .NET 8, referencias a `StickyNotes.Core`, `StickyNotes.Data`, `StickyNotes.Sync` y paquetes NuGet necesarios (`xunit`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`, `FluentAssertions`, `Microsoft.EntityFrameworkCore.Sqlite`).
- [x] **T-501.2**: Implementar `TestDbContextFactory.cs` / `SqliteTestContext.cs` para SQLite in-memory (`DataSource=:memory:`).
- [x] **T-501.3**: Implementar suite de pruebas `SqliteNoteRepositoryTests.cs` (cobertura total de CRUD, soft-delete, papelera, filtros y sincronización).
- [x] **T-501.4**: Implementar suite de pruebas `NoteModelTests.cs` y `DriveNotePayloadTests.cs`.
- [x] **T-501.5**: Registrar `StickyNotes.Tests.csproj` en `StickyNotes.sln`.
- [x] **T-501.6**: Ejecutar `dotnet test` y validar que el 100% de las pruebas pasan con éxito.
- [x] **T-502.1**: Crear el archivo de workflow de GitHub Actions `.github/workflows/build-and-test.yml`.
- [x] **T-502.2**: Validar compilación completa de la solución en local con MSBuild.
- [x] **T-502.3**: Actualizar `ROADMAP_TAREAS.md` y registrar avance de Fase 5.
