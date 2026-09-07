# PLAN-002: Arquitectura del WindowManager y Servicios de Plataforma

> **Especificación asociada:** `[SPEC-002](./spec.md)`  
> **Estado:** Aprobado  
> **Fecha:** 2026-09-07  

---

## 1. Diseño Técnico

### 1.1 `WindowManager`
Transformar `StickyNotes.App/Services/WindowManager.cs` en el orquestador principal de UI:
- `SideNotesWindow? _sideNotesWindow;`
- `Dictionary<Guid, NoteWindow> _floatingWindows = new();`
- `Initialize()`: Inicializa `SideNotesWindow` y arranca servicios de bandeja y atajos globales.
- `OpenNoteAsFloatingAsync(Guid noteId)`:
  - Si ya existe en `_floatingWindows`, llamar a `_floatingWindows[noteId].Activate()` y retornar.
  - Si no, obtener la nota desde `INoteRepository` (usando `IServiceScopeFactory`).
  - Instanciar `NoteWindow(note, repo)` y registrarla en el diccionario.
  - Suscribir al evento `Closed` para desregistrarla al cerrarse.
  - Llamar a `Activate()`.
- `CreateAndOpenNewNoteFloatingAsync()`:
  - Crear una nueva `Note` en SQLite con coordenadas iniciales centradas o escalonadas.
  - Abrir la ventana flotante recién creada.
- `ToggleSideNotes()`: Si `_sideNotesWindow` está minimizada/oculta, enfocarla o alternar su visibilidad.

### 1.2 `AppManager`
- Conectar `AppManager` como fachada hacia `WindowManager` para mantener compatibilidad con llamadas existentes o unificar la lógica en `WindowManager`.

### 1.3 `TrayIconService`
- Inyectar en `WindowManager`.
- Conectar los eventos:
  - `_onNewNoteRequested` -> `WindowManager.CreateAndOpenNewNoteFloatingAsync()`
  - `_onToggleSidePanelRequested` -> `WindowManager.ToggleSideNotes()`
  - `_onShowAllFloatingRequested` -> `WindowManager.ShowAllFloatingNotes()`

### 1.4 `GlobalHotkeyService`
- Registrar atajo <kbd>Win</kbd>+<kbd>Alt</kbd>+<kbd>N</kbd> asociado al `DispatcherQueue` del hilo UI.
- Invocar `WindowManager.CreateAndOpenNewNoteFloatingAsync()`.

### 1.5 Registro en `Program.cs` y `App.xaml.cs`
- Registrar `WindowManager` y `SyncScheduler` en el contenedor DI.
- En `App.OnLaunched`: inicializar `WindowManager.Initialize()`.
