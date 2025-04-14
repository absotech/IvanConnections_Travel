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
                        WeakReferenceMessenger.Default.Send(new ShowToastMessage(message));
                        WeakReferenceMessenger.Default.Send(new ClickMessage(pinData));
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
                    foreach (var (markerOptions, label) in markers)
                    {
                        var marker = googleMap.AddMarker(markerOptions) ?? throw new InvalidOperationException("Marker is null.");
                        marker.Tag = label;
                    }
                });
            });
        }

        private static List<(MarkerOptions marker, string label)> PrepareMarkers()
        {
            var context = Platform.CurrentActivity ?? Android.App.Application.Context;
            var markers = new List<(MarkerOptions, string)>();

            foreach (var pinData in MapPinCache.Pins)
            {
                if (!pinData.Latitude.HasValue || !pinData.Longitude.HasValue || !pinData.VehicleType.HasValue)
                    continue;

                var markerOptions = new MarkerOptions();
                markerOptions.SetTitle($"Cod {Translations.GetVehicleTypeNameInRomanian(pinData.VehicleType.Value)} {pinData.RouteShortName}: {pinData.Label}");
                if (pinData.IsElectricBus)
                {
                    markerOptions.SetSnippet("Autobuz electric");
                }
                else if (pinData.IsNewTram)
                {
                    markerOptions.SetSnippet("Tramvai nou");
                }
                else
                {
                    markerOptions.SetSnippet(null);
                }
                markerOptions.SetPosition(new LatLng(pinData.Latitude.Value, pinData.Longitude.Value));

                string colorValue = ColorManagement.NormalizeColorHex(pinData.RouteColor ?? throw new InvalidOperationException($"RouteColor for {pinData.Id} is null."));
                string directionKey = pinData.Direction.HasValue ? (pinData.Direction.Value >= 0 ? "d" + Math.Round(pinData.Direction.Value) : "s") : "x";

                string bitmapKey = $"{pinData.VehicleType.Value}_{pinData.RouteShortName}_{colorValue}_{directionKey}";

                var key = new BitmapCacheKey(pinData.VehicleType.Value, pinData.RouteShortName ?? throw new InvalidOperationException($"Route Short name for {pinData.Id} is null."), colorValue, pinData.Direction);
                var bitmap = _bitmapCache.GetOrAdd(key, _ => MapBitmapFactory.CreateCustomPinBitmap(context, pinData.VehicleType.Value,
                    pinData.RouteShortName, colorValue, pinData.Direction));

                markerOptions.SetIcon(BitmapDescriptorFactory.FromBitmap(bitmap));
                markers.Add((markerOptions, pinData.Label ?? ""));
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