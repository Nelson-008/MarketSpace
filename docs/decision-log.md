# Decision log

Product and implementation decisions, newest first. Architecture decisions have full ADRs in `docs/adr/`.

| Date | Decision | Decided by | Notes |
|---|---|---|---|
| 2026-11-07 | Identity's `RequireConfirmedEmail` is off; email confirmation is enforced in `AuthService` **after** the password check | Claude (security) | Identity's built-in check runs before the password and would reveal that an unverified account exists |
| 2026-11-07 | Development email goes to files in `.local/mail` (Email:Mode=File); Mailpit via SMTP when Docker runs | Claude | Docker Desktop not running on dev machine |
| 2026-11-07 | OpenAPI document produced by a contract test (`docs/api/openapi.json`), not at build time | Claude | Build-time generation starts the app, which correctly refuses to start without secrets |
| 2026-11-07 | Transitive `System.Security.Cryptography.Pkcs` pinned to 10.0.14 | Claude | 9.0.11 (via Microsoft.Data.SqlClient) has GHSA-2fmv-rv2c-59x7 and GHSA-846f-f764-76mx |
| 2026-11-07 | Beta plan: watchlists (max 10) and instrument search | Claude (placeholder) | Product configuration, not market data; owner to confirm limits |
| 2026-11-07 | Mock instruments use **fictional symbols** (e.g. `DEMO-ALPHA`), not real NSE/BSE symbols | Owner (accepted recommendation) | Prevents simulated prices being mistaken for real ones |
| 2026-11-07 | Tenancy: one user = one tenant for MVP | Owner | ADR-0004 |
| 2026-11-07 | Repository location `D:\MarketPulse`; GitHub remote on owner's personal account | Owner | |
| 2026-11-07 | Revised roadmap approved: Events+Alerts phase and Derivatives phase added; Market Memory moved after MVP | Owner | `docs/roadmap.md` |
| 2026-11-07 | Project-per-module structure | Owner | ADR-0002 |
| 2026-11-07 | Local dev uses the existing SQL Server Express; integration tests use Testcontainers | Owner | Express 10 GB cap is acceptable for dev only |

## Open decisions (spec §20) — blocking where noted
| Decision | Status | Blocks |
|---|---|---|
| Commercial market-data provider | Undecided | Any real (non-mock) data adapter |
| NSE/BSE licence | Not confirmed | Public beta |
| Fundamentals source | Undecided | Real fundamentals data (Phase 5 runs on mock data) |
| Disclosure access channel | Undecided | Automated disclosure ingestion (CSV import meanwhile) |
| News provider | Undecided | News in AI reports |
| AI model/provider | Undecided | Real AI reports (Phase 7 uses a fake client in tests) |
| Billing / payment gateway | Not in spec | Paid plans |
| Cloud provider and region | Undecided | Deployment |
| Legal review: SEBI RA/IA boundary for signals; DPDP Act 2023 | Not started | Public beta |
| Brokerage / STT / GST / stamp-duty rates for paper trading | Must be entered with sources by the owner | Phase 8 charges (shown as "Unverified assumption" until sourced) |
