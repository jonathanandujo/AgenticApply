namespace AgenticApply.Api.Services;

public sealed class EvaluationUnavailableException(string jobId)
    : Exception($"Ollama did not return an evaluation for job {jobId}.")
{
}
