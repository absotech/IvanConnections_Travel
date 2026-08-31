using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IvanConnections_Travel.Models;
using IvanConnections_Travel.Services;
using System.Collections.ObjectModel;

namespace IvanConnections_Travel.ViewModels.Popups
{
    public partial class StopPopupViewModel : ObservableObject, IQueryAttributable
    {
        private readonly ApiService _apiService;
        private readonly IWidgetService? _widgetService;

        [ObservableProperty]
        private Stop? stop;

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private string? errorMessage;

        [ObservableProperty]
        private bool canSwitchDirection;

        private List<Stop> _allStops = [];
        private string? _deviceId;

        public ObservableCollection<StopArrival> Arrivals { get; } = [];

        public StopPopupViewModel(ApiService apiService, IWidgetService? widgetService = null)
        {
            _apiService = apiService;
            _widgetService = widgetService;
        }

        [RelayCommand]
        private async Task PinWidgetAsync()
        {
            if (Stop != null && _widgetService != null)
            {
                await _widgetService.PinWidgetAsync(Stop.StopId, Stop.StopName);
            }
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            var stop = (Stop)query["stop"];
            var deviceId = query.TryGetValue("deviceId", out var d) ? d as string : null;
            if (query.TryGetValue("allStops", out var s) && s is List<Stop> allStops)
            {
                _allStops = allStops;
            }
            _ = LoadAsync(stop, deviceId);
        }

        [RelayCommand]
        private async Task SwitchDirectionAsync()
        {
            if (Stop?.OppositeStopId is not int oppositeId)
            {
                return;
            }

            var oppositeStop = _allStops.FirstOrDefault(s => s.StopId == oppositeId);
            if (oppositeStop == null)
            {
                return;
            }

            await LoadAsync(oppositeStop, _deviceId);
        }

        partial void OnStopChanged(Stop? value)
        {
            CanSwitchDirection = value?.OppositeStopId is int oppositeId
                                 && _allStops.Any(s => s.StopId == oppositeId);
        }

        public async Task LoadAsync(Stop stop, string? deviceId = null)
        {
            _deviceId = deviceId;
            Stop = stop;
            IsLoading = true;
            ErrorMessage = null;
            Arrivals.Clear();

            var arrivalsData = await _apiService.GetArrivalsForStopAsync(stop.StopId, deviceId);

            if (arrivalsData.Count != 0)
            {
                var filteredArrivals = arrivalsData
                    .Where(a => a.ArrivalMinutes <= 25)
                    .OrderBy(a => a.ArrivalMinutes == -1)
                    .ThenBy(a => a.ArrivalMinutes);

                foreach (var arrival in filteredArrivals)
                {
                    Arrivals.Add(arrival);
                }
            }
            else
            {
                ErrorMessage = "Nu sunt sosiri în următoarea perioadă.";
            }

            IsLoading = false;
        }
    }
}