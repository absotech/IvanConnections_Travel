using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using IvanConnections_Travel.Messages;
using IvanConnections_Travel.Models;
using IvanConnections_Travel.Models.Enums;
using IvanConnections_Travel.Services;
using IvanConnections_Travel.Utils;
using IvanConnections_Travel.ViewModels.Popups;
using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Maui;
using Location = Microsoft.Maui.Devices.Sensors.Location;

namespace IvanConnections_Travel.ViewModels;

public partial class MainPageViewModel : ObservableObject, IDisposable
{
    private const int MaxSuggestions = 8;

    private readonly IVehicleService _vehicleService;
    private readonly ApiService _apiService;
    private readonly IPopupService _popupService;

    private bool _isInitialized;
    private IDispatcherTimer? _statusTimer;

    /// <summary>Raised when the user taps on an empty area of the map (page closes overlays / keyboard).</summary>
    public event EventHandler? DismissOverlaysRequested;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowQuickRoutes))]
    [NotifyPropertyChangedFor(nameof(ShowFilters))]
    private bool _isTracking;

    public Vehicle? TrackedVehicle
    {
        get => _vehicleService.TrackedVehicle;
        set
        {
            if (_vehicleService.TrackedVehicle == value) return;
            _vehicleService.TrackedVehicle = value;
            IsTracking = value != null;
            if (value != null) SetSearchTextSilently("");
            OnPropertyChanged();
            OnTrackedVehicleDetailsChanged();
        }
    }

    public RouteChip? TrackedRouteChip => TrackedVehicle is { RouteShortName: { } route } v
        ? new RouteChip(route, v.RouteColor)
        : null;

    public string TrackedTitle => TrackedVehicle is { } v
        ? string.IsNullOrWhiteSpace(v.TripHeadsign) ? $"Vehicul {v.Label}" : $"spre {v.TripHeadsign}"
        : string.Empty;

    public string TrackedSubtitle
    {
        get
        {
            if (TrackedVehicle is not { } v) return string.Empty;
            var type = v.VehicleType is { } t ? Translations.GetVehicleTypeNameInRomanian(t) : "Vehicul";
            var next = v.NextStop?.StopName is { Length: > 0 } stop ? $" · următoarea: {stop}" : string.Empty;
            return $"{type} {v.Label}{next}";
        }
    }

    public HashSet<string> Routes => _vehicleService.AvailableRoutes;

    public ObservableCollection<RouteChip> RouteSuggestions { get; } = [];
    public ObservableCollection<RouteChip> QuickRoutes { get; } = [];

    public bool HasSuggestions => RouteSuggestions.Count > 0;

    public bool ShowQuickRoutes => !IsTracking && string.IsNullOrWhiteSpace(SearchText) && QuickRoutes.Count > 0;

    public bool ShowNoRouteHint =>
        !string.IsNullOrWhiteSpace(SearchText) && MatchedRoute is null && RouteSuggestions.Count == 0 && Routes.Count > 0;

    public bool HasSearchText => !string.IsNullOrEmpty(SearchText);

    public string? MatchedRoute => _vehicleService.MatchedRoute;

    public string SearchText
    {
        get => _vehicleService.SearchText ?? "";
        set
        {
            if (_vehicleService.SearchText == value) return;

            // Typing a route while tracking simply stops tracking instead of blocking the search.
            if (IsTracking && !string.IsNullOrWhiteSpace(value))
            {
                _vehicleService.TrackedVehicle = null;
                IsTracking = false;
                OnPropertyChanged(nameof(TrackedVehicle));
                OnTrackedVehicleDetailsChanged();
            }

            _vehicleService.SearchText = value;
            OnSearchStateChanged();
        }
    }

    private void SetSearchTextSilently(string value)
    {
        if (_vehicleService.SearchText == value) return;
        _vehicleService.SearchText = value;
        OnSearchStateChanged();
    }

    private void OnSearchStateChanged()
    {
        OnPropertyChanged(nameof(SearchText));
        OnPropertyChanged(nameof(HasSearchText));
        OnPropertyChanged(nameof(MatchedRoute));
        OnPropertyChanged(nameof(IsDirectionFilterAvailable));
        OnPropertyChanged(nameof(FilterHeaderText));
        OnPropertyChanged(nameof(ShowQuickRoutes));
        if (MatchedRoute is { } route) RecentRoutes.Add(route);
        UpdateSuggestions();
        UpdateDirectionDestinations();
        UpdateEmptyBanner();
    }

    public ObservableCollection<Vehicle> Pins => _vehicleService.Vehicles;
    public ObservableCollection<Shape> Shapes => _vehicleService.Shapes;

    public string? ShapeFilter
    {
        get => _vehicleService.ShapeFilter;
        set
        {
            if (_vehicleService.ShapeFilter == value) return;
            _vehicleService.ShapeFilter = value;
            OnPropertyChanged();
        }
    }

    public VehicleType? VehicleTypeFilter
    {
        get => _vehicleService.VehicleTypeFilter;
        set
        {
            if (_vehicleService.VehicleTypeFilter == value) return;
            _vehicleService.VehicleTypeFilter = value;
            OnPropertyChanged();
        }
    }

    /// <summary>The direction (Tur/Retur) filter only makes sense once a route is selected.</summary>
    public bool IsDirectionFilterAvailable => MatchedRoute is not null;

    public bool ShowFilters => !IsTracking;

    public int ActiveFilterCount =>
        (VehicleTypeFilter.HasValue ? 1 : 0) + (IsDirectionFilterAvailable && ShapeFilter is not null ? 1 : 0);

    public string FilterHeaderText => ActiveFilterCount > 0 ? $"Filtre active ({ActiveFilterCount})" : "Opțiuni filtrare";

    /// <summary>Destinations served by the selected route (from live vehicles), e.g. "Tătărași Nord · Copou".</summary>
    [ObservableProperty] private string _directionDestinations = string.Empty;

    [RelayCommand]
    private void SetShapeFilter(string? filter)
    {
        ShapeFilter = ShapeFilter == filter ? null : filter;
        Haptics.Click();
    }

    [RelayCommand]
    private void SetVehicleTypeFilter(VehicleType? type)
    {
        VehicleTypeFilter = type is null || VehicleTypeFilter == type ? null : type;
        Haptics.Click();
    }

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

    [ObservableProperty] private bool _isLocating;

    public bool IsOffline => _vehicleService.IsOffline;

    [ObservableProperty] private string _lastUpdatedText = string.Empty;

    [ObservableProperty] private bool _showEmptyBanner;
    [ObservableProperty] private string _emptyBannerText = string.Empty;
    private bool _emptyBannerDismissed;

    internal string? deviceId;
    [ObservableProperty] private List<Stop> _allStops = [];

    [ObservableProperty] private bool _isLoggedIn;
    [ObservableProperty] private string _userDisplayName = string.Empty;
    [ObservableProperty] private string _userAvatarUrl = string.Empty;
    [ObservableProperty] private string _userAvatarSeed = string.Empty;

    [ObservableProperty] private bool _showStopsOnMap;
    [ObservableProperty] private bool _isTrafficEnabled;
    [ObservableProperty] private int _stopSize = 15;
    [ObservableProperty] private int _vehicleSize = 25;
    [ObservableProperty] private bool _showNumberOnly;
    [ObservableProperty] private Location? _mapCenterLocation;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CompassRotation))]
    [NotifyPropertyChangedFor(nameof(IsCompassVisible))]
    private double _mapBearing;

    public double CompassRotation => -MapBearing;
    public bool IsCompassVisible => Math.Abs(MapBearing) > 0.1;

    [RelayCommand]
    private void ResetBearing() => MapBearing = 0;

    public MainPageViewModel(IVehicleService vehicleService, ApiService apiService, IPopupService popupService)
    {
        _vehicleService = vehicleService;
        _apiService = apiService;
        _popupService = popupService;
        IsTrafficEnabled = Microsoft.Maui.Storage.Preferences.Default.Get("IsTrafficEnabled", true);
        ShowStopsOnMap = Microsoft.Maui.Storage.Preferences.Default.Get("ShowStopsOnMap", true);
        ShowNumberOnly = Microsoft.Maui.Storage.Preferences.Default.Get("ShowNumberOnly", false);

        WeakReferenceMessenger.Default.Register<ClickMessage>(this, (_, m) => HandleVehicleClickMessage(m));
        WeakReferenceMessenger.Default.Register<StopClickMessage>(this, (_, m) => HandleStopClickMessage(m));
        WeakReferenceMessenger.Default.Register<TrafficPreferenceChangedMessage>(this,
            (_, m) => IsTrafficEnabled = m.Value);
        WeakReferenceMessenger.Default.Register<StopsPreferenceChangedMessage>(this, (_, m) =>
        {
            ShowStopsOnMap = m.Value;
            _ = LoadStopsFromBackendAsync();
        });
        WeakReferenceMessenger.Default.Register<NumberOnlyPreferenceChangedMessage>(this,
            (_, m) => ShowNumberOnly = m.Value);
        WeakReferenceMessenger.Default.Register<TrackVehicleRequestMessage>(this,
            (_, m) => HandleTrackVehicleRequest(m));

        _vehicleService.PropertyChanged += (_, e) =>
        {
            switch (e.PropertyName)
            {
                case nameof(IVehicleService.Vehicles):
                    OnPropertyChanged(nameof(Pins));
                    UpdateTrackedVehicleLocation();
                    UpdateEmptyBanner();
                    break;
                case nameof(IVehicleService.TrackedVehicle):
                    IsTracking = _vehicleService.TrackedVehicle != null;
                    OnPropertyChanged(nameof(TrackedVehicle));
                    OnTrackedVehicleDetailsChanged();
                    break;
                case nameof(IVehicleService.AllVehicles):
                    UpdateDirectionDestinations();
                    break;
                case nameof(IVehicleService.Shapes):
                    OnPropertyChanged(nameof(Shapes));
                    break;
                case nameof(IVehicleService.ShapeFilter):
                    OnPropertyChanged(nameof(ShapeFilter));
                    OnPropertyChanged(nameof(ActiveFilterCount));
                    OnPropertyChanged(nameof(FilterHeaderText));
                    break;
                case nameof(IVehicleService.VehicleTypeFilter):
                    OnPropertyChanged(nameof(VehicleTypeFilter));
                    OnPropertyChanged(nameof(ActiveFilterCount));
                    OnPropertyChanged(nameof(FilterHeaderText));
                    UpdateEmptyBanner();
                    break;
                case nameof(IVehicleService.IsBusy):
                    OnPropertyChanged(nameof(IsBusy));
                    break;
                case nameof(IVehicleService.IsOffline):
                    OnPropertyChanged(nameof(IsOffline));
                    UpdateEmptyBanner();
                    break;
                case nameof(IVehicleService.LastUpdatedUtc):
                    UpdateLastUpdatedText();
                    break;
                case nameof(IVehicleService.AvailableRoutes):
                    OnPropertyChanged(nameof(Routes));
                    OnPropertyChanged(nameof(MatchedRoute));
                    OnPropertyChanged(nameof(IsDirectionFilterAvailable));
                    UpdateSuggestions();
                    UpdateQuickRoutes();
                    break;
            }
        };
    }

    private void OnTrackedVehicleDetailsChanged()
    {
        OnPropertyChanged(nameof(TrackedRouteChip));
        OnPropertyChanged(nameof(TrackedTitle));
        OnPropertyChanged(nameof(TrackedSubtitle));
    }

    #region Search & suggestions

    private Dictionary<string, string?> BuildRouteColors() =>
        _vehicleService.AllVehicles
            .Where(v => !string.IsNullOrEmpty(v.RouteShortName))
            .GroupBy(v => v.RouteShortName!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().RouteColor, StringComparer.OrdinalIgnoreCase);

    private void UpdateSuggestions()
    {
        RouteSuggestions.Clear();
        var text = SearchText.Trim();
        if (text.Length > 0 && MatchedRoute is null)
        {
            var colors = BuildRouteColors();
            var matches = Routes
                .Where(r => Utils.RouteSearch.Matches(r, text))
                .OrderBy(r => Utils.RouteSearch.StartsWith(r, text) ? 0 : 1)
                .ThenBy(r => r.Length)
                .ThenBy(r => r, StringComparer.OrdinalIgnoreCase)
                .Take(MaxSuggestions);

            foreach (var route in matches)
                RouteSuggestions.Add(new RouteChip(route, colors.GetValueOrDefault(route)));
        }

        OnPropertyChanged(nameof(HasSuggestions));
        OnPropertyChanged(nameof(ShowNoRouteHint));
    }

    private void UpdateQuickRoutes()
    {
        var colors = BuildRouteColors();
        QuickRoutes.Clear();
        foreach (var route in RecentRoutes.GetAll().Where(r => Routes.Contains(r)))
            QuickRoutes.Add(new RouteChip(Routes.TryGetValue(route, out var actual) ? actual : route,
                colors.GetValueOrDefault(route)));
        OnPropertyChanged(nameof(ShowQuickRoutes));
    }

    private void UpdateDirectionDestinations()
    {
        DirectionDestinations = MatchedRoute is { } route
            ? string.Join(" · ", _vehicleService.AllVehicles
                .Where(v => string.Equals(v.RouteShortName, route, StringComparison.OrdinalIgnoreCase))
                .Select(v => v.TripHeadsign)
                .OfType<string>()
                .Where(h => h.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(2))
            : string.Empty;
    }

    [RelayCommand]
    private void SelectRoute(RouteChip? chip)
    {
        if (chip is null) return;
        SearchText = chip.ShortName;
        Haptics.Click();
        DismissOverlaysRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void ClearSearch()
    {
        SearchText = string.Empty;
        ShapeFilter = null;
        UpdateQuickRoutes();
    }

    #endregion

    #region Status (last updated / offline / empty)

    private void UpdateLastUpdatedText()
    {
        if (_vehicleService.LastUpdatedUtc is not { } last)
        {
            LastUpdatedText = string.Empty;
            return;
        }

        var seconds = (int)Math.Max(0, (DateTime.UtcNow - last).TotalSeconds);
        LastUpdatedText = seconds switch
        {
            < 3 => "Actualizat acum",
            < 60 => $"Actualizat acum {seconds}s",
            _ => $"Actualizat acum {seconds / 60} min"
        };
    }

    private void UpdateEmptyBanner()
    {
        if (!_isInitialized || _emptyBannerDismissed || IsOffline || IsTracking)
        {
            ShowEmptyBanner = false;
            return;
        }

        var hour = DateTime.Now.TimeOfDay;
        var isNight = hour >= new TimeSpan(23, 30, 0) || hour < new TimeSpan(4, 30, 0);
        var hasFilters = MatchedRoute is not null || VehicleTypeFilter.HasValue;
        var count = Pins.Count;

        (ShowEmptyBanner, EmptyBannerText) = (count, hasFilters, isNight) switch
        {
            (0, true, _) => (true, "Niciun vehicul pe această rută / filtru în acest moment."),
            (0, false, true) => (true, "Majoritatea vehiculelor nu circulă între 23:30 și 04:30."),
            (0, false, false) => (true, "Nu există vehicule momentan. Agenția poate să nu trimită date."),
            (<= 5, false, _) => (true, isNight
                ? "Puține vehicule active – majoritatea nu circulă între 23:30 și 04:30."
                : "Puține vehicule active – e posibil ca agenția să nu trimită date."),
            _ => (false, string.Empty)
        };
    }

    [RelayCommand]
    private void DismissEmptyBanner()
    {
        _emptyBannerDismissed = true;
        ShowEmptyBanner = false;
    }

    #endregion

    #region Vehicle / stop popups

    private async void HandleVehicleClickMessage(ClickMessage m)
    {
        if (m.Value is null) return;

        _vehicleService.IsPaused = true;
        try
        {
            var result = await _popupService.ShowPopupAsync<VehiclePopupViewModel, bool>(
                Shell.Current,
                new PopupOptions
                {
                    Shape = null,
                    PageOverlayColor = Color.FromArgb("#66000000")
                },
                new Dictionary<string, object>
                {
                    ["vehicle"] = m.Value,
                    ["userDisplayName"] = UserDisplayName,
                    ["userAvatarSeed"] = UserAvatarSeed,
                    ["isLoggedIn"] = IsLoggedIn,
                });

            if (result.WasDismissedByTappingOutsideOfPopup) return;
            TrackedVehicle = result.Result ? m.Value : null;
            if (result.Result) Haptics.Confirm();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[MainPageViewModel] Vehicle popup error: {ex.Message}");
        }
        finally
        {
            _vehicleService.IsPaused = false;
        }

        await _vehicleService.RefreshAsync(forced: true);
    }

    private async void HandleStopClickMessage(StopClickMessage m)
    {
        if (m.Value is null) return;

        _vehicleService.IsPaused = true;
        try
        {
            var query = new Dictionary<string, object> { ["stop"] = m.Value, ["allStops"] = AllStops };
            if (deviceId is not null) query["deviceId"] = deviceId;
            await _popupService.ShowPopupAsync<StopPopupViewModel>(
                Shell.Current,
                new PopupOptions
                {
                    Shape = null,
                    PageOverlayColor = Color.FromArgb("#66000000")
                },
                query);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[MainPageViewModel] Stop popup error: {ex.Message}");
        }
        finally
        {
            _vehicleService.IsPaused = false;
        }
    }

    private void HandleTrackVehicleRequest(TrackVehicleRequestMessage m)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            // The arrivals API sends the route number as "vehicleLabel"; the id is what matches.
            var vehicle = _vehicleService.AllVehicles.FirstOrDefault(v => v.Id == m.Value);

            if (vehicle is null)
            {
                await Toast.Make("Vehiculul nu transmite poziția momentan.").Show();
                return;
            }

            TrackedVehicle = vehicle;
            Haptics.Confirm();
            UpdateTrackedVehicleLocation();
            await _vehicleService.RefreshAsync(forced: true);
        });
    }

    #endregion

    private async Task InitializeAsync()
    {
        if (_isInitialized) return;

        StopSize = Microsoft.Maui.Storage.Preferences.Default.Get("StopSize", 15);
        VehicleSize = Microsoft.Maui.Storage.Preferences.Default.Get("VehicleSize", 25);

        await Task.WhenAll(LoadStopsFromBackendAsync(), _vehicleService.RefreshAsync(forced: true));
        _isInitialized = true;
        UpdateQuickRoutes();
        UpdateEmptyBanner();

        _ = LoginAsync();
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        try
        {
            deviceId = await SecureStorage.Default.GetAsync("device_id");
            if (string.IsNullOrEmpty(deviceId))
            {
                deviceId = Guid.NewGuid().ToString();
                await SecureStorage.Default.SetAsync("device_id", deviceId);
            }

            var (user, wasModified) = await _apiService.LoginAsync(deviceId);
            if (wasModified && user is not null)
            {
                UserDisplayName = user.DisplayName;
                UserAvatarUrl = user.AvatarUrl;
                UserAvatarSeed = user.AvatarSeed;
                IsLoggedIn = true;
                Debug.WriteLine($"[Login] Logged in as {user.DisplayName}");
                _vehicleService.DeviceId = deviceId;
            }
            else if (!wasModified)
            {
                // Not modified: we were already logged in or the ETag matched.
                IsLoggedIn = true;
                _vehicleService.DeviceId = deviceId;
            }
            else
            {
                Debug.WriteLine("[Login] Login failed.");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Login] Error: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task StopTracking()
    {
        TrackedVehicle = null;
        Haptics.Click();
        await _vehicleService.RefreshAsync(forced: true);
    }

    [RelayCommand]
    private void MarkerClicked(string? tag)
    {
        if (string.IsNullOrEmpty(tag)) return;

        if (tag.StartsWith("vehicle_"))
        {
            var vehicleLabel = tag["vehicle_".Length..];
            var vehicleData = Pins.FirstOrDefault(p => p.Label == vehicleLabel);
            if (vehicleData != null)
            {
                Haptics.Click();
                HandleVehicleClickMessage(new ClickMessage(vehicleData));
            }
        }
        else if (tag.StartsWith("stop_"))
        {
            if (int.TryParse(tag["stop_".Length..], out var stopId) &&
                AllStops.FirstOrDefault(s => s.StopId == stopId) is { } stopData)
            {
                Haptics.Click();
                HandleStopClickMessage(new StopClickMessage(stopData));
            }
        }
        else
        {
            Toast.Make(tag, ToastDuration.Short, 14).Show();
        }
    }

    [RelayCommand]
    private void MapClicked() => DismissOverlaysRequested?.Invoke(this, EventArgs.Empty);

    private void UpdateTrackedVehicleLocation()
    {
        if (TrackedVehicle is { Latitude: { } lat, Longitude: { } lon })
        {
            MainThread.BeginInvokeOnMainThread(() => MapCenterLocation = new Location(lat, lon));
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
        _emptyBannerDismissed = false;
        await _vehicleService.RefreshAsync(forced: true);
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task CenterOnUserLocationAsync()
    {
        IsLocating = true;
        try
        {
            var permissionStatus = await LocationManagement.CheckAndRequestLocationPermission();
            if (permissionStatus != PermissionStatus.Granted)
            {
                await Toast.Make("Permisiunea pentru locație nu a fost acordată.", ToastDuration.Long, 14).Show();
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
            IsLocating = false;
        }
    }

    [RelayCommand]
    private async Task ToggleTraffic()
    {
        IsTrafficEnabled = !IsTrafficEnabled;
        Microsoft.Maui.Storage.Preferences.Default.Set("IsTrafficEnabled", IsTrafficEnabled);
        Haptics.Click();
        await Toast.Make(IsTrafficEnabled ? "Trafic afișat" : "Trafic ascuns", ToastDuration.Short, 14).Show();
    }

    [RelayCommand]
    private async Task ToggleShowStops()
    {
        ShowStopsOnMap = !ShowStopsOnMap;
        Microsoft.Maui.Storage.Preferences.Default.Set("ShowStopsOnMap", ShowStopsOnMap);
        Haptics.Click();
        _ = LoadStopsFromBackendAsync();
        await Toast.Make(ShowStopsOnMap ? "Stații afișate" : "Stații ascunse", ToastDuration.Short, 14).Show();
    }

    [RelayCommand]
    private async Task AppearingAsync()
    {
        // Settings may have changed while we were away.
        StopSize = Microsoft.Maui.Storage.Preferences.Default.Get("StopSize", 15);
        VehicleSize = Microsoft.Maui.Storage.Preferences.Default.Get("VehicleSize", 25);

        await InitializeAsync();
        _vehicleService.StartPeriodicRefresh();

        _statusTimer ??= CreateStatusTimer();
        _statusTimer.Start();

        if (MapCenterLocation is null)
        {
            MapCenterLocation = await LocationManagement.GetLocationAsync();
        }
    }

    private IDispatcherTimer CreateStatusTimer()
    {
        var timer = Application.Current!.Dispatcher.CreateTimer();
        timer.Interval = TimeSpan.FromSeconds(1);
        timer.Tick += (_, _) => UpdateLastUpdatedText();
        return timer;
    }

    [RelayCommand]
    private void Disappearing()
    {
        _vehicleService.StopPeriodicRefresh();
        _statusTimer?.Stop();
    }

    public void Dispose()
    {
        _vehicleService.StopPeriodicRefresh();
        _statusTimer?.Stop();
        WeakReferenceMessenger.Default.UnregisterAll(this);
        GC.SuppressFinalize(this);
    }
}
