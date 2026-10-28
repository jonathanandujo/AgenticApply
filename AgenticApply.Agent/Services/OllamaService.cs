using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;

// using AgenticApply.Agent.Config;
using AgenticApply.Agent.Models;
using Microsoft.Extensions.Options;

namespace AgenticApply.Agent.Services;

public class OllamaService
{
    private readonly HttpClient _httpClient;
    private readonly OllamaSettings _settings;
    private readonly CandidateProfileService _candidateProfileService;

    public OllamaService(
        HttpClient httpClient,
        IOptions<OllamaSettings> options,
        CandidateProfileService candidateProfileService)
    {
        _httpClient = httpClient;
        _settings = options.Value;
        _candidateProfileService = candidateProfileService;
    }

    public async Task<JobEvaluation?> EvaluateJobAsync(
        Job job,
        string? model = null,
        CancellationToken cancellationToken = default)
    {
        var modelToUse = string.IsNullOrWhiteSpace(model)
            ? _settings.Model
            : model.Trim();

        var jsonExample =
"""
{
    "score": 100,
    "apply": true,
    "reason": "Reason for the score and apply decision."
}
""";
        var description =
            job.Description.Length > 3000
                ? job.Description[..3000]
                : job.Description;

        var candidateProfile =
            _candidateProfileService.GetProfile();

        var prompt = $"""
You are evaluating a job opportunity for a Senior .NET Engineer.

Candidate Profile:
{candidateProfile}

Job:

Title:
{job.Title}

Company:
{job.Company}

Location:
{job.Location}

Description:
{description}

Return ONLY valid JSON.

Required schema:

{jsonExample}

Rules:

- score must be between 0 and 100
- apply must be true only if score >= 80
- do not return markdown
- do not return explanations
- do not return extra properties
- output JSON only
""";

        var request = new
        {
            model = modelToUse,
            stream = false,
            format = "json",
            prompt
        };

        Console.WriteLine($"Model: {modelToUse}");

        var response = await _httpClient.PostAsJsonAsync(
            $"{_settings.BaseUrl}/api/generate",
            request,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var ollamaResponse =
            await response.Content.ReadFromJsonAsync<OllamaResponse>(
                cancellationToken);

        if (ollamaResponse is null)
        {
            return null;
        }

        OllamaLogger.Log(
            job,
            candidateProfile,
            prompt,
            ollamaResponse.Response,
            modelToUse,
            _settings.LogPath);

        Console.WriteLine("===== OLLAMA RESPONSE =====");
        Console.WriteLine(ollamaResponse.Response);
        Console.WriteLine("===========================");

        try
        {
            var root = JsonNode.Parse(
    ollamaResponse.Response);

            return new JobEvaluation
            {
                Model = modelToUse,
                Score =
                    root?["score"]?.GetValue<int>() ?? 0,

                Apply =
                    root?["apply"]?.GetValue<bool>() ?? false,

                Reason =
                    root?["reason"]?.GetValue<string>() ?? ""
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine("No pude parsear:");
            Console.WriteLine(ollamaResponse.Response);
            Console.WriteLine(ex.Message);

            return null;
        }
    }
}