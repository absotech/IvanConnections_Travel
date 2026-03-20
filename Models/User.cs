namespace IvanConnections_Travel.Models;

public class User
{
    public string DeviceId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string AvatarSeed { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int KarmaPoints { get; set; }

    public string AvatarUrl => string.IsNullOrEmpty(AvatarSeed)
        ? string.Empty
        : $"https://robohash.org/{AvatarSeed}?set=set4";
}
