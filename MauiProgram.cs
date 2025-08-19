using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Core;
using IvanConnections_Travel.Controls;
using IvanConnections_Travel.Platforms.Handlers;
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
                    fonts.AddFont("MaterialIconsRound-Regular.otf", "MaterialSymbolsRounded");
                })
                .UseMauiMaps()
                .ConfigureMauiHandlers(handlers =>
                {
                    handlers.AddHandler<CustomMauiMap, CustomMapHandler>();
                })
                .UseMauiCommunityToolkit();

#if DEBUG
            builder.Logging.AddDebug();
#endif
            DependencyService.Register<IPopupService, PopupService>();
            builder.Services.AddTransientPopup<VehiclePopup, VehiclePopupViewModel>();
            builder.Services.AddTransientPopup<StopPopup, StopPopupViewModel>();


            Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping(nameof(BorderlessEntry), (handler, view) =>
            {
                if (view is BorderlessEntry)
                {
#if ANDROID
                    handler.PlatformView.Background = null;
#elif IOS || MACCATALYST
                handler.PlatformView.BorderStyle = UIKit.UITextBorderStyle.None;
#elif WINDOWS
                handler.PlatformView.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);
                handler.PlatformView.Background = null;
#endif
                }
            });

            return builder.Build();
        }
    }
}
