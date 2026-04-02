#if !WINDOWS && !ANDROID
namespace Proiect_Planificare_Buget.Services;

public sealed partial class BudgetDataService
{
    private partial Task EnsureStorageReadyAsync()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(DatabasePath)!);
        return Task.CompletedTask;
    }

    private partial async Task<string?> ReadSerializedStateAsync()
    {
        if (!File.Exists(DatabasePath))
            return null;

        return await File.ReadAllTextAsync(DatabasePath);
    }

    private partial Task SaveSerializedStateAsync(string json) =>
        File.WriteAllTextAsync(DatabasePath, json);
}
#endif
