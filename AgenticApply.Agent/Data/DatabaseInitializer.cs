using Microsoft.Data.Sqlite;

namespace AgenticApply.Agent.Data;

public class DatabaseInitializer
{
    private readonly string _connectionString =
        "Data Source=Data/AgenticApply.db";

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory("Data");

        await using var connection =
            new SqliteConnection(_connectionString);

        await connection.OpenAsync();

        var command = connection.CreateCommand();

        command.CommandText =
        """
        CREATE TABLE IF NOT EXISTS Jobs
        (
            JobId TEXT PRIMARY KEY,
            Title TEXT NOT NULL,
            Company TEXT NOT NULL,
            Location TEXT NOT NULL,
            Description TEXT NOT NULL,
            EasyApply INTEGER NOT NULL,
            Url TEXT,
            CreatedAt TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS Evaluations
        (
            JobId TEXT PRIMARY KEY,
            Score INTEGER NOT NULL,
            Apply INTEGER NOT NULL,
            Reason TEXT,
            CreatedAt TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS Applications
        (
            JobId TEXT PRIMARY KEY,
            Status TEXT NOT NULL,
            AppliedAt TEXT,
            Notes TEXT
        );
        """;

        await command.ExecuteNonQueryAsync();
    }
}