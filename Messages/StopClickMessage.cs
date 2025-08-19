using CommunityToolkit.Mvvm.Messaging.Messages;
using IvanConnections_Travel.Models;

namespace IvanConnections_Travel.Messages;
public class StopClickMessage(Stop? stop) : ValueChangedMessage<Stop?>(stop)
{

}

