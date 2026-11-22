using System.Diagnostics;
using IvanConnections_Travel.ViewModels;

namespace IvanConnections_Travel.Views;

public partial class SchedulePage : ContentPage
{
    private readonly ScheduleViewModel _viewModel;
    private IDispatcherTimer? _minuteTimer;

    public SchedulePage(ScheduleViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        _viewModel.NextDepartureChanged -= OnNextDepartureChanged;
        _viewModel.NextDepartureChanged += OnNextDepartureChanged;
        StartMinuteTimer();

        try
        {
            await _viewModel.LoadRoutesCommand.ExecuteAsync(null);
            _viewModel.RefreshNextDeparture();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SchedulePage] Error on appearing: {ex.Message}");
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        StopMinuteTimer();
        _viewModel.NextDepartureChanged -= OnNextDepartureChanged;
    }

    private void StartMinuteTimer()
    {
        StopMinuteTimer();
        _minuteTimer = Dispatcher.CreateTimer();
        _minuteTimer.IsRepeating = true;
        _minuteTimer.Interval = TimeUntilNextMinute();
        _minuteTimer.Tick += OnMinuteTick;
        _minuteTimer.Start();
    }

    private void StopMinuteTimer()
    {
        if (_minuteTimer == null) return;
        _minuteTimer.Stop();
        _minuteTimer.Tick -= OnMinuteTick;
        _minuteTimer = null;
    }

    private void OnMinuteTick(object? sender, EventArgs e)
    {
        _viewModel.RefreshNextDeparture();

        // Keep ticks aligned to the start of each minute so the countdown stays accurate.
        if (sender is IDispatcherTimer timer)
            timer.Interval = TimeUntilNextMinute();
    }

    private static TimeSpan TimeUntilNextMinute()
    {
        var now = DateTime.Now;
        var untilNext = TimeSpan.FromSeconds(60 - now.Second) - TimeSpan.FromMilliseconds(now.Millisecond);
        return untilNext + TimeSpan.FromMilliseconds(250);
    }

    private void OnNextDepartureChanged(object? sender, TripItem trip)
    {
        // Give the CollectionView a moment to lay out a freshly assigned ItemsSource.
        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(150), () =>
        {
            try
            {
                if (!TripsCollection.IsVisible || !_viewModel.Schedules.Contains(trip)) return;
                TripsCollection.ScrollTo(trip, position: ScrollToPosition.Center, animate: true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SchedulePage] Error scrolling to next departure: {ex.Message}");
            }
        });
    }

    private void OnMenuClicked(object? sender, EventArgs eventArgs)
    {
        Shell.Current.FlyoutIsPresented = true;
    }
}
