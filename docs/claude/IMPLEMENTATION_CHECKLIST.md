# Checklist de Implementacion - FuzzySat

> Checklist por tipo de componente. Usar antes de crear cualquier archivo nuevo.
> Ultima actualizacion: 2026-03-28

---

## Al Crear una Clase en FuzzySat.Core

- [ ] Namespace correcto: `FuzzySat.Core.[Subcarpeta]`
- [ ] Nullable reference types habilitado
- [ ] Interface definida si el componente es inyectable
- [ ] XML docs en metodos publicos
- [ ] Unit tests creados en `tests/FuzzySat.Core.Tests/[Subcarpeta]/`
- [ ] Tests usan valores conocidos de la tesis cuando aplica
- [ ] Compila: `dotnet build`
- [ ] Tests pasan: `dotnet test`

---

## Al Crear un Comando CLI

- [ ] Clase en `FuzzySat.CLI/Commands/`
- [ ] Argumentos definidos con System.CommandLine
- [ ] Help text descriptivo para cada argumento
- [ ] Salida formateada con Spectre.Console
- [ ] Manejo de errores con mensajes claros al usuario
- [ ] Compila: `dotnet build`

---

## Al Crear un API Endpoint

- [ ] Controller en `FuzzySat.Api/Controllers/`
- [ ] Ruta RESTful: `api/[recurso]`
- [ ] Tipos de respuesta documentados
- [ ] Manejo de errores con ProblemDetails
- [ ] Compila: `dotnet build`

---

## Al Crear una Pagina Blazor

- [ ] Pagina en `FuzzySat.Web/Components/Pages/`
- [ ] Directiva `@page` con ruta correcta
- [ ] Componentes compartidos en `Components/Shared/`
- [ ] CSS isolation si se necesita estilo custom
- [ ] Responsive: funciona en 1024x768 minimo
- [ ] Compila: `dotnet build`

---

## Verificacion Final (SIEMPRE)

- [ ] `dotnet build` exitoso
- [ ] `dotnet test` exitoso (si hay tests)
- [ ] Cambios son <200 lineas
- [ ] task/todo.md actualizado
- [ ] No hay archivos de imagery en staging
- [ ] No hay archivos sensibles en staging

---

*Consultar GOLDEN_RULES.md para reglas inquebrantables adicionales.*
