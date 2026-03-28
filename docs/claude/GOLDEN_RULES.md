# Reglas de Oro - FuzzySat

> Reglas inquebrantables del proyecto. CERO excepciones.
> Ultima actualizacion: 2026-03-28

---

## Regla #1: NO MERGE SIN APROBACION EXPLICITA

**La regla mas importante del proyecto.**

- NUNCA mergear a main sin aprobacion EXPLICITA del usuario
- NO IMPORTA si compilacion es perfecta, tests pasan, codigo es impecable
- Primero: esperar review de bots (Claude Code Review + GitHub Copilot)
- Segundo: esperar aprobacion EXPLICITA del usuario

**Aprobaciones validas**: "Mergea esto", "Aprobado para merge", "Dale, mergea", "Merge it"
**Aprobaciones INVALIDAS**: "Gracias", "Ok", "Looks good", "Buen trabajo", silencio

---

## Regla #2: Limites de Codigo

| Nivel | Lineas | Accion |
|-------|--------|--------|
| Ideal | 100-200 | Commit normal |
| Aceptable | Hasta 500 | Solo si esta bien organizado |
| Prohibido | >1000 | NUNCA sin compilar antes |

**Compilar frecuentemente**: cada 50 lineas nuevas de codigo.

---

## Regla #3: Metodologia Micro-Commits

Un commit = Un objetivo.

**Checklist antes de cada commit:**
- [ ] Compila? (`dotnet build`)
- [ ] Objetivo unico?
- [ ] <200 lineas de cambio?
- [ ] task/todo.md actualizado?

---

## Regla #4: Tests Unitarios (NO NEGOCIABLE)

- **NUNCA** eliminar tests (comentar, borrar, skip)
- Si un test falla: investigar y corregir el codigo o el test
- Si >10 tests fallan: la causa raiz probablemente es UN bug en el codigo
- Eliminacion valida SOLO con aprobacion explicita del usuario + documentacion

**Especifico de FuzzySat:**
- Funciones de membresia DEBEN testearse con valores input/output conocidos de la tesis
- Confusion matrix y Kappa DEBEN validarse contra los resultados publicados
- Todo algoritmo del pipeline fuzzy debe tener al menos un test con datos de la tesis original

---

## Regla #5: Prohibiciones Criticas

- **NUNCA** usar propiedades que no existen
- **NUNCA** preservar funcionalidad faltante durante refactoring
- **NUNCA** crear una propiedad y usarla antes de verificar que compila
- **NUNCA** asumir que GDAL esta disponible sin verificar la inicializacion
- **NUNCA** hardcodear rutas de archivos de imagery satelital

---

## Regla #6: Arquitectura Limpia

- `FuzzySat.Core` NUNCA referencia CLI, Api, o Web
- Core contiene TODAS las interfaces y algoritmos
- CLI, Api, Web SOLO referencian Core
- Los tests SOLO referencian Core

```
Core <-- CLI
Core <-- Api
Core <-- Web
Core <-- Tests
```

---

## Regla #7: Fidelidad al Algoritmo Original

El motor de logica difusa DEBE ser fiel a la tesis original:

- **Funcion de membresia**: Gaussiana `mu(x) = exp(-0.5 * ((x - mean) / sigma)^2)`
- **Operador AND**: Minimo (no producto)
- **Defuzzificacion**: Max Weight (winner takes all), NO Sugeno weighted average
- **Entrenamiento**: Media y desviacion estandar por clase por banda
- **Reglas**: Una regla por clase, NUNCA mezclar clases entre bandas

Cualquier variacion del algoritmo original debe implementarse como OPCION ADICIONAL,
nunca como reemplazo del metodo original.

---

## Regla #8: Datos Satelitales

- **NUNCA** commitear archivos de imagery (.tif, .tiff, .hdf, .nc, .jp2)
- Los archivos satelitales van en .gitignore
- Usar rutas relativas o configurables para datos de prueba
- Documentar donde obtener datos de prueba en samples/README.md

---

*Estas reglas son inquebrantables. Si una situacion parece requerir una excepcion,
consultar con el usuario ANTES de proceder.*
