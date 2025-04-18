using Android.Gms.Maps;
using Android.Widget;
using CommunityToolkit.Mvvm.Messaging;
using IvanConnections_Travel.Messages;
using IvanConnections_Travel.Platforms.Handlers;
using IvanConnections_Travel.Shared.Cache;
using IvanConnections_Travel.Utils;
using IvanConnections_Travel.ViewModels;
using Microsoft.Maui.Maps;
using System.Diagnostics;

namespace IvanConnections_Travel
{
    public partial class MainPage : ContentPage
    {

        private readonly MainPageViewModel _vm;
        private CustomMapCallback? _customMapCallback;

        public MainPage()
        {
            InitializeComponent();
            _vm = new MainPageViewModel();
            BindingContext = _vm;
            _customMapCallback = new CustomMapCallback(MyMap);
            if (MyMap?.Handler?.PlatformView is MapView nativeMap)
            {
                MainThread.InvokeOnMainThreadAsync(() =>
                {
                    nativeMap.GetMapAsync(_customMapCallback);
                });
            }
            WeakReferenceMessenger.Default.Register<PinsUpdatedMessage>(this, (r, m) =>
            {
                (MapPinCache.Pins, MapPinCache.Stops) = m.Value;

                if (MyMap?.Handler?.PlatformView is MapView nativeMap)
                {
                    MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        nativeMap.GetMapAsync(_customMapCallback);
                    });
                }
            });

            _ = _vm.LoadPinsFromBackendAsync();
        }
        protected override async void OnAppearing()
        {
            base.OnAppearing();

            WeakReferenceMessenger.Default.Register<ShowToastMessage>(this, (r, m) =>
            {
#if ANDROID
                Toast.MakeText(Platform.CurrentActivity, m.Value, ToastLength.Long)?.Show();
#endif
            });

            var permissionStatus = await CheckAndRequestLocationPermission();
            if (permissionStatus != PermissionStatus.Granted)
            {
                Debug.WriteLine("Location permission not granted.");
                return;
            }

            Debug.WriteLine("Location permission granted.");

            var location = await LocationManagement.GetLocationAsync(
                GeolocationAccuracy.Lowest,
                3,
                updatedLocation =>
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        MyMap.MoveToRegion(MapSpan.FromCenterAndRadius(
                            updatedLocation,
                            Distance.FromKilometers(1)));
                    });
                });

            MyMap.MoveToRegion(MapSpan.FromCenterAndRadius(
                location,
                Distance.FromKilometers(1)));
        }

        public static async Task<PermissionStatus> CheckAndRequestLocationPermission()
        {
            PermissionStatus status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();

            if (status == PermissionStatus.Granted)
                return status;

            if (status == PermissionStatus.Denied && DeviceInfo.Platform == DevicePlatform.iOS)
            {
                return status;
            }

            if (Permissions.ShouldShowRationale<Permissions.LocationWhenInUse>())
            {
                // Prompt the user with additional information as to why the permission is needed
            }

            status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

            return status;
        }

    }

}
