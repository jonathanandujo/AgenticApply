using AgenticApply.Agent.Data;
using AgenticApply.Agent.Services;
using AgenticApply.Api.Endpoints;
using AgenticApply.Api.Services;
using AgenticApply.Api.Worker;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);
Directory.SetCurrentDirectory(builder.Environment.ContentRootPath);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

builder.Services
    .AddOptions<AgentOptions>()
    .Bind(builder.Configuration.GetSection(AgentOptions.SectionName))
    .Validate(
        options => options.ScanIntervalMinutes > 0,
        "Agent:ScanIntervalMinutes must be greater than zero.")
    .Validate(
        options => options.MaxNewJobsPerScan is >= 2 and <= 10,
        "Agent:MaxNewJobsPerScan must be between 2 and 10.")
    .ValidateOnStart();

var allowedOrigins =
    builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "Frontend",
        policy => policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var configuredConnectionString =
    builder.Configuration.GetConnectionString("AgenticApply")
    ?? throw new InvalidOperationException(
        "ConnectionStrings:AgenticApply must be configured.");

var sqliteConnectionString =
    new SqliteConnectionStringBuilder(configuredConnectionString);

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

builder.Services.AddSingleton(
    new JobRepository(sqliteConnectionString.ConnectionString));
builder.Services.AddSingleton(
    new EvaluationRepository(sqliteConnectionString.ConnectionString));
builder.Services.AddSingleton(
    new ApplicationRepository(sqliteConnectionString.ConnectionString));
builder.Services.AddSingleton(
    new AgentSettingsRepository(sqliteConnectionString.ConnectionString));
builder.Services.AddSingleton(
    new DatabaseInitializer(sqliteConnectionString.ConnectionString));
builder.Services.AddScoped<IJobQueryService, JobQueryService>();
builder.Services.AddScoped<IApplicationService, ApplicationService>();
builder.Services.AddScoped<IJobReevaluationService, JobReevaluationService>();
builder.Services.AddScoped<ILinkedInSettingsService, LinkedInSettingsService>();
builder.Services.AddScoped<IOllamaSettingsService, OllamaSettingsService>();
builder.Services.Configure<LinkedInSettings>(
    builder.Configuration.GetSection("LinkedIn"));
builder.Services.Configure<OllamaSettings>(
    builder.Configuration.GetSection("Ollama"));
builder.Services.PostConfigure<OllamaSettings>(settings =>
{
    if (!string.IsNullOrWhiteSpace(settings.LogPath)
        && !Path.IsPathRooted(settings.LogPath))
    {
        settings.LogPath = Path.GetFullPath(
            Path.Combine(builder.Environment.ContentRootPath, settings.LogPath));
    }
});
builder.Services.AddSingleton<CandidateProfileService>();
builder.Services.AddHttpClient<OllamaService>((serviceProvider, httpClient) =>
{
    var settings = serviceProvider
        .GetRequiredService<IOptions<OllamaSettings>>()
        .Value;

    if (settings.TimeoutMinutes > 0)
    {
        httpClient.Timeout = TimeSpan.FromMinutes(settings.TimeoutMinutes);
    }
});

builder.Services.AddSingleton<BrowserService>();
builder.Services.AddSingleton<IAgentPipelineService, AgentPipelineService>();
builder.Services.AddSingleton<AgentWorker>();
builder.Services.AddHostedService(
    serviceProvider => serviceProvider.GetRequiredService<AgentWorker>());

var app = builder.Build();

await app.Services
    .GetRequiredService<DatabaseInitializer>()
    .InitializeAsync();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCors("Frontend");
app.UseDefaultFiles();
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapJobEndpoints();
app.MapApplicationEndpoints();
app.MapLinkedInSettingsEndpoints();
app.MapOllamaSettingsEndpoints();
app.MapAgentEndpoints();
app.MapStatsEndpoint();
app.MapFallbackToFile("index.html");

app.Run();
