using AgenticApply.Agent.Data;
using AgenticApply.Agent.Services;
using Microsoft.Extensions.Options;

namespace AgenticApply.Worker;

public sealed class AgentPipelineService(
    BrowserService browserService,
    JobRepository jobRepository,
    EvaluationRepository evaluationRepository,
    AgentSettingsRepository agentSettingsRepository,
    OllamaService ollamaService,
    IOptions<AgentOptions> options,
    IOptions<LinkedInSettings> linkedInSettings,
    ILogger<AgentPipelineService> logger) : IAgentPipelineService
{
    private readonly AgentOptions _options = options.Value;

    public async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Starting LinkedIn job scan. DryRun: {DryRun}",
            _options.DryRun);

        var jobsUrl = await agentSettingsRepository.GetLinkedInJobsUrlAsync(
            cancellationToken)
            ?? linkedInSettings.Value.JobsUrl;

        // Scraping persists new jobs in live mode; dry-run returns them without database writes.
        var scannedJobs = await browserService.GetJobsAsync(
            persistJobs: !_options.DryRun,
            cancellationToken: cancellationToken,
            jobsUrl: jobsUrl);

        cancellationToken.ThrowIfCancellationRequested();

        var jobsToEvaluate = await jobRepository.GetAllAsync();
        if (_options.DryRun)
        {
            jobsToEvaluate.AddRange(scannedJobs);
        }

        var jobs = jobsToEvaluate
            .DistinctBy(job => job.JobId)
            .ToList();

        logger.LogInformation(
            "Found {JobCount} stored or newly scanned jobs to check for evaluations",
            jobs.Count);

        foreach (var job in jobs)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (await evaluationRepository.ExistsAsync(job.JobId))
            {
                logger.LogDebug(
                    "Skipping job {JobId}; an evaluation already exists",
                    job.JobId);
                continue;
            }

            var evaluation = await ollamaService.EvaluateJobAsync(job);
            if (evaluation is null)
            {
                logger.LogWarning(
                    "Ollama returned no evaluation for job {JobId}",
                    job.JobId);
                continue;
            }

            if (!_options.DryRun)
            {
                await evaluationRepository.InsertAsync(job.JobId, evaluation);
            }

            logger.LogInformation(
                "Evaluated job {JobId} ({Title}) with score {Score}; Apply: {Apply}; Persisted: {Persisted}",
                job.JobId,
                job.Title,
                evaluation.Score,
                evaluation.Apply,
                !_options.DryRun);
        }

        logger.LogInformation("LinkedIn job scan and evaluation cycle completed");
    }
}
