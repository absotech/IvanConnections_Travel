using IvanConnections_Travel.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace IvanConnections_Travel.Views;

public partial class OnboardingPage : ContentPage
{
    private static bool _isShowing;
    private readonly OnboardingViewModel _viewModel;
    private bool _isClosing;

    public OnboardingPage(OnboardingViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
        _viewModel.Completed += OnCompleted;
    }

    /// <summary>Shows the onboarding tour modally (no-op if it is already visible).</summary>
    public static async Task ShowAsync()
    {
        if (_isShowing) return;

        var navigation = Shell.Current?.Navigation ?? Application.Current?.Windows.FirstOrDefault()?.Page?.Navigation;
        if (navigation is null) return;

        var page = IPlatformApplication.Current?.Services.GetService<OnboardingPage>()
                   ?? new OnboardingPage(new OnboardingViewModel());

        _isShowing = true;
        try
        {
            await navigation.PushModalAsync(page, true);
        }
        catch (Exception ex)
        {
            _isShowing = false;
            System.Diagnostics.Debug.WriteLine($"[OnboardingPage] Show error: {ex.Message}");
        }
    }

    /// <summary>Shows the tour once, on first launch (pref "OnboardingDone").</summary>
    public static Task ShowIfNeededAsync() =>
        OnboardingViewModel.IsOnboardingDone ? Task.CompletedTask : ShowAsync();

    private async void OnCompleted(object? sender, EventArgs e) => await CloseAsync();

    private async Task CloseAsync()
    {
        if (_isClosing) return;
        _isClosing = true;
        _viewModel.MarkDone();
        try
        {
            if (Navigation.ModalStack.Contains(this))
                await Navigation.PopModalAsync(true);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[OnboardingPage] Close error: {ex.Message}");
        }
    }

    protected override bool OnBackButtonPressed()
    {
        _ = CloseAsync();
        return true;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.MarkDone();
        _viewModel.Completed -= OnCompleted;
        _isShowing = false;
    }
}
