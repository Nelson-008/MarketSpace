# ADR-0003: Keys, numeric types and time

- Status: Accepted
- Date: 2026-11-07
- Spec reference: §7, §11.1

## Keys
| Entity class | Key type |
|---|---|
| User-owned or API-exposed aggregates (users, tenants, portfolios, watchlists, instruments, companies, reports) | `uniqueidentifier`, GUID v7 (`Guid.CreateVersion7()`), wrapped in strongly-typed IDs |
| High-volume / append-only rows (quotes, candles, ledger entries, audit logs, outbox) | `bigint IDENTITY` |

## Numeric types (never `float`/`real` for financial values)
| Use | SQL type |
|---|---|
| Prices | `decimal(19,4)` |
| Share/unit quantities | `bigint` |
| Lots | `int` |
| Values, turnover, money | `decimal(24,4)` + currency column |
| Ratios / percentages (stored as fractions) | `decimal(18,8)` |
| Reported financial facts | `decimal(28,6)` + `Unit`, `Scale`, `Currency`, `Basis` |

## Time
- All instants are stored as `datetime2(7)` UTC, with column names ending in `Utc`. An EF converter enforces `DateTimeKind.Utc`.
- Trading date is a separate `date` column holding the exchange-local (Asia/Kolkata) calendar date.
- Exchange event time, provider time and received time are separate columns.
- Trading days and sessions come only from imported calendars, never from weekday rules.
