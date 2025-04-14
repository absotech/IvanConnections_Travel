using CommunityToolkit.Maui.Views;
using IvanConnections_Travel.ViewModels.Popups;

namespace IvanConnections_Travel.Views.Popups;

public partial class VehiclePopup : Popup
{
    private readonly VehiclePopupViewModel VehiclePopupViewModel;
    public VehiclePopup(VehiclePopupViewModel vehiclePopupViewModel)
	{
		InitializeComponent();
        BindingContext = VehiclePopupViewModel = vehiclePopupViewModel;

        VehiclePopupViewModel.FollowVehicle += (s, e) =>
        {
            Close(e);
        };
    }
}