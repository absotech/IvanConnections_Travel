using IvanConnections_Travel.Models;
using Microsoft.AspNetCore.SignalR.Client;
using System.Diagnostics;
using System.Text.Json;

namespace IvanConnections_Travel.Services;

public class ChatHubService : IAsyncDisposable
{
    private HubConnection? _connection;
    private string? _currentVehicleId;
    private const string HubUrl = "https://server.ivanconnections.com/ivanconnectionstravel/hubs/chat";

    public event Action<ChatMessage>? MessageReceived;

    public bool IsConnected => _connection?.State == HubConnectionState.Connected;

    public async Task ConnectAsync(string deviceId, string vehicleId)
    {
        if (_connection != null)
        {
            await DisconnectAsync();
        }

        _currentVehicleId = vehicleId;

        _connection = new HubConnectionBuilder()
            .WithUrl($"{HubUrl}?deviceId={Uri.EscapeDataString(deviceId)}")
            .WithAutomaticReconnect()
            .Build();

        _connection.On<JsonElement>("ReceiveMessage", element =>
        {
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var msg = element.Deserialize<ChatMessage>(options);
                if (msg != null)
                {
                    MainThread.BeginInvokeOnMainThread(() => MessageReceived?.Invoke(msg));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ChatHubService] Error deserializing message: {ex.Message}");
            }
        });

        try
        {
            await _connection.StartAsync();
            await _connection.InvokeAsync("JoinVehicleRoom", vehicleId);
            Debug.WriteLine($"[ChatHubService] Connected and Joined group for vehicle {vehicleId}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ChatHubService] Connection failed: {ex.Message}");
        }
    }

    public async Task SendMessageAsync(int vehicleId, string text)
    {
        if (_connection?.State != HubConnectionState.Connected)
        {
            Debug.WriteLine("[ChatHubService] Not connected, cannot send message.");
            return;
        }

        try
        {
            await _connection.InvokeAsync("SendMessage", vehicleId, text);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ChatHubService] Error sending message: {ex.Message}");
            throw;
        }
    }

    public async Task DisconnectAsync()
    {
        if (_connection != null)
        {
            try
            {
                await _connection.StopAsync();
                await _connection.DisposeAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ChatHubService] Error disconnecting: {ex.Message}");
            }
            finally
            {
                _connection = null;
                _currentVehicleId = null;
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
    }
}
