using Microsoft.Maui.Maps;

namespace IvanConnections_Travel.CustomControls.Interfaces
{
    public interface ICustomPin : IMapPin
    {
        IView? Icon { get; set; }

        string? PinColor { get; }
    }
}
