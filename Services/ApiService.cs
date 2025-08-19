using IvanConnections_Travel.Models;
using System.Net.Http.Json;
using System.Diagnostics;

namespace IvanConnections_Travel.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "http://server.ivanconnections.cloud:5000/ivanconnectionstravel/api";

        public ApiService()
        {
            _httpClient = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        }

        public async Task<List<StopArrival>> GetArrivalsForStopAsync(int stopId)
        {
            try
            {
                var url = $"{BaseUrl}/Stops/{stopId}/arrivals";
                var arrivals = await _httpClient.GetFromJsonAsync<List<StopArrival>>(url);
                return arrivals ?? new List<StopArrival>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ApiService] Error fetching arrivals: {ex.Message}");
                return [];
            }
        }
    }
}