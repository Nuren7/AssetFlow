# AssetFlow — IT Asset Management Platform

**Live Demo:** [https://asset-flow-chi.vercel.app](https://asset-flow-chi.vercel.app)
*(Note: The backend runs on a free-tier container and may take ~30 seconds to wake up on the initial request.)*

AssetFlow is a fullstack IT Asset Management (ITAM) platform built with **.NET 8 (C#)**, **Angular**, and **PostgreSQL**. It is designed to support the IT asset lifecycle through structured inventory management, warranty and status reporting, and a governed, read-only AI assistant powered by deterministic tool calling.

---

## Core Capabilities

- **IT Asset Lifecycle & Inventory:** Register, search, filter, update, and retire hardware assets with full tracking of serial numbers, categories, manufacturers, models, locations, purchase dates, and warranty expirations.
- **Operational Reporting:** Real-time dashboard metrics summarizing total inventory, available hardware, assigned devices, assets in repair, and upcoming warranty expirations.
- **Governed AI Asset Assistant:** Natural-language Q&A backed by LLM tool calling (Groq API). The assistant operates strictly through deterministic, read-only application tools with zero direct database access, surfacing an audit log of executed tools directly in the UI for full transparency.

---

## Architecture & Agentic Development Practices

The solution follows a **4-layer Clean Architecture** where all dependencies point inward toward the core domain:

- `backend/src/AssetFlow.Domain` — Core entities (`Asset`), lifecycle enums, and repository contracts with zero external dependencies.
- `backend/src/AssetFlow.Application` — Business logic, DTOs, validation rules, reporting aggregations, and read-only AI tool definitions (`IAssetToolExecutor`).
- `backend/src/AssetFlow.Infrastructure` — PostgreSQL persistence via Entity Framework Core, automated idempotent database seeding, and Groq LLM integration.
- `backend/src/AssetFlow.API` — REST API endpoints, RFC 7807 `ProblemDetails` error handling, Swagger/OpenAPI documentation, and health checks.
- `frontend/` — Angular standalone application built with TypeScript, Signals, and a low-glare dark interface.

### Engineering & Documentation Standards
- **Architecture Decision Records (`docs/adr/`):** Documents key architectural trade-offs, including Clean Architecture boundaries, PostgreSQL/EF Core persistence, and isolated AI tool execution.
- **Progressive Disclosure (`AGENTS.md`):** Provides structured architectural rules, coding conventions, and testing workflows to guide both human contributors and AI coding agents consistently.
- **CI/CD & Containerization:** Automated build, unit testing, and Docker image verification via GitHub Actions workflows (`.github/workflows/`).

---

## Local Development

### Prerequisites
- [.NET 8 SDK](https://microsoft.com)
- [Node.js 20+](https://nodejs.org)
- PostgreSQL instance (e.g., Neon or local PostgreSQL)
- Groq API key (optional, required only for AI assistant queries)

### 1. Backend (.NET 8 API)

Configure your environment variables in a local, git-ignored `.env` file (see `.env.example`):

```env
DATABASE_URL="Host=...;Database=assetflow;Username=...;Password=...;SSL Mode=Require"
GROQ_API_KEY="gsk_..."
GROQ_MODEL="llama-3.3-70b-versatile"
```

Restore, test, and run the API:

```powershell
dotnet restore backend\AssetFlow.sln
dotnet test backend\AssetFlow.sln
dotnet run --project backend\src\AssetFlow.API\AssetFlow.API.csproj
```

The API listens on `http://localhost:5073` in development. Interactive API documentation is available at `/swagger`, and the health check endpoint is at `/api/health`.

### 2. Frontend (Angular)

```powershell
Set-Location frontend
npm ci
npm start
```

Open `http://localhost:4200`. In development, the frontend connects to `http://localhost:5073/api`.

---

## Deployment

### Frontend (Vercel)
1. Import the repository into Vercel with `frontend` set as the **Root Directory**. The included [`frontend/vercel.json`](./frontend/vercel.json) configures the Angular output directory and SPA route fallback.
2. Add the public runtime environment variable:
   ```text
   ASSETFLOW_API_URL=https://assetflow-xyyv.onrender.com/api
   ```
3. Redeploy after updating environment variables. *(Do not expose `DATABASE_URL` or `GROQ_API_KEY` in the frontend project.)*

### Backend (Docker / Render)
1. Deploy `backend/Dockerfile` as a Web Service with `backend` as the **Root Directory**.
2. Configure `DATABASE_URL`, `GROQ_API_KEY`, and `GROQ_MODEL` on the backend host. The API allows the current production and preview Vercel origins from `appsettings.json`; add other exact origins to `CORS_ALLOWED_ORIGINS` as a comma-separated list.

Vercel preview URLs change between deployments. Use the stable production domain for normal access, or add the current preview origin to the Render service's `CORS_ALLOWED_ORIGINS` environment variable and redeploy the backend.

For detailed frontend setup instructions, see [`frontend/README.md`](./frontend/README.md).
