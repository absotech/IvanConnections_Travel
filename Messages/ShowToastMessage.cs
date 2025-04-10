using CommunityToolkit.Mvvm.Messaging.Messages;

namespace IvanConnections_Travel.Messages
{
    public class ShowToastMessage(string content) : ValueChangedMessage<string>(content)
    {
    }
}
