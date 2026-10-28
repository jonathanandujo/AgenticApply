namespace AgenticApply.Api.Dtos;

/// <summary>Confirms user intent to submit a job application via LinkedIn.</summary>
public sealed record AutoApplyRequest(bool ConfirmSubmission);
