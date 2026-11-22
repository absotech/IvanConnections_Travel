namespace IvanConnections_Travel.Utils;

/// <summary>
/// Rutele căutate recent pe hartă (cele mai noi primele), salvate în preferințe.
/// </summary>
public static class RecentRoutes
{
    private const string RecentRoutesKey = "RecentMapRoutes";
    private const int MaxItems = 6;

    public static IReadOnlyList<string> GetAll()
    {
        var raw = Microsoft.Maui.Storage.Preferences.Default.Get(RecentRoutesKey, string.Empty);
        return string.IsNullOrWhiteSpace(raw)
            ? []
            : raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    public static void Add(string route)
    {
        if (string.IsNullOrWhiteSpace(route)) return;
        List<string> items = [route, .. GetAll().Where(r => !r.Equals(route, StringComparison.OrdinalIgnoreCase))];
        Microsoft.Maui.Storage.Preferences.Default.Set(RecentRoutesKey, string.Join(",", items.Take(MaxItems)));
    }
}
