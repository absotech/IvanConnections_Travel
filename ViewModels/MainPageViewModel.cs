using CommunityToolkit.Mvvm.Messaging;
using IvanConnections_Travel.Messages;
using IvanConnections_Travel.Models;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json;
using IvanConnections_Travel.Utils;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Maui.Core;
using IvanConnections_Travel.ViewModels.Popups;

namespace IvanConnections_Travel.ViewModels
{
    public partial class MainPageViewModel : ObservableObject, IDisposable
    {
        private readonly IPopupService _popupService;
        private static readonly HttpClient _httpClient = new HttpClient(); // Reuse HttpClient
        private readonly System.Timers.Timer _refreshTimer;
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private string? _lastVehicleHash = null;
        private List<Stop> _stops = new();
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
                        _lastVehicleHash = null;
                        _ = LoadPinsFromBackendAsync();
                    }
                }
                else
                {
                    SelectedId = null;
                    _lastVehicleHash = null;
                    _ = LoadPinsFromBackendAsync();
                }
                //Debug.WriteLine($"SelectedId: {SelectedId}");
#endif
            });
            _refreshTimer = new System.Timers.Timer(3000);
            _refreshTimer.Elapsed += async (s, e) => await RefreshPinsAsync();
            _refreshTimer.AutoReset = true;
            _ = LoadStopsFromBackendAsync();
            StartPeriodicRefresh();
        }

        public void StartPeriodicRefresh()
        {
            _refreshTimer.Start();
            _ = LoadPinsFromBackendAsync();
        }

        public void StopPeriodicRefresh()
        {
            _refreshTimer.Stop();
        }

        [RelayCommand]
        private async Task RefreshPinsAsync()
        {
            if (!await _semaphore.WaitAsync(0))
                return;

            try
            {
                await LoadPinsFromBackendAsync();
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
                    _stops = stops;
                    Debug.WriteLine($"Loaded {_stops.Count} stops from backend.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading stops: {ex.Message}");
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
                if (_stops.Count == 0)
                    _ = LoadStopsFromBackendAsync();
                var apiUrl = BuildApiUrl();
                var response = await _httpClient.GetAsync(apiUrl);

                if (!response.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"API error: {response.StatusCode} for {apiUrl}");
                    return;
                }

                var json = await response.Content.ReadAsStringAsync();
                var currentHash = ComputingHelpers.ComputeMd5Hash(json);
                if (currentHash == _lastVehicleHash)
                    return;

                _lastVehicleHash = currentHash;

                var vehicles = JsonSerializer.Deserialize<List<Vehicle>>(json, _jsonSerializerOptions);
                if (vehicles == null)
                    return;
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Pins.Clear();
                    if (SelectedId.HasValue && SelectedId != 0)
                    {
                        var selectedVehicle = vehicles.FirstOrDefault(v => v.Id == SelectedId);
                        if (selectedVehicle != null)
                        {
                            Pins.Add(selectedVehicle);
                        }
                    }
                    else
                        foreach (var vehicle in vehicles)
                        {
                            Pins.Add(vehicle);
                        }

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
                    
                    WeakReferenceMessenger.Default.Send(new PinsUpdatedMessage([.. Pins], _stops));
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
        public async Task Search()
        {
            await RefreshPinsAsync();
        }
        [RelayCommand]
        private async Task Appearing()
        {
            Debug.WriteLine("MainPageViewModel Appearing");
        }
        [RelayCommand]
        private async Task Disappearing()
        {
            Dispose();
        }
        public void Dispose()
        {
            _refreshTimer?.Stop();
            _refreshTimer?.Dispose();
            _semaphore?.Dispose();
        }
    }
}