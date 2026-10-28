using AgenticApply.Agent.Data;
using AgenticApply.Agent.Services;
using AgenticApply.Api.Dtos;

namespace AgenticApply.Api.Services;

public sealed class JobReevaluationService(
    JobRepository jobRepository,
    EvaluationRepository evaluationRepository,
    OllamaService ollamaService,
    IJobQueryService jobQueryService) : IJobReevaluationService
{
    public async Task<JobResponse?> ReevaluateAsync(
        string jobId,
        string? model,
        CancellationToken cancellationToken)
    {
        var job = await jobRepository.GetAsync(jobId, cancellationToken);
        if (job is null)
        {
            return null;
        }

        var evaluation = await ollamaService.EvaluateJobAsync(
            job,
            model,
            cancellationToken);

        if (evaluation is null)
        {
            throw new EvaluationUnavailableException(jobId);
        }

        await evaluationRepository.InsertAsync(
            jobId,
            evaluation,
            cancellationToken);

        return await jobQueryService.GetByIdAsync(jobId, cancellationToken);
    }
}
