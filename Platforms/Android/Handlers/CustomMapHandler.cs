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
using Android.Content;
using Color = Android.Graphics.Color;

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
                [nameof(CustomMauiMap.Shapes)] = MapShapes,
                [nameof(CustomMauiMap.Stops)] = MapStops,
                [nameof(CustomMauiMap.ShowStops)] = MapStops,
                [nameof(CustomMauiMap.MoveToLocation)] = MapMoveToLocation,
                [nameof(CustomMauiMap.MapBearing)] = MapMapBearing,
                [nameof(CustomMauiMap.StopPinSize)] = MapStopPinSize,
                [nameof(CustomMauiMap.VehiclePinSize)] = MapVehiclePinSize,
                [nameof(Microsoft.Maui.Maps.IMap.IsTrafficEnabled)] = MapIsTrafficEnabled
            };

        private static class BitmapCache
        {
            private static readonly ConcurrentDictionary<BitmapCacheKey, BitmapDescriptor> _descriptorCache = new();
            public static BitmapDescriptor GetOrAddDescriptor(BitmapCacheKey key, Func<BitmapCacheKey, Bitmap> factory)
            {
                return _descriptorCache.GetOrAdd(key, k => 
                {
                    using var bitmap = factory(k);
                    var descriptor = BitmapDescriptorFactory.FromBitmap(bitmap);
                    if (bitmap != null && !bitmap.IsRecycled)
                    {
                        bitmap.Recycle();
                    }
                    return descriptor;
                });
            }
        }

        private readonly Dictionary<string, BitmapCacheKey> _markerIconKeys = new();

        private static void MapIsTrafficEnabled(CustomMapHandler handler, CustomMauiMap map)
        {
            handler._googleMap?.TrafficEnabled = map.IsTrafficEnabled;
        }

        private static void MapVehicles(CustomMapHandler handler, CustomMauiMap map)
            => handler.UpdateVehicleMarkers();

        private static void MapShapes(CustomMapHandler handler, CustomMauiMap map)
            => handler.UpdateShapes();

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
            if (!(Math.Abs(currentBearing - (float)map.MapBearing) > 0.1)) return;
            var cameraUpdate = CameraUpdateFactory.NewCameraPosition(
                new CameraPosition.Builder(handler._googleMap.CameraPosition)
                    .Bearing((float)map.MapBearing)
                    .Build());
            handler._googleMap.AnimateCamera(cameraUpdate);
        }


        private GoogleMap? _googleMap;
        private readonly List<Polyline> _activeRoutePolylines = [];
        private readonly CustomMapCallback _mapCallback;
        private Location? _pendingLocation;

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
            UpdateShapes();
        }

        private async void UpdateVehicleMarkers()
        {
            if ( _googleMap is null || VirtualView is not CustomMauiMap mauiMap) return;

            try
            {
                var context = Platform.CurrentActivity;

                var vehicleData = await Task.Run(() =>
                {
                    var info = new List<(string Label, LatLng Pos, BitmapDescriptor Icon, BitmapCacheKey Key)>();
                    foreach (var v in mauiMap.Vehicles)
                    {
                        if (!v.Latitude.HasValue || !v.Longitude.HasValue || v.Label is null) continue;

                        var key = new BitmapCacheKey(v.VehicleType.Value, v.RouteShortName ?? "",
                            ColorManagement.NormalizeColorHex(v.RouteColor ?? "#000000"), v.Direction, false,
                            mauiMap.VehiclePinSize);
                        var descriptor = BitmapCache.GetOrAddDescriptor(key, _ => 
                            MapBitmapFactory.CreateCustomPinBitmap(
                                context,
                                v.VehicleType.Value, 
                                v.RouteShortName, 
                                v.RouteColor, 
                                v.Direction, 
                                mauiMap.VehiclePinSize));

                        info.Add((v.Label, new LatLng(v.Latitude.Value, v.Longitude.Value), descriptor, key));
                    }

                    return info;
                });

                var visibleVehicleKeys = new HashSet<string>(_vehicleMarkers.Keys);

                foreach (var (vehicleId, pos, icon, key) in vehicleData)
                {
                    string expectedTag = $"vehicle_{vehicleId}";

                    if (_vehicleMarkers.TryGetValue(vehicleId, out var existingMarker))
                    {
                        existingMarker.Position = pos;
        
                        if (existingMarker.Tag == null) 
                            existingMarker.Tag = expectedTag;

                        if (!_markerIconKeys.TryGetValue(vehicleId, out var oldKey) || !oldKey.Equals(key))
                        {
                            existingMarker.SetIcon(icon);
                            _markerIconKeys[vehicleId] = key;
                        }
                        visibleVehicleKeys.Remove(vehicleId);
                    }
                    else
                    {
                        var markerOptions = new MarkerOptions()
                            .SetPosition(pos)
                            .SetIcon(icon);

                        var newMarker = _googleMap.AddMarker(markerOptions);
                        if (newMarker == null) continue;
        
                        // Setting the tag here for new markers
                        newMarker.Tag = expectedTag;
        
                        _vehicleMarkers[vehicleId] = newMarker;
                        _markerIconKeys[vehicleId] = key;
                    }
                }

                // Cleanup removed vehicles
                foreach (var keyToRemove in visibleVehicleKeys)
                {
                    _vehicleMarkers[keyToRemove].Remove();
                    _vehicleMarkers.Remove(keyToRemove);
                    _markerIconKeys.Remove(keyToRemove);
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating vehicle markers: {e.Message}");
            }
        }

        private void UpdateStopMarkers()
        {
            if (_googleMap is null || VirtualView is not CustomMauiMap mauiMap) return;

            // 1. Handle the "Hide Stops" case efficiently
            if (!mauiMap.ShowStops)
            {
                foreach (var marker in _stopMarkers.Values) 
                {
                    marker.Remove();
                    marker.Dispose();
                }
                _stopMarkers.Clear();
                return;
            }

            var context = Platform.CurrentActivity;
    
            // 2. Track which stops should be visible
            var currentStopIds = mauiMap.Stops.Select(s => s.StopId).ToHashSet();

            // 3. Remove markers that are no longer in the data
            var idsToRemove = _stopMarkers.Keys.Where(id => !currentStopIds.Contains(id)).ToList();
            foreach (var id in idsToRemove)
            {
                _stopMarkers[id].Remove();
                _stopMarkers[id].Dispose();
                _stopMarkers.Remove(id);
            }

            // 4. Add ONLY new markers
            foreach (var stop in mauiMap.Stops)
            {
                if (_stopMarkers.ContainsKey(stop.StopId)) continue; // Already on map
                if (string.IsNullOrWhiteSpace(stop.StopName)) continue;

                var key = new BitmapCacheKey(VehicleType.Bus, stop.StopName, "#000088", null, isStopIcon: true, mauiMap.StopPinSize);
        
                // Get the DESCRIPTOR directly from the cache
                var descriptor = BitmapCache.GetOrAddDescriptor(key, 
                    _ => MapBitmapFactory.CreateStopPinBitmap(context, mauiMap.StopPinSize));

                var markerOptions = new MarkerOptions()
                    .SetPosition(new LatLng(stop.StopLat, stop.StopLon))
                    .SetTitle($"Stație: {stop.StopName}")
                    .SetIcon(descriptor); // Use the descriptor directly!

                var newMarker = _googleMap.AddMarker(markerOptions);
                if (newMarker == null) continue;
        
                newMarker.Tag = $"stop_{stop.StopId}";
                _stopMarkers[stop.StopId] = newMarker;
            }
        }

        private void UpdateShapes()
        {
            if (_googleMap is null || VirtualView is not CustomMauiMap mauiMap) return;
            foreach (var poly in _activeRoutePolylines)
            {
                poly.Remove();
            }

            _activeRoutePolylines.Clear();
            if (mauiMap.Shapes.Count == 0) return;

            var firstVehicle = mauiMap.Vehicles.FirstOrDefault();
            var routeColorHex = ColorManagement.NormalizeColorHex(firstVehicle?.RouteColor ?? "#4A90E2");
            int color;
            try
            {
                color = Color.ParseColor(routeColorHex.StartsWith('#') ? routeColorHex : '#' + routeColorHex);
            }
            catch
            {
                color = Color.Blue;
            }

            var shapeGroups = mauiMap.Shapes
                .GroupBy(s => s.ShapeId)
                .ToList();

            foreach (var group in shapeGroups)
            {
                var options = new PolylineOptions()
                    .InvokeWidth(10f)
                    .InvokeColor(color)
                    .InvokeZIndex(1);
                var sortedPoints = group.OrderBy(s => s.ShapePtSequence);

                foreach (var shape in sortedPoints)
                {
                    options.Add(new LatLng(shape.ShapePtLat, shape.ShapePtLon));
                }

                var polyline = _googleMap.AddPolyline(options);
                _activeRoutePolylines.Add(polyline);
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