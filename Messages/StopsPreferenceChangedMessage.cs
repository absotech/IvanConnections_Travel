using CommunityToolkit.Mvvm.Messaging.Messages;

namespace IvanConnections_Travel.Messages;

public class StopsPreferenceChangedMessage(bool value) : ValueChangedMessage<bool>(value)
{
}
