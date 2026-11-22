using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using IvanConnections_Travel.Messages;
using IvanConnections_Travel.Models;

namespace IvanConnections_Travel.Services;

public partial class VehicleService : ObservableObject, IVehicleService, IDisposable
{
    public const string RefreshIntervalPreferenceKey = "RefreshIntervalSeconds";
    public const int DefaultRefreshIntervalSeconds = 5;

    private readonly ApiService _apiService;
    private readonly SemaphoreSlim _refreshGate = new(1, 1);
    private CancellationTokenSource? _cts;
    private PeriodicTimer? _timer;

    public string? DeviceId { get; set; }

    [ObservableProperty]
    private ObservableCollection<Vehicle> _vehicles = [];

    [ObservableProperty]
    private HashSet<string> _availableRoutes = new(StringComparer.OrdinalIgnoreCase);

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isOffline;

    [ObservableProperty]
    private DateTime? _lastUpdatedUtc;

    [ObservableProperty]
    private bool _isPaused;

    [ObservableProperty]
    private int _refreshIntervalSeconds;

    [ObservableProperty]
    private Vehicle? _trackedVehicle;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MatchedRoute))]
    private string? _searchText;

    [ObservableProperty]
    private string? _shapeFilter;

    [ObservableProperty]
    private Models.Enums.VehicleType? _vehicleTypeFilter;

    [ObservableProperty]
    private ObservableCollection<Shape> _shapes = [];

    private int? _previousRouteId;
    private string? _previousShapeFilter;
    private List<Vehicle> _allVehicles = [];

    /// <summary>Unfiltered snapshot of the last refresh (used for route suggestions).</summary>
    public IReadOnlyList<Vehicle> AllVehicles => _allVehicles;

    public VehicleService(ApiService apiService)
    {
        _apiService = apiService;
        _refreshIntervalSeconds = Math.Clamp(
            Microsoft.Maui.Storage.Preferences.Default.Get(RefreshIntervalPreferenceKey, DefaultRefreshIntervalSeconds), 2, 60);

        WeakReferenceMessenger.Default.Register<RefreshIntervalChangedMessage>(this,
            (_, m) => RefreshIntervalSeconds = m.Value);
    }

    /// <summary>
    /// Resolves the typed search to an existing route, ignoring case and surrounding whitespace.
    /// </summary>
    public string? MatchedRoute
    {
        get
        {
            var text = SearchText?.Trim();
            if (string.IsNullOrEmpty(text)) return null;
            if (AvailableRoutes.TryGetValue(text, out var actual)) return actual;

            // Lenient match: "cu" -> "C-U", "1 b" -> "1B".
            return AvailableRoutes.FirstOrDefault(r => Utils.RouteSearch.AreEquivalent(r, text));
        }
    }

    partial void OnAvailableRoutesChanged(HashSet<string> value) => OnPropertyChanged(nameof(MatchedRoute));

    partial void OnRefreshIntervalSecondsChanged(int value)
    {
        var seconds = Math.Clamp(value, 2, 60);
        if (_timer is not null) _timer.Period = TimeSpan.FromSeconds(seconds);
    }

    public void StartPeriodicRefresh()
    {
        if (_cts != null) return;
        _cts = new CancellationTokenSource();
        _timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Clamp(RefreshIntervalSeconds, 2, 60)));
        _ = RefreshCoreAsync(forced: false, showBusy: false);
        _ = PeriodicRefreshLoopAsync(_timer, _cts.Token);
    }

    public void StopPeriodicRefresh()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        _timer?.Dispose();
        _timer = null;
    }

    private async Task PeriodicRefreshLoopAsync(PeriodicTimer timer, CancellationToken token)
    {
        try
        {
            while (await timer.WaitForNextTickAsync(token).ConfigureAwait(false))
            {
                if (IsPaused) continue;
                try
                {
                    await RefreshCoreAsync(forced: false, showBusy: false).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[VehicleService] Periodic Refresh Error: {ex.Message}");
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (ObjectDisposedException) { }
    }

    public Task RefreshAsync(bool forced = false) => RefreshCoreAsync(forced, showBusy: forced);

    private async Task RefreshCoreAsync(bool forced, bool showBusy)
    {
        // Background polls are simply skipped if a refresh is already running.
        if (!await _refreshGate.WaitAsync(forced ? Timeout.Infinite : 0).ConfigureAwait(false)) return;

        if (showBusy) SetOnMain(() => IsBusy = true);
        try
        {
            var (freshVehicles, isNotModified) = await _apiService.GetVehiclesAsync(null, forced).ConfigureAwait(false);

            if (isNotModified)
            {
                SetOnMain(() =>
                {
                    IsOffline = false;
                    LastUpdatedUtc = DateTime.UtcNow;
                });
                return;
            }

            if (freshVehicles is null)
            {
                SetOnMain(() => IsOffline = true);
                return;
            }

            _allVehicles = freshVehicles;

            await UpdateShapesInternalAsync().ConfigureAwait(false);

            SetOnMain(() =>
            {
                IsOffline = false;
                LastUpdatedUtc = DateTime.UtcNow;
                OnPropertyChanged(nameof(AllVehicles));
                PopulateRoutes(_allVehicles);
                UpdateVehiclesInternal(GetFilteredVehicles(_allVehicles));
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[VehicleService] Refresh Error: {ex.Message}");
            SetOnMain(() => IsOffline = true);
        }
        finally
        {
            if (showBusy) SetOnMain(() => IsBusy = false);
            _refreshGate.Release();
        }
    }

    private static void SetOnMain(Action action)
    {
        if (MainThread.IsMainThread) action();
        else MainThread.BeginInvokeOnMainThread(action);
    }

    private void UpdateVehiclesInternal(List<Vehicle> freshVehicles)
    {
        if (TrackedVehicle != null)
        {
            var updatedTrackedVehicle = _allVehicles.FirstOrDefault(v => v.Id == TrackedVehicle.Id);
            if (updatedTrackedVehicle != null)
            {
                updatedTrackedVehicle.IsTracked = true;
                TrackedVehicle = updatedTrackedVehicle;
                Vehicles = [TrackedVehicle];
            }
            else
            {
                TrackedVehicle = null;
                Vehicles = new ObservableCollection<Vehicle>(freshVehicles);
            }
        }
        else
        {
            Vehicles = new ObservableCollection<Vehicle>(freshVehicles);
        }
    }

    private void PopulateRoutes(List<Vehicle> freshVehicles)
    {
        if (freshVehicles.Count == 0) return;

        var routesSet = new HashSet<string>(
            freshVehicles
                .Select(v => v.RouteShortName)
                .OfType<string>()
                .Where(name => name.Length > 0),
            StringComparer.OrdinalIgnoreCase);

        if (!routesSet.SetEquals(AvailableRoutes))
            AvailableRoutes = routesSet;
    }

    partial void OnSearchTextChanged(string? value)
    {
        SetOnMain(() => UpdateVehiclesInternal(GetFilteredVehicles(_allVehicles)));
        _ = UpdateShapesInternalAsync();
    }

    partial void OnShapeFilterChanged(string? value)
    {
        _ = UpdateShapesInternalAsync();
    }

    partial void OnVehicleTypeFilterChanged(Models.Enums.VehicleType? value)
    {
        SetOnMain(() => UpdateVehiclesInternal(GetFilteredVehicles(_allVehicles)));
    }

    private List<Vehicle> GetFilteredVehicles(List<Vehicle> allVehicles)
    {
        IEnumerable<Vehicle> filtered = allVehicles;

        if (MatchedRoute is { } route)
        {
            filtered = filtered.Where(v => string.Equals(v.RouteShortName, route, StringComparison.OrdinalIgnoreCase));
        }

        if (VehicleTypeFilter is { } type)
        {
            filtered = filtered.Where(v => v.VehicleType == type);
        }

        return [.. filtered];
    }

    private async Task UpdateShapesInternalAsync()
    {
        var route = MatchedRoute;
        var currentRouteId = route is not null
            ? _allVehicles.FirstOrDefault(v => string.Equals(v.RouteShortName, route, StringComparison.OrdinalIgnoreCase) && v.RouteId.HasValue)?.RouteId
            : null;

        if (currentRouteId == _previousRouteId && ShapeFilter == _previousShapeFilter) return;

        _previousRouteId = currentRouteId;
        _previousShapeFilter = ShapeFilter;

        if (currentRouteId != null)
        {
            var shapesList = await _apiService.GetShapesAsync(currentRouteId.Value, DeviceId).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(ShapeFilter))
            {
                shapesList = [.. shapesList.Where(s => s.ShapeId.EndsWith(ShapeFilter))];
            }

            SetOnMain(() => Shapes = new ObservableCollection<Shape>(shapesList));
        }
        else
        {
            SetOnMain(() => Shapes = []);
        }
    }

    public void Dispose()
    {
        StopPeriodicRefresh();
        WeakReferenceMessenger.Default.UnregisterAll(this);
        _refreshGate.Dispose();
        GC.SuppressFinalize(this);
    }
}
