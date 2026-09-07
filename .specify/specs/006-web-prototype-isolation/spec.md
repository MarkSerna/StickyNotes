# SPEC-006: Aislamiento y Desacoplamiento del Prototipo Web

> **Estado:** Aprobado  
> **Autor:** Antigravity / MarkSerna  
> **Fecha:** 2026-09-07  

---

## 1. Visión General

### 1.1 Declaración del Problema
1. La raíz del repositorio contiene una mezcla de artefactos de un prototipo web de React/Vite (`package.json`, `vite.config.ts`, `tsconfig.json`, `index.html`, `bun.lock`, `src/`, `public/`, `metadata.json`) junto con la solución nativa .NET 8 / Windows App SDK (`StickyNotes.sln`, `StickyNotes.App`, etc.).
2. Esta mezcla genera ruido cognitivo para desarrolladores de .NET, contamina la raíz del proyecto y puede confundir a herramientas de análisis estático o linters.
3. El código generado en `src/components/CodeGeneratedView.tsx` contiene más de 100 KB de código C# duplicado en cadenas crudas que fueron útiles durante la conceptualización inicial pero ya no son la fuente de verdad.

### 1.2 Propuesta de Solución
- Crear el directorio `web/` en la raíz del proyecto.
- Trasladar todos los archivos y carpetas del simulador web a `web/`:
  - `package.json`
  - `vite.config.ts`
  - `tsconfig.json`
  - `index.html`
  - `bun.lock`
  - `metadata.json`
  - `generate-solution.ts`
  - Carpeta `src/`
  - Carpeta `public/`
- Actualizar cualquier ruta relativa interna en `web/` si es necesario para asegurar que el simulador web pueda ejecutarse de forma independiente con `npm run dev` dentro de `web/`.
- Actualizar `README.md` documentando la nueva estructura limpia del repositorio.

---

## 2. Requisitos del Sistema

### 2.1 Requisitos Funcionales
- **RF-01 (Raíz Limpia):** La raíz del proyecto debe contener exclusivamente la solución .NET (`StickyNotes.sln`), las carpetas de proyectos C# (`StickyNotes.*`), la documentación principal (`README.md`, `ROADMAP_TAREAS.md`, `SETUP_AND_BUILD_GUIDE.md`), la configuración Git (`.gitignore`, `.github/`) y el marco SDD (`.specify/`).
- **RF-02 (Aislamiento Web):** El prototipo web reside en `web/` y mantiene su capacidad de arrancar con `npm run dev` (o `bun dev`).
- **RF-03 (Independencia de Compilación):** La solución .NET 8 y la suite de pruebas unitarias (`StickyNotes.Tests`) deben compilar y ejecutarse con 0 errores tras la reorganización.
