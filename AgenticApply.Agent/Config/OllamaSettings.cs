public sealed class OllamaSettings
{
    public string BaseUrl { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public int TimeoutMinutes { get; set; }
}