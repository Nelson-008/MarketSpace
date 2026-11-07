# Phase 1 report — Foundation

- Date: 2026-11-07
- Status: **complete locally.** CI is written but has not run yet, because the repository has no GitHub remote. Per spec §18, Phase 1 is not "done" until CI passes.

## Implemented
| Area | What exists |
|---|---|
| Repo & build | .NET 10 solution (`.slnx`), central package versions with transitive security pinning, analyzers with warnings-as-errors, EF tool manifest, docker-compose (Redis, Mailpit) |
| SharedKernel | `Result`/`Error`, typed IDs (GUID v7), UTC↔IST `MarketTime`, `Price`/`Quantity`/`Money`, `DataQualityStatus` (§7.1), `DataDelivery`, `Provenance`, and `FreshnessAssessor`, the single rule for LIVE/DELAYED/STALE/SIMULATED labels |
| Infrastructure.Common | Module DbContext base. It provides: UTC enforcement, decimal/string defaults, and tenant isolation on both reads (named query filter) and writes (blocked in `SaveChanges`). Also: the §10.1 error envelope, correlation IDs, validation and entitlement endpoint filters, HTTP/system user contexts, SMTP and file email senders |
| Identity (`auth`) | Account workflows: register (anti-enumeration), email verification, login with lockout, password reset. Access tokens are JWTs (10 min, held in memory). Refresh tokens: hashed, rotated on every use, HttpOnly Secure SameSite=Strict cookie, reuse detection that revokes the whole session family. Also: logout, sign-out-everywhere, profile, roles from §4.1, plans/features/subscriptions with a seeded Beta plan, server-side entitlement checks, append-only audit log (DB trigger), admin audit endpoint |
| Instruments (`master`) | Exchanges, segments, sectors, companies, instruments, effective-dated attribute versions, provider mappings, aliases, master import versions, trading calendar days, sessions. Check constraints enforce option/future terms, positive lot/tick sizes and valid date ranges. Read endpoints are in place; **no data is seeded** |
| Workspace (`workspace`) | Watchlists (CRUD, unique names, plan limit) and Explore/Pro preference. These are the first tenant-owned tables |
| Administration (`ops`) | Provider registry, capabilities (with verification source) and licences. Every permission flag defaults to *denied*. `/system/status` derives the data mode, so the DEMO banner is data-driven |
| API host | `/api/v1` minimal APIs, string enums, rate limits (auth per IP; per user), CORS with credentials, security headers, HSTS outside Development, health endpoints (live/ready), OpenAPI + Scalar (dev), OpenTelemetry (OTLP when configured), migrate-on-start in Development |
| Workers host | Config-driven worker registry with a heartbeat worker |
| Web app | React 19 + Vite 6 + Tailwind 4. Access token in memory; refresh is single-flight so it can't trip reuse detection. Persistent fail-safe DEMO banner. Explore/Pro navigation (§9). Pages: auth flows, watchlists, settings, market overview with an honest "no data", and planned sections. API types are generated from the OpenAPI contract |
| CI | GitHub Actions: backend build and tests on a SQL Server service container, a check that SQL scripts are current, frontend `npm audit`, an API-types-vs-contract check, type-check, tests and build |

## Tests (all run on 2026-11-07)
| Suite | Result |
|---|---|
| Unit (`MarketPulse.UnitTests`) | 45 / 45 passed |
| Integration (`MarketPulse.IntegrationTests`, real SQL Server Express) | 31 / 31 passed |
| Architecture (`MarketPulse.ArchitectureTests`) | 15 / 15 passed |
| Frontend (Vitest) | 11 / 11 passed; `tsc -b` clean; production build OK |
| Manual smoke run (Development) | Migrations applied for 4 modules; `/health/ready` 200; `/system/status` = NoProvider + banner; anonymous `/watchlists` 401; register 202; email written to `.local/mail`; Scalar UI 200 |

Spec §16.2 acceptance criteria covered in this phase:
- A user cannot access another user's watchlist (HTTP 404 for read/update/delete).
- The persistence layer itself blocks a cross-tenant write.
- No market data is faked. The provider-disconnect → STALE rule is implemented and unit-tested in `FreshnessAssessor`; its end-to-end test comes with the Phase 2 feed.
- Architecture tests fail the build if any broker or order-execution SDK is referenced.

## Commands used
```
dotnet build; dotnet test
dotnet ef migrations add InitialCreate --project <module> --startup-project src/Hosts/MarketPulse.Api --context <Context> --output-dir Infrastructure/Migrations
dotnet ef migrations script --idempotent ... --output deployment/sql/0N-<schema>.sql
npm run generate:api; npx tsc -b; npx vitest run; npx vite build
```

## Known limitations / follow-ups
1. **CI has not run.** It runs on the first push to GitHub.
2. Access tokens stay valid for up to 10 minutes after "sign out everywhere" or a password reset. There is no per-request security-stamp check, by design, to avoid a DB hit on every call. Revisit if needed.
3. Two browser **tabs** refreshing at the same moment can look like token reuse and sign the user out. Single-flight only covers one tab. Phase 2 options: a short grace window, or cross-tab coordination via BroadcastChannel.
4. Emails are sent inline, with no outbox yet. If sending fails after the account is created, the user must use "resend verification". The outbox arrives in Phase 2.
5. Audit records are written in their own transaction, not atomically with the business change.
6. Rate limiting is per API instance (in-memory). It needs a distributed limiter before running multiple instances.
7. Audit logs store client IP addresses, which are personal data. The retention period is a DPDP/legal decision.
8. **First admin user:** there is no bootstrap command yet; the role must be granted in the database. A CLI command is planned for Phase 2.
9. Idempotency keys (§10.1) and SQL Server row-level security are deferred. Idempotency comes with the first non-idempotent business commands (paper orders); RLS gets evaluated in beta hardening.
10. JSON numbers lose precision in JavaScript beyond about 15 significant digits. Phase 2 must decide whether prices and values travel as strings.
11. Not yet tested in a real browser. The Secure refresh cookie over `http://localhost` works in Chromium and Firefox; Safari may refuse it (use HTTPS dev certs there).
12. The local dev database `MarketPulse` on `.\SQLEXPRESS` now holds one unverified smoke-test account, `smoke@example.test`.

## Next: Phase 2 (Market foundation)
Planned scope:
- Provider interfaces (§6.1) and the mock provider, with fictional `DEMO-*` instruments on a simulated exchange.
- Instrument master and calendar import with versioning; the first-admin bootstrap command; outbox.
- Ingestion worker with reconnect, gap detection and heartbeats; candle aggregation; Redis latest-quote cache; SignalR `/hubs/market` with entitlement checks.
- Data-quality worker, market overview dashboard and watchlist items.

Redis and Mailpit need Docker Desktop running.
