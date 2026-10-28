using AgenticApply.Api.Dtos;

namespace AgenticApply.Api.Services;

public interface IApplicationService
{
    Task<ApplicationStatusResponse?> MarkAppliedAsync(
        string jobId,
        CancellationToken cancellationToken);
}
