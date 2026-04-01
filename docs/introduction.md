# Welcome to the FinanceTracker Documentation

FinanceTracker is a comprehensive personal finance management application. This documentation covers the internal architecture, API endpoints, and domain logic for the platform.

## Architecture Overview

This project is built using **Clean Architecture** principles to ensure separation of concerns, testability, and maintainability. It is divided into five core layers:

- **Domain (`FinanceTracker.Domain`):** Contains enterprise logic, core entities, enums, and repository interfaces. This layer has no external dependencies.
- **Application (`FinanceTracker.Application`):** Contains business logic, DTOs, and service interfaces. It implements the CQRS pattern using MediatR for handling commands and queries.
- **Infrastructure (`FinanceTracker.Infrastructure`):** Handles external concerns, including Entity Framework Core data access, Identity management, JWT authentication, and external Exchange Rate API integrations.
- **API (`FinanceTracker.API`):** The ASP.NET Core Web API that serves as the entry point for frontend clients. It maps HTTP requests to Application layer commands/queries.
- **Web (`FinanceTracker.Web`):** The user-facing Blazor WebAssembly frontend application.

## Tech Stack Highlights

- **Backend:** .NET 10, ASP.NET Core Web API, C#
- **Frontend:** Blazor WebAssembly
- **Data & Auth:** Entity Framework Core, JWT Authentication
- **Patterns:** CQRS, Mediator Pattern, Dependency Injection
