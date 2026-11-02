# AgenticApply

AgenticApply is a local job-search assistant. The ASP.NET Core API hosts the
React dashboard and a background worker in one process. The worker scans a
LinkedIn jobs search, stores jobs in SQLite, and evaluates jobs without an
evaluation using a local Ollama model.

Applications are never submitted without an explicit user action. You can apply
manually on LinkedIn and track the result, or explicitly start Auto Apply for a
job identified as Easy Apply. Review the confirmation before proceeding;
automation stops when required answers are missing and records success only
after LinkedIn confirms submission.

## Prerequisites

- Windows 10/11 (the LinkedIn browser automation uses Microsoft Edge).
- The .NET 11 SDK. The projects target `net11.0`; install the SDK appropriate
  for your environment from [dotnet.microsoft.com/download](https://dotnet.microsoft.com/download).
- Node.js 20.19+ or 22.12+ and npm, for the Vite frontend build.
- [Ollama for Windows](https://ollama.com/download/windows).
- A LinkedIn account. The application uses a visible Edge window so you can
  sign in if LinkedIn requests authentication.

Check that the tools are installed:

```powershell
dotnet --list-sdks
node --version
npm --version
ollama --version
```

## Install an Ollama model

Ollama must be running locally at `http://localhost:11434`, which is the
configured default. The Ollama Windows application normally starts its local
service after installation. If it is not running, start Ollama from the Start
menu.

Download at least one model. For example:

```powershell
ollama pull phi:latest
```

Other models can be installed the same way, for example:

```powershell
ollama pull llama3.1:8b
ollama list
```

The model must be downloaded before selecting it in AgenticApply. The API reads
the installed model list from Ollama's `/api/tags` endpoint. If Ollama uses a
different address, update `Ollama:BaseUrl` in
`AgenticApply.Api\appsettings.json`.

## Configure AgenticApply

### Ollama model

The initial default is `phi:latest`, configured as `Ollama:Model` in
`AgenticApply.Api\appsettings.json`. Start the API and open **Settings >
Ollama model** to choose and save an installed model. The selection is stored
in the SQLite `AgentSettings` table and is used by future worker evaluations
and re-evaluations. A one-off model can also be chosen when re-evaluating an
individual job.

The model chosen in the dashboard is the persistent default; `Ollama:Model` is
the fallback used before a dashboard selection has been saved.

### LinkedIn search URL

Set the initial search URL in `LinkedIn:JobsUrl` in
`AgenticApply.Api\appsettings.json`. It must be an HTTPS LinkedIn jobs-search
URL. After startup, you can change it from **Settings > LinkedIn job search**.
The dashboard setting is saved in SQLite and takes precedence over the
appsettings value.

The worker uses this search on its next scan. The default scan interval is two
hours; use **Scan now** in the dashboard to queue a scan immediately. The
default limit is three new jobs per scan and can be set to 2–5 using
`Agent:MaxNewJobsPerScan` in `appsettings.json`.

### Candidate profile

Personalize `AgenticApply.Agent\Knowledge\CandidateProfile.md` before running
evaluations. This Markdown file is included in the model prompt and is copied
into the API output when the project builds. Edit the source file in the
repository and rebuild the API for changes to be copied. Keep your profile
accurate and avoid putting credentials or other secrets in it.

### Other settings and data

Edit `AgenticApply.Api\appsettings.json` for the worker schedule, Ollama
address/default model, logging, and initial LinkedIn URL. The common defaults
are:

```json
{
  "Agent": {
    "Enabled": true,
    "ScanIntervalMinutes": 120,
    "MaxNewJobsPerScan": 3,
    "DryRun": false
  },
  "Ollama": {
    "BaseUrl": "http://localhost:11434",
    "Model": "phi:latest",
    "TimeoutMinutes": 2
  }
}
```

By default, the SQLite database is
`AgenticApply.Agent\Data\AgenticApply.db`. It is created and initialized
automatically at startup. Ollama evaluation logs are written to
`AgenticApply.Agent\Logs\ollama.log`.

## Build and run

Install frontend dependencies once:

```powershell
cd .\AgenticApply.Api\ClientApp
npm ci
cd ..\..
```

Run the API from the repository root:

```powershell
dotnet run --project .\AgenticApply.Api\AgenticApply.Api.csproj
```

In Development, this single command starts both the API and Vite. Open
`http://localhost:5000`; the SPA proxy redirects the browser to Vite, where
TSX/CSS edits hot-reload automatically. The Vite server proxies API requests
back to port 5000. `npm run build` is only needed when preparing static
frontend assets for publishing. Refreshing frontend routes such as `/jobs`
serves the React app; JSON requests to `/jobs` continue to reach the API.

The API is available at:

- `http://localhost:5000`
- `https://localhost:5001`

The HTTPS launch profile may require trusting the local ASP.NET development
certificate:

```powershell
dotnet dev-certs https --trust
```

On the first run, the worker opens Microsoft Edge to LinkedIn. Sign in if
prompted and leave the browser profile available for subsequent scans. The
worker waits for manual authentication when LinkedIn redirects to a login or
checkpoint page.

Outside Development, the dashboard is served from
`AgenticApply.Api\wwwroot`; rebuild the frontend with `npm run build` after
frontend changes before running the production-style API.

## Usage

- **Dashboard** shows job, evaluation, recommendation, and application totals.
- **Top Matches** and **Jobs** show discovered opportunities and scores.
- Open a job to review its description and evaluation, choose an Ollama model
  for re-evaluation, or open the listing on LinkedIn.
- For jobs identified as Easy Apply, use the row's Auto Apply action to start
  LinkedIn's application flow after confirming. The automation does not invent
  screening answers; it stops for unanswered fields and does not mark the job
  as applied unless LinkedIn confirms submission.
- After submitting an application on LinkedIn, mark the job as applied in the
  dashboard. This updates the SQLite application tracker; it does not submit
  the application itself.
- **Settings** lets you select the default installed Ollama model and update
  the LinkedIn search URL.
- Use **Scan now** to queue an immediate scan. A scan already in progress is
  not run concurrently with another scan.

In Development, the API's OpenAPI document is available at
`http://localhost:5000/openapi/v1.json`.

## Frontend checks

```powershell
cd .\AgenticApply.Api\ClientApp
npm run build
npm run lint
```
