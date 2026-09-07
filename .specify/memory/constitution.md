# Constitución del Proyecto: StickyNotes (.specify/memory/constitution.md)

<!--
  Esta constitución define las reglas innegociables y principios arquitectónicos
  que TODO desarrollador y agente de Inteligencia Artificial DEBE respetar
  al diseñar, refactorizar o implementar código en este repositorio.
-->

## 🏛️ Principios Fundamentales

### I. Integridad Absoluta y Protección de Datos del Usuario (REGLA CRÍTICA)
1. **Cero tolerancia a pérdida de datos:** Ningún flujo de actualización, migración o recuperación puede invocar métodos destructivos como `EnsureDeleted()`, `DROP TABLE` o truncado de bases de datos sin consentimiento explícito y previo del usuario.
2. **Backups preventivos obligatorios:** Antes de ejecutar cualquier migración de base de datos (`Migrate()`) o sincronización masiva, el sistema debe garantizar la existencia de una copia de seguridad local (`.db.bak`).
3. **Borrado suave (*Soft-Delete*):** Las notas eliminadas por el usuario pasan primero al estado de papelera (`DeletedAt != null`). La eliminación permanente solo ocurre por acción explícita del usuario desde la vista de papelera.

### II. Arquitectura Limpia y Gestión de Dependencias
1. **Separación estricta de capas:**
   - `StickyNotes.Core`: Entidades de dominio puras y enums, sin dependencias de frameworks externos.
   - `StickyNotes.Data`: Implementación de persistencia (EF Core 8, SQLite, migraciones, repositorios).
   - `StickyNotes.Sync`: Servicios de red, integración con Google Drive API y cifrado DPAPI.
   - `StickyNotes.App`: Interfaz de usuario WinUI 3, ViewModels, gestión de ventanas y servicios del SO.
2. **Prevención de *Captive Dependencies*:**
   - Los servicios `Singleton` NUNCA deben inyectar directamente servicios `Scoped` (como `INoteRepository` o `NotesDbContext`).
   - Cuando un servicio en segundo plano requiera acceso a datos, debe utilizar `IServiceScopeFactory` o `IDbContextFactory` para instanciar y desechar scopes aislados por operación.

### III. Rendimiento, Fluidez y Experiencia Nativa Windows 11
1. **I/O no bloqueante:** Toda operación de disco, base de datos o red debe ser asíncrona (`async/await`) con soporte para `CancellationToken`.
2. **Debouncing en persistencia de UI:** La escritura continua de texto en el editor no debe saturar la base de datos. Se exige un *debounce* de al menos 500 ms antes de disparar el guardado automático.
3. **Estética Fluent Design:** Las ventanas deben adoptar el estándar visual de Windows 11 (Mica Alt, esquinas redondeadas, contrastes adecuados y temas Claro/Oscuro dinámicos).

### IV. Seguridad y Privacidad
1. **Principio de Privilegio Mínimo en la Nube:** Únicamente solicitar el scope `https://www.googleapis.com/auth/drive.appdata`. Queda estrictamente prohibido solicitar acceso a archivos personales o al Drive general del usuario.
2. **Cifrado de Credenciales:** Los tokens de autenticación OAuth 2.0 y secretos no deben almacenarse en texto plano. Se debe emplear la API de Protección de Datos de Windows (DPAPI) o el Administrador de Credenciales de Windows.

### V. Disciplina de Desarrollo Basado en Especificaciones (SDD)
1. **Flujo secuencial inquebrantable:**
   `Spec (Qué/Por qué)` ➔ `Clarify (Preguntas/Dudas)` ➔ `Plan (Cómo/Arquitectura)` ➔ `Tasks (Checklist)` ➔ `Implement (Código)` ➔ `Verify (Pruebas)`
2. **Trazabilidad:** Cada cambio sustancial en la base de código debe estar vinculado a una especificación activa dentro de `.specify/specs/`.
