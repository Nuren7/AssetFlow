# ADR 002: PostgreSQL and Entity Framework Core

- Status: Accepted
- Date: 2026-11-16

## Context

AssetFlow requires persistent asset records, unique serial numbers, filtered queries, and grouped dashboard statistics. The deployment target includes PostgreSQL providers such as Neon.

## Decision

Use PostgreSQL through `Npgsql.EntityFrameworkCore.PostgreSQL`. EF Core mappings are centralized in fluent entity configurations. Serial numbers have a database-enforced unique index, and category and status enums are stored as readable strings. The connection is supplied by `DATABASE_URL`, accepting either an Npgsql connection string or a PostgreSQL URL.

On startup, the API calls `EnsureCreatedAsync` and seeds 18 representative assets only when the assets table is empty. This supports a quick initial deployment; schema changes in production should be introduced through versioned EF Core migrations rather than relying on `EnsureCreated`.

## Consequences

- PostgreSQL enforces serial-number uniqueness even when concurrent requests race.
- EF Core provides typed queries and aggregation while keeping SQL out of the application layer.
- `EnsureCreatedAsync` is suitable for the initial schema but does not apply migrations to an existing database; migration-based upgrades are the production evolution path.
- Seed data includes multiple statuses, categories, locations, and warranty dates, including near-term expirations.
