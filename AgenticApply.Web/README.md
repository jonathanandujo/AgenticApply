# AgenticApply

React, Vite, TypeScript, TanStack Query, Axios, Tailwind CSS, and Recharts
frontend for the AgenticApply job-search platform.

## Run locally

```powershell
cd AgenticApply.Web
npm install
Copy-Item .env.example .env.local
npm run dev
```

By default, the app requests the API at `https://localhost:7001`. Override
`VITE_API_BASE_URL` in `.env.local` when the API is hosted elsewhere. To display
the configured Ollama model in Settings, set `VITE_OLLAMA_MODEL`.

The API must allow the frontend's origin in `Cors:AllowedOrigins`. The API's
default configuration allows the Vite development server (`localhost:5173` and
`127.0.0.1:5173`) and preview server (`localhost:4173` and `127.0.0.1:4173`).
Set additional origins in API configuration for other deployments.

## Available scripts

```powershell
npm run dev
npm run build
npm run lint
npm run preview
```

## Pages

- **Dashboard** — job and evaluation KPIs, top matches, and score distribution.
- **Top Matches** — sortable, scored opportunities.
- **Jobs** — searchable and filterable job library.
- **Job Details** — job description, evaluation, and LinkedIn link.
- **Settings** — database counts and available runtime configuration.

The current API does not expose application counts, job discovery dates, worker
execution time, or Ollama model configuration. Those values are shown as
unavailable rather than inferred or fabricated. The discovery chart displays
when the API provides dated discovery data.
