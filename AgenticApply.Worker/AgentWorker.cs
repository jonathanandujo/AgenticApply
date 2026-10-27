using AgenticApply.Agent.Data;
using Microsoft.Extensions.Options;

namespace AgenticApply.Worker;

public sealed class AgentWorker(
    DatabaseInitializer databaseInitializer,
    IAgentPipelineService pipelineService,
    IOptions<AgentOptions> options,
    ILogger<AgentWorker> logger) : BackgroundService
{
    private readonly AgentOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation("Agent worker is disabled by configuration");
            return;
        }

        var interval = TimeSpan.FromMinutes(_options.ScanIntervalMinutes);
        logger.LogInformation(
            "Agent worker started. Scan interval: {ScanInterval}; DryRun: {DryRun}",
            interval,
            _options.DryRun);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Initialize storage before each attempt so a transient database failure can recover.
                await databaseInitializer.InitializeAsync();
                await pipelineService.RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Agent pipeline cycle failed; the worker will retry after {ScanInterval}",
                    interval);
            }

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        logger.LogInformation("Agent worker is stopping");
    }
}
