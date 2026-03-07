using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using IvanConnections_Travel.Models;

namespace IvanConnections_Travel.Services;

public partial class VehicleService : ObservableObject, IVehicleService, IDisposable
{
    private readonly ApiService _apiService;
    private CancellationTokenSource? _cts;

    [ObservableProperty]
    private ObservableCollection<Vehicle> _vehicles = [];

    [ObservableProperty]
    private HashSet<string> _availableRoutes = [];

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private Vehicle? _trackedVehicle;

    [ObservableProperty]
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

    public VehicleService(ApiService apiService)
    {
        _apiService = apiService;
    }

    public void StartPeriodicRefresh()
    {
        if (_cts != null) return;
        _cts = new CancellationTokenSource();
        Task.Run(() => PeriodicRefreshLoopAsync(_cts.Token));
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
            try
            {
                await RefreshAsync(forced: false);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Debug.WriteLine($"[VehicleService] Periodic Refresh Error: {ex.Message}");
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(3), token);
            }
            catch (OperationCanceledException) { }
        }
    }

    public async Task RefreshAsync(bool forced = false)
    {
        IsBusy = true;
        try
        {
            var (freshVehicles, isNotModified) = await _apiService.GetVehiclesAsync(null, forced);

            if (isNotModified || freshVehicles is null) return;

            _allVehicles = freshVehicles;

            await UpdateShapesInternalAsync();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                PopulateRoutes(_allVehicles);
                UpdateVehiclesInternal(GetFilteredVehicles(_allVehicles));
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[VehicleService] Refresh Error: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void UpdateVehiclesInternal(List<Vehicle> freshVehicles)
    {
        if (TrackedVehicle != null)
        {
            var updatedTrackedVehicle = freshVehicles.FirstOrDefault(v => v.Id == TrackedVehicle.Id);
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

        var uniqueRoutes = freshVehicles
            .Select(v => v.RouteShortName)
            .Where(name => !string.IsNullOrEmpty(name))
            .Distinct()
            .ToList();

        var routesSet = new HashSet<string>();
        foreach (var route in uniqueRoutes.OfType<string>())
        {
            routesSet.Add(route);
        }
        AvailableRoutes = routesSet;
    }

    partial void OnSearchTextChanged(string? value)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            UpdateVehiclesInternal(GetFilteredVehicles(_allVehicles));
        });
        _ = UpdateShapesInternalAsync();
    }

    partial void OnShapeFilterChanged(string? value)
    {
        _ = UpdateShapesInternalAsync();
    }

    partial void OnVehicleTypeFilterChanged(Models.Enums.VehicleType? value)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            UpdateVehiclesInternal(GetFilteredVehicles(_allVehicles));
        });
    }

    private List<Vehicle> GetFilteredVehicles(List<Vehicle> allVehicles)
    {
        var filtered = allVehicles;
        if (!string.IsNullOrWhiteSpace(SearchText) && AvailableRoutes.Contains(SearchText))
        {
            filtered = filtered
                .Where(v => v.RouteShortName?.Equals(SearchText, StringComparison.OrdinalIgnoreCase) == true)
                .ToList();
        }

        if (VehicleTypeFilter.HasValue)
        {
            filtered = filtered
                .Where(v => v.VehicleType == VehicleTypeFilter.Value)
                .ToList();
        }

        return filtered;
    }

    private async Task UpdateShapesInternalAsync()
    {
        var currentRouteId = !string.IsNullOrWhiteSpace(SearchText) && _allVehicles.Count > 0
            ? _allVehicles.FirstOrDefault(v => v.RouteShortName?.Equals(SearchText, StringComparison.OrdinalIgnoreCase) == true && v.RouteId.HasValue)?.RouteId
            : null;

        if (currentRouteId != _previousRouteId || ShapeFilter != _previousShapeFilter)
        {
            _previousRouteId = currentRouteId;
            _previousShapeFilter = ShapeFilter;
            
            if (currentRouteId != null)
            {
                var shapesList = await _apiService.GetShapesAsync(currentRouteId.Value);

                if (!string.IsNullOrEmpty(ShapeFilter))
                {
                    shapesList = shapesList.Where(s => s.ShapeId.EndsWith(ShapeFilter)).ToList();
                }

                MainThread.BeginInvokeOnMainThread(() => Shapes = new ObservableCollection<Shape>(shapesList));
            }
            else
            {
                MainThread.BeginInvokeOnMainThread(() => Shapes = []);
            }
        }
    }

    public void Dispose()
    {
        StopPeriodicRefresh();
        GC.SuppressFinalize(this);
    }
}
