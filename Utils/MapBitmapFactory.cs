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

        public static void ClearBitmapCache()
        {
            foreach (var bitmap in _bitmapCache.Values)
            {
                bitmap?.Recycle();
            }
            _bitmapCache.Clear();
        }
        public static Bitmap CreateStopPinBitmap(Context context, string label)
        {
            int size = 64;
            var baseBitmap = BitmapFactory.DecodeResource(context.Resources, Resource.Drawable.bus_stop)
                ?? throw new InvalidOperationException($"Failed to decode resource for icon type: {Resource.Drawable.bus_stop}");

            var scaledBitmap = Bitmap.CreateScaledBitmap(baseBitmap, size, size, true);
            baseBitmap.Recycle();
            int resultWidth = scaledBitmap.Width;
            int resultHeight = scaledBitmap.Height;

            var resultBitmap = Bitmap.CreateBitmap(resultWidth, resultHeight, Bitmap.Config.Argb8888);

            using (var canvas = new Canvas(resultBitmap))
            {
                var paint = new Paint();
                paint.SetColorFilter(new PorterDuffColorFilter(Color.White, PorterDuff.Mode.SrcIn));
                canvas.DrawBitmap(scaledBitmap, 0, 0, paint);
            }

            return resultBitmap;
        }

        public static Bitmap CreateCustomPinBitmap(Context context, VehicleType iconType, string label, string colorHex, double? bearing)
        {
            int baseIconSize = 128;
            int overlaySize = 60;
            int padding = 20;

            int resultWidth = baseIconSize + overlaySize + padding;
            int resultHeight = baseIconSize + overlaySize + padding;

            var resultBitmap = Bitmap.CreateBitmap(resultWidth, resultHeight, Bitmap.Config.Argb8888);
            using var canvas = new Canvas(resultBitmap);
            var iconPaint = new Paint { AntiAlias = true };
            try
            {
                var tintColor = Color.ParseColor(colorHex);
                iconPaint.SetColorFilter(new PorterDuffColorFilter(tintColor, PorterDuff.Mode.SrcIn));
            }
            catch { iconPaint.SetColorFilter(new PorterDuffColorFilter(Color.Black, PorterDuff.Mode.SrcIn)); }

            var textPaint = new Paint { Color = ColorManagement.IsColorDark(iconPaint.Color) ? Color.Yellow : Color.White, TextSize = 40, AntiAlias = true, TextAlign = Paint.Align.Center };
            var bgPaint = new Paint { Color = Color.Argb(170, 0, 0, 0), AntiAlias = true };
            int iconResId = iconType == VehicleType.Bus ? Resource.Drawable.bus_icon : Resource.Drawable.tram_icon;
            using (var baseBitmap = BitmapFactory.DecodeResource(context.Resources, iconResId))
            using (var scaledBitmap = Bitmap.CreateScaledBitmap(baseBitmap, baseIconSize, baseIconSize, true))
            {
                float iconX = (resultWidth - baseIconSize) / 2f;
                float iconY = (resultHeight - baseIconSize) / 2f;
                canvas.DrawBitmap(scaledBitmap, iconX, iconY, iconPaint);
            }
            float textX = resultWidth / 2f;
            float textY = resultHeight / 2f - ((textPaint.Descent() + textPaint.Ascent()) / 2);
            Rect textBounds = new();
            textPaint.GetTextBounds(label, 0, label.Length, textBounds);

            int textPadding = 8;
            var bgRect = new RectF(textX - textBounds.Width() / 2f - textPadding, textY + textBounds.Top - textPadding, textX + textBounds.Width() / 2f + textPadding, textY + textBounds.Bottom + textPadding);
            canvas.DrawRoundRect(bgRect, 12, 12, bgPaint);
            canvas.DrawText(label, textX, textY, textPaint);

            bool isStopped = !bearing.HasValue || bearing.Value < 0;
            int overlayResId = isStopped ? Resource.Drawable.stop_icon : Resource.Drawable.arrow_icon_notfilled;
            var overlayPaint = new Paint { AntiAlias = true };
            overlayPaint.SetColorFilter(new PorterDuffColorFilter(isStopped ? Color.Red : Color.White, PorterDuff.Mode.SrcIn));

            using (var overlayBase = BitmapFactory.DecodeResource(context.Resources, overlayResId))
            using (var scaledOverlay = Bitmap.CreateScaledBitmap(overlayBase, overlaySize, overlaySize, true))
            {
                float overlayX = 0;
                float overlayY = 0;

                if (!isStopped)
                {
                    canvas.Save();
                    canvas.Rotate((float)bearing!.Value, overlayX + overlaySize / 2f, overlayY + overlaySize / 2f);
                    canvas.DrawBitmap(scaledOverlay, overlayX, overlayY, overlayPaint);
                    canvas.Restore();
                }
                else
                {
                    canvas.DrawBitmap(scaledOverlay, overlayX, overlayY, overlayPaint);
                }
            }

            return resultBitmap;
        }
    }

    public readonly struct BitmapCacheKey : IEquatable<BitmapCacheKey>
    {
        public VehicleType VehicleType { get; }
        public string RouteShortName { get; }
        public string ColorValue { get; }
        public int RoundedBearing { get; }
        public bool IsStopped { get; }
        public bool IsStopIcon { get; }

        public BitmapCacheKey(VehicleType vehicleType, string routeShortName, string colorValue, double? bearing, bool isStopIcon)
        {
            VehicleType = vehicleType;
            RouteShortName = routeShortName ?? string.Empty;
            ColorValue = colorValue ?? string.Empty;
            IsStopIcon = isStopIcon;
            IsStopped = !bearing.HasValue || bearing.Value < 0;
            RoundedBearing = IsStopped ? -1 : (int)Math.Round(bearing!.Value);
        }

        public override int GetHashCode() => HashCode.Combine(VehicleType, RouteShortName, ColorValue, RoundedBearing, IsStopped, IsStopIcon);

        public bool Equals(BitmapCacheKey other)
        {
            return VehicleType == other.VehicleType &&
                   RouteShortName == other.RouteShortName &&
                   ColorValue == other.ColorValue &&
                   RoundedBearing == other.RoundedBearing &&
                   IsStopIcon == other.IsStopIcon &&
                   IsStopped == other.IsStopped;
        }

        public override bool Equals(object? obj) => obj is BitmapCacheKey key && Equals(key);
    }

}
