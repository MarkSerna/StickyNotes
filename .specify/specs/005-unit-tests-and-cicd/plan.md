# PLAN-005: Plan de Implementación de Pruebas Unitarias y CI/CD

> **Spec:** [SPEC-005](spec.md)  
> **Fecha:** 2026-09-07  

---

## 1. Arquitectura del Proyecto de Pruebas

```
StickyNotes/
│
├── StickyNotes.Tests/
│   ├── StickyNotes.Tests.csproj          # xUnit, FluentAssertions, EFCore.Sqlite, Test SDK
│   ├── Repositories/
│   │   └── SqliteNoteRepositoryTests.cs  # CRUD, Soft-delete, Trash, Query Filters, Sync marks
│   ├── Models/
│   │   ├── NoteModelTests.cs             # Lógica de DisplayTitle, coordenadas y defaults
│   │   └── DriveNotePayloadTests.cs      # Mapeo dominio-DTO, serialización y LWW
│   └── TestHelpers/
│       └── TestDbContextFactory.cs       # Creador de SQLite in-memory DbContext
│
├── .github/
│   └── workflows/
│       └── build-and-test.yml            # Pipeline de CI en GitHub Actions
└── StickyNotes.sln                       # Incorpora StickyNotes.Tests
```

---

## 2. Estrategia de Pruebas

1. **`TestDbContextFactory`**:
   - Inicializa una `SqliteConnection("DataSource=:memory:")`.
   - Mantiene la conexión abierta durante la vida del test (necesario en SQLite in-memory para no perder el esquema entre operaciones).
   - Ejecuta `context.Database.EnsureCreated()` para inicializar tablas y filtros de Fluent API.

2. **`SqliteNoteRepositoryTests`**:
   - `CreateAsync_ShouldPersistNote_WithCorrectUtcDatesAndPendingStatus`
   - `GetActiveNotesAsync_ShouldExcludeSoftDeletedNotes`
   - `SoftDeleteAsync_ShouldPopulateDeletedAt_AndAppearInTrashNotes`
   - `RestoreFromTrashAsync_ShouldClearDeletedAt_AndReturnToActive`
   - `PermanentDeleteAsync_ShouldPhysicallyRemoveNote`
   - `MarkAsSyncedAsync_ShouldUpdateStatusToSynced`
   - `GetPendingSyncNotesAsync_ShouldReturnActiveAndTrashPendingNotes`

3. **`NoteModelTests`**:
   - `DisplayTitle_WhenTitleSet_ReturnsTitle`
   - `DisplayTitle_WhenTitleEmpty_ReturnsFirstLineOfContent`
   - `DisplayTitle_WhenTitleAndContentEmpty_ReturnsDefaultPlaceholder`
   - `DisplayTitle_WhenFirstLineExceedsLimit_TruncatesWithEllipsis`

4. **`DriveNotePayloadTests`**:
   - `FromDomainModel_And_ToDomainModel_ShouldPreserveAllFields`
   - `ApplyTo_ShouldUpdateDomainModelFieldsCorrectly`
   - `JsonSerialization_ShouldRoundtripSuccessfully`

5. **CI/CD (`build-and-test.yml`)**:
   - Usa `windows-latest`.
   - Paso 1: `actions/checkout@v4`.
   - Paso 2: `actions/setup-dotnet@v4` con `dotnet-version: '8.0.x'`.
   - Paso 3: `dotnet restore StickyNotes.Tests/StickyNotes.Tests.csproj`.
   - Paso 4: `dotnet test StickyNotes.Tests/StickyNotes.Tests.csproj --configuration Release --verbosity normal`.
   - Paso 5: `msbuild StickyNotes.sln /p:Configuration=Release /p:Platform=x64` para certificar la compilación completa de la solución WinUI 3.
