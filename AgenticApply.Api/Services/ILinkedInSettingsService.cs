using AgenticApply.Api.Dtos;

namespace AgenticApply.Api.Services;

public interface ILinkedInSettingsService
{
    Task<LinkedInSearchSettingsResponse> GetAsync(
        CancellationToken cancellationToken);

    Task<LinkedInSearchSettingsResponse> UpdateAsync(
        string jobsUrl,
        CancellationToken cancellationToken);
}
