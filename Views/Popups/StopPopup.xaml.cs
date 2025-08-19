using CommunityToolkit.Maui.Views;
using IvanConnections_Travel.ViewModels.Popups;

namespace IvanConnections_Travel.Views.Popups;

public partial class StopPopup : Popup
{
    public StopPopup(StopPopupViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    private void CloseButton_Clicked(object sender, EventArgs e)
    {
        Close();
    }
}