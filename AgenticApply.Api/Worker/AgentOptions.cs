namespace AgenticApply.Api.Worker;

public sealed class AgentOptions
{
    public const string SectionName = "Agent";

    public bool Enabled { get; set; } = true;

    public int ScanIntervalMinutes { get; set; } = 120;

    public int MaxNewJobsPerScan { get; set; } = 3;

    public bool DryRun { get; set; }
}
