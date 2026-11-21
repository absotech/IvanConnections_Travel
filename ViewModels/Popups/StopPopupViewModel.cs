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

        // Informații pentru antetul și rezumatul popup-ului.
        [ObservableProperty]
        private string? stopSubtitle;

        [ObservableProperty]
        private string arrivalsSummary = string.Empty;

        [ObservableProperty]
        private bool hasArrivals;

        [ObservableProperty]
        private string? lastUpdateText;

        [ObservableProperty]
        private bool canPinWidget;

        private List<Stop> _allStops = [];
        private string? _deviceId;

        public ObservableCollection<StopArrival> Arrivals { get; } = [];

        public StopPopupViewModel(ApiService apiService, IWidgetService? widgetService = null)
        {
            _apiService = apiService;
            _widgetService = widgetService;
            CanPinWidget = widgetService?.IsPinningSupported() == true;
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

        [RelayCommand]
        private async Task RefreshAsync()
        {
            if (Stop is null || IsLoading)
            {
                return;
            }

            await LoadAsync(Stop, _deviceId);
        }

        partial void OnStopChanged(Stop? value)
        {
            CanSwitchDirection = value?.OppositeStopId is int oppositeId
                                 && _allStops.Any(s => s.StopId == oppositeId);
            StopSubtitle = BuildStopSubtitle(value);
        }

        /// <summary>
        /// Detaliile secundare ale stației (codul și descrierea din GTFS), afișate sub nume.
        /// </summary>
        private static string? BuildStopSubtitle(Stop? stop)
        {
            if (stop is null)
            {
                return null;
            }

            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(stop.StopCode))
            {
                parts.Add($"Cod {stop.StopCode!.Trim()}");
            }
            if (!string.IsNullOrWhiteSpace(stop.StopDesc))
            {
                parts.Add(stop.StopDesc!.Trim());
            }

            return parts.Count == 0 ? null : string.Join(" · ", parts);
        }

        public async Task LoadAsync(Stop stop, string? deviceId = null)
        {
            _deviceId = deviceId;
            Stop = stop;
            IsLoading = true;
            ErrorMessage = null;
            Arrivals.Clear();
            HasArrivals = false;
            ArrivalsSummary = string.Empty;

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

            if (Arrivals.Count == 0)
            {
                ErrorMessage = "Nu sunt sosiri în următoarea perioadă.";
            }

            HasArrivals = Arrivals.Count > 0;
            ArrivalsSummary = Arrivals.Count switch
            {
                0 => string.Empty,
                1 => "1 sosire în următoarele 25 de minute",
                _ => $"{Arrivals.Count} sosiri în următoarele 25 de minute"
            };
            LastUpdateText = $"Actualizat la {DateTime.Now:HH:mm}";

            IsLoading = false;
        }
    }
}