using Android.Graphics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using IvanConnections_Travel.Messages;
using IvanConnections_Travel.Models.Enums;
using IvanConnections_Travel.Utils;
using Microsoft.Maui.Storage;
using Preferences = Microsoft.Maui.Storage.Preferences;

namespace IvanConnections_Travel.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly Debouncer _vehicleDebouncer;
    private readonly Debouncer _stopDebouncer;
    
    private const string PrefTrafficKey = "IsTrafficEnabled";
    private const string PrefStopsKey = "ShowStopsOnMap";
    private const string PrefVehicleSize = "VehicleSize";
    private const string PrefStopSize = "StopSize";
    
    [ObservableProperty]
    private bool _isTrafficEnabled;

    [ObservableProperty]
    private bool _showStopsOnMap;


    private int _vehicleSize;
    public int VehicleSize
    {
        get => _vehicleSize;
        set
        {
            if (_vehicleSize == value) return;
            _vehicleSize = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(VehicleDisplaySize));
            _ = UpdateVehiclePreview();
        }
    }
    
    private int _stopSize ;
    public int StopSize
    {
        get => _stopSize;
        set
        {
            if (_stopSize == value) return;
            _stopSize = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StopDisplaySize));
            _ = UpdateStopPreview();
        }
    }

    [ObservableProperty]
    private ImageSource _vehiclePreview;
    
    [ObservableProperty]
    private ImageSource _stopPreview;
    
    [ObservableProperty]
    private string _appVersion = AppInfo.VersionString;
    
    private static float VehiclePinOverlayScale = 0.48f;
    private static float VehiclePinPaddingScale = 0.20f;

    public double VehicleDisplaySize 
    {
        get 
        {
            double overlay = VehicleSize * VehiclePinOverlayScale;
            double padding = VehicleSize * VehiclePinPaddingScale;
            return VehicleSize + overlay + padding;
        }
    }
    public double StopDisplaySize 
    {
        get 
        {
            double overlay = StopSize * VehiclePinOverlayScale;
            double padding = StopSize * VehiclePinPaddingScale;
            return StopSize + overlay + padding;
        }
    }
    public SettingsViewModel()
    {
        _vehicleDebouncer = new Debouncer();
        _stopDebouncer = new Debouncer();
        IsTrafficEnabled = Preferences.Default.Get(PrefTrafficKey, true);
        ShowStopsOnMap = Preferences.Default.Get(PrefStopsKey, true);
        StopSize = Preferences.Default.Get(PrefStopSize, 15);
        VehicleSize = Preferences.Default.Get(PrefVehicleSize, 25);
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
    private async Task UpdateVehiclePreview()
    {
        await _vehicleDebouncer.RunAsync(async () =>
        {
            var bitmap = MapBitmapFactory.CreateCustomPinBitmap(Platform.CurrentActivity, VehicleType.Bus, "12", "#FF0000", 45, VehicleSize);

            using var ms = new MemoryStream();
            bitmap.Compress(Bitmap.CompressFormat.Png, 100, ms);
            ms.Seek(0, SeekOrigin.Begin);

            VehiclePreview = ImageSource.FromStream(() => new MemoryStream(ms.ToArray()));
            Preferences.Default.Set(PrefVehicleSize, VehicleSize);
        }, 20);
    }

    private async Task UpdateStopPreview()
    {
        await _stopDebouncer.RunAsync(async () =>
        {
            var bitmap = MapBitmapFactory.CreateStopPinBitmap(Platform.CurrentActivity, StopSize);

            using var ms = new MemoryStream();
            bitmap.Compress(Bitmap.CompressFormat.Png, 100, ms);
            ms.Seek(0, SeekOrigin.Begin);

            StopPreview = ImageSource.FromStream(() => new MemoryStream(ms.ToArray()));
            Preferences.Default.Set(PrefStopSize, StopSize);
        }, 20);
    }

    [RelayCommand]
    private async Task Appearing()
    {
        await UpdateVehiclePreview();
        await UpdateStopPreview();
    }
}
