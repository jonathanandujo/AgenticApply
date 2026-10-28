using AgenticApply.Api.Dtos;
using AgenticApply.Api.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace AgenticApply.Api.Endpoints;

public static class LinkedInSettingsEndpoints
{
    public static WebApplication MapLinkedInSettingsEndpoints(
        this WebApplication app)
    {
        app.MapGet(
                "/settings/linkedin-search",
                async Task<Ok<LinkedInSearchSettingsResponse>> (
                    ILinkedInSettingsService service,
                    CancellationToken cancellationToken) =>
                    TypedResults.Ok(await service.GetAsync(cancellationToken)))
            .WithName("GetLinkedInSearchSettings")
            .WithSummary("Get the LinkedIn search URL")
            .WithDescription("Returns the search URL the worker will use on its next scan.")
            .Produces<LinkedInSearchSettingsResponse>(StatusCodes.Status200OK);

        app.MapPut(
                "/settings/linkedin-search",
                async Task<Results<
                    Ok<LinkedInSearchSettingsResponse>,
                    BadRequest<ProblemDetails>>> (
                    UpdateLinkedInSearchSettingsRequest request,
                    ILinkedInSettingsService service,
                    CancellationToken cancellationToken) =>
                {
                    if (!IsValidLinkedInJobsUrl(request.JobsUrl))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Status = StatusCodes.Status400BadRequest,
                            Title = "Invalid LinkedIn jobs URL",
                            Detail = "Provide an HTTPS URL on linkedin.com that points to a jobs search."
                        });
                    }

                    return TypedResults.Ok(await service.UpdateAsync(
                        request.JobsUrl.Trim(),
                        cancellationToken));
                })
            .WithName("UpdateLinkedInSearchSettings")
            .WithSummary("Update the LinkedIn search URL")
            .WithDescription("Persists a LinkedIn jobs search URL for the worker to use on its next scan.")
            .Produces<LinkedInSearchSettingsResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return app;
    }

    private static bool IsValidLinkedInJobsUrl(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && value.Length <= 2048
        && Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && uri.UserInfo.Length == 0
        && uri.Scheme == Uri.UriSchemeHttps
        && (uri.Host.Equals("linkedin.com", StringComparison.OrdinalIgnoreCase)
            || uri.Host.EndsWith(".linkedin.com", StringComparison.OrdinalIgnoreCase))
        && (uri.AbsolutePath.Equals(
                "/jobs/search",
                StringComparison.OrdinalIgnoreCase)
            || uri.AbsolutePath.StartsWith(
                "/jobs/search/",
                StringComparison.OrdinalIgnoreCase));
}
