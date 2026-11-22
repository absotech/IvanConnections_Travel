using IvanConnections_Travel.ViewModels;

namespace IvanConnections_Travel
{
    public partial class App : Application
    {
        private readonly IServiceProvider _services;

        // AppShell must be resolved lazily: its XAML uses StaticResources (design tokens) that only
        // exist after this App's InitializeComponent() has merged Colors.xaml / Styles.xaml.
        public App(IServiceProvider services)
        {
            InitializeComponent();
            SettingsViewModel.ApplySavedTheme(this);
            _services = services;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(_services.GetRequiredService<AppShell>());
        }
    }
}
