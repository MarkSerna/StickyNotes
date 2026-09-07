# PLAN-006: Plan de Aislamiento del Prototipo Web

> **Spec:** [SPEC-006](spec.md)  
> **Fecha:** 2026-09-07  

---

## 1. Movimiento de Archivos y Carpetas

Usar `git mv` para preservar el historial de Git:
```bash
mkdir web
git mv package.json web/
git mv vite.config.ts web/
git mv tsconfig.json web/
git mv index.html web/
git mv bun.lock web/
git mv metadata.json web/
git mv generate-solution.ts web/
git mv src web/src
git mv public web/public
```

Nota: `Generar-Solucion-CSharp.ps1` puede mantenerse en la raíz o en `scripts/` como script de utilidad para generar la solución en máquinas limpias, o trasladarse a `scripts/`.

---

## 2. Actualización de Documentación

- Actualizar el árbol de directorios en `README.md`.
- Añadir sección en `README.md` explicando cómo ejecutar el simulador web (`cd web && npm install && npm run dev`).

---

## 3. Verificación

1. Ejecutar `dotnet test StickyNotes.Tests\StickyNotes.Tests.csproj`.
2. Compilar con MSBuild `StickyNotes.sln`.
3. Verificar `git status`.
