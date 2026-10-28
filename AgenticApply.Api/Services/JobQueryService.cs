using AgenticApply.Agent.Data;
using AgenticApply.Agent.Models;
using AgenticApply.Api.Dtos;

namespace AgenticApply.Api.Services;

public sealed class JobQueryService(
    JobRepository jobRepository,
    EvaluationRepository evaluationRepository) : IJobQueryService
{
    private const int TopJobsLimit = 10;

    public async Task<IReadOnlyList<JobResponse>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        var (jobs, evaluations) = await LoadJobsAndEvaluationsAsync(cancellationToken);

        return jobs
            .Select(job => ToResponse(job, evaluations.GetValueOrDefault(job.JobId)))
            .ToList();
    }

    public async Task<IReadOnlyList<JobResponse>> GetTopAsync(
        CancellationToken cancellationToken)
    {
        var (jobs, evaluations) = await LoadJobsAndEvaluationsAsync(cancellationToken);

        return jobs
            .Where(job => evaluations.ContainsKey(job.JobId))
            .OrderByDescending(job => evaluations[job.JobId].Score)
            .ThenBy(job => job.Title, StringComparer.OrdinalIgnoreCase)
            .ThenBy(job => job.JobId, StringComparer.Ordinal)
            .Take(TopJobsLimit)
            .Select(job => ToResponse(job, evaluations[job.JobId]))
            .ToList();
    }

    public async Task<JobResponse?> GetByIdAsync(
        string jobId,
        CancellationToken cancellationToken)
    {
        var job = await jobRepository.GetAsync(jobId, cancellationToken);
        if (job is null)
        {
            return null;
        }

        var evaluation = await evaluationRepository.GetAsync(
            jobId,
            cancellationToken);

        return ToResponse(job, evaluation);
    }

    public async Task<StatsResponse> GetStatsAsync(
        CancellationToken cancellationToken)
    {
        var (jobs, evaluations) = await LoadJobsAndEvaluationsAsync(cancellationToken);
        var jobEvaluations = jobs
            .Select(job => evaluations.GetValueOrDefault(job.JobId))
            .Where(evaluation => evaluation is not null)
            .Cast<JobEvaluation>()
            .ToList();

        return new StatsResponse(
            TotalJobs: jobs.Count,
            EvaluatedJobs: jobEvaluations.Count,
            RecommendedToApply: jobEvaluations.Count(evaluation => evaluation.Apply),
            AverageScore: jobEvaluations.Count == 0
                ? null
                : (decimal)jobEvaluations.Average(evaluation => evaluation.Score));
    }

    private async Task<(
        List<Job> Jobs,
        Dictionary<string, JobEvaluation> Evaluations)> LoadJobsAndEvaluationsAsync(
        CancellationToken cancellationToken)
    {
        var jobsTask = jobRepository.GetAllAsync(cancellationToken);
        var evaluationsTask = evaluationRepository.GetAllAsync(cancellationToken);

        await Task.WhenAll(jobsTask, evaluationsTask);

        return (await jobsTask, await evaluationsTask);
    }

    private static JobResponse ToResponse(Job job, JobEvaluation? evaluation) =>
        new(
            job.JobId,
            job.Title,
            job.Company,
            job.Location,
            job.Description,
            job.EasyApply,
            job.ApplyAvailable,
            job.Url,
            evaluation is null
                ? null
                : new EvaluationResponse(
                    evaluation.Score,
                    evaluation.Apply,
                    evaluation.Reason,
                    evaluation.Model));
}
