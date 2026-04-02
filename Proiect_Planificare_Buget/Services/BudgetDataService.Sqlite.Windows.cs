#if WINDOWS
using Microsoft.Data.Sqlite;
using SQLitePCL;

namespace Proiect_Planificare_Buget.Services;

public sealed partial class BudgetDataService
{
    private bool _windowsStorageReady;

    private partial async Task EnsureStorageReadyAsync()
    {
        if (_windowsStorageReady)
            return;

        Batteries_V2.Init();

        await using var connection = new SqliteConnection($"Data Source={DatabasePath}");
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS AppState (
                Id INTEGER NOT NULL PRIMARY KEY CHECK (Id = 1),
                DataJson TEXT NOT NULL,
                LastUpdatedUtc TEXT NOT NULL
            );
            """;

        await command.ExecuteNonQueryAsync();
        _windowsStorageReady = true;
    }

    private partial async Task<string?> ReadSerializedStateAsync()
    {
        await using var connection = new SqliteConnection($"Data Source={DatabasePath}");
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT DataJson FROM AppState WHERE Id = 1 LIMIT 1;";
        return (string?)await command.ExecuteScalarAsync();
    }

    private partial async Task SaveSerializedStateAsync(string json)
    {
        await using var connection = new SqliteConnection($"Data Source={DatabasePath}");
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO AppState (Id, DataJson, LastUpdatedUtc)
            VALUES (1, $json, $updatedUtc)
            ON CONFLICT(Id) DO UPDATE SET
                DataJson = excluded.DataJson,
                LastUpdatedUtc = excluded.LastUpdatedUtc;
            """;
        command.Parameters.AddWithValue("$json", json);
        command.Parameters.AddWithValue("$updatedUtc", DateTime.UtcNow.ToString("O"));
        await command.ExecuteNonQueryAsync();
    }
}
#endif
