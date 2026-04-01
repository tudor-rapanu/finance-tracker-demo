# Welcome to FinanceTracker

FinanceTracker is a comprehensive personal finance management application. This developer portal contains the internal architecture guidelines, setup instructions, and auto-generated API documentation for the platform.

---

## Quick Navigation

- **[Getting Started](docs/getting-started.md)**: Step-by-step instructions for cloning the repo, restoring dependencies, and running the full stack locally.
- **[Architecture Guide](articles/architecture.md)**: Deep dive into our Clean Architecture implementation and CQRS patterns.
- **[Introduction](docs/introduction.md)**: High-level overview of the project.
- **[Code API Reference](api/index.md)**: Browse the auto-generated documentation for our internal .NET classes, interfaces, and methods.

---

## Architecture Overview

This project strictly adheres to **Clean Architecture** principles to ensure separation of concerns, testability, and maintainability.

- **`FinanceTracker.Domain`**: The core of the system. Contains enterprise logic, entities, enums, and repository interfaces. Has no external dependencies.
- **`FinanceTracker.Application`**: Contains business logic and DTOs. Implements the CQRS pattern using MediatR for handling commands and queries.
- **`FinanceTracker.Infrastructure`**: Handles external concerns, including Entity Framework Core data access, JWT authentication, and the Exchange Rate API.
- **`FinanceTracker.API`**: The ASP.NET Core Web API that maps HTTP requests to Application layer commands and queries.
- **`FinanceTracker.Web`**: The user-facing Blazor WebAssembly frontend client.

---

## Tech Stack

- **Framework:** .NET 10
- **Backend:** ASP.NET Core Web API, C#
- **Frontend:** Blazor WebAssembly
- **Data & Auth:** Entity Framework Core (SQL Server), JWT Authentication
- **Core Patterns:** CQRS, Mediator Pattern, Dependency Injection
