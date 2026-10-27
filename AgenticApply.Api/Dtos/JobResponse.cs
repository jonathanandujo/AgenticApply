namespace AgenticApply.Api.Dtos;

/// <summary>Job details and its evaluation, when one has been recorded.</summary>
public sealed record JobResponse(
    string JobId,
    string Title,
    string Company,
    string Location,
    string Description,
    bool EasyApply,
    string Url,
    EvaluationResponse? Evaluation);
