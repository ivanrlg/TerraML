# Reglas de Seguridad Git - FuzzySat

> Proceso de PR, review de bots, y reglas de merge.
> Ultima actualizacion: 2026-03-28

---

## 1. Estrategia de Branches

| Branch | Proposito | Merge a |
|--------|-----------|---------|
| `main` | Estable, release-ready | - |
| `feature/*` | Features nuevas | main |
| `fix/*` | Bug fixes | main |
| `epic-NNN-*` | Branches de epic (multi-PR) | main |

---

## 2. Proceso de Pull Request

### Paso 1: Crear PR
```bash
gh pr create --title "tipo: descripcion corta" --body "..."
```

**Formato de titulo**: `tipo: descripcion` donde tipo es:
- `feat`: Feature nueva
- `fix`: Bug fix
- `chore`: Mantenimiento, scaffolding
- `docs`: Solo documentacion
- `test`: Solo tests
- `refactor`: Refactoring sin cambio de comportamiento

### Paso 2: Esperar Review de Bots (5-15 min)
- Claude Code Review
- GitHub Copilot

**NUNCA** mergear antes de que los bots terminen su review.

### Paso 3: Corregir Issues de Bots
- Revisar TODOS los comentarios de bots
- Corregir issues P1 (criticos) inmediatamente
- Documentar decisiones sobre P2 (sugerencias) que no se apliquen

### Paso 4: Esperar Aprobacion del Usuario
- Ver GOLDEN_RULES.md Regla #1
- Solo aprobaciones EXPLICITAS son validas

### Paso 5: Merge
- Solo despues de pasos 2, 3 y 4
- Preferir squash merge para mantener historial limpio

---

## 3. Comandos Git Prohibidos

```bash
# PROHIBIDO sin aprobacion explicita del usuario:
git push --force
git reset --hard
git checkout -- .
git clean -fd
git branch -D
git rebase -i  # (interactivo, no soportado en automatizacion)
```

---

## 4. Comandos Git Seguros

```bash
# Siempre seguros:
git status
git diff
git log
git branch
git stash
git stash pop
git fetch
git pull
```

---

## 5. Formato de Commit

```
tipo: descripcion corta (max 72 chars)

Descripcion detallada si es necesaria.
- Bullet points para cambios multiples.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>
```

---

## 6. Formato de PR Body

```markdown
## Summary
- Punto 1
- Punto 2

## Test plan
- [ ] Tests unitarios pasan
- [ ] Build exitoso
- [ ] Verificacion manual (si aplica)

Generated with Claude Code
```

---

## 7. Reglas de Staging

- Preferir `git add <archivos especificos>` sobre `git add -A`
- NUNCA commitear archivos sensibles (.env, credentials)
- NUNCA commitear archivos de imagery satelital (.tif, .hdf, etc.)
- Verificar `git status` antes de cada commit

---

*Seguir estas reglas previene perdida de trabajo y mantiene el historial limpio.*
