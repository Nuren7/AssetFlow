# AssetFlow Frontend

The Angular 21 frontend provides the asset dashboard, inventory CRUD workflows, warranty alerts, and the read-only asset assistant. It communicates with the .NET API using the contracts in `../backend`.

## Local development

From the repository root, start the API:

```powershell
dotnet run --project backend\src\AssetFlow.API\AssetFlow.API.csproj
```

In another terminal:

```powershell
Set-Location frontend
npm ci
npm start
```

The frontend is available at `http://localhost:4200` and uses `http://localhost:5073/api` in development.

## Vercel deployment

1. Deploy the .NET API to a public HTTPS host first.
2. In Vercel, import the repository and set **Root Directory** to `frontend`.
3. Use the build command `npm run build -- --configuration production` and output directory `dist/assetflow/browser`. The included `vercel.json` configures these values and enables Angular route refreshes.
4. Add the Vercel environment variable `ASSETFLOW_API_URL` with the public API base URL, including `/api`, for example `https://api.example.com/api`.
5. Add the deployed Vercel origin to the backend's `CORS_ALLOWED_ORIGINS` setting, then redeploy both projects after changing environment variables.

The API URL is included in public frontend JavaScript. Never put database credentials, `GROQ_API_KEY`, or other secrets in Vercel frontend variables; configure those on the backend host only.

## Build and tests

```powershell
npm run build -- --configuration production
npm test -- --watch=false
```
