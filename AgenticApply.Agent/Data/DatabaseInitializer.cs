using Microsoft.Data.Sqlite;

namespace AgenticApply.Agent.Data;

public class DatabaseInitializer
{
    private readonly string _connectionString;

    public DatabaseInitializer()
        : this("Data Source=Data/AgenticApply.db")
    {
    }

    public DatabaseInitializer(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task InitializeAsync()
    {
        var dataSource =
            new SqliteConnectionStringBuilder(_connectionString).DataSource;
        if (dataSource != ":memory:"
            && !dataSource.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
        {
            var databaseDirectory = Path.GetDirectoryName(
                Path.GetFullPath(dataSource));
            if (!string.IsNullOrEmpty(databaseDirectory))
            {
                Directory.CreateDirectory(databaseDirectory);
            }
        }

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
            Model TEXT,
            CreatedAt TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS Applications
        (
            JobId TEXT PRIMARY KEY,
            Status TEXT NOT NULL,
            AppliedAt TEXT,
            Notes TEXT
        );

        CREATE TABLE IF NOT EXISTS AgentSettings
        (
            SettingKey TEXT PRIMARY KEY,
            SettingValue TEXT NOT NULL,
            UpdatedAt TEXT NOT NULL
        );
        """;

        await command.ExecuteNonQueryAsync();

        command.CommandText =
        """
        SELECT COUNT(*)
        FROM pragma_table_info('Evaluations')
        WHERE name = 'Model'
        """;

        var hasModelColumn =
            Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;

        if (!hasModelColumn)
        {
            command.CommandText =
                "ALTER TABLE Evaluations ADD COLUMN Model TEXT;";
            await command.ExecuteNonQueryAsync();
        }
    }
}