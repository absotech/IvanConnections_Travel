using CommunityToolkit.Mvvm.Messaging;
using IvanConnections_Travel.Messages;
using IvanConnections_Travel.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.Maui.ApplicationModel;
namespace IvanConnections_Travel.ViewModels
{
    public class MainPageViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly System.Timers.Timer _refreshTimer;
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        public ObservableCollection<Vehicle> Pins { get; set; } = [];
        public event PropertyChangedEventHandler? PropertyChanged;

        public MainPageViewModel()
        {            _refreshTimer = new System.Timers.Timer(2000);
            _refreshTimer.Elapsed += (s, e) =>
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await RefreshPinsAsync();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Background refresh error: {ex.Message}");
                    }
                });
            };
            _refreshTimer.AutoReset = true;
            StartPeriodicRefresh();
        }

        public void StartPeriodicRefresh()
        {
            _refreshTimer.Start();
            _ = Task.Run(async () => await LoadPinsFromBackendAsync());
        }

        public void StopPeriodicRefresh()
        {
            _refreshTimer.Stop();
        }

        private async Task RefreshPinsAsync()
        {
            if (await _semaphore.WaitAsync(0))
            {
                try
                {
                    await LoadPinsFromBackendAsync();
                }
                finally
                {
                    _semaphore.Release();
                }
            }
        }

        public async Task LoadPinsFromBackendAsync()
        {
            try
            {
                using var httpClient = new HttpClient();
#if DEBUG
                httpClient.DefaultRequestHeaders.Add("Accept", "*/*");
#endif
                var response = await httpClient.GetAsync("http://192.168.0.99:5000/ivanconnectionstravel/api/Vehicles/valid");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    var vehicles = JsonSerializer.Deserialize<List<Vehicle>>(json, options);
                    if (vehicles is not null)
                    {
                        Pins.Clear();
                        foreach (var v in vehicles)
                        {
                            Pins.Add(v);
                        }
                        WeakReferenceMessenger.Default.Send(new PinsUpdatedMessage([.. Pins]));
                    }
                }
                else
                {
                    Debug.WriteLine($"API error: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _refreshTimer?.Stop();
            _refreshTimer?.Dispose();
            _semaphore?.Dispose();
        }
    }
}