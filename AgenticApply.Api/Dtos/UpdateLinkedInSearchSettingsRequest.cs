using System.ComponentModel.DataAnnotations;

namespace AgenticApply.Api.Dtos;

/// <summary>Payload for updating the LinkedIn job-search URL.</summary>
public sealed record UpdateLinkedInSearchSettingsRequest
{
    /// <summary>The HTTPS URL of a LinkedIn jobs search.</summary>
    [Required, MaxLength(2048)]
    public required string JobsUrl { get; init; }
}
