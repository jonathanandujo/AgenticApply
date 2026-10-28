using AgenticApply.Api.Dtos;
using AgenticApply.Api.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

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

        app.MapPost(
                "/jobs/{jobId}/reevaluate",
                async Task<Results<
                    Ok<JobResponse>,
                    NotFound,
                    BadRequest<ProblemDetails>,
                    ProblemHttpResult>> (
                    string jobId,
                    ReevaluateJobRequest request,
                    IJobReevaluationService service,
                    ILogger<JobReevaluationService> logger,
                    CancellationToken cancellationToken) =>
                {
                    if (request.Model is { } model
                        && (string.IsNullOrWhiteSpace(model) || model.Length > 100))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Status = StatusCodes.Status400BadRequest,
                            Title = "Invalid model name",
                            Detail = "Model must be non-empty and no longer than 100 characters."
                        });
                    }

                    try
                    {
                        var job = await service.ReevaluateAsync(
                            jobId,
                            request.Model,
                            cancellationToken);

                        return job is null
                            ? TypedResults.NotFound()
                            : TypedResults.Ok(job);
                    }
                    catch (HttpRequestException exception)
                    {
                        logger.LogError(
                            exception,
                            "Ollama failed to re-evaluate job {JobId} using model {Model}",
                            jobId,
                            request.Model);

                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status502BadGateway,
                            title: "Ollama evaluation failed",
                            detail: "The evaluation service could not complete the request.");
                    }
                    catch (EvaluationUnavailableException exception)
                    {
                        logger.LogError(
                            exception,
                            "Ollama returned no evaluation for job {JobId}",
                            jobId);

                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status502BadGateway,
                            title: "Ollama returned no evaluation",
                            detail: "The evaluation service returned an empty result.");
                    }
                    catch (OperationCanceledException exception)
                        when (!cancellationToken.IsCancellationRequested)
                    {
                        logger.LogError(
                            exception,
                            "Ollama timed out while re-evaluating job {JobId}",
                            jobId);

                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status504GatewayTimeout,
                            title: "Ollama evaluation timed out",
                            detail: "The evaluation service did not respond in time.");
                    }
                })
            .WithName("ReevaluateJob")
            .WithSummary("Re-evaluate a job")
            .WithDescription("Scores a stored job again using the configured Ollama model or an optional model override, then replaces its stored evaluation.")
            .Produces<JobResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status502BadGateway)
            .ProducesProblem(StatusCodes.Status504GatewayTimeout);

        return app;
    }
}
