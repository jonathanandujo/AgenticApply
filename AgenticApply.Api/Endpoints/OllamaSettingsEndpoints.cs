using AgenticApply.Api.Dtos;
using AgenticApply.Api.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace AgenticApply.Api.Endpoints;

public static class OllamaSettingsEndpoints
{
    public static WebApplication MapOllamaSettingsEndpoints(
        this WebApplication app)
    {
        app.MapGet(
                "/settings/ollama",
                async Task<Ok<OllamaSettingsResponse>> (
                    IOllamaSettingsService service,
                    CancellationToken cancellationToken) =>
                    TypedResults.Ok(await service.GetAsync(cancellationToken)))
            .WithName("GetOllamaSettings")
            .WithSummary("Get available Ollama models")
            .WithDescription("Returns the models available from Ollama and the default model used for evaluations.")
            .Produces<OllamaSettingsResponse>(StatusCodes.Status200OK);

        app.MapPut(
                "/settings/ollama",
                async Task<Results<
                    Ok<OllamaSettingsResponse>,
                    BadRequest<ProblemDetails>>> (
                    UpdateOllamaSettingsRequest request,
                    IOllamaSettingsService service,
                    CancellationToken cancellationToken) =>
                {
                    if (string.IsNullOrWhiteSpace(request.Model)
                        || request.Model.Length > 200)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Status = StatusCodes.Status400BadRequest,
                            Title = "Invalid Ollama model",
                            Detail = "Select an available Ollama model."
                        });
                    }

                    var settings = await service.UpdateAsync(
                        request.Model.Trim(),
                        cancellationToken);

                    return settings is null
                        ? TypedResults.BadRequest(new ProblemDetails
                        {
                            Status = StatusCodes.Status400BadRequest,
                            Title = "Ollama model unavailable",
                            Detail = "Select a model currently available from Ollama."
                        })
                        : TypedResults.Ok(settings);
                })
            .WithName("UpdateOllamaSettings")
            .WithSummary("Set the default Ollama model")
            .WithDescription("Persists a model currently available from Ollama as the default for worker and manual evaluations.")
            .Produces<OllamaSettingsResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return app;
    }
}
