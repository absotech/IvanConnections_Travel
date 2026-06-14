namespace IvanConnections_Travel.Models;

public class Route
{
    public int RouteId { get; set; }
    public string? RouteShortName { get; set; }
    public string? RouteLongName { get; set; }
    public string? RouteColor { get; set; }
    public string? RouteTextColor { get; set; }
    public string? AgencyId { get; set; }
}
