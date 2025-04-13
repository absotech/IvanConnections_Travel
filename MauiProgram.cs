using CommunityToolkit.Maui;
using IvanConnections_Travel.Platforms.Android.Handlers;
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
                .UseMauiCommunityToolkit()
#if ANDROID || IOS
            .ConfigureMauiHandlers(handlers => handlers.AddHandler<Microsoft.Maui.Controls.Maps.Map, CustomMapHandler>());
#endif

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
