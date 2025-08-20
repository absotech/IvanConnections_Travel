using IvanConnections_Travel.Models;
using System.Collections.ObjectModel;

namespace IvanConnections_Travel.Controls;

public class CustomMauiMap : Microsoft.Maui.Controls.Maps.Map
{
    public static readonly BindableProperty VehiclesProperty =
        BindableProperty.Create(nameof(Vehicles), typeof(ObservableCollection<Vehicle>), typeof(CustomMauiMap), new ObservableCollection<Vehicle>());

    public ObservableCollection<Vehicle> Vehicles
    {
        get => (ObservableCollection<Vehicle>)GetValue(VehiclesProperty);
        set => SetValue(VehiclesProperty, value);
    }
    public static readonly BindableProperty StopsProperty =
        BindableProperty.Create(nameof(Stops), typeof(List<Stop>), typeof(CustomMauiMap), new List<Stop>());

    public List<Stop> Stops
    {
        get => (List<Stop>)GetValue(StopsProperty);
        set => SetValue(StopsProperty, value);
    }
    public static readonly BindableProperty ShowStopsProperty =
        BindableProperty.Create(nameof(ShowStops), typeof(bool), typeof(CustomMauiMap), true);

    public bool ShowStops
    {
        get => (bool)GetValue(ShowStopsProperty);
        set => SetValue(ShowStopsProperty, value);
    }

    public static readonly BindableProperty MoveToLocationProperty =
        BindableProperty.Create(nameof(MoveToLocation), typeof(Location), typeof(CustomMauiMap), null);

    public Location MoveToLocation
    {
        get => (Location)GetValue(MoveToLocationProperty);
        set => SetValue(MoveToLocationProperty, value);
    }

}
