using AgenticApply.Api.Dtos;
using AgenticApply.Api.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AgenticApply.Api.Endpoints;

public static class StatsEndpoints
{
    public static WebApplication MapStatsEndpoint(this WebApplication app)
    {
        app.MapGet(
                "/stats",
                async Task<Ok<StatsResponse>> (
                    IJobQueryService service,
                    CancellationToken cancellationToken) =>
                    TypedResults.Ok(
                        await service.GetStatsAsync(cancellationToken)))
            .WithName("GetStats")
            .WithSummary("Get job and evaluation statistics")
            .WithDescription("Returns job totals, evaluation totals, apply recommendations, and the average score.")
            .Produces<StatsResponse>(StatusCodes.Status200OK);

        return app;
    }
}
