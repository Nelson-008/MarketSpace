# MarketPulse

An Indian market intelligence platform. Product baseline: *MarketPulse Product & Domain Specification v1.0 (7 Nov 2026)*.

> **Status: pre-alpha. No licensed market-data provider is configured. All market data is SIMULATED.**
> There is no live trading and no broker order routing; paper trading only. Nothing here is investment advice.

## Prerequisites
- .NET SDK 10.0.4xx (pinned in `global.json`)
- Node.js 22 LTS (for the web app, from Phase 1 task 12)
- SQL Server (local SQL Express is fine for development)
- Docker Desktop (Redis, Mailpit, and Testcontainers for integration tests)

## Getting started
One-time setup (the JWT signing key lives in your user profile, never in the repo):
```powershell
# 48 random bytes, base64
$b = New-Object byte[] 48; [Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($b)
dotnet user-secrets set "Auth:Jwt:SigningKey" ([Convert]::ToBase64String($b)) --project src/Hosts/MarketPulse.Api
dotnet tool restore
cd src/Web/marketpulse-web; npm install; cd ../../..
```

Run (two terminals):
```powershell
dotnet run --project src/Hosts/MarketPulse.Api --launch-profile http   # http://localhost:5142, API docs at /scalar/v1
cd src/Web/marketpulse-web; npm run dev                               # http://localhost:5173
```
In Development the API applies migrations to `MarketPulse` on `.\SQLEXPRESS` at startup. Verification and
password-reset emails are written to `.local/mail/` (set `Email:Mode` to `Smtp` and run `docker compose up -d` to use
Mailpit at http://localhost:8025 instead).

Tests:
```powershell
dotnet test                                  # unit + architecture + integration (needs SQL Server; see below)
cd src/Web/marketpulse-web; npm test         # frontend
```
Integration tests create and drop a throwaway database on `.\SQLEXPRESS`. To use another server, set
`MARKETPULSE_TEST_SQL` to a connection string without a database name.

After changing the API: re-run the tests with `UPDATE_OPENAPI_SNAPSHOT=1` to accept the new `docs/api/openapi.json`,
then `npm run generate:api` in the web app. After changing a model: add a migration and regenerate the idempotent script
in `deployment/sql/` (CI checks both).

## Repository layout
| Path | Contents |
|---|---|
| `src/BuildingBlocks/` | SharedKernel, application abstractions, common infrastructure |
| `src/Modules/` | One project per business module (ADR-0002) |
| `src/Providers/` | Provider interfaces, mock provider, licensed vendor adapters (only after approval) |
| `src/Hosts/` | API and Workers |
| `src/Web/` | React + TypeScript + Vite app |
| `tests/` | Unit, integration, contract and architecture tests |
| `docs/` | ADRs, decision log, roadmap, calculation definitions, phase reports |

## Ground rules
See `docs/adr/` and spec §18. In short:
- Never fabricate market data or provider capabilities.
- Mock data is always labelled as simulated.
- `decimal` for money; UTC internally.
- No real-money trading.
