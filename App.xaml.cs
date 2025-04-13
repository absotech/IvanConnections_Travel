using IvanConnections_Travel.Services;
using IvanConnections_Travel.Services.Interfaces;

namespace IvanConnections_Travel
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

        }
        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
        public static ILogService LogService { get; set; } = new LogService();
    }
}
