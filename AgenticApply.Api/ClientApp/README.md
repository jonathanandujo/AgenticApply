# AgenticApply Dashboard

```powershell
cd AgenticApply.Api\ClientApp
npm install
```

In Development, start the API from the repository root:

```powershell
dotnet run --project AgenticApply.Api
```

The API automatically starts Vite. Open `http://localhost:5000`; the browser
is redirected to the Vite dashboard, and TSX/CSS changes hot-reload without a
separate terminal or rebuild. Vite proxies API requests to `http://localhost:5000`.
Refreshing routes such as `/jobs` loads the React app, while JSON requests to
`/jobs` continue to reach the API.

Run `npm run build` in `ClientApp` when preparing static assets for publishing.
Outside Development, ASP.NET Core serves the built dashboard from `wwwroot`.

The API's `Agent:Enabled` setting controls the hosted worker, and
`Agent:ScanIntervalMinutes` sets the delay between scans. The default
configuration enables persistent writes. The existing SQLite database remains
at `AgenticApply.Agent/Data/AgenticApply.db`; configure
`ConnectionStrings:AgenticApply` if your database is elsewhere.
Set `Agent:MaxNewJobsPerScan` from 2 to 5 to limit new records per scan
(default: 3). Use **Scan now** on the dashboard or `POST /agent/scan` to queue a
scan immediately instead of waiting for the interval.

The first hosted-worker run uses the LinkedIn browser profile in
`AgenticApply.Api/BrowserData`. During migration, the existing worker profile
was moved from `AgenticApply.Worker/bin/Debug/net11.0/BrowserData`.
