# ADR-0002: One project per module, with a public Contracts project

- Status: Accepted (approved 2026-11-07)
- Spec reference: §12 (solution tree)

## Context
Spec §12 lists `MarketPulse.Domain/Application/Infrastructure` layer projects **and** a `Modules/` folder without defining how they relate.

## Decision
- `src/BuildingBlocks/` holds cross-cutting code:
  - `MarketPulse.SharedKernel`: primitives and provenance.
  - `MarketPulse.Application.Abstractions`: handler and context interfaces.
  - `MarketPulse.Infrastructure.Common`: EF base, outbox, audit, idempotency, observability.

  These play the role of the spec's Domain/Application/Infrastructure layers for shared concerns.
- Each module is `src/Modules/<Name>/MarketPulse.Modules.<Name>` with internal folders `Domain/ Application/ Infrastructure/ Endpoints/`. The module owns one DbContext and one SQL schema.
- A module exposes integration events and query interfaces only via `MarketPulse.Modules.<Name>.Contracts`. Other modules may reference only `*.Contracts`.
- Architecture tests enforce:
  - Module isolation.
  - No reference from any domain code to provider or vendor assemblies.
  - **PaperTrading has no reference to any broker or execution assembly.**

## Consequences
- Boundaries are enforced by the compiler plus tests.
- More projects to manage, which is acceptable.
