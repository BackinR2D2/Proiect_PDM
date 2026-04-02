#if ANDROID
using Android.Content;
using Android.Database.Sqlite;

namespace Proiect_Planificare_Buget.Services;

public sealed partial class BudgetDataService
{
    private bool _androidStorageReady;

    private partial Task EnsureStorageReadyAsync()
    {
        if (_androidStorageReady)
            return Task.CompletedTask;

        using var database = SQLiteDatabase.OpenOrCreateDatabase(DatabasePath, null)!;
        database.ExecSQL("""
            CREATE TABLE IF NOT EXISTS AppState (
                Id INTEGER NOT NULL PRIMARY KEY,
                DataJson TEXT NOT NULL,
                LastUpdatedUtc TEXT NOT NULL
            );
            """);

        _androidStorageReady = true;
        return Task.CompletedTask;
    }

    private partial Task<string?> ReadSerializedStateAsync()
    {
        using var database = SQLiteDatabase.OpenOrCreateDatabase(DatabasePath, null)!;
        using var cursor = database.RawQuery("SELECT DataJson FROM AppState WHERE Id = 1 LIMIT 1;", null);

        if (!cursor.MoveToFirst())
            return Task.FromResult<string?>(null);

        return Task.FromResult<string?>(cursor.GetString(0));
    }

    private partial Task SaveSerializedStateAsync(string json)
    {
        using var database = SQLiteDatabase.OpenOrCreateDatabase(DatabasePath, null)!;
        using var values = new ContentValues();
        values.Put("Id", 1);
        values.Put("DataJson", json);
        values.Put("LastUpdatedUtc", DateTime.UtcNow.ToString("O"));
        database.Delete("AppState", "Id = 1", null);
        database.Insert("AppState", null, values);
        return Task.CompletedTask;
    }
}
#endif
