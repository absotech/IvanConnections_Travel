using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using IvanConnections_Travel.Messages;
using Microsoft.Maui.Storage;

namespace IvanConnections_Travel.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private const string PrefTrafficKey = "IsTrafficEnabled";
    private const string PrefStopsKey = "ShowStopsOnMap";

    [ObservableProperty]
    private bool _isTrafficEnabled;

    [ObservableProperty]
    private bool _showStopsOnMap;
    
    [ObservableProperty]
    private string _appVersion = AppInfo.VersionString;
    
    public SettingsViewModel()
    {
        IsTrafficEnabled = Preferences.Default.Get(PrefTrafficKey, true);
        ShowStopsOnMap = Preferences.Default.Get(PrefStopsKey, true);
    }

    partial void OnIsTrafficEnabledChanged(bool value)
    {
        Preferences.Default.Set(PrefTrafficKey, value);
        WeakReferenceMessenger.Default.Send(new TrafficPreferenceChangedMessage(value));
    }

    partial void OnShowStopsOnMapChanged(bool value)
    {
        Preferences.Default.Set(PrefStopsKey, value);
        WeakReferenceMessenger.Default.Send(new StopsPreferenceChangedMessage(value));
    }
}
