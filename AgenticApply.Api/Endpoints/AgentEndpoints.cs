using AgenticApply.Api.Dtos;
using AgenticApply.Api.Worker;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace AgenticApply.Api.Endpoints;

public static class AgentEndpoints
{
    public static WebApplication MapAgentEndpoints(this WebApplication app)
    {
        app.MapPost(
                "/agent/scan",
                Task<Results<
                    Accepted<ScanRequestAcceptedResponse>,
                    Conflict<ProblemDetails>>> (
                    AgentWorker worker,
                    CancellationToken cancellationToken) =>
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (!worker.TryQueueScan())
                    {
                        return Task.FromResult<
                            Results<
                                Accepted<ScanRequestAcceptedResponse>,
                                Conflict<ProblemDetails>>>(
                            TypedResults.Conflict(new ProblemDetails
                            {
                                Status = StatusCodes.Status409Conflict,
                                Title = "Scan unavailable",
                                Detail = "The agent is disabled or an immediate scan is already queued."
                            }));
                    }

                    return Task.FromResult<
                        Results<
                            Accepted<ScanRequestAcceptedResponse>,
                            Conflict<ProblemDetails>>>(
                        TypedResults.Accepted(
                            "/agent/scan",
                            new ScanRequestAcceptedResponse(
                                "A scan was queued and will run as soon as the worker is available.")));
                })
            .WithName("RequestAgentScan")
            .WithSummary("Queue an immediate agent scan")
            .WithDescription("Queues a LinkedIn scan and evaluation cycle without waiting for the configured interval. The worker runs it after any current cycle completes.")
            .Produces<ScanRequestAcceptedResponse>(StatusCodes.Status202Accepted)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }
}
