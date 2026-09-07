# SPEC-005: Arquitectura, Pruebas Unitarias y CI/CD

> **Estado:** Aprobado  
> **Autor:** Antigravity / MarkSerna  
> **Fecha:** 2026-09-07  

---

## 1. Visión General

### 1.1 Declaración del Problema
1. Actualmente el repositorio no cuenta con un arnés automatizado de pruebas unitarias que certifique el comportamiento del repositorio de datos, los filtros de consulta, el ciclo de vida de las notas y las reglas de resolución de conflictos de sincronización.
2. Cada modificación manual requiere verificación humana o compilación local, incrementando el riesgo de regresiones (como la reaparición inadvertida de borrado de esquemas o fallos en filtros de papelera).
3. No existe un pipeline de Integración Continua (CI) en GitHub Actions que garantice que cualquier commit o pull request compile limpiamente y supere todas las pruebas de regresión en un runner estándar de Windows.

### 1.2 Propuesta de Solución
- Crear el proyecto `StickyNotes.Tests` basado en `net8.0` utilizando **xUnit**, **FluentAssertions** y **Microsoft.EntityFrameworkCore.Sqlite**.
- Implementar una suite exhaustiva de pruebas unitarias:
  - **`SqliteNoteRepositoryTests`**: Verificación de CRUD completo, soft-delete, exclusión en consultas activas vía global query filter, recuperación desde papelera, borrado permanente y marcado de sincronización con base de datos SQLite en memoria (`DataSource=:memory:`).
  - **`NoteModelTests`**: Verificación de lógica de negocio en la entidad `Note` (cálculo reactivo de `DisplayTitle` con títulos vacíos, truncamiento de primera línea, notas en blanco).
  - **`DriveNotePayloadTests`**: Verificación de mapeo bidireccional dominio ↔ DTO de Google Drive, serialización JSON y aplicación de cambios con timestamp UTC para Last-Write-Wins.
- Incorporar `StickyNotes.Tests` en la solución `StickyNotes.sln`.
- Crear el flujo de trabajo de GitHub Actions `.github/workflows/build-and-test.yml` configurado para ejecutar restore, build y test en `windows-latest`.

---

## 2. Requisitos del Sistema

### 2.1 Requisitos Funcionales
- **RF-01 (Proyecto de Pruebas):** Proyecto `StickyNotes.Tests` compatible con `net8.0` y ejecutable mediante `dotnet test`.
- **RF-02 (Aislamiento de Pruebas de Datos):** Las pruebas de base de datos deben ejecutarse contra conexiones SQLite en memoria (`SqliteConnection`) para garantizar velocidad, aislamiento total e independencia de archivos en disco.
- **RF-03 (Cobertura de Ciclo de Vida de Notas):**
  - Creación y asignación automática de UUID, fechas UTC y estado `PendingUpload`.
  - Soft-delete: ocultamiento en `GetActiveNotesAsync` y presencia en `GetTrashNotesAsync`.
  - Restauración: vuelta a `GetActiveNotesAsync` con `DeletedAt == null`.
  - Borrado definitivo: eliminación física del registro.
- **RF-04 (Cobertura de DTOs y Sincronización):**
  - Mapeo `FromDomainModel` y `ToDomainModel`.
  - Comportamiento de `ApplyTo` y consistencia de atributos visuales/geométricos (X, Y, Width, Height, Monitor, AlwaysOnTop).
- **RF-05 (Pipeline CI GitHub Actions):**
  - Disparado en `push` a `main` y en `pull_request`.
  - Configura el entorno .NET 8 en `windows-latest`.
  - Ejecuta `dotnet test` y valida que el 100% de las pruebas pasen.

### 2.2 Requisitos No Funcionales
- **RNF-01 (Principio I de la Constitución):** Las pruebas deben certificar la imposibilidad de pérdida de datos y la preservación de notas.
- **RNF-02 (Velocidad de Ejecución):** La suite de pruebas debe ejecutarse en menos de 5 segundos en local.
