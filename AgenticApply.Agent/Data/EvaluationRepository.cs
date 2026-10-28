using AgenticApply.Agent.Models;
using Microsoft.Data.Sqlite;

namespace AgenticApply.Agent.Data;

public class EvaluationRepository
{
    private const string DefaultConnectionString =
        "Data Source=Data/AgenticApply.db";

    private readonly string _connectionString;

    public EvaluationRepository()
        : this(DefaultConnectionString)
    {
    }

    public EvaluationRepository(string connectionString)
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
        FROM Evaluations
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
        string jobId,
        JobEvaluation evaluation,
        CancellationToken cancellationToken = default)
    {
        await using var connection =
            new SqliteConnection(_connectionString);

        await connection.OpenAsync(cancellationToken);

        var command = connection.CreateCommand();

        command.CommandText =
        """
        INSERT OR REPLACE INTO Evaluations
        (
            JobId,
            Score,
            Apply,
            Reason,
            Model,
            CreatedAt
        )
        VALUES
        (
            $jobId,
            $score,
            $apply,
            $reason,
            $model,
            $createdAt
        )
        """;

        command.Parameters.AddWithValue(
            "$jobId",
            jobId);

        command.Parameters.AddWithValue(
            "$score",
            evaluation.Score);

        command.Parameters.AddWithValue(
            "$apply",
            evaluation.Apply ? 1 : 0);

        command.Parameters.AddWithValue(
            "$reason",
            evaluation.Reason);

        command.Parameters.AddWithValue(
            "$model",
            (object?)evaluation.Model ?? DBNull.Value);

        command.Parameters.AddWithValue(
            "$createdAt",
            DateTime.UtcNow.ToString("O"));

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<JobEvaluation?> GetAsync(
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
            Score,
            Apply,
            Reason,
            Model
        FROM Evaluations
        WHERE JobId = $jobId
        """;

        command.Parameters.AddWithValue(
            "$jobId",
            jobId);

        await using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new JobEvaluation
        {
            Score = reader.GetInt32(0),
            Apply = reader.GetInt32(1) == 1,
            Reason = reader.IsDBNull(2)
                ? ""
                : reader.GetString(2),
            Model = reader.IsDBNull(3)
                ? null
                : reader.GetString(3)
        };
    }

    public async Task<Dictionary<string, JobEvaluation>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var evaluations = new Dictionary<string, JobEvaluation>();

        await using var connection =
            new SqliteConnection(_connectionString);

        await connection.OpenAsync(cancellationToken);

        var command = connection.CreateCommand();
        command.CommandText =
        """
        SELECT JobId, Score, Apply, Reason, Model
        FROM Evaluations
        """;

        await using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            evaluations.Add(
                reader.GetString(0),
                new JobEvaluation
                {
                    Score = reader.GetInt32(1),
                    Apply = reader.GetInt32(2) == 1,
                    Reason = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    Model = reader.IsDBNull(4) ? null : reader.GetString(4)
                });
        }

        return evaluations;
    }
}