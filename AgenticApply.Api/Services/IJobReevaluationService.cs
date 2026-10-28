using AgenticApply.Api.Dtos;

namespace AgenticApply.Api.Services;

public interface IJobReevaluationService
{
    Task<JobResponse?> ReevaluateAsync(
        string jobId,
        string? model,
        CancellationToken cancellationToken);
}
