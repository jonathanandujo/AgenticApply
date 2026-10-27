using AgenticApply.Agent.Data;
using AgenticApply.Agent.Services;
using AgenticApply.Worker;

// Keep the database, browser profile, logs, and knowledge files beside the worker.
Directory.SetCurrentDirectory(AppContext.BaseDirectory);

var builder = Host.CreateApplicationBuilder(args);

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

builder.Services.AddSingleton<DatabaseInitializer>();
builder.Services.AddSingleton<JobRepository>();
builder.Services.AddSingleton<EvaluationRepository>();
builder.Services.AddSingleton<CandidateProfileService>();
builder.Services.AddSingleton<BrowserService>();
builder.Services.AddHttpClient<OllamaService>();

builder.Services.AddSingleton<IAgentPipelineService, AgentPipelineService>();
builder.Services.AddHostedService<AgentWorker>();

await builder.Build().RunAsync();
