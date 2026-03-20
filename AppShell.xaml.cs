using IvanConnections_Travel.ViewModels;

namespace IvanConnections_Travel
{
    public partial class AppShell : Shell
    {
        private readonly AppShellViewModel _viewModel;

        public AppShell(AppShellViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.TryAutoLoginAsync();
        }
    }
}
