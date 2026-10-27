namespace AgenticApply.Agent.Models;

public class Job
{
    public string JobId { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Company { get; set; } = "";
    public string Location { get; set; } = "";
    public string Url { get; set; } = "";
    public bool EasyApply { get; set; }
}