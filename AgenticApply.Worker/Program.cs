using AgenticApply.Agent.Data;
using AgenticApply.Agent.Services;
using AgenticApply.Worker;

// Keep the database, browser profile, logs, and knowledge files beside the worker.
Directory.SetCurrentDirectory(AppContext.BaseDirectory);

var builder = Host.CreateApplicationBuilder(args);

var configuredConnectionString =
    builder.Configuration.GetConnectionString("AgenticApply")
    ?? throw new InvalidOperationException(
        "ConnectionStrings:AgenticApply must be configured.");
var sqliteConnectionString =
    new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(
        configuredConnectionString);

if (sqliteConnectionString.DataSource != ":memory:"
    && !sqliteConnectionString.DataSource.StartsWith(
        "file:",
        StringComparison.OrdinalIgnoreCase)
    && !Path.IsPathRooted(sqliteConnectionString.DataSource))
{
    sqliteConnectionString.DataSource = Path.GetFullPath(
        Path.Combine(
            builder.Environment.ContentRootPath,
            sqliteConnectionString.DataSource));
}

builder.Services
    .AddOptions<AgentOptions>()
    .Bind(builder.Configuration.GetSection(AgentOptions.SectionName))
    .Validate(
        options => options.ScanIntervalMinutes > 0,
        "Agent:ScanIntervalMinutes must be greater than zero.")
    .ValidateOnStart();

builder.Services.Configure<OllamaSettings>(
    builder.Configuration.GetSection("Ollama"));
builder.Services.Configure<LinkedInSettings>(
    builder.Configuration.GetSection("LinkedIn"));

builder.Services.AddSingleton(
    new DatabaseInitializer(sqliteConnectionString.ConnectionString));
builder.Services.AddSingleton(
    new JobRepository(sqliteConnectionString.ConnectionString));
builder.Services.AddSingleton(
    new EvaluationRepository(sqliteConnectionString.ConnectionString));
builder.Services.AddSingleton(
    new AgentSettingsRepository(sqliteConnectionString.ConnectionString));
builder.Services.AddSingleton<CandidateProfileService>();
builder.Services.AddSingleton<BrowserService>();
builder.Services.AddHttpClient<OllamaService>();

builder.Services.AddSingleton<IAgentPipelineService, AgentPipelineService>();
builder.Services.AddHostedService<AgentWorker>();

await builder.Build().RunAsync();
