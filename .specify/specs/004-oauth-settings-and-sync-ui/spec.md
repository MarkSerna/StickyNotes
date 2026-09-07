# SPEC-004: Configuración OAuth para Google Drive y Feedback Visual de Sincronización

> **Estado:** Aprobado  
> **Autor:** Antigravity / MarkSerna  
> **Fecha:** 2026-09-07  

---

## 1. Visión General

### 1.1 Declaración del Problema
1. Los usuarios comunes no tienen forma fácil de configurar sus credenciales OAuth (`ClientId` y `ClientSecret`) sin buscar y editar manualmente el archivo `appsettings.json` en el disco.
2. Si las credenciales están vacías o son inválidas, la sincronización falla silenciosamente sin explicar al usuario qué falta.
3. No hay una forma visual de cerrar sesión en Google Drive (para cambiar de cuenta de Google o revocar acceso).
4. El indicador de sincronización en las notas a veces se mantiene estático ("Guardado local") y no refleja dinámicamente el progreso de subida o descarga en segundo plano.

### 1.2 Propuesta de Solución
- Crear un diálogo nativo de configuración (*SettingsDialog*) accesible desde la cabecera de `SideNotesWindow` y el menú de la bandeja del sistema (*Tray Icon*).
- El diálogo permitirá:
  - Ver el estado actual de la cuenta (No conectado / Conectado como usuario de Google Drive).
  - Ingresar o modificar `ClientId` y `ClientSecret` con enlaces de ayuda a Google Cloud Console.
  - Iniciar el flujo de inicio de sesión OAuth con un botón "Conectar cuenta".
  - Cerrar sesión con un botón "Desconectar", purgando los tokens cifrados con DPAPI.
- Proveer eventos y notificaciones visuales dinámicas: icono animado o texto claro ("Sincronizando...", "Sincronizado", "Pendiente de conexión").

---

## 2. Requisitos del Sistema

### 2.1 Requisitos Funcionales
- **RF-01 (Diálogo de Ajustes):** Accesible mediante un botón de engranaje en la cabecera de `SideNotesWindow` o la opción "Configuración" del System Tray.
- **RF-02 (Gestión de Credenciales):** Guardar las credenciales `ClientId` y `ClientSecret` de forma persistente en `appsettings.json` y actualizar la instancia de `GoogleDriveSyncService` en tiempo de ejecución.
- **RF-03 (Conexión / Desconexión):** 
  - El botón "Conectar" dispara `AuthenticateAsync` y actualiza el estado a "Conectado".
  - El botón "Desconectar" limpia los tokens guardados en `WindowsCredentialDataStore` y reinicia el cliente.
- **RF-04 (Feedback de Sincronización):** Cada nota debe reflejar si el cambio está guardado localmente o sincronizado en Drive.

### 2.2 Requisitos No Funcionales
- **RNF-01 (Seguridad):** Cumplimiento estricto del Principio IV de la Constitución (scope exclusivo `drive.appdata` y DPAPI).
- **RNF-02 (Estética Fluent):** El diálogo debe usar diseño Fluent de Windows 11 con fondo Mica/Acrylic.
