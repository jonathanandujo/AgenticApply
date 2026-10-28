namespace AgenticApply.Agent.Models;

public enum EasyApplyAttemptStatus
{
    Submitted,
    NotFound,
    NotEasyApply,
    AlreadyApplied,
    NeedsManualInput,
    SubmissionUnconfirmed
}

public sealed record EasyApplyAttemptResult(
    EasyApplyAttemptStatus Status,
    string Message);
