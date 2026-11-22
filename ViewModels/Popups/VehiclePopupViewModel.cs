using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IvanConnections_Travel.Models;
using IvanConnections_Travel.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace IvanConnections_Travel.ViewModels.Popups
{
    public partial class VehiclePopupViewModel : ObservableObject, IQueryAttributable
    {
        private const string DefaultRouteColor = "#5763FF";
        private const string GenericVehicleGlyph = Utils.VehicleVisuals.GenericGlyph;

        private readonly ApiService _apiService;
        private readonly ChatHubService _chatHubService;
        private TimeZoneInfo _timeZoneInfo;
        readonly WeakEventManager followVehicleManager = new();
        public bool ShouldFollow = false;
        private string? _deviceId;

        public event EventHandler<bool?> FollowVehicle
        {
            add => followVehicleManager.AddEventHandler(value);
            remove => followVehicleManager.RemoveEventHandler(value);
        }

        [ObservableProperty]
        private Vehicle? vehicle;

        [ObservableProperty]
        private string vehicleType;

        [ObservableProperty]
        private string? vehicleInfo;

        // Header (identitate traseu)
        [ObservableProperty]
        private string routeLabel = "—";

        [ObservableProperty]
        private string vehicleGlyph = GenericVehicleGlyph;

        [ObservableProperty]
        private string vehicleSubtitle = string.Empty;

        [ObservableProperty]
        private Color headerColor = Color.FromArgb(DefaultRouteColor);

        [ObservableProperty]
        private Color headerTextColor = Colors.White;

        [ObservableProperty]
        private Color headerMutedTextColor = Color.FromRgba(255, 255, 255, 200);

        [ObservableProperty]
        private Color headerOverlayColor = Color.FromRgba(255, 255, 255, 56);

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ArrivalRelativeText))]
        private DateTime? timeOfArrival;

        /// <summary>"în 4 min" / "acum" – easier to read at a glance than a clock time.</summary>
        public string ArrivalRelativeText
        {
            get
            {
                if (TimeOfArrival is not { } eta) return string.Empty;
                var minutes = (int)Math.Round((eta - DateTime.Now).TotalMinutes);
                return minutes <= 0 ? "acum" : $"în {minutes} min";
            }
        }

        /// <summary>True when the user has an account (needed to send chat messages).</summary>
        [ObservableProperty]
        private bool isLoggedIn;

        [ObservableProperty]
        private bool isLoggingIn;

        [ObservableProperty]
        private bool loading = true;

        [ObservableProperty]
        private string followText = "Urmărește";

        [ObservableProperty]
        private bool isFollowing;

        [ObservableProperty]
        private bool isDataStale;

        [ObservableProperty]
        private string? staleMessage;

        // Extra info (setare "Arată mai multe informații")
        [ObservableProperty]
        private bool showMoreInfo;

        [ObservableProperty]
        private string vehicleCode = "-";

        [ObservableProperty]
        private string lastUpdateText = "-";

        [ObservableProperty]
        private string? speedText;

        [ObservableProperty]
        private string bikeAccessibleText = "Nu";

        [ObservableProperty]
        private string wheelchairAccessibleText = "Nu";

        [ObservableProperty]
        private bool isBikeAccessible;

        [ObservableProperty]
        private bool isWheelchairAccessible;

        // Chat
        [ObservableProperty]
        private bool isChatExpanded = false;

        [ObservableProperty]
        private bool isChatLoading = false;

        [ObservableProperty]
        private bool isSending = false;

        [ObservableProperty]
        private string messageText = string.Empty;

        [ObservableProperty]
        private bool hasChatError = false;
        
        [ObservableProperty]
        private string userDisplayName = string.Empty;

        [ObservableProperty]
        private string userAvatarSeed = string.Empty;

        public ObservableCollection<ChatMessage> Messages { get; } = new();

        public VehiclePopupViewModel(ApiService apiService, ChatHubService chatHubService)
        {
            _apiService = apiService;
            _chatHubService = chatHubService;
            _timeZoneInfo = TimeZoneInfo.Local;

            _chatHubService.MessageReceived += OnMessageReceived;
        }

        private void OnMessageReceived(ChatMessage msg)
        {
            if (Vehicle is null || msg.VehicleId != Vehicle.Id.ToString()) return;
            if (Messages.Any(m => m.Id == msg.Id)) return;
            msg.IsSentByMe = !string.IsNullOrEmpty(UserAvatarSeed) && msg.AvatarSeed == UserAvatarSeed;
            Messages.Add(msg);
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            var vehicle = (Vehicle)query["vehicle"];
            var displayName = query.TryGetValue("userDisplayName", out var n) ? n as string ?? string.Empty : string.Empty;
            var avatarSeed = query.TryGetValue("userAvatarSeed", out var s) ? s as string ?? string.Empty : string.Empty;
            IsLoggedIn = query.TryGetValue("isLoggedIn", out var l) && l is true
                         || Shell.Current?.BindingContext is AppShellViewModel { IsLoggedIn: true };
            Load(vehicle, displayName, avatarSeed);
        }

        internal void Load(Vehicle vehicle, string userDisplayName, string userAvatarSeed)
        {
            Loading = true;

            ShouldFollow = vehicle.IsTracked;
            IsFollowing = ShouldFollow;
            FollowText = ShouldFollow ? "Nu mai urmări" : "Urmărește";
            Vehicle = vehicle;
            VehicleType = Utils.Translations.GetVehicleTypeNameInRomanian(vehicle.VehicleType);
            UpdateHeaderInfo(vehicle);
            UserDisplayName = userDisplayName;
            UserAvatarSeed = userAvatarSeed;
            if (vehicle.IsElectricBus)
                VehicleInfo = "Autobuz electric";
            else if (vehicle.IsNewTram)
                VehicleInfo = "Tramvai nou";
            else
                VehicleInfo = null;

            UpdateStaleInfo(vehicle);
            UpdateExtraInfo(vehicle);

            if (vehicle.NextStop != null)
            {
                _ = Task.Run(async () =>
                {
                    var durationInSeconds = await _apiService.GetTravelDurationAsync(
                        vehicle.Latitude.Value,
                        vehicle.Longitude.Value,
                        vehicle.NextStop.StopLat,
                        vehicle.NextStop.StopLon);

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        Loading = false;
                        if (durationInSeconds.HasValue)
                        {
                            TimeOfArrival = TimeZoneInfo.ConvertTimeFromUtc(vehicle.Timestamp.Value + TimeSpan.FromSeconds(durationInSeconds.Value), _timeZoneInfo);
                        }
                        else
                        {
                            TimeOfArrival = null;
                        }
                    });
                });
            }
            else
            {
                Loading = false;
            }

            _ = LoadDeviceIdAsync();
        }

        private void UpdateHeaderInfo(Vehicle vehicle)
        {
            RouteLabel = string.IsNullOrWhiteSpace(vehicle.RouteShortName) ? "—" : vehicle.RouteShortName!.Trim();
            VehicleGlyph = GetVehicleGlyph(vehicle.VehicleType);

            var typeTitle = string.IsNullOrEmpty(VehicleType)
                ? "Vehicul"
                : char.ToUpperInvariant(VehicleType[0]) + VehicleType[1..];
            VehicleSubtitle = string.IsNullOrWhiteSpace(vehicle.TripHeadsign)
                ? typeTitle
                : $"{typeTitle} · către {vehicle.TripHeadsign!.Trim()}";

            ApplyRouteColors(vehicle.RouteColor);
        }

        private void ApplyRouteColors(string? routeColor)
        {
            var color = ParseHexColor(routeColor) ?? Color.FromArgb(DefaultRouteColor);
            HeaderColor = color;

            if (IsDark(color))
            {
                HeaderTextColor = Colors.White;
                HeaderMutedTextColor = Color.FromRgba(255, 255, 255, 200);
                HeaderOverlayColor = Color.FromRgba(255, 255, 255, 56);
            }
            else
            {
                HeaderTextColor = Color.FromArgb("#111827");
                HeaderMutedTextColor = Color.FromRgba(17, 24, 39, 170);
                HeaderOverlayColor = Color.FromRgba(17, 24, 39, 36);
            }
        }

        private static Color? ParseHexColor(string? hex)
        {
            if (string.IsNullOrWhiteSpace(hex)) return null;

            var value = hex.Trim();
            if (!value.StartsWith('#')) value = '#' + value;
            if (value.Length is not (4 or 7 or 9)) return null;

            try
            {
                return Color.FromArgb(value);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[VehiclePopupViewModel] Invalid route color '{hex}': {ex.Message}");
                return null;
            }
        }

        private static bool IsDark(Color color)
            => (0.299 * color.Red + 0.587 * color.Green + 0.114 * color.Blue) < 0.6;

        private static string GetVehicleGlyph(Models.Enums.VehicleType? vehicleType)
            => Utils.VehicleVisuals.GetGlyph(vehicleType);

        private void UpdateStaleInfo(Vehicle vehicle)
        {
            IsDataStale = vehicle.IsDataStale;
            if (!IsDataStale || !vehicle.LastUpdateUtc.HasValue)
            {
                StaleMessage = null;
                return;
            }

            var localTimestamp = TimeZoneInfo.ConvertTimeFromUtc(vehicle.LastUpdateUtc.Value, _timeZoneInfo);
            var timeText = Utils.TimeFormatUtils.FormatTimeDifferenceInRomanian(localTimestamp);
            StaleMessage = $"Poziția nu a mai fost actualizată de {timeText}. Este posibil ca acest {VehicleType} să fie pe capăt sau să nu se mai afle în acest loc.";
        }

        private void UpdateExtraInfo(Vehicle vehicle)
        {
            ShowMoreInfo = Utils.Preferences.ShowMoreVehicleInfo;

            VehicleCode = string.IsNullOrWhiteSpace(vehicle.Label) ? "-" : vehicle.Label!;

            LastUpdateText = vehicle.LastUpdateUtc.HasValue
                ? $"acum {Utils.TimeFormatUtils.FormatTimeDifferenceInRomanian(TimeZoneInfo.ConvertTimeFromUtc(vehicle.LastUpdateUtc.Value, _timeZoneInfo))}"
                : "-";

            SpeedText = vehicle.Speed.HasValue ? $"{vehicle.Speed.Value} km/h" : null;

            IsBikeAccessible = Utils.Translations.IsAccessible(vehicle.BikeAccessible);
            IsWheelchairAccessible = Utils.Translations.IsAccessible(vehicle.WheelchairAccessible);
            BikeAccessibleText = Utils.Translations.GetAccessibilityInRomanian(vehicle.BikeAccessible);
            WheelchairAccessibleText = Utils.Translations.GetAccessibilityInRomanian(vehicle.WheelchairAccessible);
        }

        private async Task LoadDeviceIdAsync()
        {
            try
            {
                _deviceId = await SecureStorage.Default.GetAsync("device_id");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[VehiclePopupViewModel] Error loading device id: {ex.Message}");
            }
        }

        [RelayCommand]
        private void Follow()
        {
            ShouldFollow = !ShouldFollow;
            IsFollowing = ShouldFollow;
            Utils.Haptics.Click();
            FollowText = ShouldFollow ? "Nu mai urmări" : "Urmărește";
            followVehicleManager.HandleEvent(this, ShouldFollow, nameof(FollowVehicle));
        }

        [RelayCommand]
        private async Task ToggleChatAsync()
        {
            IsChatExpanded = !IsChatExpanded;
            if (IsChatExpanded)
            {
                await LoadMessagesAsync();
                await ConnectToChatHubAsync();
            }
            else
            {
                await _chatHubService.DisconnectAsync();
            }
        }

        /// <summary>Lets the user enable chat right from the vehicle popup instead of hunting in the menu.</summary>
        [RelayCommand]
        private async Task LoginForChatAsync()
        {
            if (IsLoggingIn) return;
            IsLoggingIn = true;
            try
            {
                if (Shell.Current?.BindingContext is AppShellViewModel shell)
                {
                    await shell.LoginCommand.ExecuteAsync(null);
                    IsLoggedIn = shell.IsLoggedIn;
                }

                if (IsLoggedIn)
                {
                    await LoadDeviceIdAsync();
                    await ConnectToChatHubAsync();
                    Utils.Haptics.Confirm();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[VehiclePopupViewModel] Login from chat failed: {ex.Message}");
            }
            finally
            {
                IsLoggingIn = false;
            }
        }

        [RelayCommand]
        private async Task RefreshMessagesAsync()
        {
            await LoadMessagesAsync();
        }

        private async Task ConnectToChatHubAsync()
        {
            if (Vehicle is null || string.IsNullOrEmpty(_deviceId)) return;
            try
            {
                await _chatHubService.ConnectAsync(_deviceId, Vehicle.Id.ToString());
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[VehiclePopupViewModel] Error connecting to chat hub: {ex.Message}");
            }
        }

        private async Task LoadMessagesAsync()
        {
            if (Vehicle is null) return;
            IsChatLoading = true;
            HasChatError = false;
            try
            {
                var history = await _apiService.GetMessageHistoryAsync(Vehicle.Id.ToString());
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Messages.Clear();
                    foreach (var msg in history.AsEnumerable().Reverse())
                    {
                        msg.IsSentByMe = !string.IsNullOrEmpty(UserAvatarSeed) && msg.AvatarSeed == UserAvatarSeed;
                        Messages.Add(msg);
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[VehiclePopupViewModel] Error loading messages: {ex.Message}");
                MainThread.BeginInvokeOnMainThread(() => HasChatError = true);
            }
            finally
            {
                MainThread.BeginInvokeOnMainThread(() => IsChatLoading = false);
            }
        }

        [RelayCommand]
        private async Task SendMessageAsync()
        {
            if (string.IsNullOrWhiteSpace(MessageText) || Vehicle is null || IsSending) return;
            if (string.IsNullOrEmpty(_deviceId)) return;

            var text = MessageText.Trim();
            IsSending = true;
            MessageText = string.Empty;
            try
            {
                await _chatHubService.SendMessageAsync(Vehicle.Id, text);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[VehiclePopupViewModel] Error sending message: {ex.Message}");
                MessageText = text;
            }
            finally
            {
                IsSending = false;
            }
        }

        public async Task CleanupAsync()
        {
            _chatHubService.MessageReceived -= OnMessageReceived;
            await _chatHubService.DisconnectAsync();
        }
    }
}
