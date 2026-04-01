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

    [ObservableProperty] private bool _isLoggedIn;

    [ObservableProperty] private bool _isLoggingIn;

    public AppShellViewModel(ApiService apiService)
    {
        _apiService = apiService;
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
        var user = await _apiService.LoginAsync(deviceId);
        if (user != null)
        {
            CurrentUser = user;
            IsLoggedIn = true;
        }
    }
}