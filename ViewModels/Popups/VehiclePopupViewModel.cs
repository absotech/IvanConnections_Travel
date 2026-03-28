using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IvanConnections_Travel.Models;
using IvanConnections_Travel.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace IvanConnections_Travel.ViewModels.Popups
{
    public partial class VehiclePopupViewModel : ObservableObject
    {
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

        [ObservableProperty]
        private DateTime? timeOfArrival;

        [ObservableProperty]
        private bool loading = true;

        [ObservableProperty]
        private string followText = "Urmărește ";

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

        internal void Load(Vehicle vehicle, string userDisplayName, string userAvatarSeed)
        {
            Loading = true;

            ShouldFollow = vehicle.IsTracked;
            FollowText = ShouldFollow ? "Anulare urmărire " : "Urmărește ";
            Vehicle = vehicle;
            VehicleType = Utils.Translations.GetVehicleTypeNameInRomanian(vehicle.VehicleType);
            UserDisplayName = userDisplayName;
            UserAvatarSeed = userAvatarSeed;
            if (vehicle.IsElectricBus)
                VehicleInfo = "Autobuz electric";
            else if (vehicle.IsNewTram)
                VehicleInfo = "Tramvai nou";
            else
                VehicleInfo = null;

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
