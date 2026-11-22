using Android.Graphics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using IvanConnections_Travel.Messages;
using IvanConnections_Travel.Models.Enums;
using IvanConnections_Travel.Utils;
using IvanConnections_Travel.Views;
using Microsoft.Maui.ApplicationModel.Communication;
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
    private const string PrefNumberOnlyKey = "ShowNumberOnly";
    public const string PrefAppThemeKey = "AppTheme";
    public const string PrefRefreshIntervalKey = "RefreshIntervalSeconds";
    public const string PrefStartPageKey = "StartPage";
    public const string StartPageMap = "map";
    public const string StartPageSchedule = "schedule";
    public const int DefaultRefreshIntervalSeconds = 5;
    private const string FeedbackEmail = "feedback@ivanconnections.ro";

    private static readonly int[] RefreshIntervalValues = [3, 5, 10, 15];

    /// <summary>True while values are being (re)loaded from Preferences — suppresses side effects.</summary>
    private bool _isLoading;

    public IReadOnlyList<string> ThemeOptions { get; } = ["Sistem", "Luminos", "Întunecat"];
    public IReadOnlyList<string> RefreshIntervalOptions { get; } = ["3 secunde", "5 secunde", "10 secunde", "15 secunde"];
    public IReadOnlyList<string> StartPageOptions { get; } = ["Hartă", "Orar"];

    [ObservableProperty]
    private bool _isTrafficEnabled;

    [ObservableProperty]
    private bool _showStopsOnMap;

    [ObservableProperty]
    private bool _showNumberOnly;

    [ObservableProperty]
    private bool _showMoreVehicleInfo;

    [ObservableProperty]
    private int _themeIndex;

    [ObservableProperty]
    private int _refreshIntervalIndex = 1;

    [ObservableProperty]
    private int _startPageIndex;

    private int _vehicleSize;
    public int VehicleSize
    {
        get => _vehicleSize;
        set
        {
            if (_vehicleSize == value) return;
            _vehicleSize = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(VehicleSizeSlider));
            OnPropertyChanged(nameof(VehicleDisplaySize));
            _ = UpdateVehiclePreview();
        }
    }

    /// <summary>Slider-facing value; snaps to whole numbers.</summary>
    public double VehicleSizeSlider
    {
        get => VehicleSize;
        set
        {
            var rounded = (int)Math.Round(value, MidpointRounding.AwayFromZero);
            VehicleSize = rounded;
            if (Math.Abs(value - rounded) > double.Epsilon)
                OnPropertyChanged();
        }
    }

    private int _stopSize;
    public int StopSize
    {
        get => _stopSize;
        set
        {
            if (_stopSize == value) return;
            _stopSize = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StopSizeSlider));
            OnPropertyChanged(nameof(StopDisplaySize));
            _ = UpdateStopPreview();
        }
    }

    /// <summary>Slider-facing value; snaps to whole numbers.</summary>
    public double StopSizeSlider
    {
        get => StopSize;
        set
        {
            var rounded = (int)Math.Round(value, MidpointRounding.AwayFromZero);
            StopSize = rounded;
            if (Math.Abs(value - rounded) > double.Epsilon)
                OnPropertyChanged();
        }
    }

    [ObservableProperty]
    private ImageSource? _vehiclePreview;

    [ObservableProperty]
    private ImageSource? _stopPreview;

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
        LoadFromPreferences();
    }

    /// <summary>
    /// Re-reads every value from Preferences (the map can change some of them itself).
    /// Side effects (writing prefs, sending messages, haptics) are suppressed while loading.
    /// </summary>
    private void LoadFromPreferences()
    {
        _isLoading = true;
        try
        {
            IsTrafficEnabled = Preferences.Default.Get(PrefTrafficKey, true);
            ShowStopsOnMap = Preferences.Default.Get(PrefStopsKey, true);
            ShowNumberOnly = Preferences.Default.Get(PrefNumberOnlyKey, false);
            ShowMoreVehicleInfo = Utils.Preferences.ShowMoreVehicleInfo;
            StopSize = Preferences.Default.Get(PrefStopSize, 15);
            VehicleSize = Preferences.Default.Get(PrefVehicleSize, 25);
            ThemeIndex = Math.Clamp(Preferences.Default.Get(PrefAppThemeKey, 0), 0, 2);

            var seconds = Preferences.Default.Get(PrefRefreshIntervalKey, DefaultRefreshIntervalSeconds);
            var index = Array.IndexOf(RefreshIntervalValues, seconds);
            RefreshIntervalIndex = index >= 0 ? index : Array.IndexOf(RefreshIntervalValues, DefaultRefreshIntervalSeconds);

            StartPageIndex = Preferences.Default.Get(PrefStartPageKey, StartPageMap) == StartPageSchedule ? 1 : 0;
        }
        finally
        {
            _isLoading = false;
        }
    }

    /// <summary>Applies the persisted theme (0 = system, 1 = light, 2 = dark).</summary>
    public static void ApplyTheme(int themeIndex, Application? application = null)
    {
        application ??= Application.Current;
        if (application is null) return;
        application.UserAppTheme = themeIndex switch
        {
            1 => AppTheme.Light,
            2 => AppTheme.Dark,
            _ => AppTheme.Unspecified
        };
    }

    public static void ApplySavedTheme(Application? application = null) =>
        ApplyTheme(Preferences.Default.Get(PrefAppThemeKey, 0), application);

    private static void PerformHaptic()
    {
        try
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
        }
        catch
        {
            // Haptics not supported / not permitted — ignore.
        }
    }

    partial void OnIsTrafficEnabledChanged(bool value)
    {
        if (_isLoading) return;
        PerformHaptic();
        Preferences.Default.Set(PrefTrafficKey, value);
        WeakReferenceMessenger.Default.Send(new TrafficPreferenceChangedMessage(value));
    }

    partial void OnShowStopsOnMapChanged(bool value)
    {
        if (_isLoading) return;
        PerformHaptic();
        Preferences.Default.Set(PrefStopsKey, value);
        WeakReferenceMessenger.Default.Send(new StopsPreferenceChangedMessage(value));
    }

    partial void OnShowNumberOnlyChanged(bool value)
    {
        _ = UpdateVehiclePreview();
        if (_isLoading) return;
        PerformHaptic();
        Preferences.Default.Set(PrefNumberOnlyKey, value);
        WeakReferenceMessenger.Default.Send(new NumberOnlyPreferenceChangedMessage(value));
    }

    partial void OnShowMoreVehicleInfoChanged(bool value)
    {
        if (_isLoading) return;
        PerformHaptic();
        Utils.Preferences.ShowMoreVehicleInfo = value;
    }

    partial void OnThemeIndexChanged(int value)
    {
        if (_isLoading || value < 0) return;
        Preferences.Default.Set(PrefAppThemeKey, value);
        ApplyTheme(value);
    }

    partial void OnRefreshIntervalIndexChanged(int value)
    {
        if (_isLoading || value < 0 || value >= RefreshIntervalValues.Length) return;
        var seconds = RefreshIntervalValues[value];
        Preferences.Default.Set(PrefRefreshIntervalKey, seconds);
        WeakReferenceMessenger.Default.Send(new RefreshIntervalChangedMessage(seconds));
    }

    partial void OnStartPageIndexChanged(int value)
    {
        if (_isLoading || value < 0) return;
        Preferences.Default.Set(PrefStartPageKey, value == 1 ? StartPageSchedule : StartPageMap);
    }

    private async Task UpdateVehiclePreview()
    {
        await _vehicleDebouncer.RunAsync(async () =>
        {
            var pin = MapBitmapFactory.CreateCustomPinBitmap(Platform.CurrentActivity, VehicleType.Bus, "12", "#FF0000", 45, VehicleSize, ShowNumberOnly);

            using var ms = new MemoryStream();
            pin.Bitmap.Compress(Bitmap.CompressFormat.Png, 100, ms);
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
        LoadFromPreferences();
        await UpdateVehiclePreview();
        await UpdateStopPreview();
    }

    [RelayCommand]
    private async Task SendFeedback()
    {
        const string subject = "Feedback IvanConnections Travel";
        var body = $"\n\n---\nVersiune: {AppVersion}\nDispozitiv: {DeviceInfo.Manufacturer} {DeviceInfo.Model} (Android {DeviceInfo.VersionString})";
        try
        {
            if (Email.Default.IsComposeSupported)
            {
                await Email.Default.ComposeAsync(new EmailMessage
                {
                    Subject = subject,
                    Body = body,
                    To = [FeedbackEmail]
                });
                return;
            }
        }
        catch
        {
            // Fall back to a mailto link below.
        }

        try
        {
            var uri = $"mailto:{FeedbackEmail}?subject={Uri.EscapeDataString(subject)}&body={Uri.EscapeDataString(body)}";
            await Launcher.Default.OpenAsync(uri);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SettingsViewModel] Feedback error: {ex.Message}");
        }
    }

    [RelayCommand]
    private Task ShowOnboarding() => OnboardingPage.ShowAsync();
}
