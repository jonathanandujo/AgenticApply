namespace AgenticApply.Api.Dtos;

/// <summary>Options for scoring a job again, optionally using a different Ollama model.</summary>
public sealed record ReevaluateJobRequest
{
    /// <summary>
    /// Optional Ollama model name. When omitted, the API's configured model is used.
    /// </summary>
    public string? Model { get; init; }
}
