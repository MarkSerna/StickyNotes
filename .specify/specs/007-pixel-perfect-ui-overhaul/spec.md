# SPEC-007: Rediseño Visual y Funcional Fiel de Notas y Barra Lateral

> **Estado:** Aprobado  
> **Autor:** Antigravity / MarkSerna  
> **Fecha:** 2026-09-07  

---

## 1. Visión General

### 1.1 Objetivo
Reestructurar visual y funcionalmente la aplicación de escritorio WinUI 3 (`NoteWindow` y `SideNotesWindow`) para que sea una réplica exacta del diseño, paleta cromática y ergonomía de interacción del simulador interactivo de Windows 11 presentado en las capturas de pantalla.

### 1.2 Requisitos Clave
1. **Paleta de Colores Curada:**
   - Amarillo (`#FFF385`, cabecera `#FEE75C`, borde `#F6D83B`, texto `#2D2817`).
   - Verde (`#D2F8B8`, cabecera `#BAF096`, borde `#A2E278`, texto `#1A3311`).
   - Rosa (`#FFCEE8`, cabecera `#FCAFD9`, borde `#F389C3`, texto `#3B1528`).
   - Morado (`#E7DCFF`, cabecera `#D6C3FF`, borde `#BF9FFF`, texto `#261543`).
   - Azul (`#CEECFE`, cabecera `#AFDDFC`, borde `#8CCBF7`, texto `#0F2B40`).
   - Gris (`#E9ECEF`, cabecera `#DEE2E6`, borde `#CED4DA`, texto `#212529`).
2. **Notas Flotantes (`NoteWindow`):**
   - Tarjetas de radio 12px con cabecera en `headerHex` y cuerpo en `bgHex`.
   - Botón `+`, Pin siempre encima, Título editable in-place, Nube de sync, Selector de colores circular y botón de papelera.
   - Barra inferior con botones **B**, *I*, <u>U</u>, ~~S~~, Checklist y *"Auto-guardado 500ms"*.
3. **Franja de Reposo Lateral (`PeekingHandle`):**
   - Cápsulas verticales de color apiladas según las notas activas guardadas.
   - Despliegue fluido por hover o clic.
4. **Panel Lateral Desplegado (`SideNotesWindow`):**
   - Disposición en **dos columnas**:
     - Columna lateral derecha (~140px): Cabecera `NOTAS`, botón Pin, lista de tarjetas con insignias circulares de color y botón `+ Nueva`.
     - Columna izquierda (`*`): Tarjeta completa de la nota seleccionada idéntica al diseño flotante.
