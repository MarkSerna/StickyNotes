# SPEC-003: Experiencia de Usuario: Búsqueda, Papelera de Reciclaje y Exportación

> **Estado:** Aprobado  
> **Autor:** Antigravity / MarkSerna  
> **Fecha:** 2026-09-07  

---

## 1. Visión General

### 1.1 Declaración del Problema
1. Los usuarios que acumulan decenas de notas no tienen un mecanismo para encontrarlas rápidamente por palabras clave o título dentro del panel lateral `SideNotesWindow`.
2. El repositorio ya cuenta con métodos de borrado suave (`SoftDeleteAsync`) y consulta de papelera (`GetTrashNotesAsync`), pero la interfaz gráfica carece de una vista para inspeccionar, recuperar o vaciar notas eliminadas.
3. No existe una vía directa para respaldar o compartir notas en formatos abiertos y portables como Markdown (`.md`) o JSON.

### 1.2 Propuesta de Solución
- Incorporar una barra de búsqueda instantánea (`AutoSuggestBox`) en la cabecera de `SideNotesWindow` que filtre en tiempo real tanto por título como por contenido de la nota.
- Implementar una vista alternable para la **Papelera de Reciclaje** en `SideNotesWindow`, permitiendo al usuario:
  - Ver las notas eliminadas con su fecha de borrado.
  - Restaurar notas a su estado activo con un solo clic (`RestoreFromTrashAsync`).
  - Purgar notas individuales o vaciar toda la papelera permanentemente con confirmación previa.
- Incorporar funcionalidad de exportación:
  - Exportar la nota activa a un archivo Markdown estándar (`.md`).
  - Exportar el catálogo completo a un archivo JSON de respaldo.

---

## 2. Requisitos del Sistema

### 2.1 Requisitos Funcionales
- **RF-01 (Búsqueda Instantánea):** Escribir en el buscador debe filtrar la lista de notas de `SideNotesWindow` sin recargar la base de datos de manera innecesaria. Limpiar la búsqueda debe restaurar la lista completa.
- **RF-02 (Navegación a Papelera):** Un botón en la cabecera debe permitir alternar entre la vista "Mis Notas" y "Papelera".
- **RF-03 (Restauración de Notas):** En la papelera, cada nota debe ofrecer un botón "Restaurar", devolviéndola inmediatamente a la lista activa.
- **RF-04 (Vaciado de Papelera):** Debe existir un botón para vaciar la papelera con confirmación previa, eliminando permanentemente los registros mediante `PermanentDeleteAsync`.
- **RF-05 (Exportación a Markdown):** Desde `NoteWindow` y `SideNotesWindow`, el usuario debe poder exportar la nota a formato `.md` utilizando el selector de archivos nativo de Windows (`FileSavePicker`).

### 2.2 Requisitos No Funcionales
- **RNF-01 (Rendimiento):** El filtrado por texto en memoria debe responder con latencia < 30 ms.
- **RNF-02 (Seguridad de Datos):** Ninguna eliminación permanente ocurre sin confirmación del usuario.
