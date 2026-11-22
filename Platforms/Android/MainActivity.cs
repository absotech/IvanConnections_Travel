using Android.App;
using Android.Content.PM;
using Android.OS;
using Microsoft.Maui;

namespace IvanConnections_Travel
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, WindowSoftInputMode = global::Android.Views.SoftInput.AdjustResize, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
    }
}
