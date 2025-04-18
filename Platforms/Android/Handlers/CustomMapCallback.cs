using Android.Graphics;
using IvanConnections_Travel.Shared.Cache;
using Map = Microsoft.Maui.Controls.Maps.Map;
using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using Android.Content.Res;
using System.Collections.Concurrent;
using IvanConnections_Travel.Utils;
using CommunityToolkit.Mvvm.Messaging;
using IvanConnections_Travel.Messages;
using IvanConnections_Travel.Models.Enums;

namespace IvanConnections_Travel.Platforms.Handlers
{
    public partial class CustomMapCallback : Java.Lang.Object, IOnMapReadyCallback
    {
        private readonly Map _mauiMap;
        private static readonly ConcurrentDictionary<BitmapCacheKey, Bitmap> _bitmapCache = new();
        public CustomMapCallback(Map mauiMap)
        {
            _mauiMap = mauiMap;
        }

        public void OnMapReady(GoogleMap googleMap)
        {
            try
            {
                googleMap.TrafficEnabled = true;
                googleMap.UiSettings.ZoomControlsEnabled = false;
                bool success = googleMap.SetMapStyle(
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

            googleMap.MarkerClick += (s, e) =>
            {
                if (e.Marker?.Tag?.ToString() is string label)
                {
                    var pinData = MapPinCache.Pins.FirstOrDefault(p => p.Label == label);

                    if (pinData != null && pinData.LocalTimestamp.HasValue && pinData.VehicleType.HasValue)
                    {
                        string vehicleTypeName = Translations.GetVehicleTypeNameInRomanian(pinData.VehicleType.Value);
                        string timeText = TimeFormatUtils.FormatTimeDifferenceInRomanian(pinData.LocalTimestamp.Value);
                        string message = $"Cod {vehicleTypeName}: {label}, actualizat acum {timeText}";
                        WeakReferenceMessenger.Default.Send(new ClickMessage(pinData));
                        WeakReferenceMessenger.Default.Send(new ShowToastMessage(message));
                    }
                    else
                    {
                        WeakReferenceMessenger.Default.Send(new ShowToastMessage(label));
                    }
                }

                e.Handled = false;
            };
            googleMap.MapClick += (s, e) =>
            {
                WeakReferenceMessenger.Default.Send(new ClickMessage(null));
            };
            Task.Run(() =>
            {
                var markers = PrepareMarkers();

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    googleMap.Clear();
                    foreach (var (markerOptions, label, type) in markers)
                    {
                        var marker = googleMap.AddMarker(markerOptions) ?? throw new InvalidOperationException("Marker is null.");
                        marker.Tag = label;
                    }
                });
            });
        }

        private static List<(MarkerOptions marker, string label, MarkerType markerType)> PrepareMarkers()
        {
            var context = Platform.CurrentActivity ?? Android.App.Application.Context;
            var markers = new List<(MarkerOptions, string, MarkerType)>();

            // Vehicles
            foreach (var pinData in MapPinCache.Pins)
            {
                if (!pinData.Latitude.HasValue || !pinData.Longitude.HasValue || !pinData.VehicleType.HasValue)
                    continue;

                var markerOptions = new MarkerOptions();
                markerOptions.SetTitle($"Cod {Translations.GetVehicleTypeNameInRomanian(pinData.VehicleType.Value)} {pinData.RouteShortName}: {pinData.Label}");
                markerOptions.SetSnippet(pinData.IsElectricBus ? "Autobuz electric" : pinData.IsNewTram ? "Tramvai nou" : null);
                markerOptions.SetPosition(new LatLng(pinData.Latitude.Value, pinData.Longitude.Value));

                string colorValue = ColorManagement.NormalizeColorHex(pinData.RouteColor ?? "#000000");
                var key = new BitmapCacheKey(pinData.VehicleType.Value, pinData.RouteShortName ?? "", colorValue, pinData.Direction, isStop: false);
                var bitmap = _bitmapCache.GetOrAdd(key, _ => MapBitmapFactory.CreateCustomPinBitmap(context, pinData.VehicleType.Value, pinData.RouteShortName, colorValue, pinData.Direction));

                markerOptions.SetIcon(BitmapDescriptorFactory.FromBitmap(bitmap));
                markers.Add((markerOptions, pinData.Label ?? "", MarkerType.Vehicle));
            }

            // Stops
            foreach (var stop in MapPinCache.Stops)
            {
                if (string.IsNullOrWhiteSpace(stop.StopName))
                    continue;

                var markerOptions = new MarkerOptions();
                markerOptions.SetTitle($"Stație: {stop.StopName}");
                markerOptions.SetSnippet("Stație de transport");
                markerOptions.SetPosition(new LatLng(stop.StopLat, stop.StopLon));

                var key = new BitmapCacheKey(VehicleType.Bus, stop.StopName, "#000088", null, isStop: true);
                var bitmap = _bitmapCache.GetOrAdd(key, _ => MapBitmapFactory.CreateStopPinBitmap(context, stop.StopName));

                markerOptions.SetIcon(BitmapDescriptorFactory.FromBitmap(bitmap));
                markers.Add((markerOptions, stop.StopName, MarkerType.Stop));
            }

            return markers;
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
}