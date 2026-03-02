using Android.Content.Res;
using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using Android.Graphics;
using IvanConnections_Travel.Controls;
using IvanConnections_Travel.Models.Enums;
using IvanConnections_Travel.Utils;
using Microsoft.Maui.Maps.Handlers;
using System.Collections.Concurrent;
using _Microsoft.Android.Resource.Designer;

namespace IvanConnections_Travel.Platforms.Handlers
{
    /// <summary>
    /// The primary handler for the CustomMauiMap control on Android.
    /// It uses a PropertyMapper to react to changes in the custom BindableProperties
    /// (like Vehicles, Stops, ShowStops) and updates the native GoogleMap accordingly.
    /// </summary>
    public class CustomMapHandler : MapHandler
    {
        private static readonly IPropertyMapper<CustomMauiMap, CustomMapHandler> CustomMapper =
            new PropertyMapper<CustomMauiMap, CustomMapHandler>(Mapper)
            {
                [nameof(CustomMauiMap.Vehicles)] = MapVehicles,
                [nameof(CustomMauiMap.Stops)] = MapStops,
                [nameof(CustomMauiMap.ShowStops)] = MapStops,
                [nameof(CustomMauiMap.MoveToLocation)] = MapMoveToLocation,
                [nameof(CustomMauiMap.MapBearing)] = MapMapBearing,
                [nameof(CustomMauiMap.StopPinSize)] = MapStopPinSize,
                [nameof(CustomMauiMap.VehiclePinSize)] = MapVehiclePinSize,
                [nameof(Microsoft.Maui.Maps.IMap.IsTrafficEnabled)] = MapIsTrafficEnabled
            };

        private static void MapIsTrafficEnabled(CustomMapHandler handler, CustomMauiMap map)
        {
            if (handler._googleMap is null) return;
            handler._googleMap.TrafficEnabled = map.IsTrafficEnabled;
        }

        private static void MapVehicles(CustomMapHandler handler, CustomMauiMap map)
            => handler.UpdateVehicleMarkers();

        private static void MapStops(CustomMapHandler handler, CustomMauiMap map)
            => handler.UpdateStopMarkers();

        private static void MapStopPinSize(CustomMapHandler handler, CustomMauiMap map)
            => handler.UpdateStopMarkers();

        private static void MapVehiclePinSize(CustomMapHandler handler, CustomMauiMap map)
            => handler.UpdateVehicleMarkers();

        private static void MapMapBearing(CustomMapHandler handler, CustomMauiMap map)
        {
            if (handler._googleMap is null) return;
            var currentBearing = handler._googleMap.CameraPosition.Bearing;
            if (Math.Abs(currentBearing - (float)map.MapBearing) > 0.1)
            {
                var cameraUpdate = CameraUpdateFactory.NewCameraPosition(
                    new CameraPosition.Builder(handler._googleMap.CameraPosition)
                        .Bearing((float)map.MapBearing)
                        .Build());
                handler._googleMap.AnimateCamera(cameraUpdate);
            }
        }


        private GoogleMap? _googleMap;
        private readonly CustomMapCallback _mapCallback;
        private Location? _pendingLocation;

        private static readonly ConcurrentDictionary<BitmapCacheKey, Bitmap> BitmapCache = [];

        private readonly Dictionary<string, Marker> _vehicleMarkers = [];
        private readonly Dictionary<int, Marker> _stopMarkers = [];

        public CustomMapHandler() : base(CustomMapper, CommandMapper)
        {
            _mapCallback = new CustomMapCallback(this);
        }

        protected override void ConnectHandler(MapView platformView)
        {
            base.ConnectHandler(platformView);
            platformView.GetMapAsync(_mapCallback);
        }

        internal void OnMapReady(GoogleMap googleMap)
        {
            _googleMap = googleMap;

            try
            {
                if (VirtualView is CustomMauiMap mauiMap)
                {
                    _googleMap.TrafficEnabled = mauiMap.IsTrafficEnabled;
                }
                _googleMap.UiSettings.ZoomControlsEnabled = false;
                _googleMap.UiSettings.MyLocationButtonEnabled = false;
                _googleMap.UiSettings.CompassEnabled = false;
                var success = _googleMap.SetMapStyle(
                    MapStyleOptions.LoadRawResourceStyle(
                        Platform.CurrentActivity ?? throw new InvalidOperationException("Context is null."),
                        ResourceConstant.Raw.map_style));
                if (!success)
                {
                    System.Diagnostics.Debug.WriteLine("Failed to apply map style.");
                }
            }
            catch (Resources.NotFoundException e)
            {
                System.Diagnostics.Debug.WriteLine($"Map style resource not found: {e.Message}");
            }

            _googleMap.MarkerClick += OnMarkerClick;
            _googleMap.MapClick += OnMapClick;
            _googleMap.CameraMove += (s, e) =>
            {
                if (VirtualView is CustomMauiMap mauiMap)
                {
                    mauiMap.MapBearing = _googleMap.CameraPosition.Bearing;
                }
            };

            UpdateVehicleMarkers();
            UpdateStopMarkers();
        }

        private async void UpdateVehicleMarkers()
        {
            if (_googleMap is null || VirtualView is not CustomMauiMap mauiMap) return;
            var context = Platform.CurrentActivity ?? Android.App.Application.Context;

            var vehicleData = await Task.Run(() =>
            {
                var vehicleInfo = new Dictionary<string, (LatLng Position, BitmapDescriptor Icon)>();
                foreach (var v in mauiMap.Vehicles)
                {
                    if (!v.Latitude.HasValue || !v.Longitude.HasValue || !v.VehicleType.HasValue ||
                        v.Label is null) continue;

                    var key = new BitmapCacheKey(v.VehicleType.Value, v.RouteShortName ?? "",
                        ColorManagement.NormalizeColorHex(v.RouteColor ?? "#000000"), v.Direction, false, mauiMap.VehiclePinSize);
                    var bitmap = BitmapCache.GetOrAdd(key,
                        _ => MapBitmapFactory.CreateCustomPinBitmap(context, v.VehicleType.Value, v.RouteShortName,
                            v.RouteColor, v.Direction, mauiMap.VehiclePinSize));

                    vehicleInfo[v.Label] = (new LatLng(v.Latitude.Value, v.Longitude.Value),
                        BitmapDescriptorFactory.FromBitmap(bitmap));
                }

                return vehicleInfo;
            });

            var visibleVehicleKeys = new HashSet<string>(_vehicleMarkers.Keys);

            foreach (var (vehicleId, value) in vehicleData)
            {
                if (_vehicleMarkers.TryGetValue(vehicleId, out var existingMarker))
                {
                    existingMarker.Position = value.Position;
                    existingMarker.SetIcon(value.Icon);
                    visibleVehicleKeys.Remove(vehicleId);
                }
                else
                {
                    var markerOptions = new MarkerOptions()
                        .SetPosition(value.Position)
                        .SetIcon(value.Icon);

                    var newMarker = _googleMap.AddMarker(markerOptions);
                    if (newMarker != null)
                    {
                        newMarker.Tag = $"vehicle_{vehicleId}";
                        _vehicleMarkers[vehicleId] = newMarker;
                    }
                }
            }

            foreach (var vehicleKeyToRemove in visibleVehicleKeys)
            {
                if (_vehicleMarkers.TryGetValue(vehicleKeyToRemove, out var markerToRemove))
                {
                    markerToRemove.Remove();
                    _vehicleMarkers.Remove(vehicleKeyToRemove);
                }
            }
        }

        private void UpdateStopMarkers()
        {
            if (_googleMap is null || VirtualView is not CustomMauiMap mauiMap) return;

            foreach (var marker in _stopMarkers.Values) marker.Remove();
            _stopMarkers.Clear();

            if (!mauiMap.ShowStops) return;

            var context = Platform.CurrentActivity ?? Android.App.Application.Context;
            foreach (var stop in mauiMap.Stops)
            {
                if (string.IsNullOrWhiteSpace(stop.StopName)) continue;

                var markerOptions = new MarkerOptions()
                    .SetPosition(new LatLng(stop.StopLat, stop.StopLon))
                    .SetTitle($"Stație: {stop.StopName}");

                var key = new BitmapCacheKey(VehicleType.Bus, stop.StopName, "#000088", null, isStopIcon: true, mauiMap.StopPinSize);
                var bitmap = BitmapCache.GetOrAdd(key,
                    _ => MapBitmapFactory.CreateStopPinBitmap(context, stop.StopName, mauiMap.StopPinSize));
                markerOptions.SetIcon(BitmapDescriptorFactory.FromBitmap(bitmap));

                var newMarker = _googleMap.AddMarker(markerOptions);
                if (newMarker != null)
                {
                    newMarker.Tag = $"stop_{stop.StopId.ToString()}";
                    _stopMarkers[stop.StopId] = newMarker;
                }
            }
        }

        private static void MapMoveToLocation(CustomMapHandler handler, CustomMauiMap map)
        {
            handler._pendingLocation = map.MoveToLocation;
            handler.ProcessMapMove();
        }

        internal void ProcessMapMove()
        {
            if (_googleMap == null || _pendingLocation == null) return;
            var latLng = new LatLng(_pendingLocation.Latitude, _pendingLocation.Longitude);
            var cameraUpdate = CameraUpdateFactory.NewLatLngZoom(latLng, 15);
            _googleMap.AnimateCamera(cameraUpdate);
            _pendingLocation = null;
        }

        private void OnMarkerClick(object? sender, GoogleMap.MarkerClickEventArgs e)
        {
            if (VirtualView is not CustomMauiMap mauiMap || e.Marker.Tag?.ToString() is not { } tag)
                return;
            mauiMap.MarkerClickCommand?.Execute(tag);

            e.Handled = false;
        }

        private void OnMapClick(object? sender, GoogleMap.MapClickEventArgs e)
        {
            if (VirtualView is not CustomMauiMap mauiMap)
                return;
            mauiMap.MapClickCommand.Execute(null);
        }

        public static void ClearBitmapCache()
        {
            foreach (var bitmap in BitmapCache.Values)
            {
                bitmap.Recycle();
            }

            BitmapCache.Clear();
        }
    }

    /// <summary>
    /// Its only job is to notify the handler when the GoogleMap instance is ready to be used.
    /// </summary>
    public class CustomMapCallback : Java.Lang.Object, IOnMapReadyCallback
    {
        private readonly CustomMapHandler _handler;

        public CustomMapCallback(CustomMapHandler handler)
        {
            _handler = handler;
        }

        public void OnMapReady(GoogleMap googleMap)
        {
            _handler.OnMapReady(googleMap);
            _handler.ProcessMapMove();
        }
    }
}