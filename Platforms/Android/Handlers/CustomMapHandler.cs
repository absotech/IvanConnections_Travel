using Android.Content.Res;
using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using Android.Graphics;
using Android.Nfc;
using CommunityToolkit.Mvvm.Messaging;
using IvanConnections_Travel.Controls;
using IvanConnections_Travel.Messages;
using IvanConnections_Travel.Models.Enums;
using IvanConnections_Travel.Utils;
using Microsoft.Maui.Maps.Handlers;
using System.Collections.Concurrent;

namespace IvanConnections_Travel.Platforms.Handlers
{
    /// <summary>
    /// The primary handler for the CustomMauiMap control on Android.
    /// It uses a PropertyMapper to react to changes in the custom BindableProperties
    /// (like Vehicles, Stops, ShowStops) and updates the native GoogleMap accordingly.
    /// </summary>
    public class CustomMapHandler : MapHandler
    {
        public static readonly IPropertyMapper<CustomMauiMap, CustomMapHandler> CustomMapper =
            new PropertyMapper<CustomMauiMap, CustomMapHandler>(Mapper)
            {
                [nameof(CustomMauiMap.Vehicles)] = MapVehicles,
                [nameof(CustomMauiMap.Stops)] = MapStops,
                [nameof(CustomMauiMap.ShowStops)] = MapStops
            };
        private static void MapVehicles(CustomMapHandler handler, CustomMauiMap map)
    => handler.UpdateVehicleMarkers();

        private static void MapStops(CustomMapHandler handler, CustomMauiMap map)
            => handler.UpdateStopMarkers();


        private GoogleMap? _googleMap;
        private readonly CustomMapCallback _mapCallback;

        private static readonly ConcurrentDictionary<BitmapCacheKey, Bitmap> _bitmapCache = [];

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
                _googleMap.TrafficEnabled = true;
                _googleMap.UiSettings.ZoomControlsEnabled = false;
                bool success = _googleMap.SetMapStyle(
                    MapStyleOptions.LoadRawResourceStyle(Platform.CurrentActivity ?? throw new InvalidOperationException("Context is null."), Resource.Raw.map_style));
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
                    if (!v.Latitude.HasValue || !v.Longitude.HasValue || !v.VehicleType.HasValue || v.Label is null) continue;

                    var key = new BitmapCacheKey(v.VehicleType.Value, v.RouteShortName ?? "", ColorManagement.NormalizeColorHex(v.RouteColor ?? "#000000"), v.Direction, false);
                    var bitmap = _bitmapCache.GetOrAdd(key, _ => MapBitmapFactory.CreateCustomPinBitmap(context, v.VehicleType.Value, v.RouteShortName, v.RouteColor, v.Direction));

                    vehicleInfo[v.Label] = (new LatLng(v.Latitude.Value, v.Longitude.Value), BitmapDescriptorFactory.FromBitmap(bitmap));
                }
                return vehicleInfo;
            });

            var visibleVehicleKeys = new HashSet<string>(_vehicleMarkers.Keys);

            foreach (var vehicle in vehicleData)
            {
                var vehicleId = vehicle.Key;
                if (_vehicleMarkers.TryGetValue(vehicleId, out var existingMarker))
                {
                    existingMarker.Position = vehicle.Value.Position;
                    existingMarker.SetIcon(vehicle.Value.Icon);
                    visibleVehicleKeys.Remove(vehicleId);
                }
                else
                {
                    var markerOptions = new MarkerOptions()
                        .SetPosition(vehicle.Value.Position)
                        .SetIcon(vehicle.Value.Icon);

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

                var key = new BitmapCacheKey(VehicleType.Bus, stop.StopName, "#000088", null, isStopIcon: true);
                var bitmap = _bitmapCache.GetOrAdd(key, _ => MapBitmapFactory.CreateStopPinBitmap(context, stop.StopName));
                markerOptions.SetIcon(BitmapDescriptorFactory.FromBitmap(bitmap));

                var newMarker = _googleMap.AddMarker(markerOptions);
                if (newMarker != null)
                {
                    newMarker.Tag = $"stop_{stop.StopId.ToString()}";
                    _stopMarkers[stop.StopId] = newMarker;
                }
            }
        }

        private void OnMarkerClick(object? sender, GoogleMap.MarkerClickEventArgs e)
        {
            if (VirtualView is not CustomMauiMap mauiMap || e.Marker?.Tag?.ToString() is not string tag)
                return;

            if (tag.StartsWith("vehicle_"))
            {
                var vehicleLabel = tag.Substring("vehicle_".Length);
                var vehicleData = mauiMap.Vehicles.FirstOrDefault(p => p.Label == vehicleLabel);
                if (vehicleData != null)
                {
                    if (vehicleData.LocalTimestamp.HasValue && vehicleData.VehicleType.HasValue)
                    {
                        string vehicleTypeName = Translations.GetVehicleTypeNameInRomanian(vehicleData.VehicleType.Value);
                        string timeText = TimeFormatUtils.FormatTimeDifferenceInRomanian(vehicleData.LocalTimestamp.Value);
                        string message = $"Cod {vehicleTypeName}: {vehicleLabel}, actualizat acum {timeText}";

                        WeakReferenceMessenger.Default.Send(new ClickMessage(vehicleData));
                        WeakReferenceMessenger.Default.Send(new ShowToastMessage(message));
                    }
                }
            }
            else if (tag.StartsWith("stop_"))
            {
                var stopIdString = tag.Substring("stop_".Length);
                if (int.TryParse(stopIdString, out int stopId))
                {
                    var stopData = mauiMap.Stops.FirstOrDefault(s => s.StopId == stopId);
                    if (stopData != null)
                    {
                        WeakReferenceMessenger.Default.Send(new StopClickMessage(stopData));
                    }
                }
            }
            else
            {
                WeakReferenceMessenger.Default.Send(new ShowToastMessage(tag));
            }

            e.Handled = false;
        }

        private void OnMapClick(object? sender, GoogleMap.MapClickEventArgs e)
        {
            WeakReferenceMessenger.Default.Send(new ClickMessage(null));
        }

        public static void ClearBitmapCache()
        {
            foreach (var bitmap in _bitmapCache.Values)
            {
                bitmap?.Recycle();
            }
            _bitmapCache.Clear();
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
        }
    }
}