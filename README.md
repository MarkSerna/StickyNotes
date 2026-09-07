# StickyNotes para Windows 11

<div align="center">

![Windows 11](https://img.shields.io/badge/Windows_11-Fluent_Design_%7C_Mica-0078D4?style=for-the-badge&logo=windows11&logoColor=white)
![.NET 8](https://img.shields.io/badge/.NET_8.0-WinUI_3-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![SQLite](https://img.shields.io/badge/SQLite-EF_Core_8-003B57?style=for-the-badge&logo=sqlite&logoColor=white)
![Google Drive](https://img.shields.io/badge/Google_Drive-OAuth_2.0_Sync-4285F4?style=for-the-badge&logo=googledrive&logoColor=white)
![License](https://img.shields.io/badge/License-MIT-green?style=for-the-badge)

**Aplicación nativa moderna de notas rápidas para Windows 11 y 10, con diseño Fluent/Mica, doble modalidad (panel lateral acoplado *SideNotes* y notas flotantes independientes), persistencia ultrarrápida en SQLite local y sincronización en la nube mediante Google Drive.**

🔗 **Repositorio oficial:** [https://github.com/MarkSerna/StickyNotes.git](https://github.com/MarkSerna/StickyNotes.git)

</div>

---

## 🌟 Características Principales

- 🪟 **Diseño Nativo Windows 11 (WinUI 3):** Material Mica Alt, esquinas redondeadas, sombras Fluent y tema adaptable (Claro / Oscuro).
- 📌 **Doble Modo de Trabajo:**
  - **Modo Flotante:** Ventanas de notas independientes y reubicables en cualquier monitor, con soporte para *Always-on-Top* (fijar en primer plano).
  - **Modo Lateral (*SideNotes*):** Panel acoplable al borde de la pantalla que se auto-oculta y se despliega al pasar el cursor o hacer clic en la pestaña de reposo.
- ⚡ **Persistencia Local Inmediata:** SQLite embebido con Entity Framework Core 8 y modo WAL (*Write-Ahead Logging*). Auto-guardado inteligente mediante *debounce* de 500 ms.
- ☁️ **Sincronización Segura con Google Drive:**
  - Emplea exclusivamente el scope restringido `drive.appdata` (únicamente accede a la carpeta oculta de la app, sin acceso a tus documentos o fotos).
  - Resolución automática de conflictos mediante *Last-Write-Wins* (UTC).
  - Tokens OAuth 2.0 cifrados localmente mediante la API de seguridad de Windows (DPAPI).
- ⌨️ **Atajo Global de Teclado:** <kbd>Win</kbd> + <kbd>Alt</kbd> + <kbd>N</kbd> para crear una nota rápida desde cualquier ventana o aplicación.
- 🎨 **Paleta de Colores Curada:** Amarillo clásico, verde menta, rosa pastel, morado lavanda, azul cielo y gris carbón neutro.
- 🗑️ **Papelera y Borrado Seguro:** Sistema de *Soft-delete* para evitar pérdidas accidentales.

---

## 🏗️ Arquitectura de la Solución (.NET 8)

El proyecto sigue principios de **Clean Architecture** y modularidad estricta:

```text
StickyNotes/
├── .specify/                   # Ecosistema SDD (Spec-Driven Development / Spec-Kit)
│   ├── memory/                 # Constitución y principios arquitectónicos
│   ├── specs/                  # Especificaciones de características
│   └── templates/              # Plantillas estandarizadas (Spec, Plan, Tasks)
├── StickyNotes.Core/           # Modelos de dominio puros (Note, Enums, Interfaces)
├── StickyNotes.Data/           # Persistencia SQLite, EF Core 8, DbContext y Repositorio
├── StickyNotes.Sync/           # Sincronización Google Drive, OAuth 2.0 y DPAPI
├── StickyNotes.App/            # Aplicación WinUI 3 (Vistas, ViewModels, Tray y Hotkeys)
├── StickyNotes.sln             # Solución principal de Visual Studio / .NET CLI
└── ROADMAP_TAREAS.md           # Seguimiento interno de tareas (ignorado en Git)
```

---

## 🚀 Requisitos Previos

- **Sistema Operativo:** Windows 11 (versión 22H2 o superior) o Windows 10 (versión 20H2+).
- **SDK de .NET:** [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) o superior.
- **Herramientas de compilación:** Visual Studio 2022 (con la carga de trabajo *Desarrollo para el escritorio con .NET* y plantillas de *Windows App SDK*) o [.NET CLI](https://learn.microsoft.com/dotnet/core/tools/).

---

## 🛠️ Compilación y Ejecución

### 1. Clonar el repositorio
```bash
git clone https://github.com/MarkSerna/StickyNotes.git
cd StickyNotes
```

### 2. Restaurar dependencias NuGet
```powershell
dotnet restore
```

### 3. Compilar en modo Debug
```powershell
dotnet build StickyNotes.sln
```

### 4. Ejecutar la aplicación
```powershell
dotnet run --project StickyNotes.App/StickyNotes.App.csproj
```

### 5. Publicar un ejecutable único (.exe sin dependencias)
Para distribuir un binario unpackaged independiente de alto rendimiento:

```powershell
dotnet publish StickyNotes.App/StickyNotes.App.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  /p:PublishSingleFile=true `
  /p:WindowsPackageType=None `
  /p:IncludeNativeLibrariesForSelfExtract=true `
  -o ./dist
```

El ejecutable `./dist/StickyNotes.App.exe` puede iniciarse directamente en cualquier equipo Windows 11/10 compatible.

---

## ☁️ Configuración de Google Drive (Opcional para Sincronización)

Para habilitar la sincronización en tu cuenta personal:
1. Crea un proyecto en [Google Cloud Console](https://console.cloud.google.com/).
2. Habilita la **Google Drive API**.
3. En la pantalla de consentimiento de OAuth, añade el permiso: `https://www.googleapis.com/auth/drive.appdata`.
4. Crea una credencial de tipo **Aplicación de escritorio** (*Desktop App*).
5. Configura tu `ClientId` y `ClientSecret` en `StickyNotes.App/appsettings.json`.

---

## 📐 Metodología de Desarrollo: SDD (Spec-Driven Development)

Este repositorio incorpora los estándares de **SDD** inspirados en [GitHub Spec-Kit](https://github.com/github/spec-kit):

- **Constitución (`.specify/memory/constitution.md`):** Establece las reglas inquebrantables del proyecto (estabilidad de datos, separación de capas, UX nativa).
- **Especificaciones (`.specify/specs/`):** Cada funcionalidad cuenta con su documento formal de requerimientos (*What/Why*).
- **Planes Técnicos (`.specify/templates/plan-template.md`):** Diseño de ingeniería antes de escribir código (*How*).
- **Lista de Tareas (`.specify/templates/tasks-template.md`):** Pasos atómicos, ordenados y verificables.

---

## 📄 Licencia

Este proyecto está bajo la Licencia MIT. Consulta el archivo `LICENSE` para más detalles.
