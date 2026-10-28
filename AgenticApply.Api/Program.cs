using AgenticApply.Agent.Data;
using AgenticApply.Agent.Services;
using AgenticApply.Api.Endpoints;
using AgenticApply.Api.Services;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

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
    new DatabaseInitializer(sqliteConnectionString.ConnectionString));
builder.Services.AddScoped<IJobQueryService, JobQueryService>();
builder.Services.AddScoped<IJobReevaluationService, JobReevaluationService>();
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

var app = builder.Build();

await app.Services
    .GetRequiredService<DatabaseInitializer>()
    .InitializeAsync();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCors("Frontend");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapJobEndpoints();
app.MapStatsEndpoint();

app.Run();
