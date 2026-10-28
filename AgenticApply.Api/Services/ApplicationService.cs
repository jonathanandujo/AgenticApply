using AgenticApply.Agent.Data;
using AgenticApply.Agent.Models;
using AgenticApply.Agent.Services;
using AgenticApply.Api.Dtos;

namespace AgenticApply.Api.Services;

public sealed class ApplicationService(
    ApplicationRepository applicationRepository,
    BrowserService browserService) : IApplicationService
{
    private static readonly SemaphoreSlim AutoApplyLock = new(1, 1);

    public async Task<ApplicationStatusResponse?> MarkAppliedAsync(
        string jobId,
        CancellationToken cancellationToken)
    {
        var markedApplied = await applicationRepository.MarkAppliedAsync(
            jobId,
            cancellationToken);

        return markedApplied
            ? new ApplicationStatusResponse(Applied: true)
            : null;
    }

    public async Task<AutoApplyResponse?> AutoApplyAsync(
        string jobId,
        CancellationToken cancellationToken)
    {
        await AutoApplyLock.WaitAsync(cancellationToken);
        try
        {
            var appliedJobIds = await applicationRepository.GetAppliedJobIdsAsync(
                cancellationToken);
            if (appliedJobIds.Contains(jobId))
            {
                return new AutoApplyResponse(
                    "AlreadyApplied",
                    "This job is already marked as applied.",
                    Applied: true);
            }

            var result = await browserService.ApplyEasyApplyAsync(
                jobId,
                cancellationToken);

            if (result.Status == EasyApplyAttemptStatus.NotFound)
            {
                return null;
            }

            var applied = result.Status == EasyApplyAttemptStatus.Submitted;
            if (applied)
            {
                var markedApplied = await applicationRepository.MarkAppliedAsync(
                    jobId,
                    cancellationToken);
                if (!markedApplied)
                {
                    throw new InvalidOperationException(
                        $"LinkedIn confirmed application for job {jobId}, but it could not be recorded.");
                }
            }

            return new AutoApplyResponse(
                result.Status.ToString(),
                result.Message,
                applied);
        }
        finally
        {
            AutoApplyLock.Release();
        }
    }
}
