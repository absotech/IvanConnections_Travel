using Android.Locations;
using IvanConnections_Travel.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Maps;
using Location = Microsoft.Maui.Devices.Sensors.Location;

namespace IvanConnections_Travel
{
    public partial class MainPage : ContentPage
    {

        public MainPage(MainPageViewModel mainPageViewModel)
        {
            InitializeComponent();
            Location location = new(47.1585, 27.6014);
            MapSpan mapSpan = new(location, 0.01, 0.01);
            MyMap.MoveToRegion(mapSpan);
            BindingContext = mainPageViewModel;

        }
    }
}