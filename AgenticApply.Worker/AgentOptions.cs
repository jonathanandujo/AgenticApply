namespace AgenticApply.Worker;

public sealed class AgentOptions
{
    public const string SectionName = "Agent";

    public bool Enabled { get; set; } = true;

    public int ScanIntervalMinutes { get; set; } = 120;

    public bool DryRun { get; set; } = true;
}
