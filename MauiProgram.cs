using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Core;
using IvanConnections_Travel.ViewModels.Popups;
using IvanConnections_Travel.Views.Popups;
using Microsoft.Extensions.Logging;

namespace IvanConnections_Travel
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                })
                .UseMauiMaps()
                .UseMauiCommunityToolkit();

#if DEBUG
    		builder.Logging.AddDebug();
#endif
            DependencyService.Register<IPopupService, PopupService>();
            builder.Services.AddTransientPopup<VehiclePopup, VehiclePopupViewModel>();
            return builder.Build();
        }
    }
}
