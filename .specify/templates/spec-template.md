# [SPEC-XXX]: [Nombre de la Funcionalidad o Módulo]

> **Estado:** Borrador | En Revisión | Aprobado | En Implementación | Completado  
> **Autor(es):** [Nombre del autor o Agente]  
> **Fecha de creación:** YYYY-MM-DD  
> **Última actualización:** YYYY-MM-DD  

---

## 1. Visión General (El "Qué" y el "Por Qué")

### 1.1 Declaración del Problema
[Describe el dolor, la limitación actual o la oportunidad que motiva este cambio.]

### 1.2 Propuesta de Solución
[Resumen de alto nivel de lo que se implementará para resolver el problema.]

### 1.3 Objetivos y Metas Fuera de Alcance
- **Objetivos directos (In Scope):**
  - [ ] Objetivo 1
  - [ ] Objetivo 2
- **Fuera de alcance (Out of Scope):**
  - Objetivo descartado o pospuesto para futuras iteraciones.

---

## 2. Requisitos del Sistema

### 2.1 Requisitos Funcionales (RF)
- **RF-01:** El sistema DEBE permitir...
- **RF-02:** Cuando el usuario haga clic en..., el sistema DEBE...
- **RF-03:** Si ocurre un error de red, el sistema DEBE...

### 2.2 Requisitos No Funcionales (RNF)
- **RNF-01 (Rendimiento):** La acción no debe bloquear el hilo de UI de WinUI 3 y debe responder en < 100 ms.
- **RNF-02 (Seguridad):** Cumplir con el Principio IV de la Constitución (DPAPI / scope mínimo).
- **RNF-03 (Confiabilidad):** Ningún fallo transitorio debe provocar pérdida de datos o estados inconsistentes.

---

## 3. Casos de Uso y Criterios de Aceptación (Gherkin)

### Escenario 1: Flujo Exitoso Principal
- **Dado que** [el usuario tiene la aplicación abierta con al menos una nota]
- **Cuando** [realiza la acción X]
- **Entonces** [el sistema debe responder con Y]

### Escenario 2: Flujo Alternativo o Manejo de Errores
- **Dado que** [no hay conectividad a Internet]
- **Cuando** [se intenta sincronizar]
- **Entonces** [se debe registrar un estado pendiente y notificar sutilmente en la barra inferior sin bloquear la edición]

---

## 4. Preguntas Abiertas y Decisiones Pendientes
- [ ] *Pregunta 1:* ¿Qué comportamiento se espera en caso de colisión de archivos?
  - *Decisión:* Resolución Last-Write-Wins (LWW) basada en fecha UTC de `UpdatedAt`.
