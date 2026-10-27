using AgenticApply.Agent.Models;

namespace AgenticApply.Agent.Services;

public static class OllamaLogger
{
    private static readonly string LogPath =
        Path.Combine(
            Environment.CurrentDirectory,
            "Logs",
            "ollama.log");

    public static void Log(
    Job job,
    string candidateProfile,
    string prompt,
    string response)
    {
        try
        {
            Console.WriteLine("Entrando al logger...");

            Directory.CreateDirectory(
                Path.GetDirectoryName(LogPath)!);

            var content = $"""

==========================================================
Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}

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

            File.AppendAllText(LogPath, content);

            Console.WriteLine($"Log escrito en: {LogPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }
}