namespace AgenticApply.Agent.Models;

public class JobEvaluation
{
    public int Score { get; set; }
    public bool Apply { get; set; }
    public string Reason { get; set; } = "";
    public string? Model { get; set; }
}
