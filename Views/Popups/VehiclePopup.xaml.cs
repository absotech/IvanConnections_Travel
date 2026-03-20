using CommunityToolkit.Maui.Views;
using IvanConnections_Travel.ViewModels.Popups;
using System.Collections.Specialized;

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

        VehiclePopupViewModel.Messages.CollectionChanged += OnMessagesChanged;
    }

    private void OnMessagesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (VehiclePopupViewModel.Messages.Count > 0)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                MessagesCollection.ScrollTo(
                    VehiclePopupViewModel.Messages[^1],
                    animate: false);
            });
        }
    }
}
