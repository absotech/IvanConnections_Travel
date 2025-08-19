using CommunityToolkit.Mvvm.ComponentModel;
using IvanConnections_Travel.Models;
using IvanConnections_Travel.Services;
using System.Collections.ObjectModel;

namespace IvanConnections_Travel.ViewModels.Popups
{
    public partial class StopPopupViewModel : ObservableObject
    {
        private readonly ApiService _apiService;

        [ObservableProperty]
        private Stop? stop;

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private string? errorMessage;

        public ObservableCollection<StopArrival> Arrivals { get; } = new();

        public StopPopupViewModel()
        {
            _apiService = new ApiService();
        }
        public async Task LoadAsync(Stop stop)
        {
            if (stop == null) return;

            Stop = stop;
            IsLoading = true;
            ErrorMessage = null;
            Arrivals.Clear();

            var arrivalsData = await _apiService.GetArrivalsForStopAsync(stop.StopId);

            if (arrivalsData != null && arrivalsData.Any())
            {
                foreach (var arrival in arrivalsData.OrderBy(a => a.ArrivalMinutes))
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