using IvanConnections_Travel.ViewModels;

namespace IvanConnections_Travel.Views;

public partial class BadgesPage : ContentPage
{
    private readonly BadgesViewModel _viewModel;

    public BadgesPage(BadgesViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadBadgesCommand.ExecuteAsync(null);
    }
    private void OnMenuClicked(object sender, EventArgs e)
    {
        Shell.Current.FlyoutIsPresented = true;
    }
}
