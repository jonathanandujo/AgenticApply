using AgenticApply.Agent.Data;
using Microsoft.Extensions.Options;
using System.Threading.Channels;

namespace AgenticApply.Api.Worker;

public sealed class AgentWorker(
    DatabaseInitializer databaseInitializer,
    IAgentPipelineService pipelineService,
    IOptions<AgentOptions> options,
    ILogger<AgentWorker> logger) : BackgroundService
{
    private readonly AgentOptions _options = options.Value;
    private readonly Channel<bool> _scanRequests =
        Channel.CreateBounded<bool>(
            new BoundedChannelOptions(1)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false
            });

    public bool TryQueueScan() =>
        _options.Enabled && _scanRequests.Writer.TryWrite(true);

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
                await WaitForNextScanAsync(interval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        logger.LogInformation("Agent worker is stopping");
    }

    private async Task WaitForNextScanAsync(
        TimeSpan interval,
        CancellationToken stoppingToken)
    {
        using var readCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            stoppingToken);
        var delay = Task.Delay(interval, stoppingToken);
        var scanRequest = _scanRequests.Reader
            .ReadAsync(readCancellation.Token)
            .AsTask();

        var completedTask = await Task.WhenAny(delay, scanRequest);
        if (scanRequest.IsCompleted || completedTask == scanRequest)
        {
            await scanRequest;
            while (_scanRequests.Reader.TryRead(out _))
            {
            }

            return;
        }

        readCancellation.Cancel();
        try
        {
            await scanRequest;
        }
        catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
        {
        }

        await delay;
    }
}
