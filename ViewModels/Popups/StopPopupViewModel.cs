using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IvanConnections_Travel.Models;
using IvanConnections_Travel.Services;
using System.Collections.ObjectModel;

namespace IvanConnections_Travel.ViewModels.Popups
{
    public partial class StopPopupViewModel : ObservableObject
    {
        private readonly ApiService _apiService;
        private readonly IWidgetService? _widgetService;

        [ObservableProperty]
        private Stop? stop;

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private string? errorMessage;

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

        public async Task LoadAsync(Stop stop)
        {
            Stop = stop;
            IsLoading = true;
            ErrorMessage = null;
            Arrivals.Clear();

            var arrivalsData = await _apiService.GetArrivalsForStopAsync(stop.StopId);

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