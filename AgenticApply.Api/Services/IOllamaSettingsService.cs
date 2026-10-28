using AgenticApply.Api.Dtos;

namespace AgenticApply.Api.Services;

public interface IOllamaSettingsService
{
    Task<OllamaSettingsResponse> GetAsync(CancellationToken cancellationToken);

    Task<OllamaSettingsResponse?> UpdateAsync(
        string model,
        CancellationToken cancellationToken);
}
