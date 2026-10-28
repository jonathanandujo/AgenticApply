namespace AgenticApply.Api.Dtos;

/// <summary>Available Ollama models and the model selected for agent evaluations.</summary>
public sealed record OllamaSettingsResponse(
    string Model,
    IReadOnlyList<string> AvailableModels);
