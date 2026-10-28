namespace AgenticApply.Agent.Services;

public class CandidateProfileService
{
    private readonly string _path =
        Path.Combine(
            AppContext.BaseDirectory,
            "Knowledge",
            "CandidateProfile.md");

    public string GetProfile()
    {

        if (!File.Exists(_path))
        {
            return string.Empty;
        }

        return File.ReadAllText(_path);
    }
}