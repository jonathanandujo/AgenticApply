using System.ComponentModel.DataAnnotations;

namespace AgenticApply.Api.Dtos;

/// <summary>Payload for selecting the default Ollama model.</summary>
public sealed record UpdateOllamaSettingsRequest
{
    /// <summary>Name of a model available from the configured Ollama instance.</summary>
    [Required, MaxLength(200)]
    public required string Model { get; init; }
}
