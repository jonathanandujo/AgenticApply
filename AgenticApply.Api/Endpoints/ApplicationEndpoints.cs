using AgenticApply.Api.Dtos;
using AgenticApply.Api.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace AgenticApply.Api.Endpoints;

public static class ApplicationEndpoints
{
    public static WebApplication MapApplicationEndpoints(
        this WebApplication app)
    {
        app.MapPost(
                "/jobs/{jobId}/auto-apply",
                async Task<Results<
                    Ok<AutoApplyResponse>,
                    BadRequest<ProblemDetails>,
                    NotFound>> (
                    string jobId,
                    AutoApplyRequest request,
                    IApplicationService service,
                    CancellationToken cancellationToken) =>
                {
                    if (!request.ConfirmSubmission)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Status = StatusCodes.Status400BadRequest,
                            Title = "Submission was not confirmed",
                            Detail = "Confirm the application submission in the dashboard before continuing."
                        });
                    }

                    var result = await service.AutoApplyAsync(
                        jobId,
                        cancellationToken);

                    return result is null
                        ? TypedResults.NotFound()
                        : TypedResults.Ok(result);
                })
            .WithName("AutoApplyToJob")
            .WithSummary("Submit a LinkedIn Easy Apply application")
            .WithDescription("After explicit user confirmation, opens the LinkedIn job in the saved browser session and progresses through Easy Apply. Stops for unanswered required fields and records success only after LinkedIn confirms submission.")
            .Produces<AutoApplyResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        app.MapPost(
                "/jobs/{jobId}/application",
                async Task<Results<Ok<ApplicationStatusResponse>, NotFound>> (
                    string jobId,
                    IApplicationService service,
                    CancellationToken cancellationToken) =>
                {
                    var status = await service.MarkAppliedAsync(
                        jobId,
                        cancellationToken);

                    return status is null
                        ? TypedResults.NotFound()
                        : TypedResults.Ok(status);
                })
            .WithName("MarkJobApplied")
            .WithSummary("Mark a job as applied")
            .WithDescription("Records that the user completed the application process for this job on LinkedIn or the employer's application site.")
            .Produces<ApplicationStatusResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }
}
