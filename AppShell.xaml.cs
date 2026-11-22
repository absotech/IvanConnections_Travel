using System.Diagnostics;
using IvanConnections_Travel.ViewModels;
using IvanConnections_Travel.Views;

namespace IvanConnections_Travel
{
    public partial class AppShell : Shell
    {
        private readonly AppShellViewModel _viewModel;
        private bool _startupHandled;

        public AppShell(AppShellViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            var loginTask = _viewModel.TryAutoLoginAsync();

            if (!_startupHandled)
            {
                _startupHandled = true;
                await HandleStartupAsync();
            }

            await loginTask;
        }

        private async Task HandleStartupAsync()
        {
            try
            {
                var startPage = Preferences.Default.Get(SettingsViewModel.PrefStartPageKey, SettingsViewModel.StartPageMap);
                if (startPage == SettingsViewModel.StartPageSchedule)
                    await GoToAsync($"//{AppShellViewModel.RouteSchedule}", false);

                // Let the first page render before presenting the first-run tour.
                await Task.Delay(400);
                await OnboardingPage.ShowIfNeededAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AppShell] Startup handling failed: {ex.Message}");
            }
        }
    }
}
