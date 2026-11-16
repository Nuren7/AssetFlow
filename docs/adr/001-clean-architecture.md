# ADR 001: Four-layer Clean Architecture

- Status: Accepted
- Date: 2026-11-16

## Context

AssetFlow needs an HTTP API, independently testable business rules, PostgreSQL persistence, and an external AI integration. Letting these concerns depend directly on one another would make core behavior difficult to test and would couple the domain to framework and provider changes.

## Decision

The backend uses four layers: `Domain`, `Application`, `Infrastructure`, and `API`. Dependencies point inward: Application depends only on Domain; Infrastructure implements Application and Domain contracts; API composes the services and exposes HTTP endpoints.

## Consequences

- Domain entities and repository abstractions do not depend on EF Core, ASP.NET Core, or the AI SDK.
- Use cases and validation can be tested without a database or network.
- Database and AI providers can be replaced behind application-facing contracts.
- API controllers map HTTP requests and responses through DTOs rather than exposing persistence entities.
