# TASKS-007: Lista de Tareas para Rediseño Visual y Funcional

> **Spec:** [SPEC-007](spec.md) | **Plan:** [PLAN-007](plan.md)  
> **Fecha:** 2026-09-07  

---

## Tareas

- [x] **T-701**: Actualizar `ColorHelper.cs` con la paleta armónica exacta del prototipo (Amarillo, Verde, Rosa, Morado, Azul, Gris) con `HeaderBrush`, `BodyBrush`, `BorderBrush` y `ForegroundBrush`.
- [x] **T-702**: Rediseñar `NoteWindow.xaml` y `NoteWindow.xaml.cs` (cabecera con `+`, Pin, Título, Nube Sync, Flyout de círculos de color, Papelera; pie de página con formato y "Auto-guardado 500ms").
- [x] **T-703**: Rediseñar `SideNotesWindow.xaml` y `SideNotesWindow.xaml.cs`:
  - [x] **T-703.1**: Franja de reposo `PeekingHandle` con cápsulas verticales de color por cada nota existente.
  - [x] **T-703.2**: Reestructurar `ExpandedPanel` en dos columnas (editor de nota completa a la izquierda, lista `NOTAS` con tarjetas e insignias a la derecha).
  - [x] **T-703.3**: Conectar botón `+ Nueva`, alternador de fijar (*Pin*), menú contextual de nota y buscador.
- [x] **T-704**: Validar compilación con MSBuild y ejecutar suite de pruebas unitarias.
- [x] **T-705**: Lanzar la aplicación nativa y verificar funcionamiento en Windows 11.
