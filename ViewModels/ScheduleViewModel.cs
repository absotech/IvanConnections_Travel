using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IvanConnections_Travel.Models;
using IvanConnections_Travel.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace IvanConnections_Travel.ViewModels;

public partial class StopTabItem : ObservableObject
{
    public string StartingStop { get; set; } = string.Empty;
    [ObservableProperty] private bool _isSelected;
}

public partial class ScheduleViewModel : ObservableObject
{
    private readonly ApiService _apiService;

    private List<ScheduledTrip> _allSchedules = [];

    [ObservableProperty] private ObservableCollection<Models.Route> _routes = [];
    [ObservableProperty] private ObservableCollection<ScheduledTrip> _schedules = [];
    [ObservableProperty] private ObservableCollection<StopTabItem> _stopTabs = [];
    [ObservableProperty] private Models.Route? _selectedRoute;
    [ObservableProperty] private string? _selectedStartingStop;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isLoadingSchedules;
    [ObservableProperty] private string _selectedDayType = "weekday";
    [ObservableProperty] private bool _hasSchedules;
    [ObservableProperty] private bool _hasNoResults;
    [ObservableProperty] private bool _isFavorite;
    [ObservableProperty] private bool _hasSchoolSchedules;

    private bool _dayTypeDefaultApplied;

    public ObservableCollection<string> DayTypes { get; } = ["weekday", "weekend"];

    public ScheduleViewModel(ApiService apiService)
    {
        _apiService = apiService;

        var today = DateTime.Now.DayOfWeek;
        _selectedDayType = today is DayOfWeek.Saturday or DayOfWeek.Sunday ? "weekend" : "weekday";
    }

    [RelayCommand]
    public async Task LoadRoutesAsync()
    {
        _ = ApplyHolidayDefaultAsync();
        if (IsLoading) return;
        IsLoading = true;
        try
        {
            var routes = await _apiService.GetRoutesAsync();
            var sortedRoutes = routes.OrderBy(r => r.RouteShortName, StringComparer.OrdinalIgnoreCase).ToList();
            
            Routes.Clear();
            foreach (var r in sortedRoutes)
                Routes.Add(r);
            if (SelectedRoute == null)
            {
                var favoriteId = Utils.Preferences.FavoriteRouteId;
                if (favoriteId != -1)
                    SelectedRoute = Routes.FirstOrDefault(r => r.RouteId == favoriteId);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ScheduleViewModel] Error loading routes: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }
    private async Task ApplyHolidayDefaultAsync()
    {
        if (_dayTypeDefaultApplied) return;
        _dayTypeDefaultApplied = true;

        if (await _apiService.HolidayCheckAsync())
            SelectedDayType = "weekend";
    }

    partial void OnSelectedRouteChanged(Models.Route? value)
    {
        SelectedStartingStop = null;
        StopTabs.Clear();
        Schedules.Clear();
        _allSchedules = [];
        HasSchedules = false;
        HasNoResults = false;
        IsFavorite = value != null && value.RouteId == Utils.Preferences.FavoriteRouteId;

        if (value != null)
            _ = LoadRouteSchedulesAsync(value.RouteId.ToString());
    }
    [RelayCommand]
    private void ToggleFavorite()
    {
        if (SelectedRoute == null) return;

        if (IsFavorite)
        {
            Utils.Preferences.FavoriteRouteId = -1;
            IsFavorite = false;
        }
        else
        {
            Utils.Preferences.FavoriteRouteId = SelectedRoute.RouteId;
            IsFavorite = true;
        }
    }

    partial void OnSelectedStartingStopChanged(string? value)
    {
        if (value != null)
            ApplyFilter();
    }

    partial void OnSelectedDayTypeChanged(string value)
    {
        if (SelectedStartingStop != null)
            ApplyFilter();
    }

    private async Task LoadRouteSchedulesAsync(string routeId)
    {
        IsLoadingSchedules = true;
        try
        {
            var all = await _apiService.GetSchedulesAsync(routeId: routeId);
            _allSchedules = all.ToList();

            var stops = _allSchedules
                .Select(s => s.StartingStop)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct()
                .ToList();

            StopTabs.Clear();
            foreach (var stop in stops)
                StopTabs.Add(new StopTabItem { StartingStop = stop! });

            if (stops.Count == 0)
            {
                SelectedStartingStop = null;
                HasSchedules = false;
                HasNoResults = true;
                return;
            }

            var nearest = await FindNearestStartingStopAsync(stops!) ?? stops[0]!;

            foreach (var tab in StopTabs)
                tab.IsSelected = tab.StartingStop == nearest;

            SelectedStartingStop = nearest;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ScheduleViewModel] Error loading schedules: {ex.Message}");
        }
        finally
        {
            IsLoadingSchedules = false;
        }
    }
    private async Task<string?> FindNearestStartingStopAsync(List<string> stops)
    {
        try
        {
            var location = await Utils.LocationManagement.GetCurrentLocationAsync();
            if (location == null) return null;
            var coords = stops
                .Select(name => new
                {
                    Name = name,
                    Stop = _allSchedules.FirstOrDefault(s =>
                        string.Equals(s.StartingStop, name, StringComparison.OrdinalIgnoreCase) && s.Stop != null)?.Stop
                })
                .Where(x => x.Stop != null)
                .ToList();

            if (coords.Count == 0) return null;

            var here = new Location(location.Latitude, location.Longitude);
            return coords
                .OrderBy(x => here.CalculateDistance(
                    new Location(x.Stop!.StopLat, x.Stop.StopLon), DistanceUnits.Kilometers))
                .First().Name;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ScheduleViewModel] Error finding nearest stop: {ex.Message}");
            return null;
        }
    }

    private void ApplyFilter()
    {
        var filtered = _allSchedules
            .Where(s => string.Equals(s.StartingStop, SelectedStartingStop, StringComparison.OrdinalIgnoreCase))
            .Where(s => MatchesDayType(s.ServiceDayType))
            .OrderBy(s => s.DepartureTimeSpan)
            .ToList();

        Schedules.Clear();
        foreach (var s in filtered)
            Schedules.Add(s);

        HasSchedules = Schedules.Count > 0;
        HasNoResults = Schedules.Count == 0;
        HasSchoolSchedules = SelectedDayType == "weekday" && filtered.Any(IsSchool);
    }

    private static bool IsSchool(ScheduledTrip trip) =>
        string.Equals(trip.ServiceDayType, "weekday_school", StringComparison.OrdinalIgnoreCase);

    private bool MatchesDayType(string? serviceDayType)
    {
        var isWeekday = string.Equals(serviceDayType, "weekday", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(serviceDayType, "weekday_school", StringComparison.OrdinalIgnoreCase);
        return SelectedDayType == "weekday" ? isWeekday : !isWeekday;
    }

    [RelayCommand]
    private void SelectDayType(string dayType)
    {
        SelectedDayType = dayType;
    }

    [RelayCommand]
    private void SelectStartingStop(string stop)
    {
        foreach (var tab in StopTabs)
            tab.IsSelected = tab.StartingStop == stop;
        SelectedStartingStop = stop;
    }
}
