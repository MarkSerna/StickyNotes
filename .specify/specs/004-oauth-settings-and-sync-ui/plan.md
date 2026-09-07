# PLAN-004: Implementación de Configuración OAuth y Estado de Sync

> **Especificación asociada:** `[SPEC-004](./spec.md)`  
> **Estado:** Aprobado  
> **Fecha:** 2026-09-07  

---

## 1. Diseño Técnico

### 1.1 `SettingsDialog` (ContentDialog WinUI 3)
- Ubicación: `StickyNotes.App/Views/SettingsDialog.xaml` y `.xaml.cs`.
- Controles:
  - `TextBox TxtClientId`: Campo de texto para el Client ID de Google Cloud.
  - `PasswordBox TxtClientSecret`: Campo de contraseña para el Client Secret.
  - `TextBlock TxtAuthStatus`: Muestra "Conectado a Google Drive" o "No conectado".
  - `Button BtnAuthToggle`: Alterna entre "Iniciar sesión con Google" y "Cerrar sesión".
  - `HyperlinkButton`: Enlace a la consola de Google Cloud para obtener credenciales.
- Operaciones:
  - Al guardar: persistir en `appsettings.json` o configuración local de `%LOCALAPPDATA%`.
  - Invocar `googleDriveSyncService.UpdateCredentials(clientId, clientSecret)`.

### 1.2 Métodos en `GoogleDriveSyncService`
- `UpdateCredentials(string clientId, string clientSecret)`: actualiza credenciales y permite reautenticar.
- `DisconnectAsync()`: elimina el almacén de tokens en `WindowsCredentialDataStore` y anula `_driveService`.
- Evento `event Action<bool>? AuthStatusChanged;` para notificar a la UI.

### 1.3 Conexión con `SideNotesWindow` y `TrayIconService`
- Botón engranaje `\uE713` en la cabecera de `SideNotesWindow.xaml`.
- Opción "Configuración de Google Drive" en el menú contextual de `TrayIconService.cs`.
- Mostrar el `SettingsDialog` en el hilo de UI mediante `dialog.ShowAsync()`.

---

## 2. Impacto en Componentes

- **`StickyNotes.Sync/Services/GoogleDriveSyncService.cs`:** Adición de `UpdateCredentials`, `DisconnectAsync` y evento `AuthStatusChanged`.
- **`StickyNotes.App/Views/SettingsDialog.xaml` [NUEVO]:** Diálogo modal de configuración.
- **`StickyNotes.App/Views/SettingsDialog.xaml.cs` [NUEVO]:** Lógica de conexión y guardado.
- **`StickyNotes.App/Views/SideNotesWindow.xaml`:** Botón de configuración.
- **`StickyNotes.App/Services/TrayIconService.cs`:** Opción de menú para configuración.
