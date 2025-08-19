using CommunityToolkit.Mvvm.Messaging;
using IvanConnections_Travel.Messages;
using IvanConnections_Travel.Utils;
using IvanConnections_Travel.ViewModels;
using System.Diagnostics;
using Microsoft.Maui.Maps;

#if ANDROID
using Android.Widget;
#endif

namespace IvanConnections_Travel
{
    public partial class MainPage : ContentPage
    {
        private readonly MainPageViewModel _vm;

        public MainPage()
        {
            InitializeComponent();
            _vm = new MainPageViewModel();
            BindingContext = _vm;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _vm.InitializeAsync();
            _vm.StartPeriodicRefresh();
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

            var location = await LocationManagement.GetLocationAsync();
            if (location != null)
            {
                MyMap.MoveToRegion(MapSpan.FromCenterAndRadius(
                    location,
                    Distance.FromKilometers(1)));
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _vm.StopPeriodicRefresh();
            WeakReferenceMessenger.Default.Unregister<ShowToastMessage>(this);
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

            status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            return status;
        }
    }
}