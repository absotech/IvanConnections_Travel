using System.Collections.ObjectModel;
using System.ComponentModel;
using IvanConnections_Travel.Models;
using IvanConnections_Travel.Models.Enums;

namespace IvanConnections_Travel.Services;

public interface IVehicleService : INotifyPropertyChanged
{
    ObservableCollection<Vehicle> Vehicles { get; }
    ObservableCollection<Shape> Shapes { get; }
    HashSet<string> AvailableRoutes { get; }

    /// <summary>Unfiltered snapshot of the last refresh.</summary>
    IReadOnlyList<Vehicle> AllVehicles { get; }

    /// <summary>True only for user-visible loads (first load, manual/forced refresh) – not for background polling.</summary>
    bool IsBusy { get; set; }

    /// <summary>True when the last refresh failed (no network / API error).</summary>
    bool IsOffline { get; }

    /// <summary>UTC moment of the last successful refresh.</summary>
    DateTime? LastUpdatedUtc { get; }

    /// <summary>When true the periodic loop skips refreshing (e.g. while a popup is open).</summary>
    bool IsPaused { get; set; }

    int RefreshIntervalSeconds { get; set; }

    Vehicle? TrackedVehicle { get; set; }
    string? SearchText { get; set; }

    /// <summary>The route that <see cref="SearchText"/> resolves to (case-insensitive), or null.</summary>
    string? MatchedRoute { get; }

    string? ShapeFilter { get; set; }
    VehicleType? VehicleTypeFilter { get; set; }
    string? DeviceId { get; set; }

    void StartPeriodicRefresh();
    void StopPeriodicRefresh();
    Task RefreshAsync(bool forced = false);
}
