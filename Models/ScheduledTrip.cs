namespace IvanConnections_Travel.Models;

public class ScheduledTrip
{
    public string? TripId { get; set; }
    public string? ServiceDayType { get; set; }
    public string? DepartureTime { get; set; }
    public string? StartingStop { get; set; }
    public Stop? Stop { get; set; }

    public TimeSpan? DepartureTimeSpan =>
        TimeSpan.TryParse(DepartureTime, out var ts) ? ts : null;

    public string DepartureDisplay =>
        DepartureTimeSpan?.ToString(@"hh\:mm") ?? DepartureTime ?? string.Empty;

    public bool IsPast =>
        DepartureTimeSpan.HasValue && DepartureTimeSpan.Value < DateTime.Now.TimeOfDay;
}
