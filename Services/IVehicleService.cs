using System.Collections.ObjectModel;
using System.ComponentModel;
using IvanConnections_Travel.Models;

namespace IvanConnections_Travel.Services;

public interface IVehicleService : INotifyPropertyChanged
{
    ObservableCollection<Vehicle> Vehicles { get; }
    HashSet<string> AvailableRoutes { get; }
    bool IsBusy { get; set; }
    Vehicle? TrackedVehicle { get; set; }
    string? SearchText { get; set; }

    void StartPeriodicRefresh();
    void StopPeriodicRefresh();
    Task RefreshAsync(bool forced = false);
}
