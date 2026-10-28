using AgenticApply.Api.Dtos;

namespace AgenticApply.Api.Services;

public interface IJobQueryService
{
    Task<IReadOnlyList<JobResponse>> GetAllAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<JobResponse>> GetTopAsync(
        bool easyApplyOnly,
        CancellationToken cancellationToken);

    Task<JobResponse?> GetByIdAsync(
        string jobId,
        CancellationToken cancellationToken);

    Task<StatsResponse> GetStatsAsync(CancellationToken cancellationToken);
}
