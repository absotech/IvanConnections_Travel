using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using IvanConnections_Travel.Messages;
using IvanConnections_Travel.Models;
using IvanConnections_Travel.Services;
using IvanConnections_Travel.Utils;
using System.Collections.ObjectModel;

namespace IvanConnections_Travel.ViewModels.Popups
{
    public partial class StopPopupViewModel : ObservableObject, IQueryAttributable
    {
        // Sosirile se reîmprospătează automat cât timp popup-ul este deschis; eticheta
        // „Actualizat acum Xs” este recalculată la fiecare secundă.
        private static readonly TimeSpan AutoRefreshInterval = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan LabelTickInterval = TimeSpan.FromSeconds(1);

        private readonly ApiService _apiService;
        private readonly IWidgetService? _widgetService;

        [ObservableProperty]
        private Stop? stop;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsBusy))]
        private bool isLoading;

        // Reîmprospătare „silențioasă”: lista rămâne afișată cât timp se încarcă datele noi.
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsBusy))]
        private bool isRefreshing;

        [ObservableProperty]
        private string? errorMessage;

        [ObservableProperty]
        private bool canSwitchDirection;

        // Informații pentru antetul și rezumatul popup-ului.
        [ObservableProperty]
        private string? stopSubtitle;

        [ObservableProperty]
        private string arrivalsSummary = string.Empty;

        [ObservableProperty]
        private bool hasArrivals;

        [ObservableProperty]
        private string? lastUpdateText;

        [ObservableProperty]
        private bool canPinWidget;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(FavoriteDescription))]
        private bool isFavorite;

        public bool IsBusy => IsLoading || IsRefreshing;

        public string FavoriteDescription => IsFavorite
            ? "Elimină stația din favorite"
            : "Adaugă stația la favorite";

        /// <summary>Cere închiderea popup-ului (tratat în code-behind).</summary>
        public event EventHandler? CloseRequested;

        private List<Stop> _allStops = [];
        private string? _deviceId;
        private DateTime? _lastUpdatedAt;
        private DateTime _lastRefreshAttemptAt = DateTime.MinValue;
        private int _loadVersion;
        private CancellationTokenSource? _autoRefreshCts;

        public ObservableCollection<StopArrivalItem> Arrivals { get; } = [];

        public StopPopupViewModel(ApiService apiService, IWidgetService? widgetService = null)
        {
            _apiService = apiService;
            _widgetService = widgetService;
            CanPinWidget = widgetService?.IsPinningSupported() == true;
        }

        [RelayCommand]
        private async Task PinWidgetAsync()
        {
            if (Stop != null && _widgetService != null)
            {
                await _widgetService.PinWidgetAsync(Stop.StopId, Stop.StopName);
            }
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            var stop = (Stop)query["stop"];
            var deviceId = query.TryGetValue("deviceId", out var d) ? d as string : null;
            if (query.TryGetValue("allStops", out var s) && s is List<Stop> allStops)
            {
                _allStops = allStops;
            }
            _ = LoadAsync(stop, deviceId);
        }

        [RelayCommand]
        private async Task SwitchDirectionAsync()
        {
            if (Stop?.OppositeStopId is not int oppositeId)
            {
                return;
            }

            var oppositeStop = _allStops.FirstOrDefault(s => s.StopId == oppositeId);
            if (oppositeStop == null)
            {
                return;
            }

            await LoadAsync(oppositeStop, _deviceId);
        }

        [RelayCommand]
        private async Task RefreshAsync()
        {
            if (Stop is null || IsBusy)
            {
                return;
            }

            // Dacă există deja sosiri, nu golim lista: o actualizăm pe loc.
            await LoadAsync(Stop, _deviceId, silent: HasArrivals);
        }

        [RelayCommand]
        private void ToggleFavorite()
        {
            if (Stop is null)
            {
                return;
            }

            IsFavorite = FavoriteStops.Toggle(Stop.StopId);

            try
            {
                HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            }
            catch
            {
                // Feedback-ul haptic nu este disponibil pe toate dispozitivele.
            }
        }

        [RelayCommand(CanExecute = nameof(CanTrackArrival))]
        private void TrackArrival(StopArrivalItem? item)
        {
            if (item is null || !item.CanTrack)
            {
                return;
            }

            StopAutoRefresh();
            WeakReferenceMessenger.Default.Send(new TrackVehicleRequestMessage(item.Arrival.VehicleId!.Value, item.VehicleLabel.Trim()));
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        private static bool CanTrackArrival(StopArrivalItem? item) => item?.CanTrack == true;

        partial void OnStopChanged(Stop? value)
        {
            CanSwitchDirection = value?.OppositeStopId is int oppositeId
                                 && _allStops.Any(s => s.StopId == oppositeId);
            StopSubtitle = BuildStopSubtitle(value);
            IsFavorite = value is not null && FavoriteStops.IsFavorite(value.StopId);
        }

        /// <summary>
        /// Detaliile secundare ale stației (codul și descrierea din GTFS), afișate sub nume.
        /// </summary>
        private static string? BuildStopSubtitle(Stop? stop)
        {
            if (stop is null)
            {
                return null;
            }

            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(stop.StopCode))
            {
                parts.Add($"Cod {stop.StopCode!.Trim()}");
            }
            if (!string.IsNullOrWhiteSpace(stop.StopDesc))
            {
                parts.Add(stop.StopDesc!.Trim());
            }

            return parts.Count == 0 ? null : string.Join(" · ", parts);
        }

        /// <summary>
        /// Pornește reîmprospătarea automată (idempotent). Apelat când popup-ul este afișat.
        /// </summary>
        public void StartAutoRefresh()
        {
            if (_autoRefreshCts is not null)
            {
                return;
            }

            var cts = new CancellationTokenSource();
            _autoRefreshCts = cts;
            _ = RunAutoRefreshAsync(cts.Token);
        }

        /// <summary>
        /// Oprește reîmprospătarea automată. Apelat când popup-ul se închide.
        /// </summary>
        public void StopAutoRefresh()
        {
            var cts = _autoRefreshCts;
            _autoRefreshCts = null;
            if (cts is null)
            {
                return;
            }

            cts.Cancel();
            cts.Dispose();
        }

        private async Task RunAutoRefreshAsync(CancellationToken token)
        {
            using var timer = new PeriodicTimer(LabelTickInterval);
            try
            {
                while (await timer.WaitForNextTickAsync(token))
                {
                    UpdateLastUpdateText();

                    if (Stop is null || IsBusy || DateTime.Now - _lastRefreshAttemptAt < AutoRefreshInterval)
                    {
                        continue;
                    }

                    await LoadAsync(Stop, _deviceId, silent: true);
                }
            }
            catch (OperationCanceledException)
            {
                // Popup-ul a fost închis.
            }
            catch (ObjectDisposedException)
            {
                // Sursa de anulare a fost eliberată între timp.
            }
        }

        private void UpdateLastUpdateText()
        {
            if (_lastUpdatedAt is not DateTime updatedAt)
            {
                LastUpdateText = null;
                return;
            }

            var elapsed = DateTime.Now - updatedAt;
            LastUpdateText = elapsed.TotalSeconds switch
            {
                < 1 => "Actualizat chiar acum",
                < 60 => $"Actualizat acum {(int)elapsed.TotalSeconds}s",
                _ => $"Actualizat acum {(int)elapsed.TotalMinutes} min"
            };
        }

        public async Task LoadAsync(Stop stop, string? deviceId = null, bool silent = false)
        {
            var version = ++_loadVersion;
            var isSameStop = Stop?.StopId == stop.StopId;
            _deviceId = deviceId;
            Stop = stop;
            _lastRefreshAttemptAt = DateTime.Now;

            // O reîmprospătare silențioasă are sens doar pentru aceeași stație.
            silent = silent && isSameStop;

            if (silent)
            {
                IsRefreshing = true;
            }
            else
            {
                IsLoading = true;
                ErrorMessage = null;
                Arrivals.Clear();
                HasArrivals = false;
                ArrivalsSummary = string.Empty;
            }

            List<StopArrival> arrivalsData;
            try
            {
                arrivalsData = await _apiService.GetArrivalsForStopAsync(stop.StopId, deviceId);
            }
            catch
            {
                arrivalsData = [];
            }

            // Între timp a pornit o altă încărcare (ex. schimbarea sensului): o ignorăm pe aceasta.
            if (version != _loadVersion)
            {
                return;
            }

            var now = DateTime.Now;
            var items = arrivalsData
                .Where(a => a.ArrivalMinutes <= 25)
                .OrderBy(a => a.ArrivalMinutes == -1)
                .ThenBy(a => a.ArrivalMinutes)
                .Select(a => new StopArrivalItem(a, now))
                .ToList();

            Arrivals.Clear();
            foreach (var item in items)
            {
                Arrivals.Add(item);
            }

            ErrorMessage = Arrivals.Count == 0 ? "Nu sunt sosiri în următoarea perioadă." : null;
            HasArrivals = Arrivals.Count > 0;
            ArrivalsSummary = Arrivals.Count switch
            {
                0 => string.Empty,
                1 => "1 sosire în următoarele 25 de minute",
                _ => $"{Arrivals.Count} sosiri în următoarele 25 de minute"
            };

            _lastUpdatedAt = now;
            UpdateLastUpdateText();

            IsLoading = false;
            IsRefreshing = false;
        }
    }

    /// <summary>
    /// Rândul afișat pentru o sosire: proprietățile modelului plus textele relative
    /// („în 5 min”, „acum”) și informația dacă vehiculul poate fi urmărit pe hartă.
    /// </summary>
    public sealed class StopArrivalItem
    {
        public StopArrivalItem(StopArrival arrival, DateTime loadedAt)
        {
            Arrival = arrival;

            if (!arrival.IsAtTerminus)
            {
                ClockText = $"la {loadedAt.AddMinutes(arrival.ArrivalMinutes):HH:mm}";
            }
        }

        public StopArrival Arrival { get; }

        public string VehicleLabel => Arrival.VehicleLabel ?? string.Empty;

        public string TypeName => Arrival.TypeName;

        public string VehicleGlyph => Arrival.VehicleGlyph;

        public Color AccentColor => Arrival.AccentColor;

        public bool IsAtTerminus => Arrival.IsAtTerminus;

        public bool IsImminent => Arrival.IsImminent;

        public string ArrivalHint => Arrival.ArrivalHint;

        /// <summary>Doar sosirile cu un vehicul identificat pot fi urmărite pe hartă.</summary>
        public bool CanTrack => Arrival.VehicleId is not null;

        /// <summary>Timpul relativ până la sosire: „în 5 min”, „acum” sau „pe capăt”.</summary>
        public string RelativeText => IsAtTerminus
            ? "pe capăt"
            : Arrival.ArrivalMinutes < 1
                ? "acum"
                : $"în {Math.Round(Arrival.ArrivalMinutes):0} min";

        /// <summary>Ora estimată a sosirii (ex. „la 20:45”); nulă pentru vehiculele pe capăt.</summary>
        public string? ClockText { get; }

        public bool HasClockText => !string.IsNullOrEmpty(ClockText);

        public string AccessibilityText
        {
            get
            {
                var text = $"{TypeName} {VehicleLabel}, {RelativeText}";
                if (HasClockText)
                {
                    text += $", {ClockText}";
                }

                return CanTrack ? $"{text}. Atinge pentru a urmări vehiculul pe hartă." : text;
            }
        }
    }
}
