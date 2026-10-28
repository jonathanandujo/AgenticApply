namespace AgenticApply.Api.Dtos;

/// <summary>Aggregate counts and scoring statistics for stored jobs.</summary>
public sealed record StatsResponse(
    int TotalJobs,
    int EvaluatedJobs,
    int RecommendedToApply,
    decimal? AverageScore,
    int AppliedJobs);
