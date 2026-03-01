using CoreGraphics;
using CoreLocation;
using Foundation;
using IvanConnections_Travel.Controls;
using IvanConnections_Travel.Models.Enums;
using IvanConnections_Travel.Utils;
using MapKit;
using Microsoft.Maui.Maps.Handlers;
using Microsoft.Maui.Platform;
using System.Collections.Concurrent;
using UIKit;

namespace IvanConnections_Travel.Platforms.Handlers
{
    /// <summary>
    /// The primary handler for the CustomMauiMap control on iOS.
    /// It uses a PropertyMapper to react to changes in the custom BindableProperties
    /// (like Vehicles, Stops, ShowStops) and updates the native MKMapView accordingly.
    /// </summary>
    public class CustomMapHandler : MapHandler
    {
        private static readonly IPropertyMapper<CustomMauiMap, CustomMapHandler> CustomMapper =
            new PropertyMapper<CustomMauiMap, CustomMapHandler>(Mapper)
            {
                [nameof(CustomMauiMap.Vehicles)] = MapVehicles,
                [nameof(CustomMauiMap.Stops)] = MapStops,
                [nameof(CustomMauiMap.ShowStops)] = MapStops,
                [nameof(CustomMauiMap.MoveToLocation)] = MapMoveToLocation
            };

        private static void MapVehicles(CustomMapHandler handler, CustomMauiMap map)
            => handler.UpdateVehicleMarkers();

        private static void MapStops(CustomMapHandler handler, CustomMauiMap map)
            => handler.UpdateStopMarkers();

        private MKMapView? _mapView;
        private Location? _pendingLocation;

        private static readonly ConcurrentDictionary<BitmapCacheKey, UIImage> ImageCache = new();

        private readonly Dictionary<string, IMKAnnotation> _vehicleAnnotations = new();
        private readonly Dictionary<int, IMKAnnotation> _stopAnnotations = new();

        public CustomMapHandler() : base(CustomMapper, CommandMapper)
        {
        }

        protected override void ConnectHandler(MKMapView platformView)
        {
            base.ConnectHandler(platformView);
            _mapView = platformView;

            if (_mapView != null)
            {
                _mapView.ShowsTraffic = true;
                _mapView.ShowsUserLocation = true;
                _mapView.Delegate = new CustomMapDelegate(this);

                UpdateVehicleMarkers();
                UpdateStopMarkers();
            }
        }

        private async void UpdateVehicleMarkers()
        {
            if (_mapView is null || VirtualView is not CustomMauiMap mauiMap) return;

            var annotationData = await Task.Run(() =>
            {
                var vehicleInfo = new Dictionary<string, (double Lat, double Lon, UIImage Icon)>();
                foreach (var v in mauiMap.Vehicles)
                {
                    if (!v.Latitude.HasValue || !v.Longitude.HasValue || !v.VehicleType.HasValue ||
                        v.Label is null) continue;

                    var key = new BitmapCacheKey(v.VehicleType.Value, v.RouteShortName ?? "",
                        ColorManagement.NormalizeColorHex(v.RouteColor ?? "#000000"), v.Direction, false);
                    var image = ImageCache.GetOrAdd(key,
                        _ => MapBitmapFactory.CreateCustomPinImage(v.VehicleType.Value, v.RouteShortName,
                            v.RouteColor, v.Direction));

                    vehicleInfo[v.Label] = (v.Latitude.Value, v.Longitude.Value, image);
                }

                return vehicleInfo;
            });

            var visibleVehicleKeys = new HashSet<string>(_vehicleAnnotations.Keys);

            foreach (var (vehicleId, value) in annotationData)
            {
                if (_vehicleAnnotations.TryGetValue(vehicleId, out var existingAnnotation))
                {
                    if (existingAnnotation is CustomAnnotation customAnnotation)
                    {
                        customAnnotation.SetCoordinate(new CLLocationCoordinate2D(value.Lat, value.Lon));
                        customAnnotation.Icon = value.Icon;
                    }
                    visibleVehicleKeys.Remove(vehicleId);
                }
                else
                {
                    var annotation = new CustomAnnotation
                    {
                        Coordinate = new CLLocationCoordinate2D(value.Lat, value.Lon),
                        Title = vehicleId,
                        Tag = $"vehicle_{vehicleId}",
                        Icon = value.Icon
                    };

                    _mapView.AddAnnotation(annotation);
                    _vehicleAnnotations[vehicleId] = annotation;
                }
            }

            foreach (var vehicleKeyToRemove in visibleVehicleKeys)
            {
                if (_vehicleAnnotations.TryGetValue(vehicleKeyToRemove, out var annotationToRemove))
                {
                    _mapView.RemoveAnnotation(annotationToRemove);
                    _vehicleAnnotations.Remove(vehicleKeyToRemove);
                }
            }
        }

        private void UpdateStopMarkers()
        {
            if (_mapView is null || VirtualView is not CustomMauiMap mauiMap) return;

            foreach (var annotation in _stopAnnotations.Values)
                _mapView.RemoveAnnotation(annotation);
            _stopAnnotations.Clear();

            if (!mauiMap.ShowStops) return;

            foreach (var stop in mauiMap.Stops)
            {
                if (string.IsNullOrWhiteSpace(stop.StopName)) continue;

                var key = new BitmapCacheKey(VehicleType.Bus, stop.StopName, "#000088", null, isStopIcon: true);
                var image = ImageCache.GetOrAdd(key,
                    _ => MapBitmapFactory.CreateStopPinImage(stop.StopName));

                var annotation = new CustomAnnotation
                {
                    Coordinate = new CLLocationCoordinate2D(stop.StopLat, stop.StopLon),
                    Title = $"Stație: {stop.StopName}",
                    Tag = $"stop_{stop.StopId}",
                    Icon = image
                };

                _mapView.AddAnnotation(annotation);
                _stopAnnotations[stop.StopId] = annotation;
            }
        }

        private static void MapMoveToLocation(CustomMapHandler handler, CustomMauiMap map)
        {
            handler._pendingLocation = map.MoveToLocation;
            handler.ProcessMapMove();
        }

        internal void ProcessMapMove()
        {
            if (_mapView == null || _pendingLocation == null) return;
            var coordinate = new CLLocationCoordinate2D(_pendingLocation.Latitude, _pendingLocation.Longitude);
            var span = new MKCoordinateSpan(0.01, 0.01); // Roughly equivalent to zoom 15
            var region = new MKCoordinateRegion(coordinate, span);
            _mapView.SetRegion(region, animated: true);
            _pendingLocation = null;
        }

        internal void OnAnnotationSelected(IMKAnnotation annotation)
        {
            if (VirtualView is not CustomMauiMap mauiMap) return;
            if (annotation is CustomAnnotation customAnnotation && !string.IsNullOrEmpty(customAnnotation.Tag))
            {
                mauiMap.MarkerClickCommand?.Execute(customAnnotation.Tag);
            }
        }

        internal void OnMapTapped()
        {
            if (VirtualView is not CustomMauiMap mauiMap) return;
            mauiMap.MapClickCommand?.Execute(null);
        }

        public static void ClearImageCache()
        {
            foreach (var image in ImageCache.Values)
            {
                image.Dispose();
            }

            ImageCache.Clear();
        }
    }

    /// <summary>
    /// Custom annotation class for iOS maps
    /// </summary>
    public class CustomAnnotation : MKAnnotation
    {
        private CLLocationCoordinate2D _coordinate;

        public override CLLocationCoordinate2D Coordinate => _coordinate;

        public override void SetCoordinate(CLLocationCoordinate2D value)
        {
            _coordinate = value;
        }

        public override string? Title { get; set; }
        public string? Tag { get; set; }
        public UIImage? Icon { get; set; }
    }

    /// <summary>
    /// Custom map delegate for handling iOS map events
    /// </summary>
    public class CustomMapDelegate : MKMapViewDelegate
    {
        private readonly CustomMapHandler _handler;

        public CustomMapDelegate(CustomMapHandler handler)
        {
            _handler = handler;
        }

        public override MKAnnotationView? GetViewForAnnotation(MKMapView mapView, IMKAnnotation annotation)
        {
            if (annotation is MKUserLocation)
                return null;

            if (annotation is CustomAnnotation customAnnotation)
            {
                var identifier = "CustomPin";
                var annotationView = mapView.DequeueReusableAnnotation(identifier) as MKAnnotationView;

                if (annotationView == null)
                {
                    annotationView = new MKAnnotationView(annotation, identifier);
                    annotationView.CanShowCallout = true;
                }
                else
                {
                    annotationView.Annotation = annotation;
                }

                if (customAnnotation.Icon != null)
                {
                    annotationView.Image = customAnnotation.Icon;
                }

                return annotationView;
            }

            return null;
        }

        public override void DidSelectAnnotationView(MKMapView mapView, MKAnnotationView view)
        {
            if (view.Annotation != null)
            {
                _handler.OnAnnotationSelected(view.Annotation);
            }
        }

        public override void RegionChanged(MKMapView mapView, bool animated)
        {
            // Handle map tap by deselecting annotations
            if (mapView.SelectedAnnotations?.Length == 0)
            {
                _handler.OnMapTapped();
            }
        }
    }
}
