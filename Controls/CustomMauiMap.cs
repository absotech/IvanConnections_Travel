using IvanConnections_Travel.Models;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace IvanConnections_Travel.Controls;

public class CustomMauiMap : Microsoft.Maui.Controls.Maps.Map
{
    public static readonly BindableProperty MarkerClickCommandProperty =
        BindableProperty.Create(nameof(MarkerClickCommand), typeof(ICommand), typeof(CustomMauiMap));

    public ICommand MarkerClickCommand
    {
        get => (ICommand)GetValue(MarkerClickCommandProperty);
        set => SetValue(MarkerClickCommandProperty, value);
    }
    public static readonly BindableProperty MapClickCommandProperty =
        BindableProperty.Create(nameof(MapClickCommand), typeof(ICommand), typeof(CustomMauiMap));

    public ICommand MapClickCommand
    {
        get => (ICommand)GetValue(MapClickCommandProperty);
        set => SetValue(MapClickCommandProperty, value);
    }

    public static readonly BindableProperty VehiclesProperty =
        BindableProperty.Create(nameof(Vehicles), typeof(ObservableCollection<Vehicle>), typeof(CustomMauiMap), new ObservableCollection<Vehicle>());
    public ObservableCollection<Vehicle> Vehicles
    {
        get => (ObservableCollection<Vehicle>)GetValue(VehiclesProperty);
        set => SetValue(VehiclesProperty, value);
    }
    public static readonly BindableProperty ShapesProperty =
        BindableProperty.Create(nameof(Shapes), typeof(ObservableCollection<Shape>), typeof(CustomMauiMap), new ObservableCollection<Shape>());
    public ObservableCollection<Shape> Shapes
    {
        get => (ObservableCollection<Shape>)GetValue(ShapesProperty);
        set => SetValue(ShapesProperty, value);
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

    public static readonly BindableProperty MapBearingProperty =
        BindableProperty.Create(nameof(MapBearing), typeof(double), typeof(CustomMauiMap), 0.0);

    public double MapBearing
    {
        get => (double)GetValue(MapBearingProperty);
        set => SetValue(MapBearingProperty, value);
    }

    public static readonly BindableProperty StopPinSizeProperty =
        BindableProperty.Create(nameof(StopPinSize), typeof(int), typeof(CustomMauiMap), 15);

    public int StopPinSize
    {
        get => (int)GetValue(StopPinSizeProperty);
        set => SetValue(StopPinSizeProperty, value);
    }

    public static readonly BindableProperty VehiclePinSizeProperty =
        BindableProperty.Create(nameof(VehiclePinSize), typeof(int), typeof(CustomMauiMap), 25);

    public int VehiclePinSize
    {
        get => (int)GetValue(VehiclePinSizeProperty);
        set => SetValue(VehiclePinSizeProperty, value);
    }
}
