using AgenticApply.Agent.Data;
using AgenticApply.Agent.Models;
using AgenticApply.Api.Dtos;

namespace AgenticApply.Api.Services;

public sealed class JobQueryService(
    JobRepository jobRepository,
    EvaluationRepository evaluationRepository,
    ApplicationRepository applicationRepository) : IJobQueryService
{
    private const int TopJobsLimit = 10;

    public async Task<IReadOnlyList<JobResponse>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        var (jobs, evaluations, appliedJobIds) =
            await LoadJobsAndEvaluationsAsync(cancellationToken);

        return jobs
            .Select(job => ToResponse(
                job,
                evaluations.GetValueOrDefault(job.JobId),
                appliedJobIds.Contains(job.JobId)))
            .ToList();
    }

    public async Task<IReadOnlyList<JobResponse>> GetTopAsync(
        CancellationToken cancellationToken)
    {
        var (jobs, evaluations, appliedJobIds) =
            await LoadJobsAndEvaluationsAsync(cancellationToken);

        return jobs
            .Where(job => evaluations.ContainsKey(job.JobId))
            .OrderByDescending(job => evaluations[job.JobId].Score)
            .ThenBy(job => job.Title, StringComparer.OrdinalIgnoreCase)
            .ThenBy(job => job.JobId, StringComparer.Ordinal)
            .Take(TopJobsLimit)
            .Select(job => ToResponse(
                job,
                evaluations[job.JobId],
                appliedJobIds.Contains(job.JobId)))
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
        var appliedJobIds = await applicationRepository.GetAppliedJobIdsAsync(
            cancellationToken);

        return ToResponse(job, evaluation, appliedJobIds.Contains(jobId));
    }

    public async Task<StatsResponse> GetStatsAsync(
        CancellationToken cancellationToken)
    {
        var (jobs, evaluations, appliedJobIds) =
            await LoadJobsAndEvaluationsAsync(cancellationToken);
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
                : (decimal)jobEvaluations.Average(evaluation => evaluation.Score),
            AppliedJobs: appliedJobIds.Count);
    }

    private async Task<(
        List<Job> Jobs,
        Dictionary<string, JobEvaluation> Evaluations,
        HashSet<string> AppliedJobIds)> LoadJobsAndEvaluationsAsync(
        CancellationToken cancellationToken)
    {
        var jobsTask = jobRepository.GetAllAsync(cancellationToken);
        var evaluationsTask = evaluationRepository.GetAllAsync(cancellationToken);
        var appliedJobIdsTask =
            applicationRepository.GetAppliedJobIdsAsync(cancellationToken);

        await Task.WhenAll(jobsTask, evaluationsTask, appliedJobIdsTask);

        return (await jobsTask, await evaluationsTask, await appliedJobIdsTask);
    }

    private static JobResponse ToResponse(
        Job job,
        JobEvaluation? evaluation,
        bool applied) =>
        new(
            job.JobId,
            job.Title,
            job.Company,
            job.Location,
            job.Description,
            job.EasyApply,
            job.ApplyAvailable,
            applied,
            job.Url,
            evaluation is null
                ? null
                : new EvaluationResponse(
                    evaluation.Score,
                    evaluation.Apply,
                    evaluation.Reason,
                    evaluation.Model));
}
