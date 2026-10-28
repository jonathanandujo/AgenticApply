using AgenticApply.Agent.Data;
using AgenticApply.Api.Dtos;
using Microsoft.Extensions.Options;

namespace AgenticApply.Api.Services;

public sealed class LinkedInSettingsService(
    AgentSettingsRepository settingsRepository,
    IOptions<LinkedInSettings> linkedInSettings) : ILinkedInSettingsService
{
    public async Task<LinkedInSearchSettingsResponse> GetAsync(
        CancellationToken cancellationToken)
    {
        var jobsUrl = await settingsRepository.GetLinkedInJobsUrlAsync(
            cancellationToken);

        return new LinkedInSearchSettingsResponse(
            jobsUrl ?? linkedInSettings.Value.JobsUrl);
    }

    public async Task<LinkedInSearchSettingsResponse> UpdateAsync(
        string jobsUrl,
        CancellationToken cancellationToken)
    {
        await settingsRepository.SetLinkedInJobsUrlAsync(
            jobsUrl,
            cancellationToken);

        return new LinkedInSearchSettingsResponse(jobsUrl);
    }
}
