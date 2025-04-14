using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IvanConnections_Travel.Models;

namespace IvanConnections_Travel.ViewModels.Popups
{
    public partial class VehiclePopupViewModel : ObservableObject
    {
        readonly WeakEventManager followVehicleManager = new();
        public bool? ShouldFollow = null;
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


        internal void Load(Vehicle vehicle)
        {
            if (vehicle == null)
                return;
            Vehicle = vehicle;
            VehicleType = Utils.Translations.GetVehicleTypeNameInRomanian(vehicle.VehicleType.Value);
            if(vehicle.IsElectricBus)
                VehicleInfo = "Autobuz electric";
            else if (vehicle.IsNewTram)
                VehicleInfo = "Tramvai nou";
            else
                VehicleInfo = null;
        }

        [RelayCommand]
        public async Task Follow()
        {
            ShouldFollow = true;
            followVehicleManager.HandleEvent(this, ShouldFollow, nameof(FollowVehicle));
        }
    }
}
