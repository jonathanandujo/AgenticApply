using Microsoft.Data.Sqlite;

namespace AgenticApply.Agent.Data;

public sealed class ApplicationRepository(string connectionString)
{
    public async Task<HashSet<string>> GetAppliedJobIdsAsync(
        CancellationToken cancellationToken = default)
    {
        var jobIds = new HashSet<string>(StringComparer.Ordinal);

        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT JobId
            FROM Applications
            WHERE Status = 'Applied'
            """;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            jobIds.Add(reader.GetString(0));
        }

        return jobIds;
    }

    public async Task<int> GetAppliedCountAsync(
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT COUNT(*)
            FROM Applications
            WHERE Status = 'Applied'
            """;

        return Convert.ToInt32(
            await command.ExecuteScalarAsync(cancellationToken));
    }

    public async Task<bool> MarkAppliedAsync(
        string jobId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO Applications (JobId, Status, AppliedAt)
            SELECT $jobId, 'Applied', $appliedAt
            WHERE EXISTS (SELECT 1 FROM Jobs WHERE JobId = $jobId)
            ON CONFLICT(JobId) DO UPDATE SET
                Status = 'Applied',
                AppliedAt = excluded.AppliedAt
            """;
        command.Parameters.AddWithValue("$jobId", jobId);
        command.Parameters.AddWithValue(
            "$appliedAt",
            DateTimeOffset.UtcNow.ToString("O"));

        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }
}
