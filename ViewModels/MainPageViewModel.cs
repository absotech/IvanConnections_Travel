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
using IvanConnections_Travel.Views.Popups;
using Microsoft.Maui.Controls.Maps;
using Location = Microsoft.Maui.Devices.Sensors.Location;

namespace IvanConnections_Travel.ViewModels;

public partial class MainPageViewModel : ObservableObject, IDisposable
{
    private readonly IVehicleService _vehicleService;
    private readonly ApiService _apiService;
    private readonly IPopupService _popupService;
    private bool _isInitialized = false;

    [ObservableProperty] private bool _isTracking;

    public Vehicle? TrackedVehicle
    {
        get => _vehicleService.TrackedVehicle;
        set
        {
            if (_vehicleService.TrackedVehicle == value) return;
            _vehicleService.TrackedVehicle = value;
            IsTracking = value != null;
            SearchText = "";
            OnPropertyChanged();
        }
    }

    public HashSet<string> Routes => _vehicleService.AvailableRoutes;

    public string SearchText
    {
        get => _vehicleService.SearchText ?? "";
        set
        {
            if (_vehicleService.SearchText == value) return;
            _vehicleService.SearchText = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<Vehicle> Pins => _vehicleService.Vehicles;

    public bool IsBusy
    {
        get => _vehicleService.IsBusy;
        set
        {
            if (_vehicleService.IsBusy == value) return;
            _vehicleService.IsBusy = value;
            OnPropertyChanged();
        }
    }

    [ObservableProperty] private List<Stop> _allStops = [];

    [ObservableProperty] private bool _showStopsOnMap;

    [ObservableProperty] private bool _isTrafficEnabled;

    [ObservableProperty] private Location? _mapCenterLocation;

    [ObservableProperty] 
    [NotifyPropertyChangedFor(nameof(CompassRotation))]
    [NotifyPropertyChangedFor(nameof(IsCompassVisible))]
    private double _mapBearing;

    public double CompassRotation => -MapBearing;
    public bool IsCompassVisible => Math.Abs(MapBearing) > 0.1;

    [RelayCommand]
    private void ResetBearing()
    {
        MapBearing = 0;
    }

    public MainPageViewModel(IVehicleService vehicleService, ApiService apiService, IPopupService popupService)
    {
        _vehicleService = vehicleService;
        _apiService = apiService;
        _popupService = popupService;

        IsTrafficEnabled = Microsoft.Maui.Storage.Preferences.Default.Get("IsTrafficEnabled", true);
        ShowStopsOnMap = Microsoft.Maui.Storage.Preferences.Default.Get("ShowStopsOnMap", true);

        WeakReferenceMessenger.Default.Register<ClickMessage>(this, (r, m) => HandleVehicleClickMessage(m));
        WeakReferenceMessenger.Default.Register<StopClickMessage>(this, (r, m) => HandleStopClickMessage(m));
        WeakReferenceMessenger.Default.Register<TrafficPreferenceChangedMessage>(this, (r, m) => IsTrafficEnabled = m.Value);
        WeakReferenceMessenger.Default.Register<StopsPreferenceChangedMessage>(this, (r, m) =>
        {
            ShowStopsOnMap = m.Value;
            _ = LoadStopsFromBackendAsync();
        });

        _vehicleService.PropertyChanged += (s, e) =>
        {
            switch (e.PropertyName)
            {
                case nameof(IVehicleService.Vehicles):
                    OnPropertyChanged(nameof(Pins));
                    UpdateTrackedVehicleLocation();
                    break;
                case nameof(IVehicleService.IsBusy):
                    OnPropertyChanged(nameof(IsBusy));
                    break;
                case nameof(IVehicleService.AvailableRoutes):
                    OnPropertyChanged(nameof(Routes));
                    break;
            }
        };
    }

    private async void HandleVehicleClickMessage(ClickMessage m)
    {
        if (m.Value is null)
        {
            return;
        }

        var newTrackingState = await _popupService.ShowPopupAsync<VehiclePopupViewModel>(vm => vm.Load(m.Value));

        if (newTrackingState is not bool shouldTrack) return;
        TrackedVehicle = shouldTrack ? m.Value : null;
        await _vehicleService.RefreshAsync(forced: true);
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
            await _vehicleService.RefreshAsync(forced: true);
            _isInitialized = true;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task StopTracking()
    {
        TrackedVehicle = null;
        await _vehicleService.RefreshAsync(forced: true);
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
            var vehicleLabel = tag["vehicle_".Length..];
            var vehicleData = Pins.FirstOrDefault(p => p.Label == vehicleLabel);
            if (vehicleData != null)
            {
                var vehicleTypeName = Translations.GetVehicleTypeNameInRomanian(vehicleData.VehicleType.Value);
                var timeText = TimeFormatUtils.FormatTimeDifferenceInRomanian(vehicleData.LocalTimestamp.Value);
                var message = $"Cod {vehicleTypeName}: {vehicleLabel}, actualizat acum {timeText}";

                Toast.Make(message, ToastDuration.Long, 14).Show();
                HandleVehicleClickMessage(new ClickMessage(vehicleData));
            }
        }
        else if (tag.StartsWith("stop_"))
        {
            var stopIdString = tag["stop_".Length..];
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

    private void UpdateTrackedVehicleLocation()
    {
        if (TrackedVehicle is not null)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                MapCenterLocation = new Location(TrackedVehicle.Latitude.Value, TrackedVehicle.Longitude.Value);
            });
        }
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
            AllStops = stops.Count != 0 ? stops : [];
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Stops Loading] A critical error occurred: {ex.Message}");
            AllStops = [];
        }
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task RefreshAsync()
    {
        await _vehicleService.RefreshAsync(forced: true);
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
    private void ToggleTraffic()
    {
        IsTrafficEnabled = !IsTrafficEnabled;
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
        _vehicleService.StartPeriodicRefresh();

        if (MapCenterLocation is null)
        {
            var cachedLocation = await LocationManagement.GetLocationAsync();
            MapCenterLocation = cachedLocation;
        }

        switch (Pins.Count)
        {
            case 0:
                await MessagePopup.ShowAsync("Eroare", "A apărut o eroare la încărcarea vehiculelor.", MessagePopupType.Error, MessagePopupButtons.Ok);
                break;
            case <= 5:
                await MessagePopup.ShowAsync("Info", "E posibil ca agenția să nu trimită date.\nMajoritatea vehiculelor nu circulă între orele 23:30 si 04:30", MessagePopupType.Warning, MessagePopupButtons.Ok);
                break;
        }
    }

    [RelayCommand]
    private void Disappearing()
    {
        _vehicleService.StopPeriodicRefresh();
    }

    public void Dispose()
    {
        _vehicleService.StopPeriodicRefresh();
        WeakReferenceMessenger.Default.UnregisterAll(this);
        GC.SuppressFinalize(this);
    }
}