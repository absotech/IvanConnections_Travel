using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Core;
using IvanConnections_Travel.Controls;
using IvanConnections_Travel.Platforms.Handlers;
using IvanConnections_Travel.Services;
using IvanConnections_Travel.ViewModels;
using IvanConnections_Travel.ViewModels.Popups;
using IvanConnections_Travel.Views.Popups;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;

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
                .UseMauiCommunityToolkit()
                .RegisterViews()
                .RegisterViewModels()

                // 🔥 Add lifecycle events here
                .ConfigureLifecycleEvents(events =>
                {
#if ANDROID
                    events.AddAndroid(android =>
                    {
                        android.OnResume(activity =>
                        {
                            var page = (Application.Current?.MainPage as Shell)?.CurrentPage as MainPage;

                            if (page?.BindingContext is MainPageViewModel vm && vm.CenterOnUserLocationCommand.CanExecute(null))
                            {
                                MainThread.BeginInvokeOnMainThread(async () =>
                                {
                                    await vm.CenterOnUserLocationCommand.ExecuteAsync(null);
                                });
                            }
                        });
                    });
#endif
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif
            builder.Services.AddSingleton<ApiService>();
            builder.Services.AddSingleton<IVehicleService, VehicleService>();
            builder.Services.AddTransientPopup<VehiclePopup, VehiclePopupViewModel>();
            builder.Services.AddTransientPopup<StopPopup, StopPopupViewModel>();

            DependencyService.Register<IPopupService, PopupService>();

            Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping(nameof(BorderlessEntry), (handler, view) =>
            {
                if (view is BorderlessEntry)
                {
#if ANDROID
                    handler.PlatformView.Background =
                        new Android.Graphics.Drawables.ColorDrawable(Android.Graphics.Color.Transparent);
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

        public static MauiAppBuilder RegisterViewModels(this MauiAppBuilder mauiAppBuilder)
        {
            mauiAppBuilder.Services.AddTransient<MainPageViewModel>();
            return mauiAppBuilder;
        }

        public static MauiAppBuilder RegisterViews(this MauiAppBuilder mauiAppBuilder)
        {
            mauiAppBuilder.Services.AddSingleton<AppShell>();
            mauiAppBuilder.Services.AddTransient<MainPage>();
            return mauiAppBuilder;
        }
    }
}
