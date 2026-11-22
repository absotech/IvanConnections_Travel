using CommunityToolkit.Mvvm.Messaging.Messages;

namespace IvanConnections_Travel.Messages;

/// <summary>
/// Sent by the Settings page when the vehicle refresh interval (seconds) changes.
/// </summary>
public sealed class RefreshIntervalChangedMessage(int seconds) : ValueChangedMessage<int>(seconds);
