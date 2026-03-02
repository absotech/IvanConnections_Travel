namespace IvanConnections_Travel.Utils;

public static class Preferences
{
    private const string PendingWidgetStopIdKey = "pending_widget_stop_id";
    private const string PendingWidgetStopNameKey = "pending_widget_stop_name";

    public static int PendingWidgetStopId
    {
        get => Microsoft.Maui.Storage.Preferences.Default.Get(PendingWidgetStopIdKey, -1);
        set => Microsoft.Maui.Storage.Preferences.Default.Set(PendingWidgetStopIdKey, value);
    }

    public static string? PendingWidgetStopName
    {
        get => Microsoft.Maui.Storage.Preferences.Default.Get<string?>(PendingWidgetStopNameKey, null);
        set => Microsoft.Maui.Storage.Preferences.Default.Set(PendingWidgetStopNameKey, value);
    }
    
    public static void ClearPendingWidgetStop()
    {
        Microsoft.Maui.Storage.Preferences.Default.Remove(PendingWidgetStopIdKey);
        Microsoft.Maui.Storage.Preferences.Default.Remove(PendingWidgetStopNameKey);
    }
}
