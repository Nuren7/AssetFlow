# ADR 003: Read-only AI tool-calling architecture

- Status: Accepted
- Date: 2026-11-16

## Context

Users need natural-language answers about inventory and warranty dates. The AI provider is external and its outputs, including tool arguments, are untrusted.

## Decision

The infrastructure layer calls Groq through the official OpenAI-compatible .NET SDK at `https://api.groq.com/openai/v1`. The model is selected with `GROQ_MODEL` (default `llama-3.3-70b-versatile`) and authenticated with `GROQ_API_KEY`.

The model receives a small, explicit set of JSON-schema tools. Tool calls are validated and executed by the application `IAssetToolExecutor`, which delegates to business services. The model has no direct access to SQL, EF Core, or write operations. Tool calls and bounded result summaries are returned to the caller. Provider failures use a clear fallback response, and caller cancellation is preserved.

## Consequences

- Asset data access remains deterministic and constrained to business methods.
- Tool schemas and application validation limit the model to approved read-only queries.
- The OpenAI-compatible SDK makes the provider boundary replaceable without changing application contracts.
- The assistant remains available as an API feature without requiring an AI key, but chat requests return a temporary-unavailability answer until one is configured.
