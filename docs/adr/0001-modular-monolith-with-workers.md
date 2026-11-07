# ADR-0001: Modular monolith with separately deployable workers

- Status: Accepted
- Date: 2026-11-07
- Spec reference: §12, §12.2

## Context
The spec requires a modular monolith with independently deployable ingestion and background workers, and warns against introducing microservices prematurely.

## Decision
- Two hosts: `MarketPulse.Api` (REST + SignalR) and `MarketPulse.Workers` (all background workers).
- Workers are enabled or disabled through configuration, so the same build can be deployed as several instances, each running a different set of workers. For example, ingestion can run in isolation from research jobs.
- Worker → API communication:
  - Live quotes go through Redis pub/sub. The same Redis instance is the SignalR backplane.
  - Durable events go through a SQL outbox table in the `ops` schema.
- No message broker (RabbitMQ etc.) until measured throughput or replay needs justify one.

## Consequences
- A single database and single deployment pipeline keep operations simple.
- Market-data processing failures cannot take down the user-facing API (spec §12.2).
- Module boundaries must be enforced by tooling (see ADR-0002), not by network boundaries.
