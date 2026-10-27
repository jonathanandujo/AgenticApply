namespace AgenticApply.Api.Dtos;

/// <summary>The scoring result recorded for a job.</summary>
public sealed record EvaluationResponse(
    int Score,
    bool Apply,
    string Reason);
