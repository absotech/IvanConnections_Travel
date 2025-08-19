using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IvanConnections_Travel.Models;
using System.Text.Json;

namespace IvanConnections_Travel.ViewModels.Popups
{
    public partial class VehiclePopupViewModel : ObservableObject
    {
        readonly WeakEventManager followVehicleManager = new();
        public bool? ShouldFollow = null;
        public event EventHandler<bool?> FollowVehicle
        {
            add => followVehicleManager.AddEventHandler(value);
            remove => followVehicleManager.RemoveEventHandler(value);
        }
        [ObservableProperty]
        private Vehicle? vehicle;
        [ObservableProperty]
        private string vehicleType;
        [ObservableProperty]
        private string? vehicleInfo;
        [ObservableProperty]
        private DateTime? timeOfArrival;
        [ObservableProperty]
        private bool loading = true;


        internal async void Load(Vehicle vehicle)
        {
            Loading = true;
            if (vehicle == null)
            {
                Loading = false;
                return;
            }

            Vehicle = vehicle;
            VehicleType = Utils.Translations.GetVehicleTypeNameInRomanian(vehicle.VehicleType.Value);

            if (vehicle.IsElectricBus)
                VehicleInfo = "Autobuz electric";
            else if (vehicle.IsNewTram)
                VehicleInfo = "Tramvai nou";
            else
                VehicleInfo = null;

            if (vehicle.NextStop != null)
            {
                _ = Task.Run(async () =>
                {
                    var duration = await GetDistanceWithTrafficAsync(vehicle.Latitude.Value, vehicle.Longitude.Value, vehicle.NextStop.StopLat, vehicle.NextStop.StopLon);
                    Loading = false;
                    var seconds = duration ?? 0;
                    TimeOfArrival = vehicle.LocalTimestamp + TimeSpan.FromSeconds(seconds);
                });
            }
            else
                Loading = false;


        }

        [RelayCommand]
        public async Task Follow()
        {
            ShouldFollow = true;
            followVehicleManager.HandleEvent(this, ShouldFollow, nameof(FollowVehicle));
        }
        public static async Task<int?> GetDistanceWithTrafficAsync(double originLat, double originLng, double destLat, double destLng)
        {
            var url = $"https://api.ivanconnections.com/ictravel/distance.php?" +
                      $"origins={originLat},{originLng}" +
                      $"&destinations={destLat},{destLng}" +
                      $"&departure_time=now" +
                      $"&mode=transit" +
                      $"&traffic_model=best_guess";

            using var client = new HttpClient();
            try
            {
                var response = await client.GetStringAsync(url);

                using var jsonDoc = JsonDocument.Parse(response);
                var root = jsonDoc.RootElement;
                var status = root.GetProperty("status").GetString();
                if (status != "OK")
                {
                    Console.WriteLine($"API Error: {response}");
                    return null;
                }

                var row = root.GetProperty("rows")[0];
                var element = row.GetProperty("elements")[0];

                var elementStatus = element.GetProperty("status").GetString();
                if (elementStatus != "OK")
                    return null;

                var duration = element.GetProperty("duration").GetProperty("value").GetInt32();

                return duration;
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Error calling the proxy API: {e.Message}");
                return null;
            }
            catch (JsonException e)
            {
                Console.WriteLine($"Error parsing JSON response from proxy: {e.Message}");
                return null;
            }
        }
    }
}
