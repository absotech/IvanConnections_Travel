namespace IvanConnections_Travel.Models;

public class ChatMessage
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string VehicleId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string AvatarSeed { get; set; } = string.Empty;

    public string AvatarUrl => string.IsNullOrEmpty(AvatarSeed)
        ? string.Empty
        : $"https://robohash.org/{AvatarSeed}?set=set4";

    public string FormattedTime => Timestamp.ToLocalTime().ToString("HH:mm");
}
