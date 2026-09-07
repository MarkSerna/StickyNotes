# PLAN-003: Diseño de Búsqueda, Papelera y Exportación

> **Especificación asociada:** `[SPEC-003](./spec.md)`  
> **Estado:** Aprobado  
> **Fecha:** 2026-09-07  

---

## 1. Diseño Técnico

### 1.1 Barra de Búsqueda Reactiva en `SideNotesWindow`
- Mantener una lista maestra privada `List<Note> _allLoadedNotes` y una `ObservableCollection<Note> Notes` enlazada a la UI.
- Escuchar el evento `TextChanged` de un `AutoSuggestBox` (o `TextBox` estilizado):
  - Filtrar: `n.Title.Contains(query, StringComparison.OrdinalIgnoreCase) || n.Content.Contains(query, StringComparison.OrdinalIgnoreCase)`.
  - Actualizar los elementos de `Notes`.
  - Si la consulta está vacía, restaurar todos los elementos de `_allLoadedNotes`.

### 1.2 Modo Papelera en `SideNotesWindow`
- Bandera `bool _isTrashMode = false`.
- Botón de alternancia en la cabecera: icono de papelera (`\uE74D`).
- Cuando `_isTrashMode == true`:
  - Cabecera indica "Papelera de Reciclaje".
  - Ocultar editor o ponerlo en modo de solo lectura.
  - Mostrar barra con botón "Vaciar Papelera" y en cada elemento de la lista botones "Restaurar" y "Eliminar definitivamente".
  - Botón "Volver a notas activas" para regresar.

### 1.3 Exportación a Markdown y JSON
- En `StickyNotes.App/Helpers/ExportHelper.cs`:
  - `ExportNoteToMarkdownAsync(Note note, IntPtr hWnd)`:
    - Extrae el texto o RTF limpio a formato Markdown con encabezado YAML o título `# [Título]`.
    - Abre `FileSavePicker` de WinRT asociado al HWND de la ventana mediante `InitializeWithWindow.Initialize`.
    - Escribe el contenido en el archivo seleccionado.
  - `ExportAllNotesToJsonAsync(List<Note> notes, IntPtr hWnd)`:
    - Serializa la lista a JSON indentado y permite guardarlo.

---

## 2. Impacto en Componentes

- **`StickyNotes.App/Helpers/ExportHelper.cs` [NUEVO]:** Métodos estáticos auxiliares para exportar notas.
- **`StickyNotes.App/Views/SideNotesWindow.xaml`:** Cabecera con `AutoSuggestBox`, botón de papelera y botones de exportación.
- **`StickyNotes.App/Views/SideNotesWindow.xaml.cs`:** Lógica de filtrado, alternancia de vistas y operaciones de papelera.
- **`StickyNotes.App/Views/NoteWindow.xaml` y `.xaml.cs`:** Menú de opciones con "Exportar nota a Markdown".
