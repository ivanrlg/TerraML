# Pre-Commit Checklist - FuzzySat

> 6 pasos OBLIGATORIOS antes de cada commit.
> Ultima actualizacion: 2026-03-28

---

## PASO 1: Compilacion (NO NEGOCIABLE)

```bash
dotnet build
```

Si no compila, **NO** hacer commit. Corregir primero.

---

## PASO 2: Limites de Codigo

| Metrica | Limite |
|---------|--------|
| Lineas cambiadas | <200 (ideal), <500 (max aceptable) |
| Archivos cambiados | <10 (ideal) |
| Objetivo del commit | Exactamente UNO |

Si excede los limites, dividir en commits mas pequenos.

---

## PASO 3: Tests

```bash
dotnet test
```

- Si se modifico logica de negocio: los tests DEBEN pasar
- Si se agrego funcionalidad nueva: tests nuevos DEBEN existir
- Si un test falla: investigar y corregir ANTES de commit

---

## PASO 4: Verificar Staging

```bash
git status
git diff --staged
```

Verificar que NO esten en staging:
- [ ] Archivos de imagery (.tif, .tiff, .hdf, .nc, .jp2)
- [ ] Archivos sensibles (.env, appsettings.Development.json)
- [ ] Archivos binarios grandes
- [ ] Archivos temporales

---

## PASO 5: Actualizar Documentacion

- [ ] task/todo.md actualizado con progreso
- [ ] Si es nueva funcionalidad: README actualizado (si aplica)
- [ ] Si es fix: documentar en troubleshooting (si es recurrente)

---

## PASO 6: Commit

```bash
git add <archivos especificos>
git commit -m "tipo: descripcion

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

*Si alguno de los pasos 1-5 falla, NO proceder al paso 6.*
