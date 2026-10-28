using AgenticApply.Agent.Data;
using AgenticApply.Agent.Services;
using AgenticApply.Api.Dtos;

namespace AgenticApply.Api.Services;

public sealed class OllamaSettingsService(
    AgentSettingsRepository settingsRepository,
    OllamaService ollamaService) : IOllamaSettingsService
{
    public async Task<OllamaSettingsResponse> GetAsync(
        CancellationToken cancellationToken)
    {
        var models = await ollamaService.GetAvailableModelsAsync(cancellationToken);
        var selectedModel = await settingsRepository.GetOllamaModelAsync(
            cancellationToken)
            ?? ollamaService.ConfiguredModel;

        return new OllamaSettingsResponse(selectedModel, models);
    }

    public async Task<OllamaSettingsResponse?> UpdateAsync(
        string model,
        CancellationToken cancellationToken)
    {
        var models = await ollamaService.GetAvailableModelsAsync(cancellationToken);
        var selectedModel = models.FirstOrDefault(
            availableModel => string.Equals(
                availableModel,
                model,
                StringComparison.OrdinalIgnoreCase));

        if (selectedModel is null)
        {
            return null;
        }

        await settingsRepository.SetOllamaModelAsync(
            selectedModel,
            cancellationToken);

        return new OllamaSettingsResponse(selectedModel, models);
    }
}
