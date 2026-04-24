# DKP System — Claude Code Instructions

## Project Overview

DKP (Dragon Kill Points) management system for MMORPG guilds. Built with **Blazor Server** on **.NET 8** and **PostgreSQL**. Allows guilds to run events, assign DKP points, and auction items using a bidding system.

## Tech Stack

| Layer | Technology |
|---|---|
| Framework | .NET 8 / Blazor Server (Interactive Server render mode) |
| Database | PostgreSQL |
| Data Access | Dapper (no EF Core — raw SQL only) |
| Identity | ASP.NET Core Identity + custom `DapperUserStore` |
| Auth | Cookie authentication via `IdentityConstants.ApplicationScheme` |
| Testing | xUnit (`DkpSystem.Tests/`) |
| Deployment | Docker / Railway / Render |

## Architecture

```
DkpSystem/
├── Components/          # Blazor UI (.razor files)
│   └── Pages/           # Routable pages (Admin, Auctions, Auth, Events, etc.)
├── Data/
│   ├── Repositories/    # Data access — one class per aggregate
│   └── Identity/        # Custom Identity store (DapperUserStore)
├── Models/              # Plain C# domain models (no annotations beyond what's needed)
├── Services/            # Business logic — orchestrate repositories
├── Migrations/          # Raw SQL migration scripts, applied at startup
└── Program.cs           # DI composition root + middleware pipeline
```

**Flow:** Blazor component → Service → Repository → Dapper → PostgreSQL

Services contain all business rules. Repositories are pure data access with no logic. Components call services, never repositories directly.

## C# and .NET Coding Standards

### General

- Target **C# 12** features: primary constructors, collection expressions, pattern matching.
- Enable **nullable reference types** (`<Nullable>enable</Nullable>` is set). Always handle nullability explicitly — no `!` null-forgiving operator unless provably safe.
- Use `async`/`await` throughout. Never block async code with `.Result` or `.Wait()`.
- Prefer `IEnumerable<T>` in return types when callers only need to iterate. Use `List<T>` only when the caller needs `Count`, indexing, or mutation.
- Keep methods focused: one responsibility per method. Extract private helpers rather than growing methods.
- Use `var` when the type is obvious from the right-hand side. Prefer explicit types for primitives and when the inferred type would be ambiguous.
- Tuple return types `(bool Success, string ErrorMessage, T? Result)` are the pattern used in services for operations that can fail — keep it consistent.

### Naming

- PascalCase: classes, methods, properties, public fields, constants.
- camelCase with `_` prefix: private fields (`_connectionFactory`, `_auctionRepository`).
- Suffix `Async` on every async method.
- Repository methods: `GetXByYAsync`, `CreateXAsync`, `UpdateXAsync`, `DeleteXAsync`.
- Service methods: use verb-noun that expresses intent (`PlaceBidAsync`, `DeliverItemAsync`, `CloseAuctionAsync`).

### Dependency Injection

- Register services and repositories as **Scoped** (matches Blazor Server circuit lifetime).
- `DbConnectionFactory` is **Singleton** — it holds the connection string only, connections are created per-call.
- Never use `ServiceLocator` pattern. Always inject via constructor.
- Constructor injection only — no property injection.

### XML Documentation

Public methods and classes carry `/// <summary>` XML docs. Keep them factual and concise — describe what the method does and what each parameter means. Don't describe implementation details.

## Data Access (Dapper)

- Always use `const string sql` for SQL strings — never inline interpolated strings.
- Use `@ParameterName` placeholders — never string concatenation in SQL (SQL injection risk).
- Open connections with `await _connectionFactory.CreateConnectionAsync()` inside a `using` block.
- For operations that touch multiple tables (e.g., deliver item → deduct DKP), always wrap in an explicit transaction with `try/catch/rollback`.
- PostgreSQL column naming is `snake_case`. Dapper maps to C# PascalCase properties by column alias in the query or via column names that match — keep models aligned with the schema.
- Use `QuerySingleAsync` when exactly one row is expected. Use `QuerySingleOrDefaultAsync` for optional rows. Use `QueryAsync` for sets.
- Use `ExecuteScalarAsync<T>` for scalar results (COUNT, single value).

## Blazor Server Patterns

- Components use `@inject` for services, never repositories.
- State management is local to the component (`private` fields). No global state service unless justified.
- Use `@code` blocks, not code-behind `.cs` files, for component logic.
- Handle exceptions from service calls inside the component — show user-facing error messages, log to console in dev.
- Authentication state is accessed via `[CascadingParameter] Task<AuthenticationState> AuthState` or `AuthenticationStateProvider`.
- Role checks: `"admin"` and `"raider"` are the two roles. Use `[Authorize(Policy = "AdminOnly")]` or `[Authorize(Policy = "RaiderOrAdmin")]` on routable pages.
- For lists that need real-time freshness, reload by calling the service method again and calling `StateHasChanged()`.

## Authorization

- Policies defined in `Program.cs`: `"AdminOnly"` requires role `admin`; `"RaiderOrAdmin"` requires `raider` or `admin`.
- Never hardcode role strings inline in components — reference the policy names.
- Sensitive operations (create/close auctions, deliver items, create events) require `"AdminOnly"`.
- Bidding and viewing requires `"RaiderOrAdmin"`.

## Database Migrations

- SQL files in `DkpSystem/Migrations/` are applied in order at startup by `DatabaseMigrator`.
- New migrations go in new numbered files — never modify existing migration files.
- Migrations must be idempotent (`CREATE TABLE IF NOT EXISTS`, `ALTER TABLE ... ADD COLUMN IF NOT EXISTS`, etc.).
- PostgreSQL types: use `UUID` for IDs (not serial/int), `TIMESTAMPTZ` for timestamps (always UTC), `INTEGER` for DKP amounts.

## Domain Rules (Critical)

- **DKP balance integrity**: All operations that modify `users.dkp_balance` must run inside a database transaction.
- **Bid validation**: A raider's total active bids across all open auctions cannot exceed their `dkp_balance`.
- **Auction lifecycle**: `open` → `closed`. Once closed, no new bids. Delivery of items deducts DKP within transactions.
- **Bid type priority**: `Main` > `Alt` > `Greed` for tie-breaking. Secondary: bid amount. Tertiary: timestamp (earlier wins).
- **Invitation codes**: New users require a valid invitation code to register.

## Testing

- Test project: `DkpSystem.Tests/` using xUnit.
- Tests hit the actual database — do not mock the database layer.
- Unit tests for service logic can mock repositories using constructor injection.
- Test method naming: `MethodName_Scenario_ExpectedResult`.

## Running Locally

```bash
# Start PostgreSQL
docker-compose up -d

# Run the app
cd DkpSystem && dotnet run

# Run tests
cd DkpSystem.Tests && dotnet test
```

Connection string in `appsettings.Development.json` → `ConnectionStrings:DefaultConnection`.

## What NOT to Do

- Do not use the same page for C# code and razor code, separate logic from front end code.
- Do not use Entity Framework Core — this project uses Dapper intentionally.
- Do not use `dynamic` types with Dapper — always map to typed models.
- Do not call repositories from Blazor components — go through services.
- Do not store sensitive data in Blazor component state that persists across users.
- Do not add `[Required]`, `[MaxLength]`, or EF data annotations to models — validation lives in services.
- Do not use `.Result` or `.Wait()` on Tasks in Blazor Server (deadlock risk).
