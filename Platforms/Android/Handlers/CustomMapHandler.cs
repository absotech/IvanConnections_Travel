using Android.Gms.Maps.Model;
using Android.Gms.Maps;
using IvanConnections_Travel.CustomControls;
using Microsoft.Maui.Maps.Handlers;
using Microsoft.Maui.Platform;
using IMap = Microsoft.Maui.Maps.IMap;

namespace IvanConnections_Travel.Platforms.Android.Handlers
{
    public class CustomMapHandler : MapHandler
    {
        public static readonly IPropertyMapper<IMap, IMapHandler> CustomMapper =
            new PropertyMapper<IMap, IMapHandler>(Mapper)
            {
                [nameof(IMap.Pins)] = MapPins,
            };

        private static readonly Lazy<BitmapDescriptor> DefaultBitmapDescriptor = new(BitmapDescriptorFactory.DefaultMarker);

        public CustomMapHandler()
            : base(CustomMapper, CommandMapper)
        {
        }

        public CustomMapHandler(IPropertyMapper? mapper = null, CommandMapper? commandMapper = null)
            : base(
            mapper ?? CustomMapper, commandMapper ?? CommandMapper)
        {
        }

        public List<Marker>? Markers { get; private set; }

        internal static bool EnableMapSymbols { get; set; } = true;

        protected override void ConnectHandler(MapView platformView)
        {
            base.ConnectHandler(platformView);
            var mapReady = new MapCallbackHandler(this);
            this.PlatformView.GetMapAsync(mapReady);
        }

        private static new void MapPins(IMapHandler handler, IMap map)
        {
            if (handler is not CustomMapHandler mapHandler)
            {
                return;
            }

            if (mapHandler.Map is null || mapHandler.MauiContext is null)
            {
                return;
            }

            if (mapHandler.Markers is null)
            {
                mapHandler.Markers = new();
            }
            else
            {
                mapHandler.Markers.Clear();
            }

            foreach (var pin in map.Pins)
            {
                var pinHandler = pin.ToHandler(mapHandler.MauiContext);
                if (pinHandler is IMapPinHandler mapPinHandler)
                {
                    var markerOption = mapPinHandler.PlatformView;
                    if (pin is CustomPin customPin)
                    {
                        try
                        {
                            // var imageSourceHandler = new ImageLoaderSourceHandler();
                            // var bitmap = await imageSourceHandler.LoadImageAsync(customPin.ImageSource, Android.App.Application.Context);

                            var customMarker = markerOption.SetIcon(customPin.Icon is null
                                                        ? DefaultBitmapDescriptor.Value
                                                        : BitmapUtils.GetBitmapDescriptorForIcon(customPin.Icon, mapHandler.MauiContext));

                            var marker = mapHandler.Map.AddMarker(customMarker);
                            customPin.MarkerId = marker.Id;
                            mapHandler.Markers.Add(marker);

                            if (customPin.IsSelected)
                            {
                                marker.ShowInfoWindow();
                            }
                            else
                            {
                                marker.HideInfoWindow();
                            }
                        }
                        catch (Exception ex)
                        {
                            App.LogService.LogException(ex);
                        }
                    }
                }
            }
        }
    }
}
