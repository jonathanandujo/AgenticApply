using AgenticApply.Agent.Models;
using Microsoft.Data.Sqlite;

namespace AgenticApply.Agent.Data;

public class JobRepository
{
    private const string DefaultConnectionString =
        "Data Source=Data/AgenticApply.db";

    private readonly string _connectionString;

    public JobRepository()
        : this(DefaultConnectionString)
    {
    }

    public JobRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<bool> ExistsAsync(
        string jobId,
        CancellationToken cancellationToken = default)
    {
        await using var connection =
            new SqliteConnection(_connectionString);

        await connection.OpenAsync(cancellationToken);

        var command = connection.CreateCommand();

        command.CommandText =
        """
        SELECT COUNT(*)
        FROM Jobs
        WHERE JobId = $jobId
        """;

        command.Parameters.AddWithValue(
            "$jobId",
            jobId);

        var count =
            Convert.ToInt32(
                await command.ExecuteScalarAsync(cancellationToken));

        return count > 0;
    }

    public async Task InsertAsync(
        Job job,
        CancellationToken cancellationToken = default)
    {
        await using var connection =
            new SqliteConnection(_connectionString);

        await connection.OpenAsync(cancellationToken);

        var command = connection.CreateCommand();

        command.CommandText =
        """
        INSERT OR REPLACE INTO Jobs
        (
            JobId,
            Title,
            Company,
            Location,
            Description,
            EasyApply,
            ApplyAvailable,
            Url,
            CreatedAt
        )
        VALUES
        (
            $jobId,
            $title,
            $company,
            $location,
            $description,
            $easyApply,
            $applyAvailable,
            $url,
            $createdAt
        )
        """;

        command.Parameters.AddWithValue("$jobId", job.JobId);
        command.Parameters.AddWithValue("$title", job.Title);
        command.Parameters.AddWithValue("$company", job.Company);
        command.Parameters.AddWithValue("$location", job.Location);
        command.Parameters.AddWithValue("$description", job.Description);
        command.Parameters.AddWithValue("$easyApply", job.EasyApply ? 1 : 0);
        command.Parameters.AddWithValue(
            "$applyAvailable",
            job.ApplyAvailable.HasValue
                ? job.ApplyAvailable.Value ? 1 : 0
                : DBNull.Value);
        command.Parameters.AddWithValue("$url", job.Url ?? "");
        command.Parameters.AddWithValue(
            "$createdAt",
            DateTime.UtcNow.ToString("O"));

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpdateMissingScrapedDetailsAsync(
        Job job,
        CancellationToken cancellationToken = default)
    {
        await using var connection =
            new SqliteConnection(_connectionString);

        await connection.OpenAsync(cancellationToken);

        var command = connection.CreateCommand();
        command.CommandText =
            """
            UPDATE Jobs
            SET
                Title = CASE WHEN Title = '' THEN $title ELSE Title END,
                Company = CASE WHEN Company = '' THEN $company ELSE Company END,
                Location = CASE WHEN Location = '' THEN $location ELSE Location END,
                Description = CASE
                    WHEN Description = '' THEN $description
                    ELSE Description
                END,
                EasyApply = $easyApply,
                ApplyAvailable = $applyAvailable,
                Url = CASE WHEN Url IS NULL OR Url = '' THEN $url ELSE Url END
            WHERE JobId = $jobId
              AND (Description = '' OR Url IS NULL OR Url = '' OR ApplyAvailable IS NULL)
            """;

        command.Parameters.AddWithValue("$jobId", job.JobId);
        command.Parameters.AddWithValue("$title", job.Title);
        command.Parameters.AddWithValue("$company", job.Company);
        command.Parameters.AddWithValue("$location", job.Location);
        command.Parameters.AddWithValue("$description", job.Description);
        command.Parameters.AddWithValue("$easyApply", job.EasyApply ? 1 : 0);
        command.Parameters.AddWithValue(
            "$applyAvailable",
            job.ApplyAvailable.HasValue
                ? job.ApplyAvailable.Value ? 1 : 0
                : DBNull.Value);
        command.Parameters.AddWithValue("$url", job.Url);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<List<Job>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var jobs = new List<Job>();

        await using var connection =
            new SqliteConnection(_connectionString);

        await connection.OpenAsync(cancellationToken);

        var command = connection.CreateCommand();

        command.CommandText =
        """
        SELECT
            JobId,
            Title,
            Company,
            Location,
            Description,
            EasyApply,
            ApplyAvailable,
            Url
        FROM Jobs
        ORDER BY CreatedAt DESC
        """;

        await using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            jobs.Add(new Job
            {
                JobId = reader.GetString(0),
                Title = reader.GetString(1),
                Company = reader.GetString(2),
                Location = reader.GetString(3),
                Description = reader.GetString(4),
                EasyApply = reader.GetInt32(5) == 1,
                ApplyAvailable = reader.IsDBNull(6)
                    ? null
                    : reader.GetInt32(6) == 1,
                Url = reader.IsDBNull(7) ? "" : reader.GetString(7)
            });
        }

        return jobs;
    }

    public async Task<Job?> GetAsync(
        string jobId,
        CancellationToken cancellationToken = default)
    {
        await using var connection =
            new SqliteConnection(_connectionString);

        await connection.OpenAsync(cancellationToken);

        var command = connection.CreateCommand();
        command.CommandText =
        """
        SELECT
            JobId,
            Title,
            Company,
            Location,
            Description,
            EasyApply,
            ApplyAvailable,
            Url
        FROM Jobs
        WHERE JobId = $jobId
        """;
        command.Parameters.AddWithValue("$jobId", jobId);

        await using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new Job
        {
            JobId = reader.GetString(0),
            Title = reader.GetString(1),
            Company = reader.GetString(2),
            Location = reader.GetString(3),
            Description = reader.GetString(4),
            EasyApply = reader.GetInt32(5) == 1,
            ApplyAvailable = reader.IsDBNull(6)
                ? null
                : reader.GetInt32(6) == 1,
            Url = reader.IsDBNull(7) ? "" : reader.GetString(7)
        };
    }
}