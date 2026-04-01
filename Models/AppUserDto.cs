namespace IvanConnections_Travel.Models;

public class AppUserDto
{
    public string DeviceId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string AvatarSeed { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int KarmaPoints { get; set; }
    public DateTime? Lastlogin { get; set; }

    public List<UserBadgeDto> UserBadges { get; set; } = [];
    public int BadgeCount => UserBadges?.Count ?? 0;
    public string AvatarUrl => string.IsNullOrEmpty(AvatarSeed)
        ? string.Empty
        : $"https://robohash.org/{AvatarSeed}?set=set4";
}
public class BadgeRarityDto
{
    public string Name { get; set; } = null!;
    public string HexColor { get; set; } = null!;
    public int? DisplayOrder { get; set; }
}

public class BadgeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string IconCode { get; set; } = null!;
    public BadgeRarityDto Rarity { get; set; } = null!;
}

public class UserBadgeDto
{
    public DateTime EarnedAt { get; set; }
    public BadgeDto Badge { get; set; } = null!;
}
public class BadgeDisplayItem
{
    public UserBadgeDto UserBadge { get; set; } = null!;
    public bool IsUnlocked { get; set; }
    public bool IsLocked => !IsUnlocked;
}