using AgenticApply.Agent.Models;

namespace AgenticApply.Agent.Services;

public static class OllamaLogger
{
    public static void Log(
        Job job,
        string candidateProfile,
        string prompt,
        string response,
        string? model = null,
        string? logPath = null)
    {
        var resolvedLogPath = string.IsNullOrWhiteSpace(logPath)
            ? Path.Combine(
                Environment.CurrentDirectory,
                "Logs",
                "ollama.log")
            : Path.GetFullPath(logPath);

        try
        {
            Directory.CreateDirectory(
                Path.GetDirectoryName(resolvedLogPath)!);

            var content = $"""

==========================================================
Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}
Model: {model ?? "unknown"}

JOB:
Title: {job.Title}
Company: {job.Company}
Location: {job.Location}

CANDIDATE PROFILE:
{candidateProfile}

PROMPT:
{prompt}

RESPONSE:
{response}

==========================================================
""";

            File.AppendAllText(resolvedLogPath, content);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(
                $"Failed to write Ollama log to '{resolvedLogPath}': {ex}");
        }
    }
}