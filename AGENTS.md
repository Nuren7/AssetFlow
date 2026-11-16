# AssetFlow Assistant Guide

## Project overview

AssetFlow is an IT asset management platform. The .NET 8 backend is in `backend/`; it exposes asset CRUD and reporting endpoints and a Groq-powered assistant that can only use read-only application tools.

## Architecture

Keep the four backend layers and their dependency direction:

- `AssetFlow.Domain` contains entities, enums, repository contracts, and domain-level data structures. It has no project or third-party dependencies.
- `AssetFlow.Application` contains DTOs, validation, use cases, service interfaces, and safe AI tool contracts. It depends only on `Domain`.
- `AssetFlow.Infrastructure` implements persistence and external AI integrations. It may depend on `Application` and `Domain`.
- `AssetFlow.API` contains HTTP concerns and composition. It depends on `Application` and `Infrastructure`.

Do not introduce a reference from an inner layer to an outer layer. The AI integration must never access `DbContext` or execute model-generated SQL; all asset lookups go through `IAssetToolExecutor`.

## Conventions

- Write all code, comments, documentation, error messages, and seed data in English.
- Use nullable reference types, async/await, and cancellation tokens for I/O.
- Keep HTTP contracts in DTOs; do not expose EF entities from controllers.
- Use the global exception middleware for RFC 7807 `ProblemDetails` responses.
- Use UTC for timestamps and `DateOnly` for purchase and warranty dates.
- Store database and AI credentials in environment variables; never commit `.env` or credentials.

## Local development and tests

From the repository root:

```powershell
Copy-Item .env.example .env
dotnet restore backend/AssetFlow.sln
dotnet run --project backend/src/AssetFlow.API/AssetFlow.API.csproj
dotnet test backend/AssetFlow.sln
```

Set `DATABASE_URL` to a PostgreSQL connection string or PostgreSQL URL. `GROQ_API_KEY` enables assistant calls, and `GROQ_MODEL` optionally selects a Groq model. Swagger is available at `/swagger`, and the health endpoint is `/api/health`.

## Adding an AI tool safely

1. Define a narrow, read-only operation in `IAssetToolExecutor` and implement it using application services.
2. Add a strict JSON schema and a clear description to the Groq tool definitions.
3. Validate argument types, required values, enum values, and numeric bounds before invoking the service.
4. Return only the minimum asset data needed for the request; never expose database access or mutation tools to the model.
5. Add unit tests for normal execution and malformed or out-of-range arguments.
