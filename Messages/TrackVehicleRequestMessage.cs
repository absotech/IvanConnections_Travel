using CommunityToolkit.Mvvm.Messaging.Messages;

namespace IvanConnections_Travel.Messages;

/// <summary>
/// Asks the map to focus/track a vehicle. Value is the vehicle id (matches <c>Vehicle.Id</c>),
/// optionally with the route short name used as a fallback hint.
/// </summary>
public sealed class TrackVehicleRequestMessage(int vehicleId, string? routeShortName = null)
    : ValueChangedMessage<int>(vehicleId)
{
    public string? RouteShortName { get; } = routeShortName;
}
