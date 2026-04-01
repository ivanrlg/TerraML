# Security Policy

## Supported Versions

| Version | Supported          |
|---------|--------------------|
| 0.x     | :white_check_mark: |

## Reporting a Vulnerability

If you discover a security vulnerability in FuzzySat, please report it
**responsibly** by emailing **ivan@fuzzysat.dev**.

**Do NOT open a public GitHub issue for security vulnerabilities.**

### What to include

- Description of the vulnerability
- Steps to reproduce
- Potential impact
- Suggested fix (if any)

### Response timeline

- **Acknowledgment**: within 48 hours
- **Initial assessment**: within 1 week
- **Fix or mitigation**: as soon as practical, depending on severity

### Scope

FuzzySat is a scientific image classification tool. Security concerns most
likely involve:

- Path traversal in raster file loading
- Untrusted input in CLI arguments or JSON configuration
- Dependencies with known CVEs (GDAL bindings, ML.NET, Radzen)

### Dependency monitoring

We use [Dependabot](https://docs.github.com/en/code-security/dependabot) to
monitor NuGet dependencies for known vulnerabilities. Security updates are
prioritized and applied promptly.

## Acknowledgments

We appreciate responsible disclosure and will credit reporters (with permission)
in release notes.
