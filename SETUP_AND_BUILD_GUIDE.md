# Guía de Configuración, Google Cloud y Compilación (.NET 8 + WinUI 3)

## 1. Configuración de Google Cloud Console (OAuth 2.0 para Drive)

Para que la aplicación sincronice tus notas en tu propia cuenta de Google Drive sin costes ni necesidad de servidores externos:

### Paso 1.1: Crear Proyecto
1. Entra a [Google Cloud Console](https://console.cloud.google.com/).
2. Haz clic en el selector de proyectos superior y presiona **"Nuevo proyecto"** (*New Project*).
3. Nómbralo `StickyNotes-App` y haz clic en **Crear**.

### Paso 1.2: Habilitar la API de Google Drive
1. En el menú lateral izquierdo, ve a **APIs y servicios > Biblioteca** (*APIs & Services > Library*).
2. Busca `Google Drive API`.
3. Entra y presiona el botón azul **Habilitar** (*Enable*).

### Paso 1.3: Pantalla de Consentimiento OAuth (OAuth Consent Screen)
1. Ve a **APIs y servicios > Pantalla de consentimiento de OAuth**.
2. Selecciona Tipo de usuario: **Externo** (*External*) y presiona **Crear**.
3. Rellena los datos básicos:
   - **Nombre de la aplicación**: `Notas Rápidas Windows 11`
   - **Correo de asistencia del usuario**: tu correo electrónico de Google.
   - **Datos de contacto del desarrollador**: tu correo electrónico.
4. En la pestaña **Permisos (Scopes)**:
   - Presiona **Agregar o quitar permisos**.
   - Añade manualmente el permiso: `https://www.googleapis.com/auth/drive.appdata`
   - *(Este permiso solo da acceso a la carpeta oculta de configuración de la app, NO a tus fotos o documentos)*.
5. En la pestaña **Usuarios de prueba (Test Users)**:
   - Añade tu propia dirección de correo de Google (ej: `marcoesernal@gmail.com`).
   - *(Al estar en modo "Pruebas", Google permite el inicio de sesión inmediato sin requerir el proceso largo de verificación pública)*.

### Paso 1.4: Crear Credenciales de Escritorio
1. Ve a **APIs y servicios > Credenciales**.
2. Haz clic en **+ Crear credenciales > ID de cliente de OAuth** (*OAuth client ID*).
3. En **Tipo de aplicación**, selecciona: **Aplicación de escritorio** (*Desktop app*).
4. Nombre: `StickyNotes Desktop Client`.
5. Presiona **Crear**.
6. Copia el **ID de cliente** (`ClientId`) y el **Secreto de cliente** (`ClientSecret`).
7. Pégalos en el archivo `StickyNotes.App/appsettings.json`.

---

## 2. Requisitos Previos en Windows 11

- **Sistema Operativo:** Windows 11 versión 22H2 (Build 22621) o superior (o Windows 10 20H2+).
- **Entorno:** Visual Studio 2022 (Community, Professional o Enterprise) versión 17.8 o superior.
- **Cargas de trabajo en Visual Studio Installer:**
  1. *Desarrollo para el escritorio con .NET* (.NET Desktop Development).
  2. *Windows App SDK C# Templates* (plantillas de Windows App SDK).
  3. SDK de .NET 8.0.

---

## 3. Comandos de Compilación y Ejecución por Terminal

Abre una terminal PowerShell o Windows Terminal en la raíz de la solución:

```powershell
# 1. Restaurar paquetes NuGet
dotnet restore

# 2. Aplicar migraciones iniciales a SQLite (crea stickynotes.db con WAL y PRAGMAs)
dotnet ef database update --project src/StickyNotes.Data --startup-project src/StickyNotes.App

# 3. Compilar la solución en modo Debug
dotnet build

# 4. Ejecutar la aplicación
dotnet run --project src/StickyNotes.App
```

---

## 4. Publicación para Distribución (Ejecutable Independiente .exe)

Para generar un ejecutable único sin dependencias que los usuarios puedan descargar y abrir directamente (Unpackaged WinUI 3):

```powershell
dotnet publish src/StickyNotes.App/StickyNotes.App.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  /p:PublishSingleFile=true `
  /p:WindowsPackageType=None `
  /p:IncludeNativeLibrariesForSelfExtract=true `
  -o ./dist
```

El archivo `./dist/StickyNotes.App.exe` se ejecutará al instante con aceleración por GPU, efectos Mica Alt y soporte nativo para la bandeja de Windows 11.