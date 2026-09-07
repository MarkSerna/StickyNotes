# TASKS-004: Tareas de Configuración OAuth y Sincronización

> **Especificación:** `[SPEC-004](./spec.md)`  
> **Plan:** `[PLAN-004](./plan.md)`  
> **Progreso:** 100% [ 5 / 5 ]  

---

## 📋 Checklist

- [x] **TASK-004-1:** Añadir `UpdateCredentials`, `DisconnectAsync` y `AuthStatusChanged` en `GoogleDriveSyncService.cs`.
- [x] **TASK-004-2:** Crear `StickyNotes.App/Views/SettingsDialog.xaml` y `.xaml.cs` con interfaz amigable para credenciales OAuth.
- [x] **TASK-004-3:** Conectar botón de Configuración en `SideNotesWindow.xaml` y en `TrayIconService.cs`.
- [x] **TASK-004-4:** Implementar persistencia de credenciales en archivo de configuración local.
- [x] **TASK-004-5:** Compilar con MSBuild, verificar 0 errores y probar la experiencia de usuario.
