namespace AgenticApply.Worker;

public interface IAgentPipelineService
{
    Task RunOnceAsync(CancellationToken cancellationToken);
}
