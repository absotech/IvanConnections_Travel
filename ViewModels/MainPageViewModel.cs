using CommunityToolkit.Mvvm.Messaging;
using IvanConnections_Travel.Messages;
using IvanConnections_Travel.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.Maui.ApplicationModel;
using IvanConnections_Travel.Utils;
namespace IvanConnections_Travel.ViewModels
{
    public class MainPageViewModel : INotifyPropertyChanged, IDisposable
    {
        private static readonly HttpClient _httpClient = new HttpClient(); // Reuse HttpClient
        private readonly System.Timers.Timer _refreshTimer;
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private string? _lastVehicleHash = null;
        private readonly JsonSerializerOptions _jsonSerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public ObservableCollection<Vehicle> Pins { get; private set; } = new(); // Lazy initialization
        public event PropertyChangedEventHandler? PropertyChanged;

        public MainPageViewModel()
        {
            _refreshTimer = new System.Timers.Timer(5000); // Increase interval to reduce load
            _refreshTimer.Elapsed += async (s, e) => await RefreshPinsAsync();
            _refreshTimer.AutoReset = true;

            StartPeriodicRefresh();
        }

        public void StartPeriodicRefresh()
        {
            _refreshTimer.Start();
            Task.Run(LoadPinsFromBackendAsync);
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
#if DEBUG
                _httpClient.DefaultRequestHeaders.Add("Accept", "*/*");
#endif
                var response = await _httpClient.GetAsync("http://192.168.0.99:5000/ivanconnectionstravel/api/Vehicles/valid");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var vehicles = JsonSerializer.Deserialize<List<Vehicle>>(json, _jsonSerializerOptions);

                    if (vehicles is not null)
                    {
                        var currentHash = ComputingHelpers.ComputeMd5Hash(json);
                        if (currentHash != _lastVehicleHash)
                        {
                            _lastVehicleHash = currentHash;

                            // Use batch update to improve performance
                            var updatedPins = new ObservableCollection<Vehicle>(vehicles);
                            Pins = updatedPins;

                            WeakReferenceMessenger.Default.Send(new PinsUpdatedMessage(updatedPins.ToList()));
                        }
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