using AgenticApply.Api.Dtos;
using AgenticApply.Api.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AgenticApply.Api.Endpoints;

public static class JobEndpoints
{
    public static WebApplication MapJobEndpoints(this WebApplication app)
    {
        app.MapGet(
                "/jobs",
                async Task<Ok<IReadOnlyList<JobResponse>>> (
                    IJobQueryService service,
                    CancellationToken cancellationToken) =>
                    TypedResults.Ok(
                        await service.GetAllAsync(cancellationToken)))
            .WithName("GetJobs")
            .WithSummary("Get all jobs")
            .WithDescription("Returns stored jobs with evaluation data when available.")
            .Produces<IReadOnlyList<JobResponse>>(StatusCodes.Status200OK);

        app.MapGet(
                "/jobs/top",
                async Task<Ok<IReadOnlyList<JobResponse>>> (
                    IJobQueryService service,
                    CancellationToken cancellationToken) =>
                    TypedResults.Ok(
                        await service.GetTopAsync(cancellationToken)))
            .WithName("GetTopJobs")
            .WithSummary("Get the top 10 evaluated jobs")
            .WithDescription("Returns evaluated jobs ordered by score, highest first.")
            .Produces<IReadOnlyList<JobResponse>>(StatusCodes.Status200OK);

        app.MapGet(
                "/jobs/{jobId}",
                async Task<Results<Ok<JobResponse>, NotFound>> (
                    string jobId,
                    IJobQueryService service,
                    CancellationToken cancellationToken) =>
                {
                    var job = await service.GetByIdAsync(jobId, cancellationToken);

                    return job is null
                        ? TypedResults.NotFound()
                        : TypedResults.Ok(job);
                })
            .WithName("GetJobById")
            .WithSummary("Get a job by ID")
            .WithDescription("Returns a job and its evaluation, or 404 when the job does not exist.")
            .Produces<JobResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }
}
