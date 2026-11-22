using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IvanConnections_Travel.Models;
using IvanConnections_Travel.Services;
using System.Diagnostics;

namespace IvanConnections_Travel.ViewModels;

public partial class AppShellViewModel : ObservableObject
{
    private readonly ApiService _apiService;
    private const string DeviceIdKey = "device_id";

    [ObservableProperty] private AppUserDto? _currentUser;
    private CancellationTokenSource? _loginRefreshCts;

    partial void OnCurrentUserChanged(AppUserDto? value)
    {
        if (value != null)
        {
            IsLoggedIn = true;
        }
    }

    [ObservableProperty] private bool _isLoggedIn;

    [ObservableProperty] private bool _isLoggingIn;

    public AppShellViewModel(ApiService apiService)
    {
        _apiService = apiService;
    }

    public const string RouteMap = "map";
    public const string RouteBadges = "badges";
    public const string RouteSchedule = "schedule";
    public const string RouteSettings = "settings";

    [RelayCommand]
    private async Task OpenBadgesAsync()
    {
        try
        {
            Shell.Current.FlyoutIsPresented = false;
            await Shell.Current.GoToAsync($"//{RouteBadges}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AppShellViewModel] Navigation to badges failed: {ex.Message}");
        }
    }

    public async Task TryAutoLoginAsync()
    {
        try
        {
            var deviceId = await SecureStorage.GetAsync(DeviceIdKey);
            if (string.IsNullOrEmpty(deviceId))
            {
                return;
            }
            await PerformLoginAsync(deviceId);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AppShellViewModel] Auto-login error: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (IsLoggingIn) return;
        IsLoggingIn = true;
        try
        {
            var deviceId = await SecureStorage.GetAsync(DeviceIdKey);
            if (string.IsNullOrEmpty(deviceId))
            {
                deviceId = Guid.NewGuid().ToString();
                await SecureStorage.SetAsync(DeviceIdKey, deviceId);
            }

            await PerformLoginAsync(deviceId);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AppShellViewModel] Login error: {ex.Message}");
        }
        finally
        {
            IsLoggingIn = false;
        }
    }

    private async Task PerformLoginAsync(string deviceId)
    {
        var (user, wasModified) = await _apiService.LoginAsync(deviceId);
        if (wasModified && user != null)
        {
            CurrentUser = user;
        }
        else if (user == null && wasModified)
        {
            
        }
        else if (!wasModified)
        {
            IsLoggedIn = true;
        }

        StartLoginRefreshTimer(deviceId);
    }

    private void StartLoginRefreshTimer(string deviceId)
    {
        _loginRefreshCts?.Cancel();
        _loginRefreshCts = new CancellationTokenSource();
        var token = _loginRefreshCts.Token;

        Task.Run(async () =>
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(10), token);
                    if (token.IsCancellationRequested) break;

                    var (user, wasModified) = await _apiService.LoginAsync(deviceId);
                    if (wasModified && user != null)
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            CurrentUser = user;
                        });
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AppShellViewModel] Refresh login error: {ex.Message}");
            }
        }, token);
    }
}