# Roadmap (approved 2026-11-07)

Each phase is complete only when it builds, its tests pass and its exit gate is met. A phase report goes in `docs/phase-reports/`.

| Phase | Scope | Exit gate | Status |
|---|---|---|---|
| 0. Discovery | ADRs, decision log, provider matrix, legal question list | No live-data assumption without written permission | Ongoing |
| 1. Foundation | Repo, CI, building blocks, Identity, tenancy, entitlements, audit, error envelope, OpenAPI, master + ops schemas, React shell with DEMO banner | Auth works; cross-tenant isolation and architecture tests pass | **Done locally**: CI not yet run (no remote) — see `docs/phase-reports/phase-1.md` |
| 2. Market foundation | Provider abstractions, mock provider, instrument master + calendar import, ingestion worker, candles, Redis, SignalR, data-quality worker, overview dashboard, watchlists | Disconnect → STALE; reconnect/gap tests pass | Not started |
| 3. Technical analytics + scanners | Versioned indicators, time-of-day RVOL, scanners, charts | No indicator emitted with missing/incomplete candles | Not started |
| 4. Institutional intelligence | Disclosures, bulk/block deals, aggregate flows, shareholding | Source-linked, correctly classified | Not started |
| 5. Company research | Financial facts, ratios, valuation, announcements | Calculations independently verified | Not started |
| 6. Events + alerts | Rule-based events with evidence, alerts, notifications | No per-tick alert repeats | Not started |
| 7. AI research analyst | Retrieval, cited sections, number check, injection defence | Grounding and injection tests pass | Not started |
| 8. Paper trading | Orders, fills, immutable ledger, positions, charges, journal | No broker route; reconciliation tests pass | Not started |
| 9. Basic backtesting | Reproducible runs, costs, look-ahead guard, metrics | Assumptions and sample size disclosed | Not started |
| 10. Derivatives | Contracts, option chain, OI/PCR/IV, Greeks (mock until licensed) | Lot size/expiry only from master data | Not started |
| 11. Beta readiness | Admin, security review, load tests, DPDP processes | Legal, licence and operations gates approved | Not started |
| Post-MVP | Market Memory, advanced options, walk-forward, mobile | — | — |

## Phase 1 task tracker
| # | Task | Status |
|---|---|---|
| 1 | Repo scaffold (global.json, slnx, CPM, analyzers, docker-compose) | Done |
| 2 | ADRs 0001–0005 + decision log | Done |
| 3 | SharedKernel + unit tests | Done (45 tests) |
| 4 | Infrastructure.Common (EF base, UTC converters, tenant filters, error envelope, correlation ids) | Done. Outbox and idempotency keys moved to Phase 2 (first commands that need them) |
| 5 | Identity module | Done |
| 6 | Entitlements | Done |
| 7 | Audit log (append-only trigger) | Done |
| 8 | Master + ops schemas | Done |
| 9 | Migrations + idempotent SQL scripts (`deployment/sql/`) | Done |
| 10 | API host | Done |
| 11 | Workers host skeleton | Done |
| 12 | React shell | Done |
| 13 | Integration + architecture tests | Done |
| 14 | CI (GitHub Actions) | Written; runs once pushed to GitHub |
| 15 | Phase 1 report | Done |
