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

Multipart file-upload actions (`/api/batches/upload`, `/api/policies/cancel-upload`) need a small
Swashbuckle workaround to appear at all: Swashbuckle cannot generate an operation for an action
parameter explicitly bound `[FromForm] IFormFile`, so the batch upload binds through a single
`[FromForm] BatchUploadRequest` wrapper (`MotorPortal.API/Models/BatchUploadRequest.cs`) instead of
separate scalar + file parameters, the cancel-upload file parameter drops the redundant `[FromForm]`
attribute (ASP.NET Core infers `IFormFile` as form-bound automatically), and
`MotorPortal.API/Swagger/FileUploadOperationFilter.cs` renders the resulting multipart/form-data
request body. None of this changes the wire format — the same form field names are still used.

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

## Business-logic endpoints

All endpoints below require a JWT (`Authorization: Bearer <token>`). The DB-side PL/pgSQL
functions/procedures (`fn_calculate_net_premium`, `fn_calculate_gst`, `fn_generate_proposal_no`,
`fn_generate_policy_no`, `sp_process_batch_validation`, `sp_tag_payment`,
`sp_advance_batch_status`) are called via raw SQL from `MotorPortal.Infrastructure.Services.PgFunctions`
rather than re-implemented in C#.

| Method | Path | Purpose |
|---|---|---|
| POST | `/api/batches/upload` | Multipart Excel upload (`productId`, `functionId`, `file`). Validates structure/rows via ClosedXML; inserts nothing if invalid. Creates `batch_master` (UPLOADED) + `batch_detail` (PENDING) rows on success. |
| GET | `/api/batches/{id}/sample-template` | Downloads a sample `.xlsx` with the required header row + example rows. |
| POST | `/api/batches/{id}/process` | Runs validation (`sp_process_batch_validation`) then premium → GST → proposal generation for all VALID records, advancing `batch_master.status` through PREMIUM_CALCULATED → GST_CALCULATED → PROPOSAL_CREATED. |
| GET | `/api/batches/{id}/status` | Current batch row plus live per-stage counts (valid/invalid, premium, GST, proposals, processed payments, policies). |
| GET | `/api/batches/{id}/invalid-records` | Lists `invalid_records` for the batch. |
| DELETE | `/api/batches/{id}/invalid-records` | Clears `invalid_records` for the batch and zeroes `batch_master.invalid_records`. |
| POST | `/api/batches/{id}/payments` | Tags payment (`sp_tag_payment`) for every proposal in the batch without a processed payment yet. Insufficient-CD-balance and other DB errors are caught per-case and reported, not thrown as 500s. Generates `policy_master` rows for newly-successful payments and advances batch status through PAYMENT_PENDING → PAYMENT_PROCESSED → POLICY_CREATED as applicable. |
| POST | `/api/batches/{id}/bulk-print` | Generates a certificate PDF for every policy in the batch and advances status to PRINTED. |
| POST | `/api/policies/{id}/certificate` | (Re)generates the policy's certificate PDF (QuestPDF) under `wwwroot/certificates/{id}.pdf` and upserts `policy_certificate`. |
| GET | `/api/policies/{id}/certificate` | Streams the certificate PDF, generating it on-demand if missing. |
| GET | `/api/batches?fromDate=&toDate=` | Per-batch summary (optional date-range filter on `batch_master.created_on`): total/valid/invalid records, records still pending processing (VALID but no proposal yet), proposals without a PROCESSED payment, and proposals with one. |
| GET | `/api/batches/summary-counters?fromDate=&toDate=` | The same counters summed across every batch matching the date filter, for dashboard cards. |
| GET | `/api/master-policies` | Dropdown list of master policies (`masterPolicyId`, `masterPolicyNo`, `customerNo`, `cdbgNo`, `productId`). |
| GET | `/api/master-policies/{id}/cd-balance` | Live read of `master_policy.cd_balance` for one master policy. |
| POST | `/api/reports/policy-issue` | Body `{ fromDate, toDate }`. Queries `"SGInsurance".vw_policy_issue_report` filtered by Issued Date, writes a `report_log` row (`report_type = POLICY_ISSUE`), and streams a generated `.xlsx` (ClosedXML) with the view's exact columns. |
| GET | `/api/policies/search?engineNo=&chassisNo=&tcNo=&policyNo=` | Searches `policy_master` (joined through `proposal_master` → `batch_detail` for `tcNo`, which only exists on `batch_detail`). At least one parameter is required (400 otherwise). |
| POST | `/api/policies/cancel-upload` | Multipart Excel upload with a single `POLICY_NO` column. Cancels each existing, not-already-cancelled `policy_master` row (status → `CANCELLED`, one `audit_log` row per success) and reports the rest as rejected (`"Policy not found"` / `"Policy already cancelled"`) — never all-or-nothing. |

### Premium/GST configuration

`appsettings.json` carries the per-product premium inputs and the GST rate used by
`ProcessBatchAsync` — these are read from configuration, never hardcoded:

```json
"GstRate": 18.0,
"PremiumRules": {
  "CLASS_E": { "BasePremium": 12000, "AddonPremium": 800, "Discount": 200 },
  "CLASS_F": { "BasePremium": 15000, "AddonPremium": 1000, "Discount": 300 },
  "EICHER":  { "BasePremium": 20000, "AddonPremium": 1500, "Discount": 500 }
}
```

The product code (e.g. `CLASS_E`) is looked up from `batch_master.product_id -> product_master.product_code`.

### Mock PF gateway

`IPfGatewayService` / `MockPfService` (`MotorPortal.Infrastructure/Services/MockPfService.cs`)
simulates the external payment-facilitator confirmation call (`Task.Delay` + a generated
confirmation token) ahead of the DB-side `sp_tag_payment` call. Swapping in a real HTTP-backed
gateway later only requires a new class implementing `IPfGatewayService`.

## Progress

- [x] Bootstrap (ground rules, README, .gitignore)
- [x] Solution/project scaffolding, EF Core + Npgsql, JWT auth, Swagger, health check
- [x] Excel upload, batch validation/premium/GST/proposal orchestration
- [x] Payment tagging, policy generation, certificate PDF, bulk print
- [x] Batch summary, CD balance, reports, search & print, policy cancel, audit logging
