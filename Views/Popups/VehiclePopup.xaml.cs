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
        Closed += async (s, e) => await VehiclePopupViewModel.CleanupAsync();
    }
    private void OnMessagesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add && VehiclePopupViewModel.Messages.Count > 0)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Task.Delay(100); 
                var lastIndex = VehiclePopupViewModel.Messages.Count - 1;
                MessagesCollection.ScrollTo(
                    index: lastIndex, 
                    groupIndex: -1,
                    position: ScrollToPosition.End, 
                    animate: true);
            });
        }
    }
}
