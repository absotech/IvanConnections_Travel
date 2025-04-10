using CommunityToolkit.Mvvm.Messaging.Messages;
using IvanConnections_Travel.Models;

namespace IvanConnections_Travel.Messages
{
    public class PinsUpdatedMessage : ValueChangedMessage<List<Vehicle>>
    {
        public PinsUpdatedMessage(List<Vehicle> pins) : base(pins) { }
    }

}
