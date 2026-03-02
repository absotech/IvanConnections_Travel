using IvanConnections_Travel.ViewModels;

namespace IvanConnections_Travel.Views;

public partial class SettingsPage : ContentPage
{
    public SettingsPage(SettingsViewModel settingsViewModel)
    {
        InitializeComponent();
        BindingContext = settingsViewModel;
    }

    private void OnMenuClicked(object sender, EventArgs e)
    {
        Shell.Current.FlyoutIsPresented = true;
    }
}
