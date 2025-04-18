using CommunityToolkit.Mvvm.Messaging.Messages;
using IvanConnections_Travel.Models;

namespace IvanConnections_Travel.Messages
{
    public class PinsUpdatedMessage : ValueChangedMessage<(List<Vehicle>, List<Stop>)>
    {
        public PinsUpdatedMessage(List<Vehicle> pins, List<Stop> stops = null) : base((pins, stops)) { }
    }

}
