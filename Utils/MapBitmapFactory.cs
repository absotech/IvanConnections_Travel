using Android.Content;
using Android.Graphics;
using IvanConnections_Travel.Models.Enums;
using System.Collections.Concurrent;
using Color = Android.Graphics.Color;
using Paint = Android.Graphics.Paint;
using Rect = Android.Graphics.Rect;
using RectF = Android.Graphics.RectF;
using PorterDuffMode = Android.Graphics.PorterDuff.Mode;



namespace IvanConnections_Travel.Utils
{
    public static class MapBitmapFactory
    {
        private static readonly ConcurrentDictionary<BitmapCacheKey, Bitmap> _bitmapCache = new();

        public static Bitmap GetOrCreateMapPin(
            Context context,
            VehicleType vehicleType,
            string routeShortName,
            string colorHex,
            double? direction)
        {
            string colorValue = ColorManagement.NormalizeColorHex(colorHex);
            var key = new BitmapCacheKey(vehicleType, routeShortName, colorValue, direction);

            return _bitmapCache.GetOrAdd(key, _ => CreateCustomPinBitmap(
                context, vehicleType, routeShortName, colorValue, direction));
        }

        public static void ClearBitmapCache()
        {
            foreach (var bitmap in _bitmapCache.Values)
            {
                bitmap?.Recycle();
            }
            _bitmapCache.Clear();
        }

        public static Bitmap CreateCustomPinBitmap(Context context, VehicleType iconType, string label, string colorHex, double? bearing)
        {
            int iconResId = iconType == VehicleType.Bus
                ? Resource.Drawable.bus_icon
                : Resource.Drawable.tram_icon;

            var baseBitmap = BitmapFactory.DecodeResource(context.Resources, iconResId) ?? throw new InvalidOperationException($"Failed to decode resource for icon type: {iconType}");
            var scaledBitmap = Bitmap.CreateScaledBitmap(baseBitmap, 128, 128, true);
            baseBitmap.Recycle();

            // Set arrow/stop icon size to 40
            int overlaySize = 60;

            // Ensure enough space in the bitmap for the main icon plus the arrow/stop outside it
            int padding = 20; // Space between main icon and arrow/stop
            int resultWidth = scaledBitmap.Width + overlaySize + padding;
            int resultHeight = scaledBitmap.Height + overlaySize + padding;

            var resultBitmap = Bitmap.CreateBitmap(resultWidth, resultHeight, Bitmap.Config.Argb8888 ?? throw new InvalidOperationException("Bitmap.Config.Argb8888 is null."));
            var canvas = new Canvas(resultBitmap);

            // Apply color tint to icon
            var tintedIcon = Bitmap.CreateBitmap(scaledBitmap.Width, scaledBitmap.Height, Bitmap.Config.Argb8888);
            var iconCanvas = new Canvas(tintedIcon);

            var colorPaint = new Paint { AntiAlias = true };
            try
            {
                var tintColor = Color.ParseColor(colorHex);
                colorPaint.SetColorFilter(new PorterDuffColorFilter(tintColor, PorterDuffMode.SrcIn ?? throw new InvalidOperationException("Selected PorterDuffMode does not exist.")));
            }
            catch
            {
                colorPaint.SetColorFilter(new PorterDuffColorFilter(Color.Black, PorterDuffMode.SrcIn ?? throw new InvalidOperationException("Selected PorterDuffMode does not exist.")));
            }

            iconCanvas.DrawBitmap(scaledBitmap, 0, 0, colorPaint);
            scaledBitmap.Recycle();

            // Draw colored icon in the center of the canvas
            float iconX = (resultWidth - tintedIcon.Width) / 2f;
            float iconY = (resultHeight - tintedIcon.Height) / 2f;
            canvas.DrawBitmap(tintedIcon, iconX, iconY, null);
            tintedIcon.Recycle();

            // Draw label background + text
            var textPaint = new Paint
            {
                Color = Color.White,
                TextSize = 40,
                AntiAlias = true,
                TextAlign = Paint.Align.Center
            };

            if (ColorManagement.IsColorDark(colorPaint.Color))
                textPaint.Color = Color.Yellow;

            float textX = resultBitmap.Width / 2f;
            float textY = resultBitmap.Height / 2f - ((textPaint.Descent() + textPaint.Ascent()) / 2);

            Rect textBounds = new();
            textPaint.GetTextBounds(label, 0, label.Length, textBounds);

            int textPadding = 8;
            float bgLeft = textX - textBounds.Width() / 2f - textPadding;
            float bgTop = textY + textBounds.Top - textPadding;
            float bgRight = textX + textBounds.Width() / 2f + textPadding;
            float bgBottom = textY + textBounds.Bottom + textPadding;

            var bgPaint = new Paint
            {
                Color = Color.Argb(170, 0, 0, 0),
                AntiAlias = true
            };

            canvas.DrawRoundRect(new RectF(bgLeft, bgTop, bgRight, bgBottom), 12, 12, bgPaint);
            canvas.DrawText(label, textX, textY, textPaint);

            // Draw direction or stop icon
            if (bearing.HasValue)
            {
                int iconSize = overlaySize;
                Bitmap? overlayBase;

                if (bearing.Value >= 0)
                {
                    overlayBase = BitmapFactory.DecodeResource(context.Resources, Resource.Drawable.arrow_icon_notfilled);
                }
                else
                {
                    overlayBase = BitmapFactory.DecodeResource(context.Resources, Resource.Drawable.stop_icon);
                }

                if (overlayBase == null)
                {
                    throw new InvalidOperationException("Failed to decode resource for overlay icon.");
                }

                var scaledOverlay = Bitmap.CreateScaledBitmap(overlayBase, iconSize, iconSize, true);
                overlayBase.Recycle();

                var coloredOverlay = Bitmap.CreateBitmap(iconSize, iconSize, Bitmap.Config.Argb8888);
                var overlayCanvas = new Canvas(coloredOverlay);

                var overlayPaint = new Paint { AntiAlias = true };
                overlayPaint.SetColorFilter(new PorterDuffColorFilter(
                    bearing.Value >= 0 ? Color.ParseColor("#FFFFFF") : Color.Red,
                    PorterDuffMode.SrcIn));

                overlayCanvas.DrawBitmap(scaledOverlay, 0, 0, overlayPaint);
                scaledOverlay.Recycle();

                if (bearing.Value >= 0)
                {
                    // Position the arrow at the top-left corner of the bitmap, outside the main icon
                    int arrowX = 0;
                    int arrowY = 0;

                    // Save canvas state before rotation
                    canvas.Save();

                    // Rotate around the center of the arrow
                    canvas.Rotate((float)bearing.Value, arrowX + iconSize / 2f, arrowY + iconSize / 2f);

                    canvas.DrawBitmap(coloredOverlay, arrowX, arrowY, null);

                    // Restore canvas
                    canvas.Restore();
                }
                else
                {
                    // For stop icon, position it at the top-left corner
                    canvas.DrawBitmap(coloredOverlay, 0, 0, null);
                }

                coloredOverlay.Recycle();
            }

            return resultBitmap;
        }
    }

    public readonly struct BitmapCacheKey : IEquatable<BitmapCacheKey>
    {
        public VehicleType VehicleType { get; }
        public string RouteShortName { get; }
        public string ColorValue { get; }
        public string DirectionKey { get; }

        public BitmapCacheKey(VehicleType vehicleType, string routeShortName, string colorValue, double? direction)
        {
            VehicleType = vehicleType;
            RouteShortName = routeShortName ?? string.Empty;
            ColorValue = colorValue ?? string.Empty;
            DirectionKey = direction.HasValue ?
                (direction.Value >= 0 ? "d" + Math.Round(direction.Value) : "s") : "x";
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(VehicleType, RouteShortName, ColorValue, DirectionKey);
        }

        public bool Equals(BitmapCacheKey other)
        {
            return VehicleType == other.VehicleType &&
                   RouteShortName == other.RouteShortName &&
                   ColorValue == other.ColorValue &&
                   DirectionKey == other.DirectionKey;
        }

        public override bool Equals(object? obj)
        {
            return obj is BitmapCacheKey key && Equals(key);
        }

        public override string ToString()
        {
            return $"{VehicleType}_{RouteShortName}_{ColorValue}_{DirectionKey}";
        }
        public static bool operator ==(BitmapCacheKey left, BitmapCacheKey right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(BitmapCacheKey left, BitmapCacheKey right)
        {
            return !(left == right);
        }
    }
}
