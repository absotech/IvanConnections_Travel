using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IvanConnections_Travel.Models;
using IvanConnections_Travel.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;

namespace IvanConnections_Travel.ViewModels;

public partial class StopTabItem : ObservableObject
{
    public string StartingStop { get; set; } = string.Empty;
    [ObservableProperty] private bool _isSelected;
}

/// <summary>
/// A route shown as a colored chip in the searchable route selector.
/// </summary>
public partial class RouteChipItem(Models.Route route) : ObservableObject
{
    public Models.Route Route { get; } = route;
    public string ShortName { get; } = route.RouteShortName ?? route.RouteId.ToString();
    public string LongName { get; } = route.RouteLongName ?? string.Empty;

    /// <summary>Route color parsed from the API, or null when unavailable (the view falls back to Accent).</summary>
    public Color? ChipColor { get; } = ParseColor(route.RouteColor);

    /// <summary>Readable text color for <see cref="ChipColor"/>, or null when there is no route color.</summary>
    public Color? ChipTextColor { get; } = GetTextColor(route.RouteColor, route.RouteTextColor);

    [ObservableProperty] private bool _isFavorite;
    [ObservableProperty] private bool _isSelected;

    private static Color? ParseColor(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return null;
        var value = hex.Trim();
        if (!value.StartsWith('#')) value = "#" + value;
        if (value.Length is not (4 or 7 or 9)) return null;
        try
        {
            return Color.FromArgb(value);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static Color? GetTextColor(string? routeColor, string? routeTextColor)
    {
        var background = ParseColor(routeColor);
        if (background == null) return null;

        var explicitText = ParseColor(routeTextColor);
        if (explicitText != null && !IsSameColor(explicitText, background)) return explicitText;

        var brightness = 0.299 * background.Red + 0.587 * background.Green + 0.114 * background.Blue;
        return brightness < 0.6 ? Colors.White : Colors.Black;
    }

    private static bool IsSameColor(Color a, Color b) =>
        Math.Abs(a.Red - b.Red) < 0.01 && Math.Abs(a.Green - b.Green) < 0.01 && Math.Abs(a.Blue - b.Blue) < 0.01;
}

/// <summary>
/// A departure cell in the schedule grid.
/// </summary>
public partial class TripItem(ScheduledTrip trip) : ObservableObject
{
    public ScheduledTrip Trip { get; } = trip;
    public TimeSpan? Departure { get; } = trip.DepartureTimeSpan;
    public string DepartureDisplay { get; } = trip.DepartureDisplay;

    [ObservableProperty] private bool _isPast;
    [ObservableProperty] private bool _isNext;
}

public partial class ScheduleViewModel(ApiService apiService) : ObservableObject
{
    private const string Weekday = "weekday";
    private const string Weekend = "weekend";

    private readonly ApiService _apiService = apiService;

    private List<ScheduledTrip> _allSchedules = [];
    private List<RouteChipItem> _allRouteChips = [];
    private string _todayDayType = GetCalendarDayType(DateTime.Now);
    private DateOnly _dayTypeAppliedFor;
    private TripItem? _nextTrip;

    [ObservableProperty] private ObservableCollection<Models.Route> _routes = [];
    [ObservableProperty] private ObservableCollection<RouteChipItem> _routeChips = [];
    [ObservableProperty] private ObservableCollection<TripItem> _schedules = [];
    [ObservableProperty] private ObservableCollection<StopTabItem> _stopTabs = [];
    [ObservableProperty] private Models.Route? _selectedRoute;
    [ObservableProperty] private RouteChipItem? _selectedRouteChip;
    [ObservableProperty] private string _routeFilter = string.Empty;
    [ObservableProperty] private bool _isRouteSelectorOpen = true;
    [ObservableProperty] private string? _selectedStartingStop;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isLoadingSchedules;
    [ObservableProperty] private string _selectedDayType = GetCalendarDayType(DateTime.Now);
    [ObservableProperty] private bool _hasSchedules;
    [ObservableProperty] private bool _hasNoResults;
    [ObservableProperty] private bool _isFavorite;
    [ObservableProperty] private bool _hasSchoolSchedules;
    [ObservableProperty] private bool _hasNoMatchingRoutes;

    /// <summary>"Următoarea plecare în X min" / "Nu mai sunt plecări azi" (empty when not applicable).</summary>
    [ObservableProperty] private string _nextDepartureText = string.Empty;

    /// <summary>True when <see cref="NextDepartureText"/> has content to display.</summary>
    [ObservableProperty] private bool _showNextDepartureInfo;

    /// <summary>True when there is an upcoming departure today in the displayed schedule.</summary>
    [ObservableProperty] private bool _hasNextDeparture;

    /// <summary>
    /// Raised when the next departure is (re)computed after the schedule list changes or when the
    /// next departure moves to another trip. The page scrolls the grid to the given item.
    /// </summary>
    public event EventHandler<TripItem>? NextDepartureChanged;

    public ObservableCollection<string> DayTypes { get; } = [Weekday, Weekend];

    [RelayCommand]
    public async Task LoadRoutesAsync()
    {
        ApplyTodayDayType();
        if (IsLoading) return;
        IsLoading = true;
        try
        {
            var routes = await _apiService.GetRoutesAsync();
            var sortedRoutes = routes
                .OrderBy(r => GetNumericPrefix(r.RouteShortName))
                .ThenBy(r => r.RouteShortName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            Routes.Clear();
            foreach (var r in sortedRoutes)
                Routes.Add(r);

            var favoriteId = Utils.Preferences.FavoriteRouteId;
            _allRouteChips = [.. sortedRoutes.Select(r => new RouteChipItem(r) { IsFavorite = r.RouteId == favoriteId })];

            if (SelectedRoute == null && favoriteId != -1)
                SelectedRoute = Routes.FirstOrDefault(r => r.RouteId == favoriteId);

            SyncSelectedChip();
            RebuildRouteChips();
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

    /// <summary>
    /// Selects today's day type (weekday/weekend, weekend on public holidays) once per calendar day,
    /// so a manual choice is kept while the user stays on the same day.
    /// </summary>
    private void ApplyTodayDayType()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        if (_dayTypeAppliedFor == today) return;
        _dayTypeAppliedFor = today;

        _todayDayType = GetCalendarDayType(DateTime.Now);
        if (SelectedDayType != _todayDayType)
            SelectedDayType = _todayDayType;
        else
            UpdateNextDeparture(forceScroll: false);

        _ = ApplyHolidayDefaultAsync(today);
    }

    private async Task ApplyHolidayDefaultAsync(DateOnly day)
    {
        try
        {
            if (!await _apiService.HolidayCheckAsync()) return;
            if (_dayTypeAppliedFor != day) return;

            _todayDayType = Weekend;
            if (SelectedDayType != Weekend)
                SelectedDayType = Weekend;
            else
                UpdateNextDeparture(forceScroll: false);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ScheduleViewModel] Error checking holiday: {ex.Message}");
        }
    }

    private static string GetCalendarDayType(DateTime date) =>
        date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday ? Weekend : Weekday;

    private static int GetNumericPrefix(string? name)
    {
        if (string.IsNullOrEmpty(name)) return int.MaxValue;
        var digits = string.Concat(name.TakeWhile(char.IsDigit));
        return int.TryParse(digits, out var n) ? n : int.MaxValue;
    }

    partial void OnRouteFilterChanged(string value) => RebuildRouteChips();

    private void RebuildRouteChips()
    {
        var filter = RouteFilter?.Trim() ?? string.Empty;
        var compare = CultureInfo.InvariantCulture.CompareInfo;
        const CompareOptions options = CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace;

        var items = _allRouteChips
            .Where(c => filter.Length == 0
                        || Utils.RouteSearch.Matches(c.ShortName, filter)
                        || compare.IndexOf(c.LongName, filter, options) >= 0)
            .OrderByDescending(c => c.IsFavorite)
            .ToList();

        RouteChips = [.. items];
        HasNoMatchingRoutes = _allRouteChips.Count > 0 && items.Count == 0;
    }

    private void SyncSelectedChip()
    {
        RouteChipItem? selected = null;
        foreach (var chip in _allRouteChips)
        {
            chip.IsSelected = SelectedRoute != null && chip.Route.RouteId == SelectedRoute.RouteId;
            if (chip.IsSelected) selected = chip;
        }

        SelectedRouteChip = selected ?? (SelectedRoute != null ? new RouteChipItem(SelectedRoute) : null);
    }

    [RelayCommand]
    private void SelectRoute(RouteChipItem? chip)
    {
        if (chip == null) return;
        if (SelectedRoute?.RouteId != chip.Route.RouteId)
            SelectedRoute = chip.Route;

        IsRouteSelectorOpen = false;
        RouteFilter = string.Empty;
    }

    [RelayCommand]
    private void ToggleRouteSelector()
    {
        IsRouteSelectorOpen = !IsRouteSelectorOpen;
        if (!IsRouteSelectorOpen)
            RouteFilter = string.Empty;
    }

    partial void OnSelectedRouteChanged(Models.Route? value)
    {
        SelectedStartingStop = null;
        StopTabs.Clear();
        Schedules.Clear();
        _allSchedules = [];
        HasSchedules = false;
        HasNoResults = false;
        HasSchoolSchedules = false;
        IsFavorite = value != null && value.RouteId == Utils.Preferences.FavoriteRouteId;
        if (value != null)
            IsRouteSelectorOpen = false;

        SyncSelectedChip();
        UpdateNextDeparture(forceScroll: false);

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

        var favoriteId = Utils.Preferences.FavoriteRouteId;
        foreach (var chip in _allRouteChips)
            chip.IsFavorite = chip.Route.RouteId == favoriteId;
        if (SelectedRouteChip != null)
            SelectedRouteChip.IsFavorite = IsFavorite;
        RebuildRouteChips();

        try
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ScheduleViewModel] Haptic feedback unavailable: {ex.Message}");
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
        else
            UpdateNextDeparture(forceScroll: false);
    }

    private async Task LoadRouteSchedulesAsync(string routeId)
    {
        IsLoadingSchedules = true;
        try
        {
            var all = await _apiService.GetSchedulesAsync(routeId: routeId);

            // Ignore stale responses if the user switched routes meanwhile.
            if (SelectedRoute?.RouteId.ToString() != routeId) return;

            _allSchedules = all.ToList();

            var stops = _allSchedules
                .Select(s => s.StartingStop)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s!)
                .Distinct()
                .ToList();

            StopTabs.Clear();
            foreach (var stop in stops)
                StopTabs.Add(new StopTabItem { StartingStop = stop });

            if (stops.Count == 0)
            {
                SelectedStartingStop = null;
                HasSchedules = false;
                HasNoResults = true;
                UpdateNextDeparture(forceScroll: false);
                return;
            }

            var nearest = await FindNearestStartingStopAsync(stops) ?? stops[0];
            if (SelectedRoute?.RouteId.ToString() != routeId) return;

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

        Schedules = [.. filtered.Select(s => new TripItem(s))];

        HasSchedules = Schedules.Count > 0;
        HasNoResults = Schedules.Count == 0;
        HasSchoolSchedules = SelectedDayType == Weekday && filtered.Any(IsSchool);

        _nextTrip = null;
        UpdateNextDeparture(forceScroll: true);
    }

    /// <summary>
    /// Recomputes past/next departures and the header text. Called by the page every minute while visible.
    /// </summary>
    public void RefreshNextDeparture()
    {
        if (DateOnly.FromDateTime(DateTime.Now) != _dayTypeAppliedFor)
            ApplyTodayDayType();

        UpdateNextDeparture(forceScroll: false);
    }

    private void UpdateNextDeparture(bool forceScroll)
    {
        var isToday = SelectedDayType == _todayDayType;
        var now = DateTime.Now.TimeOfDay;
        var nowMinute = new TimeSpan(now.Hours, now.Minutes, 0);

        TripItem? next = null;
        foreach (var item in Schedules)
        {
            var departure = item.Departure;
            item.IsPast = isToday && departure.HasValue && departure.Value < nowMinute;

            var isNext = isToday && next == null && departure.HasValue && departure.Value >= nowMinute;
            if (isNext) next = item;
            item.IsNext = isNext;
        }

        HasNextDeparture = next != null;

        if (SelectedStartingStop == null || Schedules.Count == 0 || !isToday)
        {
            NextDepartureText = string.Empty;
        }
        else if (next?.Departure is { } nextDeparture)
        {
            var minutes = (int)(nextDeparture - nowMinute).TotalMinutes;
            NextDepartureText = minutes <= 0
                ? "Următoarea plecare acum"
                : $"Următoarea plecare în {minutes} min";
        }
        else
        {
            NextDepartureText = "Nu mai sunt plecări azi";
        }

        ShowNextDepartureInfo = NextDepartureText.Length > 0;

        var changed = !ReferenceEquals(next, _nextTrip);
        _nextTrip = next;
        if (next != null && (forceScroll || changed))
            NextDepartureChanged?.Invoke(this, next);
    }

    private static bool IsSchool(ScheduledTrip trip) =>
        string.Equals(trip.ServiceDayType, "weekday_school", StringComparison.OrdinalIgnoreCase);

    private bool MatchesDayType(string? serviceDayType)
    {
        var isWeekday = string.Equals(serviceDayType, Weekday, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(serviceDayType, "weekday_school", StringComparison.OrdinalIgnoreCase);
        return SelectedDayType == Weekday ? isWeekday : !isWeekday;
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
