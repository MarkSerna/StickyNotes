# SPEC-002: Servicios del Sistema Windows 11 y Gestor de Ventanas Flotantes

> **Estado:** Aprobado  
> **Autor:** Antigravity / MarkSerna  
> **Fecha:** 2026-09-07  

---

## 1. Visión General

### 1.1 Declaración del Problema
1. La aplicación actualmente solo muestra el panel lateral `SideNotesWindow` al iniciar.
2. `NoteWindow` (la ventana flotante de nota adhesiva individual) existe pero no puede abrirse de manera orquestada ni controlada. Si se abren múltiples notas, no hay seguimiento de qué notas están abiertas, lo que provocaría instancias duplicadas de la misma nota y conflictos de guardado concurrente.
3. `TrayIconService` (icono en el área de notificación de la barra de tareas) y `GlobalHotkeyService` (atajo de teclado global <kbd>Win</kbd>+<kbd>Alt</kbd>+<kbd>N</kbd>) están codificados pero desconectados del ciclo de vida de la aplicación.
4. `AppManager.cs` contiene métodos stub vacíos (`// Implementación mínima`).

### 1.2 Propuesta de Solución
- Crear un `WindowManager` centralizado y robusto que gestione tanto el panel lateral `SideNotesWindow` como un registro de ventanas flotantes `Dictionary<Guid, NoteWindow>`.
- Si el usuario solicita abrir una nota que ya está abierta en modo flotante, el sistema debe traerla al frente (`Activate()`) en lugar de crear una ventana duplicada.
- Conectar `TrayIconService` en el System Tray con opciones para: "Nueva nota", "Alternar SideNotes", "Mostrar todas las notas" y "Salir".
- Conectar `GlobalHotkeyService` para que al presionar <kbd>Win</kbd>+<kbd>Alt</kbd>+<kbd>N</kbd> desde cualquier programa de Windows, se cree una nueva nota flotante inmediatamente enfocada.
- Permitir abrir cualquier nota desde la lista de `SideNotesWindow` en su propia ventana flotante independiente.

---

## 2. Requisitos del Sistema

### 2.1 Requisitos Funcionales
- **RF-01 (Apertura de Notas Flotantes):** El usuario debe poder abrir cualquier nota como ventana flotante independiente (`NoteWindow`) desde el panel lateral o creando una nota nueva.
- **RF-02 (Anti-Duplicación):** Si una nota ya tiene una ventana activa, solicitar abrirla debe activar y enfocar la ventana existente sin instanciar una nueva.
- **RF-03 (Atajo Global):** Al presionar <kbd>Win</kbd>+<kbd>Alt</kbd>+<kbd>N</kbd> en cualquier parte de Windows, debe crearse una nueva nota en SQLite y abrirse su ventana flotante con el cursor en el título/editor.
- **RF-04 (Bandeja del Sistema):** El icono de la aplicación debe permanecer visible en la bandeja del sistema (*Tray*), permitiendo acceder a las acciones principales y cerrar la aplicación limpiamente.
- **RF-05 (Sincronización de Estado):** Al cerrar una ventana flotante (`NoteWindow`), los cambios deben persistirse en SQLite y la ventana debe desregistrarse del `WindowManager`.

### 2.2 Requisitos No Funcionales
- **RNF-01 (Rendimiento):** La creación y apertura de una ventana flotante debe tomar < 150 ms.
- **RNF-02 (Clean Architecture):** `WindowManager` se inyecta como `Singleton` y utiliza `IServiceScopeFactory` para resolver ventanas y repositorios aislados según sea necesario.
