# 💰 FinanceTracker

A full-stack personal finance web application built with **ASP.NET Core 10**, **Blazor WebAssembly**, and **Entity Framework Core** — following **Clean Architecture** principles.

---

## 🏗️ Architecture

```
FinanceTracker/
├── src/
│   ├── FinanceTracker.Domain/           # Entities, Enums, Repository Interfaces
│   ├── FinanceTracker.Application/      # CQRS Commands/Queries (MediatR), DTOs, Service Interfaces
│   ├── FinanceTracker.Infrastructure/   # EF Core, Identity, JWT Auth, Exchange Rate API
│   ├── FinanceTracker.API/              # ASP.NET Core Web API (Controllers, Middleware)
│   └── FinanceTracker.Web/              # Blazor WebAssembly Frontend
```

### Layer Responsibilities

| Layer | Responsibility |
|---|---|
| **Domain** | Core business entities and repository contracts. Zero dependencies. |
| **Application** | Use cases via CQRS (MediatR). Depends only on Domain. |
| **Infrastructure** | EF Core, ASP.NET Identity, JWT, HTTP clients. Implements Application interfaces. |
| **API** | HTTP endpoints, auth middleware, Swagger. Wires up DI. |
| **Web** | Blazor WASM frontend. Calls the API via `ApiClient`. |

---

## ✨ Features

- ✅ **JWT Authentication** — Register, login, protected endpoints
- ✅ **Transaction Tracking** — Income & expenses with categories
- ✅ **Monthly Budgets** — Set spending limits per category
- ✅ **Dashboard** — Net balance, top spending categories, budget status
- ✅ **Multi-currency** — Transactions stored in original currency + converted to USD
- ✅ **Exchange Rate API** — Live rates via ExchangeRate-API (with graceful fallback)
- ✅ **Clean Architecture** — Domain → Application → Infrastructure → API
- ✅ **CQRS with MediatR** — Commands and queries separated
- ✅ **Repository + Unit of Work Pattern**
- ✅ **Global Exception Middleware**
- ✅ **Swagger UI** with JWT auth support

---

## 🚀 Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) or SQL Server LocalDB (included with Visual Studio)
- (Optional) [ExchangeRate-API key](https://www.exchangerate-api.com/) — free tier available

---

### 1. Clone & Open

```bash
git clone <your-repo-url>
cd FinanceTracker
```

Open `FinanceTracker.sln` in **Visual Studio 2022** or **Rider**.

---

### 2. Configure the API Secrets

This project is configured for `dotnet user-secrets` (local machine only).

Option A: Interactive helper script

```bash
./scripts/setup-api-secrets.sh
```

Option B: Set values manually

```bash
cd src/FinanceTracker.API

dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=(localdb)\\mssqllocaldb;Database=FinanceTrackerDb;Trusted_Connection=True;"
dotnet user-secrets set "JwtSettings:SecretKey" "YOUR_LONG_RANDOM_SECRET_32_CHARS_MINIMUM"
dotnet user-secrets set "JwtSettings:Issuer" "FinanceTrackerAPI"
dotnet user-secrets set "JwtSettings:Audience" "FinanceTrackerClient"
dotnet user-secrets set "JwtSettings:ExpiryHours" "24"
dotnet user-secrets set "ExchangeRateApi:ApiKey" "YOUR_API_KEY"
```

Verify:

```bash
dotnet user-secrets list
```

> Secrets are intentionally blank in `src/FinanceTracker.API/appsettings.json` so sensitive values are not committed.

---

### 3. Run Migrations

In the **Package Manager Console** (or terminal from the solution root):

```bash
cd src/FinanceTracker.API

dotnet ef migrations add InitialCreate \
  --project ../FinanceTracker.Infrastructure \
  --startup-project .

dotnet ef database update \
  --project ../FinanceTracker.Infrastructure \
  --startup-project .
```

---

### 4. Run the API

```bash
cd src/FinanceTracker.API
dotnet run
```

Swagger UI will be available at: `https://localhost:7100/swagger`

---

### 5. Run the Blazor Frontend

In a new terminal:

```bash
cd src/FinanceTracker.Web
dotnet run
```

Frontend runs at: `https://localhost:7200`

> Make sure the API base URL in `src/FinanceTracker.Web/Program.cs` matches your API port.

---

## 🔑 API Endpoints

### Auth
| Method | Endpoint | Description |
|---|---|---|
| POST | `/api/auth/register` | Register a new user |
| POST | `/api/auth/login` | Login and get JWT |

### Transactions
| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/transactions` | List transactions (filter by `?month=&year=`) |
| GET | `/api/transactions/dashboard` | Dashboard summary |
| POST | `/api/transactions` | Create a transaction |
| DELETE | `/api/transactions/{id}` | Delete a transaction |

### Budgets
| Method | Endpoint | Description |
|---|---|---|
| POST | `/api/budgets` | Create a monthly budget |
| DELETE | `/api/budgets/{id}` | Delete a budget |

### Exchange Rates
| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/exchangerates?baseCurrency=USD` | Get live exchange rates |

---

## 🛠️ Key Design Patterns

### CQRS with MediatR
Commands mutate state; queries read state. Each lives in its own handler class.

```csharp
// Command
var result = await _mediator.Send(new CreateTransactionCommand(dto));

// Query
var result = await _mediator.Send(new GetDashboardQuery(month, year));
```

### Result<T> Pattern
Handlers return `Result<T>` instead of throwing exceptions for expected failures:

```csharp
return Result<TransactionDto>.Failure("Transaction not found.");
return Result<TransactionDto>.Success(dto);
```

### Repository + Unit of Work
```csharp
await _uow.Transactions.AddAsync(transaction);
await _uow.SaveChangesAsync();
```

---

## 📈 Next Steps & Ideas

Here are features you can add to extend the project:

- [ ] **Refresh token rotation** — persist refresh tokens to DB and rotate on use
- [ ] **Pagination** — add page/pageSize params to transaction queries
- [ ] **Charts** — add a charting library (e.g. Radzen, Chart.js via JS interop)
- [ ] **Export to CSV/PDF** — let users download their transaction history
- [ ] **Recurring transactions** — auto-generate monthly salary/rent entries
- [ ] **Email notifications** — alert users when a budget hits 80%
- [ ] **Unit tests** — add xUnit tests for Application layer handlers
- [ ] **Docker** — add Dockerfile and docker-compose for the API + SQL Server
- [ ] **CI/CD** — add a GitHub Actions workflow to build and test on push

---

## 📦 NuGet Packages Used

| Package | Purpose |
|---|---|
| `MediatR` | CQRS dispatcher |
| `FluentValidation` | Input validation |
| `Microsoft.EntityFrameworkCore.SqlServer` | EF Core SQL Server provider |
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | User management |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | JWT auth middleware |
| `Swashbuckle.AspNetCore.SwaggerUI` | Swagger UI |
| `Blazored.LocalStorage` | Token storage in Blazor WASM |

---

## 📄 License

MIT — free to use for your portfolio!
