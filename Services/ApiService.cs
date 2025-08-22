using IvanConnections_Travel.Models;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;

namespace IvanConnections_Travel.Services;
public record ApiResponse<T>(T? Data, bool IsNotModified = false);

public class ApiService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private readonly Dictionary<string, string> _etags = [];
    private const string BaseUrl = "https://server.ivanconnections.com/ivanconnectionstravel/api";

    public ApiService()
    {
        _httpClient = new HttpClient();
#if DEBUG
        _httpClient.DefaultRequestHeaders.Add("Accept", "*/*");
#endif
        _jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    /// <summary>
    /// Fetches the list of vehicles from the API. Handles caching via ETags.
    /// </summary>
    /// <param name="route">Optional route name to filter the vehicles.</param>
    /// <returns>An ApiResponse containing the list of vehicles or indicating the data was not modified.</returns>
    public async Task<ApiResponse<List<Vehicle>>> GetVehiclesAsync(string? route = null)
    {
        var apiUrl = string.IsNullOrEmpty(route)
            ? $"{BaseUrl}/Vehicles/valid"
            : $"{BaseUrl}/Vehicles/valid/byroute/{Uri.EscapeDataString(route)}";

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
            if (_etags.TryGetValue(apiUrl, out var etag))
            {
                request.Headers.Add("If-None-Match", etag);
            }

            using var response = await _httpClient.SendAsync(request);

            if (response.StatusCode == System.Net.HttpStatusCode.NotModified)
            {
                Debug.WriteLine($"[ApiService] Vehicles not modified for URL: {apiUrl}");
                return new ApiResponse<List<Vehicle>>(null, IsNotModified: true);
            }

            if (!response.IsSuccessStatusCode)
            {
                Debug.WriteLine($"[ApiService] API error: {response.StatusCode} for {apiUrl}");
                return new ApiResponse<List<Vehicle>>(null);
            }

            if (response.Headers.TryGetValues("ETag", out var etagHeaderValues))
            {
                _etags[apiUrl] = etagHeaderValues.First();
            }

            var json = await response.Content.ReadAsStringAsync();
            var vehicles = JsonSerializer.Deserialize<List<Vehicle>>(json, _jsonSerializerOptions);

            return new ApiResponse<List<Vehicle>>(vehicles ?? new List<Vehicle>());
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ApiService] Exception in GetVehiclesAsync: {ex.Message}");
            return new ApiResponse<List<Vehicle>>(null);
        }
    }

    /// <summary>
    /// Fetches the list of all stops from the API.
    /// </summary>
    /// <returns>A list of stops, or an empty list if an error occurs.</returns>
    public async Task<List<Stop>> GetStopsAsync()
    {
        var stopsUrl = $"{BaseUrl}/Stops";
        try
        {
            var stops = await _httpClient.GetFromJsonAsync<List<Stop>>(stopsUrl, _jsonSerializerOptions);
            return stops ?? new List<Stop>();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ApiService] Error fetching stops: {ex.Message}");
            return [];
        }
    }

    /// <summary>
    /// Fetches arrival times for a specific stop.
    /// </summary>
    /// <param name="stopId">The ID of the stop.</param>
    /// <returns>A list of arrivals, or an empty list if an error occurs.</returns>
    public async Task<List<StopArrival>> GetArrivalsForStopAsync(int stopId)
    {
        try
        {
            var url = $"{BaseUrl}/Stops/{stopId}/arrivals";
            var arrivals = await _httpClient.GetFromJsonAsync<List<StopArrival>>(url, _jsonSerializerOptions);
            return arrivals ?? new List<StopArrival>();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ApiService] Error fetching arrivals: {ex.Message}");
            return [];
        }
    }
}