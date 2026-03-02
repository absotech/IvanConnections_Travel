namespace IvanConnections_Travel.Services;

public interface IWidgetService
{
    bool IsPinningSupported();
    Task PinWidgetAsync(int stopId, string stopName);
}
