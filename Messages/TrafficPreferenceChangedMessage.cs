using CommunityToolkit.Mvvm.Messaging.Messages;

namespace IvanConnections_Travel.Messages;

public class TrafficPreferenceChangedMessage(bool value) : ValueChangedMessage<bool>(value)
{
}
