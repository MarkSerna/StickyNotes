# PLAN-007: Plan de Implementación de Rediseño Visual y Funcional

> **Spec:** [SPEC-007](spec.md)  
> **Fecha:** 2026-09-07  

---

## 1. Módulos a Modificar

1. **`StickyNotes.App/Helpers/ColorHelper.cs`**:
   - Actualizar `NotePalette` y `GetPalette(NoteColor color)` con los 6 esquemas cromáticos exactos del prototipo (`bgHex`, `headerHex`, `borderHex`, `textColor`).
   - Agregar métodos para obtener color crudo `Windows.UI.Color` y conversión hexadecimal limpia.

2. **`StickyNotes.App/Views/NoteWindow.xaml` y `.xaml.cs`**:
   - Cabecera: `+`, `Pin`, `TxtTitle` in-place, `IconSync`, `BtnColorPicker` (con Flyout de círculos de color), `BtnDelete`.
   - Barra inferior: `B`, `I`, `U`, `S`, Checklist y texto *"Auto-guardado 500ms"*.
   - Aplicación reactiva de estilos (`palette.HeaderBrush`, `palette.BodyBrush`, `palette.BorderBrush`, `palette.ForegroundBrush`).

3. **`StickyNotes.App/Views/SideNotesWindow.xaml` y `.xaml.cs`**:
   - `PeekingHandle`: Cápsulas verticales de color apiladas según las notas activas.
   - `ExpandedPanel`: Disposición en dos columnas:
     - Izquierda: Editor de tarjeta completa con la nota activa.
     - Derecha: Tira vertical de lista de notas con cabecera `NOTAS`, botón `Pin`, tarjetas compactas y botón `+ Nueva`.

---

## 2. Verificación
- Compilación limpia con MSBuild (`Platform=x64`).
- Ejecución de las 16 pruebas unitarias (`StickyNotes.Tests`).
- Lanzamiento de la aplicación y comprobación de interactividad visual.
