namespace AgenticApply.Agent.Services;

public class CandidateProfileService
{
    private readonly string _path =
        Path.Combine(
            Environment.CurrentDirectory,
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