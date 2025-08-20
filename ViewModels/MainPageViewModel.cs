using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using IvanConnections_Travel.Messages;
using IvanConnections_Travel.Models;
using IvanConnections_Travel.Utils;
using IvanConnections_Travel.ViewModels.Popups;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Maps;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json;

namespace IvanConnections_Travel.ViewModels
{
    public partial class MainPageViewModel : ObservableObject, IDisposable
    {
        private readonly IPopupService _popupService;
        private static readonly HttpClient _httpClient = new();
        private readonly Dictionary<string, string> _etags = [];
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private bool _isInitialized = false;
        private CancellationTokenSource? _cts;
        private readonly JsonSerializerOptions _jsonSerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };
        [ObservableProperty]
        private int? _selectedId = null;

        [ObservableProperty]
        private HashSet<string?> _routes = [];

        [ObservableProperty]
        private string? _searchText;

        [ObservableProperty]
        private bool _isSearchEnabled = true;

        [ObservableProperty]
        private ObservableCollection<Vehicle> _pins = [];

        [ObservableProperty]
        private List<Stop> _allStops = [];

        [ObservableProperty]
        private bool _showStopsOnMap = true;

        [ObservableProperty]
        private Location mapCenterLocation;

        [ObservableProperty]
        private bool isBusy;
        public MainPageViewModel()
        {
            _popupService = DependencyService.Get<IPopupService>();
            WeakReferenceMessenger.Default.Register<ClickMessage>(this, async (r, m) =>
            {
#if ANDROID
                if (m.Value != null)
                {
                    var shouldTrackVehicle = await _popupService.ShowPopupAsync<VehiclePopupViewModel>(viewModel => viewModel.Load(m.Value));
                    if ((bool?)shouldTrackVehicle == true)
                    {
                        SelectedId = m.Value.Id;
                        _ = LoadPinsFromBackendAsync();
                    }
                }
                else
                {
                    SelectedId = null;
                    _ = LoadPinsFromBackendAsync();
                }
                //Debug.WriteLine($"SelectedId: {SelectedId}");
#endif
            });
            WeakReferenceMessenger.Default.Register<StopClickMessage>(this, async (r, m) =>
            {
#if ANDROID
                if (m.Value != null)
                {
                    await _popupService.ShowPopupAsync<StopPopupViewModel>(async viewModel => await viewModel.LoadAsync(m.Value));
                }
#endif
            });
        }

        public async Task InitializeAsync()
        {
            if (_isInitialized)
                return;

            await LoadStopsFromBackendAsync();
            await LoadPinsFromBackendAsync();
            _isInitialized = true;
        }
        public void StartPeriodicRefresh()
        {
            if (_cts != null) return;

            _cts = new CancellationTokenSource();
            Task.Run(async () => await PeriodicRefreshLoopAsync(_cts.Token));
        }

        public void StopPeriodicRefresh()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }
        private async Task PeriodicRefreshLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                await RefreshPinsAsync();
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(3), token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }

        [RelayCommand]
        private async Task RefreshPinsAsync()
        {
            if (!await _semaphore.WaitAsync(0))
                return;

            try
            {
                LoadPinsFromBackendAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Refresh error: {ex.Message}");
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private string BuildApiUrl()
        {
            const string baseUrl = "http://server.ivanconnections.cloud:5000/ivanconnectionstravel/api/Vehicles";

            if (!string.IsNullOrEmpty(SearchText) && Routes.Contains(SearchText, StringComparer.OrdinalIgnoreCase))
            {
                return $"{baseUrl}/valid/byroute/{Uri.EscapeDataString(SearchText)}";
            }

            return $"{baseUrl}/valid";
        }
        private async Task LoadStopsFromBackendAsync()
        {
            try
            {
                if (!ShowStopsOnMap)
                {
                    AllStops = [];
                    return;
                }
                const string stopsUrl = "http://server.ivanconnections.cloud:5000/ivanconnectionstravel/api/Stops";
                var response = await _httpClient.GetAsync(stopsUrl);
                if (!response.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"Failed to fetch stops: {response.StatusCode}");
                    return;
                }

                var json = await response.Content.ReadAsStringAsync();
                var stops = JsonSerializer.Deserialize<List<Stop>>(json, _jsonSerializerOptions);

                if (stops != null)
                {
                    AllStops = stops;
                }
                else
                {
                    Debug.WriteLine("[Stops Loading] Deserialization resulted in a null list.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Stops Loading] A critical error occurred: {ex.Message}");
            }
        }
        public async Task LoadPinsFromBackendAsync()
        {
            try
            {
#if DEBUG
                if (!_httpClient.DefaultRequestHeaders.Contains("Accept"))
                {
                    _httpClient.DefaultRequestHeaders.Add("Accept", "*/*");
                }
#endif
                var apiUrl = BuildApiUrl();
                using var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
                if (_etags.TryGetValue(apiUrl, out var etag))
                {
                    request.Headers.Add("If-None-Match", etag);
                }
                using var response = await _httpClient.SendAsync(request);
                if (response.StatusCode == System.Net.HttpStatusCode.NotModified)
                {
                    return;
                }
                if (!response.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"API error: {response.StatusCode} for {apiUrl}");
                    return;
                }
                if (response.Headers.TryGetValues("ETag", out var etagHeaderValues))
                {
                    _etags[apiUrl] = etagHeaderValues.First();
                }
                var json = await response.Content.ReadAsStringAsync();

                var vehicles = JsonSerializer.Deserialize<List<Vehicle>>(json, _jsonSerializerOptions);
                if (vehicles == null)
                    return;
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    var newVehicleList = new List<Vehicle>();

                    if (SelectedId.HasValue && SelectedId != 0)
                    {
                        var selectedVehicle = vehicles.FirstOrDefault(v => v.Id == SelectedId);
                        if (selectedVehicle != null)
                        {
                            newVehicleList.Add(selectedVehicle);
                        }
                    }
                    else
                    {
                        foreach (var vehicle in vehicles)
                        {
                            newVehicleList.Add(vehicle);
                        }
                    }
                    Pins = new ObservableCollection<Vehicle>(newVehicleList);
                    if (vehicles.Count != 0 && Routes.Count == 0)
                    {
                        var distinctRoutes = vehicles
                            .Select(v => v.RouteShortName)
                            .Where(r => !string.IsNullOrEmpty(r))
                            .Distinct()
                            .OrderBy(r => r)
                            .ToList();

                        Routes.Clear();
                        foreach (var route in distinctRoutes)
                        {
                            Routes.Add(route);
                        }

                        Debug.WriteLine($"Updated routes collection with {Routes.Count} distinct routes");
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception in LoadPinsFromBackendAsync: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
            }
        }

        [RelayCommand]
        private async Task CenterOnUserLocation()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                var location = await LocationManagement.GetCurrentLocationAsync();

                if (location != null)
                {
                    MapCenterLocation  = new Location(location.Latitude, location.Longitude);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error centering on user location: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        void ToggleShowStops()
        {
            ShowStopsOnMap = !ShowStopsOnMap;
        }

        [RelayCommand]
        public async Task Search()
        {
            await RefreshPinsAsync();
        }
        [RelayCommand]
        private async Task Appearing()
        {
            void updateMapAction(Location newLocation)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    MapCenterLocation = newLocation;
                });
            }
            var cachedLocation = await LocationManagement.GetLocationAsync(
                GeolocationAccuracy.Lowest,
                2,
                updateMapAction);
            if (cachedLocation != null)
            {
                MapCenterLocation = cachedLocation;
            }
        }
        [RelayCommand]
        private async Task Disappearing()
        {
            Dispose();
        }
        public void Dispose()
        {
            StopPeriodicRefresh();
            _semaphore?.Dispose();
            WeakReferenceMessenger.Default.UnregisterAll(this);
        }
    }
}