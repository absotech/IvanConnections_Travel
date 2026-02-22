using CommunityToolkit.Mvvm.Messaging.Messages;
using IvanConnections_Travel.Models;

namespace IvanConnections_Travel.Messages
{
    public class PinsUpdatedMessage : ValueChangedMessage<(List<Vehicle>, List<Stop>, bool)>
    {
        public PinsUpdatedMessage(List<Vehicle> pins, List<Stop> stops, bool showStops = true) : base((pins, stops, showStops)) { }
    }

}
