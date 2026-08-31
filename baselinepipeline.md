from pathlib import Path

content = r"""# Basic .NET 8 Microservices Project – Foundation for Kubernetes/DevOps Roadmap

## Purpose

Create a **very simple .NET 8 Web API solution** that will become the foundation for a progressive learning project covering:

1. .NET 8 + Docker
2. Kubernetes
3. Helm
4. GitHub Actions
5. Octopus Deploy

The goal is **not** to build a complex business application.

The goal is to have a small, clean, working application that we can progressively package, containerize, deploy, automate, and promote through environments.

The application must therefore remain intentionally simple.

---

# 1. Technology Requirements

Use:

- .NET 8
- C#
- ASP.NET Core Web API
- Minimal APIs are preferred
- Swagger / OpenAPI
- Dependency Injection where appropriate
- No database initially
- No authentication initially
- No external cloud services initially
- No Entity Framework initially
- No unnecessary design patterns

The project must run locally with:

```bash
dotnet restore
dotnet build
dotnet run