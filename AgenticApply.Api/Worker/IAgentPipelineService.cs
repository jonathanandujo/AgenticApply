namespace AgenticApply.Api.Worker;

public interface IAgentPipelineService
{
    Task RunOnceAsync(CancellationToken cancellationToken);
}
