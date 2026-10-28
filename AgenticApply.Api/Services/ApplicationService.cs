using AgenticApply.Agent.Data;
using AgenticApply.Api.Dtos;

namespace AgenticApply.Api.Services;

public sealed class ApplicationService(
    ApplicationRepository applicationRepository) : IApplicationService
{
    public async Task<ApplicationStatusResponse?> MarkAppliedAsync(
        string jobId,
        CancellationToken cancellationToken)
    {
        var markedApplied = await applicationRepository.MarkAppliedAsync(
            jobId,
            cancellationToken);

        return markedApplied
            ? new ApplicationStatusResponse(Applied: true)
            : null;
    }
}
