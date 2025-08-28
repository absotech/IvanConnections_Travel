using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IvanConnections_Travel.Models;
using IvanConnections_Travel.Services;

namespace IvanConnections_Travel.ViewModels.Popups
{
    public partial class VehiclePopupViewModel : ObservableObject
    {
        private readonly ApiService _apiService;
        readonly WeakEventManager followVehicleManager = new();
        public bool ShouldFollow = false;

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
        public VehiclePopupViewModel(ApiService apiService)
        {
            _apiService = apiService;
        }

        internal void Load(Vehicle vehicle)
        {
            Loading = true;
            if (vehicle == null)
            {
                Loading = false;
                return;
            }

            ShouldFollow = vehicle.IsTracked;
            FollowText = ShouldFollow == true ? "Anulare urmărire " : "Urmărește ";
            Vehicle = vehicle;
            VehicleType = Utils.Translations.GetVehicleTypeNameInRomanian(vehicle.VehicleType.Value);

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
                            TimeOfArrival = vehicle.LocalTimestamp + TimeSpan.FromSeconds(durationInSeconds.Value);
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
        }

        [RelayCommand]
        public void Follow()
        {
            ShouldFollow = !ShouldFollow;
            followVehicleManager.HandleEvent(this, ShouldFollow, nameof(FollowVehicle));
        }
    }
}