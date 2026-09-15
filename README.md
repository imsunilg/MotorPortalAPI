# MotorPortalAPI

ASP.NET Core Web API backend for the Motor Portal bulk motor-insurance policy issuance and processing system.

Part of the 4-repo Motor Portal system: MotorPortalAPI, MotorPortalWEB, MotorPortalDB, MotorPortalDOC.

## Architecture

.NET 8 solution with a layered structure:

- **MotorPortal.API** — Controllers, middleware, DI/extension wiring, `Program.cs`, configuration.
- **MotorPortal.Application** — DTOs, service interfaces, application services, validators.
- **MotorPortal.Domain** — Entity classes, enums, constants (batch status lifecycle, user status).
- **MotorPortal.Infrastructure** — EF Core `AppDbContext`, repositories, JWT/auth services, migrations tooling.

Project references: `API -> Application`, `API -> Infrastructure`, `Infrastructure -> Application -> Domain`.

The database (PostgreSQL, schema `SGInsurance`) is owned by the **MotorPortalDB** repo and is
**database-first**: all tables/columns already exist with lowercase snake_case names. EF Core is
configured to map to that exact schema via Fluent API (`HasDefaultSchema("SGInsurance")` plus
explicit `.ToTable(...)` / `.HasColumnName(...)` on every entity). The app does **not** run EF Core
migrations against this database — `dotnet-ef` is kept available only as tooling for future
schema-diff work.

## Configuration

Configuration is read from `appsettings.json` (checked in, non-sensitive defaults) and
`appsettings.Development.json` (local dev secrets — connection string and JWT signing key).

**`appsettings.Development.json` is intentionally gitignored** (see `.gitignore`) — this is a
disposable local dev environment, and there is no production deployment yet, so we do not want a
real (even if low-stakes) connection string/secret sitting in git history long-term. Instead, a
committed template, **`MotorPortal.API/appsettings.Development.json.example`**, documents the real
local dev defaults that work against the seeded local database. This is a deliberate tradeoff for
this phase, not a production secrets posture — do not carry it forward once real environments exist.

To configure a fresh clone:

```bash
cp MotorPortal.API/appsettings.Development.json.example MotorPortal.API/appsettings.Development.json
```

The example file already matches the documented local Postgres setup:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=SGInsuranceDB;Username=postgres;Password=284228"
  },
  "Jwt": {
    "Issuer": "MotorPortalAPI",
    "Audience": "MotorPortalWEB",
    "SigningKey": "dev-only-super-secret-signing-key-change-me-32chars-min!",
    "ExpiryMinutes": 60
  }
}
```

Adjust the connection string / signing key if your local Postgres instance differs.

## Running

Prerequisites: .NET 8 SDK, PostgreSQL 16 running locally with the `SGInsuranceDB` database and
`SGInsurance` schema already created and seeded (owned/maintained by MotorPortalDB).

```bash
dotnet restore
dotnet build
dotnet run --project MotorPortal.API
```

The API listens on the URL printed at startup (see `MotorPortal.API/Properties/launchSettings.json`,
typically `http://localhost:5287` when run with `dotnet run`).

### Swagger UI

Open `http://localhost:<port>/swagger` in a browser (Development environment only). Use the
**Authorize** button and paste a JWT (`Bearer <token>`) obtained from `POST /api/auth/login` to
call protected endpoints from the UI.

### Verifying it works

```bash
# Health check — real DB round trip, no auth required
curl http://localhost:5287/api/health

# Login — returns a real JWT
curl -X POST http://localhost:5287/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin123"}'

# Protected endpoint without a token -> 401
curl -i http://localhost:5287/api/secure-ping

# Protected endpoint with a token -> 200
curl http://localhost:5287/api/secure-ping -H "Authorization: Bearer <token>"
```

`GET /api/secure-ping` is a trivial `[Authorize]`-protected endpoint added specifically to prove
JWT authentication/authorization works end-to-end in this scaffolding phase, ahead of the real
business-logic endpoints landing in later phases.

## Auth

- `POST /api/auth/login` — validates against `user_master` (bcrypt-verified `password_hash`,
  requires `status = 'A'`), returns a signed JWT (`user_id`, `username`, `role` claims) plus
  username/role/expiry.
- `POST /api/auth/logout` — stateless; returns 200 for the client to discard its token.
- Every controller other than `AuthController` and `GET /api/health` requires a valid JWT
  (`[Authorize]`).

## Error handling & logging

A global exception-handling middleware (`MotorPortal.API/Middleware/ExceptionHandlingMiddleware.cs`)
catches unhandled exceptions and returns a consistent
`{ "statusCode": ..., "message": ..., "traceId": ... }` JSON body — `BusinessRuleException`
(for future business-rule violations) and `ArgumentException` map to 400, everything else to 500.
Structured JSON logging is provided via Serilog, including the ASP.NET Core request id/trace id on
every request (`UseSerilogRequestLogging`).

## Progress

- [x] Bootstrap (ground rules, README, .gitignore)
- [x] Solution/project scaffolding, EF Core + Npgsql, JWT auth, Swagger, health check
- [ ] Excel upload, batch validation/premium/GST/proposal orchestration
- [ ] Payment tagging, policy generation, certificate PDF, bulk print
- [ ] Batch summary, CD balance, reports, search & print, policy cancel, audit logging
