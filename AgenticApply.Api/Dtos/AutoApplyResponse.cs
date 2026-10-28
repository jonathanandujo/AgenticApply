namespace AgenticApply.Api.Dtos;

/// <summary>Outcome of an automated LinkedIn Easy Apply attempt.</summary>
public sealed record AutoApplyResponse(
    string Status,
    string Message,
    bool Applied);
