# Epic #8 — Full Project Persistence

> **Status**: En Desarrollo
> **Priority**: P0 (Critico)
> **Creado**: 2026-03-31
> **Issue**: [#28](https://github.com/ivanrlg/FuzzySat/issues/28)
> **Prerequisito**: Epic #7 completado (PR #29)

---

## Objetivo

Implementar persistencia automatica completa para todos los artefactos de un proyecto FuzzySat.
Actualmente, `ProjectStateService` es un servicio scoped en memoria — todo el trabajo del usuario
(entrenamiento, clasificacion, validacion) se pierde al cerrar el navegador o cambiar de proyecto.

Este EPIC garantiza que al cargar un proyecto, se restauren automaticamente:
- Regiones de entrenamiento dibujadas
- Muestras etiquetadas extraidas
- Sesion de entrenamiento (estadisticas espectrales)
- Resultados de clasificacion (mapa de clases + confianza)
- Parametros de clasificacion utilizados
- Matriz de confusion y metricas de validacion

---

## Alcance

| ID | Requerimiento | Criterio de Aceptacion |
|----|---------------|------------------------|
| R1 | Auto-save de regiones de entrenamiento | Regiones dibujadas se guardan automaticamente y se restauran al recargar el proyecto |
| R2 | Auto-save de muestras de entrenamiento | Muestras (CSV format) se persisten sin accion manual del usuario |
| R3 | Auto-save de sesion de entrenamiento | TrainingSession (estadisticas) se guardan como JSON y se restauran |
| R4 | Auto-save de resultados de clasificacion | Mapa de clases + confianza se guardan comprimidos (binary+GZip) |
| R5 | Auto-save de parametros de clasificacion | Tipo de MF, operador AND, defuzzifier guardados como JSON |
| R6 | Auto-save de validacion | Matriz de confusion + metricas guardadas como JSON |
| R7 | Restauracion completa al cargar proyecto | Al seleccionar un proyecto existente, todo el estado se restaura automaticamente |
| R8 | Compatibilidad retroactiva | Proyectos existentes (solo config JSON) siguen funcionando sin errores |

---

## Fuera de Alcance

- Migracion a base de datos (SQLite/PostgreSQL) — el modelo actual es simple y no lo requiere
- Versionado de artefactos / historial de cambios
- Sincronizacion entre multiples clientes/pestanas
- Persistencia de la imagen raster cacheada (se re-lee de disco)

---

## Metricas de Exito

1. **Zero data loss**: Cerrar navegador + reabrir + cargar proyecto = estado completo restaurado
2. **Auto-save transparente**: El usuario NO necesita presionar "Guardar" — sucede automaticamente
3. **Performance**: Auto-save con debounce (500ms) no bloquea la UI
4. **Backward compatible**: Proyectos creados antes de este EPIC cargan normalmente (datos faltantes = null)
