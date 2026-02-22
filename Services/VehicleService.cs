using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using IvanConnections_Travel.Models;

namespace IvanConnections_Travel.Services;

public partial class VehicleService : ObservableObject, IVehicleService, IDisposable
{
    private readonly ApiService _apiService;
    private CancellationTokenSource? _cts;
    private string? _previousSearchText;

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
                var searchChanged = SearchText != _previousSearchText;
                if (searchChanged)
                {
                    _previousSearchText = SearchText;
                    await RefreshAsync(forced: true);
                }
                else
                {
                    await RefreshAsync(forced: false);
                }
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
            string? routeToSearch = null;
            if (!string.IsNullOrWhiteSpace(SearchText) && AvailableRoutes.Contains(SearchText))
            {
                routeToSearch = SearchText;
            }

            var (freshVehicles, isNotModified) = await _apiService.GetVehiclesAsync(routeToSearch, forced);

            if (isNotModified || freshVehicles is null) return;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                UpdateVehiclesInternal(freshVehicles);
                PopulateRoutes(freshVehicles);
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

        foreach (var route in uniqueRoutes)
        {
            if (route != null) AvailableRoutes.Add(route);
        }
    }

    public void Dispose()
    {
        StopPeriodicRefresh();
        GC.SuppressFinalize(this);
    }
}
