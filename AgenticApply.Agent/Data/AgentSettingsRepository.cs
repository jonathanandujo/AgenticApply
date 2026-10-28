using Microsoft.Data.Sqlite;

namespace AgenticApply.Agent.Data;

public sealed class AgentSettingsRepository(string connectionString)
{
    private const string LinkedInJobsUrlKey = "LinkedInJobsUrl";

    public async Task<string?> GetLinkedInJobsUrlAsync(
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT SettingValue
            FROM AgentSettings
            WHERE SettingKey = $key
            """;
        command.Parameters.AddWithValue("$key", LinkedInJobsUrlKey);

        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is string url ? url : null;
    }

    public async Task SetLinkedInJobsUrlAsync(
        string jobsUrl,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO AgentSettings (SettingKey, SettingValue, UpdatedAt)
            VALUES ($key, $value, $updatedAt)
            ON CONFLICT(SettingKey) DO UPDATE SET
                SettingValue = excluded.SettingValue,
                UpdatedAt = excluded.UpdatedAt
            """;
        command.Parameters.AddWithValue("$key", LinkedInJobsUrlKey);
        command.Parameters.AddWithValue("$value", jobsUrl);
        command.Parameters.AddWithValue("$updatedAt", DateTimeOffset.UtcNow.ToString("O"));

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
