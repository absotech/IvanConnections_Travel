namespace IvanConnections_Travel
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            //PerformanceOverlayManager.Instance.Enable();
        }
        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}
