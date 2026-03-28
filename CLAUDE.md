# CLAUDE.md -- FuzzySat AI Assistant Configuration

> **Version**: 1.0 | **Ultima actualizacion**: 2026-03-28
> **Proposito**: Guia esencial para asistentes IA trabajando con FuzzySat.
> Lee este archivo COMPLETO antes de cualquier tarea.

---

## 1. Que es FuzzySat?

Clasificador de imagenes satelitales basado en logica difusa. Reimplementacion moderna en C#/.NET 10
de una tesis de 2008 (Universidad de Los Andes, Venezuela). El clasificador original logro **81.87%
Overall Accuracy** (Kappa = 0.7637), superando Maximum Likelihood (74.27%), Decision Tree (63.74%)
y Minimum Distance (56.14%).

**Licencia**: MIT (open source, actualmente privado hasta estar listo)

---

## 2. Entorno de Desarrollo

| Componente       | Version/Detalle                           |
|------------------|-------------------------------------------|
| **OS**           | Windows 11 Pro                            |
| **IDE**          | Visual Studio 2022 / VS Code             |
| **Framework**    | .NET 10 (LTS)                             |
| **Lenguaje**     | C# 13                                     |
| **Tests**        | xUnit + FluentAssertions                  |
| **CI**           | GitHub Actions                            |
| **Raster I/O**   | GDAL via MaxRev.Gdal.Core                |
| **CLI**          | System.CommandLine + Spectre.Console      |
| **Web**          | Blazor Server + Leaflet.js + Radzen       |

---

## 3. Documentacion Esencial

| Documento | Ubicacion | Descripcion |
|-----------|-----------|-------------|
| Reglas de Oro | [/docs/claude/GOLDEN_RULES.md](/docs/claude/GOLDEN_RULES.md) | Reglas inquebrantables del proyecto |
| Seguridad Git | [/docs/claude/GIT_SAFETY_RULES.md](/docs/claude/GIT_SAFETY_RULES.md) | Proceso PR, bots, aprobaciones |
| Checklist Implementacion | [/docs/claude/IMPLEMENTATION_CHECKLIST.md](/docs/claude/IMPLEMENTATION_CHECKLIST.md) | Checklist por tipo de componente |
| Pre-Commit | [/docs/claude/PRE_COMMIT_CHECKLIST.md](/docs/claude/PRE_COMMIT_CHECKLIST.md) | 6 pasos antes de commit |
| EPICs Activos | [/docs/epics/ACTIVE_EPICS.md](/docs/epics/ACTIVE_EPICS.md) | Epics en desarrollo |
| Historial EPICs | [/docs/epics/EPIC_HISTORY.md](/docs/epics/EPIC_HISTORY.md) | Epics completados |
| Progreso | [/task/todo.md](/task/todo.md) | Tracking central de progreso |
| Issues Conocidos | [/docs/troubleshooting/KNOWN_ISSUES_INDEX.md](/docs/troubleshooting/KNOWN_ISSUES_INDEX.md) | Indice de issues |
| Issues Criticos | [/docs/troubleshooting/CRITICAL_ISSUES.md](/docs/troubleshooting/CRITICAL_ISSUES.md) | Issues criticos activos |

---

## 4. Workflow Esencial

### Ciclo de Desarrollo (8 Pasos)

```
1. ANALIZAR   --> Entender problema y contexto
2. PLANIFICAR --> Escribir plan en task/todo.md
3. ESPERAR    --> NO codificar hasta aprobacion del usuario
4. IMPLEMENTAR --> Micro-commits (<200 lineas)
5. VERIFICAR  --> Compilar despues de cada cambio (dotnet build)
6. DOCUMENTAR --> Actualizar task/todo.md
7. REVISAR    --> Agregar seccion Review en task/todo.md
8. PULL REQUEST --> Crear PR y ESPERAR review de bots (5-15 min)
```

### Reglas de Comunicacion Criticas

- **NUNCA** culpar a compilacion, caching, o el IDE por errores
- **SIEMPRE** verificar la logica antes de reportar un problema
- **SIEMPRE** leer el codigo existente antes de proponer cambios
- **NUNCA** asumir que algo funciona sin compilar

---

## 5. Reglas de Ubicacion por Arquitectura

| Tipo | Ubicacion Correcta | NUNCA en |
|------|-------------------|----------|
| Interfaces (IClassifier, etc.) | `FuzzySat.Core/` | CLI, Api, Web |
| Fuzzy Engine (MFs, Inference) | `FuzzySat.Core/FuzzyLogic/` | CLI, Api, Web |
| Raster I/O (GDAL) | `FuzzySat.Core/Raster/` | CLI, Api, Web |
| Clasificacion | `FuzzySat.Core/Classification/` | CLI, Api, Web |
| Entrenamiento | `FuzzySat.Core/Training/` | CLI, Api, Web |
| Validacion (Kappa, Confusion) | `FuzzySat.Core/Validation/` | CLI, Api, Web |
| Visualizacion | `FuzzySat.Core/Visualization/` | CLI, Api |
| Configuracion JSON | `FuzzySat.Core/Configuration/` | CLI, Api, Web |
| Comandos CLI | `FuzzySat.CLI/Commands/` | Core, Api, Web |
| API Controllers | `FuzzySat.Api/Controllers/` | Core, CLI, Web |
| Blazor Pages | `FuzzySat.Web/Components/Pages/` | Core, CLI, Api |
| Blazor Shared | `FuzzySat.Web/Components/Shared/` | Core, CLI, Api |
| Unit Tests | `tests/FuzzySat.Core.Tests/` | src/ |

---

## 6. REGLA DE ORO ABSOLUTA: NO MERGE SIN APROBACION

```
============================================================
   REGLA ABSOLUTA: NO MERGE A MAIN SIN APROBACION EXPLICITA
============================================================

NO IMPORTA SI:
- La compilacion es perfecta
- Los tests pasan al 100%
- El codigo es impecable
- El usuario tarda en responder
- El branch "se ve listo"

SIEMPRE esperar aprobacion EXPLICITA del usuario.

APROBACIONES VALIDAS:
  - "Mergea esto"           OK
  - "Aprobado para merge"   OK
  - "Dale, mergea"          OK
  - "Merge it"              OK

APROBACIONES INVALIDAS:
  - "Gracias"               NO
  - "Ok"                    NO
  - "Looks good"            NO
  - Silencio                NO
  - "Buen trabajo"          NO
```

---

## 7. Proceso de PR y Review de Bots

1. Crear PR con descripcion detallada
2. **ESPERAR** review de bots (Claude Code Review + GitHub Copilot): 5-15 min
3. Revisar y corregir TODOS los issues reportados por bots
4. **NUNCA** mergear sin aprobacion de bots
5. **LUEGO** esperar aprobacion EXPLICITA del usuario (ver Seccion 6)

---

## 8. Comandos Rapidos

```bash
# Build
dotnet build

# Tests
dotnet test

# Run CLI
dotnet run --project src/FuzzySat.CLI

# Run Web
dotnet run --project src/FuzzySat.Web

# Run API
dotnet run --project src/FuzzySat.Api
```

---

## 9. Fases del Proyecto (Roadmap)

| Epic | Fase | Descripcion | Estado |
|------|------|-------------|--------|
| #1 | Core Engine MVP | Fuzzy logic, MFs, inference, defuzzifier | Planificado |
| #2 | I/O & CLI | GDAL raster, CLI commands, config JSON | Planificado |
| #3 | Advanced Features | MFs adicionales, indices espectrales, PCA | Planificado |
| #4 | ML Hybrid | ML.NET integration | Planificado |
| #5 | Blazor Web App | Web UI con Leaflet maps | Planificado |

**Prioridad**: Core engine (matematicas) primero, luego GDAL I/O, CLI, y finalmente Blazor.
El motor de logica difusa es el nucleo intelectual y debe funcionar correctamente antes de
construir cualquier UI encima.

---

*FuzzySat v0.0.0 - Scaffolding Phase*
