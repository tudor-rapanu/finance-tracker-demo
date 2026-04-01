# Architecture Guide

FinanceTracker is built on **Clean Architecture** principles. Dependencies only flow inward — outer layers depend on inner layers, never the reverse.

```
┌──────────────────────────────────────┐
│           FinanceTracker.Web         │  Blazor WebAssembly
│           FinanceTracker.API         │  ASP.NET Core Web API
├──────────────────────────────────────┤
│       FinanceTracker.Infrastructure  │  EF Core · Identity · JWT · HTTP clients
├──────────────────────────────────────┤
│       FinanceTracker.Application     │  CQRS · MediatR · FluentValidation
├──────────────────────────────────────┤
│         FinanceTracker.Domain        │  Entities · Enums · Repository interfaces
└──────────────────────────────────────┘
```

---

## Layer Responsibilities

### Domain (`FinanceTracker.Domain`)

The innermost layer. No external dependencies.

**Entities**

| Entity        | Key Properties                                                                                    |
| ------------- | ------------------------------------------------------------------------------------------------- |
| `AppUser`     | Extends `IdentityUser`. Adds `FirstName`, `LastName`, `PreferredCurrency`, `CreatedAt`.           |
| `Transaction` | `Amount`, `Currency`, `AmountInBaseCurrency`, `Type`, `Category`, `Date`, `Description`, `Notes`. |
| `Budget`      | `Category`, `LimitAmount`, `Currency`, `Month`, `Year`.                                           |

All non-identity entities extend `BaseEntity`, which provides `Id` (GUID), `CreatedAt`, and `UpdatedAt`.

**Enums**

- `TransactionType` — `Income`, `Expense`
- `TransactionCategory` — income categories (`Salary`, `Freelance`, `Investment`, `Gift`, `OtherIncome`) and expense categories (`Housing`, `Food`, `Transport`, `Healthcare`, `Entertainment`, `Shopping`, `Utilities`, `Education`, `Travel`, `OtherExpense`)

**Repository Interfaces**

Defined in `IRepositories.cs` and implemented in Infrastructure:

- `ITransactionRepository` — CRUD + `GetTotalByTypeAndPeriodAsync`, `GetSpendingByCategoryAsync`
- `IBudgetRepository` — CRUD + `GetByUserAndPeriodAsync`
- `IUnitOfWork` — aggregates both repositories and exposes `SaveChangesAsync`

---

### Application (`FinanceTracker.Application`)

Contains business use cases. Depends only on Domain and `FinanceTracker.Contracts`.

**CQRS with MediatR**

Commands and queries are separated into folders per feature:

```
Application/
├── Auth/Commands/          RegisterCommand, LoginCommand, UpdatePreferredCurrencyCommand
├── Transactions/Commands/  CreateTransactionCommand
├── Transactions/Queries/   GetTransactionsQuery, GetDashboardQuery
├── Budgets/Commands/       CreateBudgetCommand
├── Admin/Commands/         SetUserRoleCommand
└── Admin/Queries/
```

Each command/query is a record that implements `IRequest<T>`. Handlers are registered with MediatR at startup.

**Validation Pipeline**

A `ValidationBehavior<TRequest, TResponse>` MediatR pipeline behaviour runs FluentValidation validators before every handler. Validation failures are collected and thrown as a single exception with messages joined by `"; "`. The `ExceptionHandlingMiddleware` catches this and returns a `400` response with an `{ "error": "..." }` body.

**Service Interfaces**

Defined in `IServices.cs`, implemented in Infrastructure:

- `IAuthService` — register, login, update preferred currency
- `IExchangeRateService` — fetch live rates, convert between currencies
- `ICurrentUserService` — exposes `UserId`, `Email`, `PreferredCurrency` from the current HTTP context claims
- `IAdminService` — list users, admin dashboard stats, set user role, recalculate transaction amounts

---

### Infrastructure (`FinanceTracker.Infrastructure`)

Implements all Application interfaces. Registered via `AddInfrastructure(IConfiguration)` extension method.

**Database**

`AppDbContext` extends `IdentityDbContext<AppUser>` and exposes `DbSet<Transaction>` and `DbSet<Budget>`. It auto-sets `UpdatedAt` on `SaveChangesAsync` for any modified `BaseEntity`.

Entity configuration is applied from the assembly using `ApplyConfigurationsFromAssembly`:

- `Transaction`: `Amount` and `AmountInBaseCurrency` with precision `(18,2)`, `Description` max 250 chars, `Notes` max 1000 chars, composite index on `(UserId, Date)`.
- `Budget`: `LimitAmount` with precision `(18,2)`, unique index on `(UserId, Category, Month, Year)` — one budget per category per month per user.

**Repository Pattern + Unit of Work**

`TransactionRepository` and `BudgetRepository` implement their respective Domain interfaces. `UnitOfWork` wraps both and delegates `SaveChangesAsync` to `AppDbContext`. Registered as `IUnitOfWork` (scoped).

**Identity & JWT**

`AuthService` uses ASP.NET Core Identity (`UserManager<AppUser>`) for registration and login. On success it generates a signed JWT with claims for `sub`, `email`, `preferred_currency`, and `role`. Password policy: minimum 8 characters, at least one digit, one uppercase letter, and a unique email.

JWT settings (`Issuer`, `Audience`, `SecretKey`) are read from configuration — set via `dotnet user-secrets` in development. Token validation enforces issuer, audience, lifetime, and signing key.

**Exchange Rate Service**

`ExchangeRateService` is registered with `AddHttpClient` and calls an external Exchange Rate API. Every transaction stores both the original `Amount`/`Currency` and a converted `AmountInBaseCurrency` (USD) for consistent cross-currency dashboard aggregation.

---

### API (`FinanceTracker.API`)

Thin HTTP entry point. Controllers delegate directly to MediatR — no business logic lives here.

**Controllers**

| Controller                | Responsibility                                      |
| ------------------------- | --------------------------------------------------- |
| `AuthController`          | Register, login, update preferred currency          |
| `TransactionsController`  | Create transaction, get transactions, get dashboard |
| `BudgetsController`       | Create budget, get budgets                          |
| `AdminController`         | List users, admin dashboard, set user role          |
| `ExchangeRatesController` | Fetch live exchange rates                           |

All endpoints except register/login require a valid JWT (`[Authorize]`). Admin endpoints additionally require the `Admin` role (`[Authorize(Roles = "Admin")]`).

**Exception Handling Middleware**

`ExceptionHandlingMiddleware` is the first middleware in the pipeline. It catches any unhandled exception and returns a structured JSON error:

```json
{ "error": "Descriptive message here" }
```

Validation exceptions become `400 Bad Request`. All others become `500 Internal Server Error`.

**OpenAPI / Swagger**

Uses native .NET 10 `Microsoft.AspNetCore.OpenApi` to generate the document at `/openapi/v1.json`. Swagger UI (`Swashbuckle.AspNetCore.SwaggerUI`) is mounted at `/swagger` in Development and pointed at that endpoint.

---

### Web (`FinanceTracker.Web`)

A Blazor WebAssembly SPA that communicates with the API entirely through `ApiClient`.

**ApiClient**

All HTTP calls return either `(T? Data, string? Error)` tuples (auth endpoints) or typed `ApiResult` / `ApiResult<T>` records that carry `Success`, `Data`, `Error`, and `StatusCode`. Errors are parsed from the `{ "error": "..." }` JSON body. No API call throws.

**Authentication**

JWT is stored in `localStorage` via JS interop. `IsAuthenticatedAsync()` checks for token presence. On a `401` response, the token is cleared and the user is redirected to `/login`.

**Scoped CSS**

Each page has a corresponding `.razor.css` file. Styles are bundled into `FinanceTracker.Web.styles.css` at build time. The `::deep` combinator is used in `MainLayout.razor.css` to reach `NavLink`-rendered anchor elements through Blazor's CSS isolation boundary.
