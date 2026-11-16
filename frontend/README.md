# AssetFlow

AssetFlow is the Angular frontend for the IT asset management platform. It uses standalone Angular components, strict TypeScript, reactive forms, and the .NET API in `../backend`.

## Development server

```powershell
npm start
```

The development API URL is `http://localhost:5073/api`. Start the backend before opening `http://localhost:4200/`.

## Production build

```powershell
npm run build -- --configuration production
```

Production uses a relative `/api` URL; configure the reverse proxy or hosting platform to route that path to the backend.

## Tests

```powershell
npm test -- --watch=false
```

The inventory view supports asset search, category and status filtering, warranty alerts, and create, update, and delete workflows. The assistant presents its read-only tool calls in an expandable audit log.
