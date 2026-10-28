using AgenticApply.Api.Dtos;
using AgenticApply.Api.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AgenticApply.Api.Endpoints;

public static class ApplicationEndpoints
{
    public static WebApplication MapApplicationEndpoints(
        this WebApplication app)
    {
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
