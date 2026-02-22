using Android.Locations;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using IvanConnections_Travel.Messages;
using IvanConnections_Travel.Models;
using IvanConnections_Travel.Services;
using IvanConnections_Travel.Utils;
using IvanConnections_Travel.ViewModels.Popups;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.Maui.ApplicationModel;
using Location = Microsoft.Maui.Devices.Sensors.Location;

namespace IvanConnections_Travel.ViewModels;

public partial class MainPageViewModel : ObservableObject, IDisposable
{
    private readonly ApiService _apiService;
    private readonly IPopupService _popupService;
    private bool _isInitialized = false;
    private CancellationTokenSource? _cts;
    [ObservableProperty]
    private bool isTracking;
    private Vehicle _trackedVehicle;
    public Vehicle TrackedVehicle
    {
        get => _trackedVehicle;
        set
        {
            if (SetProperty(ref _trackedVehicle, value))
            {
                IsTracking = value != null;
                SearchText = "";
            }
        }
    }

    [ObservableProperty]
    private HashSet<string?> _routes = [];

    [ObservableProperty]
    private string _searchText = "";

    private string _previousSearchText = "";

    [ObservableProperty]
    private ObservableCollection<Vehicle> _pins = [];

    [ObservableProperty]
    private List<Stop> _allStops = [];

    [ObservableProperty]
    private bool _showStopsOnMap = true;

    [ObservableProperty]
    private Location? _mapCenterLocation;

    [ObservableProperty]
    private bool _isBusy;

    public MainPageViewModel(ApiService apiService, IPopupService popupService)
    {
        _apiService = apiService;
        _popupService = popupService;
        WeakReferenceMessenger.Default.Register<ClickMessage>(this, (r, m) => HandleVehicleClickMessage(m));
        WeakReferenceMessenger.Default.Register<StopClickMessage>(this, (r, m) => HandleStopClickMessage(m));
    }

    private async void HandleVehicleClickMessage(ClickMessage m)
    {
        if (m.Value is null)
        {
            //TrackedVehicle = null;
            //await LoadPinsFromBackendAsync();
            return;
        }

        var newTrackingState = await _popupService.ShowPopupAsync<VehiclePopupViewModel>(vm => vm.Load(m.Value));

        if (newTrackingState is bool shouldTrack)
        {
            TrackedVehicle = shouldTrack ? m.Value : null;
            await LoadPinsFromBackendAsync(forced: true);
        }
    }

    private async void HandleStopClickMessage(StopClickMessage m)
    {
        if (m.Value != null)
        {
            await _popupService.ShowPopupAsync<StopPopupViewModel>(async vm => await vm.LoadAsync(m.Value));
        }
    }

    private async Task InitializeAsync()
    {
        if (_isInitialized) return;
        IsBusy = true;
        try
        {
            await LoadStopsFromBackendAsync();
            await LoadPinsFromBackendAsync();
            _isInitialized = true;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task StopTracking()
    {
        TrackedVehicle = null;
        await LoadPinsFromBackendAsync(forced: true);
    }

    [RelayCommand]
    private async Task ShowSearchDisabledToast()
    {
        await Toast.Make("Căutarea este dezactivată în timpul urmăririi.", ToastDuration.Long, 14).Show();
    }

    [RelayCommand]
    private async Task MarkerClicked(string? tag)
    {
        if (string.IsNullOrEmpty(tag)) return;

        if (tag.StartsWith("vehicle_"))
        {
            var vehicleLabel = tag.Substring("vehicle_".Length);
            var vehicleData = Pins.FirstOrDefault(p => p.Label == vehicleLabel);
            if (vehicleData != null)
            {
                string vehicleTypeName = Translations.GetVehicleTypeNameInRomanian(vehicleData.VehicleType.Value);
                string timeText = TimeFormatUtils.FormatTimeDifferenceInRomanian(vehicleData.LocalTimestamp.Value);
                string message = $"Cod {vehicleTypeName}: {vehicleLabel}, actualizat acum {timeText}";

                Toast.Make(message, ToastDuration.Long, 14).Show();
                HandleVehicleClickMessage(new ClickMessage(vehicleData));
            }
        }
        else if (tag.StartsWith("stop_"))
        {
            var stopIdString = tag.Substring("stop_".Length);
            if (int.TryParse(stopIdString, out int stopId))
            {
                var stopData = AllStops.FirstOrDefault(s => s.StopId == stopId);
                if (stopData != null)
                {
                    HandleStopClickMessage(new StopClickMessage(stopData));
                }
            }
        }
        else
        {
            Toast.Make(tag, ToastDuration.Long, 14).Show();
        }
    }

    [RelayCommand]
    private void MapClicked()
    {
        HandleVehicleClickMessage(new ClickMessage(null));
    }
    private void StartPeriodicRefresh()
    {
        if (_cts != null) return;

        _cts = new CancellationTokenSource();
        Task.Run(async () =>
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    IsBusy = true;
                });
                if (SearchText != _previousSearchText)
                    await LoadPinsFromBackendAsync(true);
                _previousSearchText = SearchText;
                await LoadPinsFromBackendAsync();

                if (TrackedVehicle is not null)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        MapCenterLocation = new Location(TrackedVehicle.Latitude.Value, TrackedVehicle.Longitude.Value);
                    });
                }
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    IsBusy = false;
                });
                await Task.Delay(TimeSpan.FromSeconds(3), _cts.Token);
            }
        }, _cts.Token);
    }

    private void StopPeriodicRefresh()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    private async Task LoadStopsFromBackendAsync()
    {
        if (!ShowStopsOnMap)
        {
            AllStops = [];
            return;
        }
        try
        {
            var stops = await _apiService.GetStopsAsync();
            AllStops = stops.Any() ? stops : [];
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Stops Loading] A critical error occurred: {ex.Message}");
            AllStops = [];
        }
    }

    private async Task LoadPinsFromBackendAsync(bool forced = false)
    {
        try
        {
            var routeToSearch = Routes.Contains(SearchText, StringComparer.OrdinalIgnoreCase) ? SearchText : null;
            var response = await _apiService.GetVehiclesAsync(routeToSearch, forced);
            Debug.WriteLine(routeToSearch);

            if (response.IsNotModified || response.Data is null) return;

            var vehicles = response.Data;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                UpdateDisplayedPins(vehicles);
                PopulateRoutesIfEmpty(vehicles);
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Exception in LoadPinsFromBackendAsync: {ex.Message}");
        }
    }

    private void UpdateDisplayedPins(List<Vehicle> freshVehicles)
    {
        if (TrackedVehicle != null)
        {
            var updatedTrackedVehicle = freshVehicles.FirstOrDefault(v => v.Id == TrackedVehicle.Id);
            if (updatedTrackedVehicle != null)
            {
                updatedTrackedVehicle.IsTracked = true;
                TrackedVehicle = updatedTrackedVehicle;
                Pins = new ObservableCollection<Vehicle> { TrackedVehicle };
            }
            else
            {
                TrackedVehicle = null;
                Pins = new ObservableCollection<Vehicle>(freshVehicles);
            }
        }
        else
        {
            Pins = new ObservableCollection<Vehicle>(freshVehicles);
        }
    }

    private void PopulateRoutesIfEmpty(List<Vehicle> freshVehicles)
    {
        if (Routes.Any() || !freshVehicles.Any()) return;

        var distinctRoutes = freshVehicles
            .Select(v => v.RouteShortName)
            .Where(r => !string.IsNullOrEmpty(r))
            .Distinct()
            .OrderBy(r => r);

        foreach (var route in distinctRoutes)
        {
            Routes.Add(route);
        }
        Debug.WriteLine($"Updated routes collection with {Routes.Count} distinct routes");
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task RefreshAsync()
    {
        IsBusy = true;
        try
        {
            await LoadPinsFromBackendAsync(forced: true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task CenterOnUserLocationAsync()
    {
        IsBusy = true;
        try
        {
            var permissionStatus = await LocationManagement.CheckAndRequestLocationPermission();
            if (permissionStatus != PermissionStatus.Granted)
            {
                Toast.Make("Permisiunea pentru locație nu a fost acordată.", ToastDuration.Long, 14).Show();
                Debug.WriteLine("Location permission not granted.");
                return;
            }

            var location = await LocationManagement.GetCurrentLocationAsync();
            if (location != null)
            {
                MapCenterLocation = new Location(location.Latitude, location.Longitude);
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
    private void ToggleShowStops()
    {
        ShowStopsOnMap = !ShowStopsOnMap;
        _ = LoadStopsFromBackendAsync();
    }

    [RelayCommand]
    private async Task AppearingAsync()
    {
        await InitializeAsync();
        StartPeriodicRefresh();

        if (MapCenterLocation is null)
        {
            var cachedLocation = await LocationManagement.GetLocationAsync();
            if (cachedLocation != null)
            {
                MapCenterLocation = cachedLocation;
            }
        }
    }

    [RelayCommand]
    private void Disappearing()
    {
        StopPeriodicRefresh();
    }

    public void Dispose()
    {
        StopPeriodicRefresh();
        WeakReferenceMessenger.Default.UnregisterAll(this);
        GC.SuppressFinalize(this);
    }
}