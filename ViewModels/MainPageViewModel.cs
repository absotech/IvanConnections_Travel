using CommunityToolkit.Mvvm.Messaging;
using IvanConnections_Travel.Messages;
using IvanConnections_Travel.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.Maui.ApplicationModel;
using IvanConnections_Travel.Utils;
using CommunityToolkit.Mvvm.ComponentModel;
namespace IvanConnections_Travel.ViewModels
{
    public class MainPageViewModel :ObservableObject, INotifyPropertyChanged, IDisposable
    {
        private static readonly HttpClient _httpClient = new HttpClient(); // Reuse HttpClient
        private readonly System.Timers.Timer _refreshTimer;
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private string? _lastVehicleHash = null;
        private readonly JsonSerializerOptions _jsonSerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };
        public ObservableCollection<Record> Records { get; }
        public ObservableCollection<Vehicle> Pins { get; private set; } = new(); // Lazy initialization
        public event PropertyChangedEventHandler? PropertyChanged;

        public MainPageViewModel()
        {
            this.Records = new ObservableCollection<Record>()
        {
            new Record { Name = "Test Pin", Location = "20.791694673458274, -156.336394697", PathGeometry = "M 50.7361,983.661 C 44.1895,983.661 38.8369,988.97 38.8369,995.517 39.8649,1003.3 45.246,1008.1 49.8547,1014.12 50.2838,1014.66 51.2336,1014.66 51.6619,1014.12 52.1384,1013.48 52.7575,1012.73 53.4248,1011.91 55.0322,1012.07 56.4727,1012.32 57.5676,1012.71 58.407,1013 59.06,1013.33 59.4192,1013.63 59.7784,1013.93 59.7716,1014.11 59.7716,1014.16 59.7716,1014.21 59.7716,1014.39 59.4192,1014.69 59.06,1014.99 58.407,1015.32 57.5676,1015.61 55.8888,1016.2 53.4519,1016.63 50.7361,1016.63 48.0204,1016.63 45.5399,1016.2 43.8611,1015.61 43.0218,1015.32 42.3695,1014.99 42.0103,1014.69 41.6504,1014.39 41.6135,1014.21 41.6135,1014.16 41.6135,1014.11 41.6511,1013.93 42.0103,1013.63 42.3695,1013.33 43.0218,1013 43.8611,1012.71 44.3158,1012.55 44.8455,1012.35 45.4039,1012.22 L 43.8611,1010.33 C 43.6124,1010.4 43.3441,1010.46 43.1119,1010.55 42.1005,1010.9 41.2318,1011.31 40.5555,1011.87 39.8799,1012.43 39.3216,1013.22 39.3216,1014.16 39.3216,1015.1 39.8799,1015.85 40.5555,1016.41 41.2318,1016.97 42.1005,1017.42 43.1119,1017.77 45.1356,1018.48 47.8025,1018.92 50.7362,1018.92 54.437,1018.81 57.9892,1018.36 60.8733,1016.41 62.5084,1014.79 62.0756,1013.4 60.8733,1011.87 60.1969,1011.31 59.3726,1010.9 58.3612,1010.55 57.4331,1010.22 56.3503,1009.94 55.1878,1009.75 56.1992,1008.51 57.2362,1007.18 58.2289,1005.79 60.5599,1002.51 62.5918,998.968 62.5918,995.517 62.5918,988.97 57.2836,983.661 50.7362,983.661 Z M 50.7361,989.655 C 47.571,989.655 44.9627,992.219 44.9627,995.385 44.9627,998.55 47.571,1001.16 50.7361,1001.16 53.902,1001.16 56.4659,998.55 56.4659,995.385 56.4659,992.219 53.902,989.655 50.7361,989.655 Z M 50.7361,991.947 C 52.6591,991.947 54.174,993.462 54.174,995.385 54.174,997.307 52.6591,998.866 50.7361,998.866 48.8139,998.866 47.2546,997.307 47.2546,995.385 47.2546,993.462 48.8139,991.947 50.7361,991.947 Z" }
        };
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