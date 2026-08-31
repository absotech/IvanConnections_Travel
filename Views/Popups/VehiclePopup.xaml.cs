using CommunityToolkit.Maui.Views;
using IvanConnections_Travel.ViewModels.Popups;
using System.Collections.Specialized;
namespace IvanConnections_Travel.Views.Popups;
public partial class VehiclePopup : Popup<bool>
{
    private readonly VehiclePopupViewModel VehiclePopupViewModel;
    public VehiclePopup(VehiclePopupViewModel vehiclePopupViewModel)
    {
        InitializeComponent();
        BindingContext = VehiclePopupViewModel = vehiclePopupViewModel;
        VehiclePopupViewModel.FollowVehicle += async (s, e) =>
        {
            await CloseAsync(e ?? false);
        };
        VehiclePopupViewModel.Messages.CollectionChanged += OnMessagesChanged;
        Closed += async (s, e) => await VehiclePopupViewModel.CleanupAsync();
    }
    private async void CloseButton_Clicked(object sender, EventArgs e)
    {
        await CloseAsync(false);
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
