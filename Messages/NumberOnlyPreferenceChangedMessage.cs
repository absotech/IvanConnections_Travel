using CommunityToolkit.Mvvm.Messaging.Messages;

namespace IvanConnections_Travel.Messages;

public class NumberOnlyPreferenceChangedMessage(bool value) : ValueChangedMessage<bool>(value)
{
}
