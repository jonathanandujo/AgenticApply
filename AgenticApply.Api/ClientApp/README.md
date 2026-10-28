# AgenticApply Dashboard

The React/Vite dashboard lives in `AgenticApply.Api/ClientApp`. Build it into
the API's `wwwroot` directory with:

```powershell
cd AgenticApply.Api\ClientApp
npm install
npm run build
```

Then start the API and hosted agent worker together:

```powershell
dotnet run --project AgenticApply.Api
```

Open `http://localhost:5000` or `https://localhost:5001`. The API serves the
dashboard and its API endpoints from the same process and origin.

The API's `Agent:Enabled` setting controls the hosted worker, and
`Agent:ScanIntervalMinutes` sets the delay between scans. The default
configuration enables persistent writes. The existing SQLite database remains
at `AgenticApply.Agent/Data/AgenticApply.db`; configure
`ConnectionStrings:AgenticApply` if your database is elsewhere.

For frontend-only development, run the API with the `Agent:Enabled` setting
disabled if you do not want the agent to scan while working on the UI, then run:

```powershell
cd AgenticApply.Api\ClientApp
npm run dev
```

Vite proxies API requests to `http://localhost:5000`. The development server is
optional; the built dashboard is served directly by ASP.NET Core.

The first hosted-worker run uses the LinkedIn browser profile in
`AgenticApply.Api/BrowserData`. During migration, the existing worker profile
was moved from `AgenticApply.Worker/bin/Debug/net11.0/BrowserData`.
