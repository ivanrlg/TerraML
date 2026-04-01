# Contributing to FuzzySat

Thank you for your interest in contributing to FuzzySat! This guide will help
you get started.

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Git
- A code editor (VS Code or Visual Studio 2022 recommended)

### Setup

```bash
git clone https://github.com/ivanrlg/FuzzySat.git
cd FuzzySat
dotnet build
dotnet test
```

## Development Workflow

FuzzySat follows a structured workflow:

1. **Fork & branch** -- create a feature branch from `main`
2. **Small commits** -- each commit should have a single objective, under 200
   lines changed
3. **Build often** -- run `dotnet build` after every change
4. **Test** -- run `dotnet test` before pushing
5. **Pull Request** -- open a PR with a clear description

### Branch naming

| Type        | Pattern                        | Example                         |
|-------------|--------------------------------|---------------------------------|
| Feature     | `feature/<short-description>`  | `feature/bell-membership`       |
| Bug fix     | `fix/<short-description>`      | `fix/kappa-edge-case`           |
| Docs        | `docs/<short-description>`     | `docs/api-reference`            |
| Refactor    | `refactor/<short-description>` | `refactor/inference-pipeline`   |

### Commit messages

Write clear, descriptive commit messages:

```
Add Gaussian membership function with sigma validation

- Implements IMembershipFunction for Gaussian curve
- Throws ArgumentException when sigma <= 0
- Includes 12 unit tests against thesis reference values
```

## Architecture Rules

FuzzySat has strict separation of concerns. Please respect these boundaries:

| Code type                  | Belongs in             | Never in            |
|----------------------------|------------------------|---------------------|
| Interfaces, fuzzy engine   | `FuzzySat.Core/`       | CLI, Web            |
| CLI commands               | `FuzzySat.CLI/`        | Core, Web           |
| Blazor pages               | `FuzzySat.Web/`        | Core, CLI           |
| Unit tests                 | `tests/`               | `src/`              |

## Testing

- **Framework**: xUnit + FluentAssertions
- **Requirement**: all core algorithms must be validated against known thesis
  values
- **Run tests**: `dotnet test`

### Writing tests

```csharp
[Fact]
public void Gaussian_AtCenter_ReturnsOne()
{
    var mf = new GaussianMembershipFunction(center: 100, sigma: 15);

    mf.Evaluate(100).Should().Be(1.0);
}
```

## Pull Request Process

1. Ensure `dotnet build` succeeds with no warnings
2. Ensure `dotnet test` passes (all tests green)
3. Fill out the PR template with a clear description
4. Wait for automated review (GitHub Copilot + CI checks)
5. Address any feedback from reviewers
6. A maintainer will merge after approval

## Code Style

- Follow standard C# conventions
- Use `var` when the type is obvious from the right side
- Prefer expression-bodied members for simple getters
- No `#region` blocks
- XML doc comments on public APIs

## Reporting Issues

Use the [GitHub issue tracker](https://github.com/ivanrlg/FuzzySat/issues)
with the appropriate template:

- **Bug report** -- something is broken
- **Feature request** -- suggest an improvement
- **Question** -- ask about usage or architecture

## License

By contributing, you agree that your contributions will be licensed under the
[MIT License](LICENSE).
