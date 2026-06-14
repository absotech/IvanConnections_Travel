using System;
using IvanConnections_Travel.ViewModels;

namespace IvanConnections_Travel.Views;

public partial class SchedulePage : ContentPage
{
    private readonly ScheduleViewModel _viewModel;

    public SchedulePage(ScheduleViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadRoutesCommand.ExecuteAsync(null);
    }

    private void OnMenuClicked(object? sender, EventArgs eventArgs)
    {
        Shell.Current.FlyoutIsPresented = true;
    }
}
