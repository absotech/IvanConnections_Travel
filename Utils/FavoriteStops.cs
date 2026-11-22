using System.Globalization;

namespace IvanConnections_Travel.Utils;

/// <summary>
/// Stațiile favorite ale utilizatorului, salvate în preferințe ca listă de id-uri
/// separate prin virgulă.
/// </summary>
public static class FavoriteStops
{
    private const string FavoriteStopIdsKey = "FavoriteStopIds";

    private static readonly Lock SyncRoot = new();

    public static bool IsFavorite(int stopId)
    {
        lock (SyncRoot)
        {
            return Read().Contains(stopId);
        }
    }

    /// <summary>
    /// Adaugă sau elimină stația din favorite.
    /// </summary>
    /// <returns><c>true</c> dacă stația este favorită după apel.</returns>
    public static bool Toggle(int stopId)
    {
        lock (SyncRoot)
        {
            var ids = Read();
            var isFavorite = !ids.Remove(stopId);
            if (isFavorite)
            {
                ids.Add(stopId);
            }

            Write(ids);
            return isFavorite;
        }
    }

    public static IReadOnlyList<int> GetAll()
    {
        lock (SyncRoot)
        {
            return Read();
        }
    }

    private static List<int> Read()
    {
        var raw = Microsoft.Maui.Storage.Preferences.Default.Get(FavoriteStopIdsKey, string.Empty);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return [];
        }

        var ids = new List<int>();
        foreach (var part in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (int.TryParse(part, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) && !ids.Contains(id))
            {
                ids.Add(id);
            }
        }

        return ids;
    }

    private static void Write(List<int> ids)
    {
        if (ids.Count == 0)
        {
            Microsoft.Maui.Storage.Preferences.Default.Remove(FavoriteStopIdsKey);
            return;
        }

        var value = string.Join(",", ids.Select(id => id.ToString(CultureInfo.InvariantCulture)));
        Microsoft.Maui.Storage.Preferences.Default.Set(FavoriteStopIdsKey, value);
    }
}
