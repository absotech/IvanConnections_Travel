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
        #region Sizes and Proportions
        
        private static float VehiclePinOverlayScale = 0.48f;
        private static float VehiclePinPaddingScale = 0.20f;
        private static float VehiclePinTextScale = 0.40f;
        private static float VehiclePinTextPaddingScale = 0.08f;
        private static float VehiclePinCornerRadiusScale = 0.12f;

        // Number-only marker proportions
        private static float NumberOnlyTextScale = 0.70f;
        private static float NumberOnlyTextPaddingScale = 0.22f;
        private static float NumberOnlyCornerRadiusScale = 0.22f;
        private static float NumberOnlyBorderScale = 0.06f;

        #endregion

        private static readonly ConcurrentDictionary<BitmapCacheKey, Bitmap> BitmapCache = new();

        public static void ClearBitmapCache()
        {
            foreach (var bitmap in BitmapCache.Values)
            {
                bitmap?.Recycle();
            }
            BitmapCache.Clear();
        }
        public static Bitmap CreateStopPinBitmap(Context context, int stopPinSize)
        {
            var density = context.Resources?.DisplayMetrics?.Density ?? 1.0f;
            var size = (int)(stopPinSize * density);
            var baseBitmap = BitmapFactory.DecodeResource(context.Resources, Resource.Drawable.bus_stop)
                             ?? throw new InvalidOperationException($"Failed to decode resource for icon type: {Resource.Drawable.bus_stop}");

            var scaledBitmap = Bitmap.CreateScaledBitmap(baseBitmap, size, size, true);
            baseBitmap.Recycle();
            var resultWidth = scaledBitmap.Width;
            var resultHeight = scaledBitmap.Height;

            var resultBitmap = Bitmap.CreateBitmap(resultWidth, resultHeight, Bitmap.Config.Argb8888);

            using var canvas = new Canvas(resultBitmap);
            var paint = new Paint();
            paint.SetColorFilter(new PorterDuffColorFilter(Color.White, PorterDuff.Mode.SrcIn));
            canvas.DrawBitmap(scaledBitmap, 0, 0, paint);

            return resultBitmap;
        }

        public static Bitmap CreateCustomPinBitmap(Context context, VehicleType iconType, string label, string colorHex, double? bearing, int vehiclePinSize, bool numberOnly = false)
        {
            var density = context.Resources?.DisplayMetrics?.Density ?? 1.0f;
            var baseIconSize = (int)(vehiclePinSize * density);
            var overlaySize = (int)(baseIconSize * VehiclePinOverlayScale);
            var padding = (int)(baseIconSize * VehiclePinPaddingScale);
            var fontSize = (int)(baseIconSize * VehiclePinTextScale);
            var textPadding = (int)(baseIconSize * VehiclePinTextPaddingScale);
            var cornerRadius = (int)(baseIconSize * VehiclePinCornerRadiusScale);

            if (numberOnly)
            {
                return CreateNumberOnlyPinBitmap(context, label, colorHex, bearing, baseIconSize, overlaySize, padding);
            }

            var resultWidth = baseIconSize + overlaySize + padding;
            var resultHeight = baseIconSize + overlaySize + padding;

            var resultBitmap = Bitmap.CreateBitmap(resultWidth, resultHeight, Bitmap.Config.Argb8888);
            using var canvas = new Canvas(resultBitmap);
            var iconPaint = new Paint { AntiAlias = true };
            try
            {
                var tintColor = Color.ParseColor(colorHex);
                iconPaint.SetColorFilter(new PorterDuffColorFilter(tintColor, PorterDuff.Mode.SrcIn));
            }
            catch { iconPaint.SetColorFilter(new PorterDuffColorFilter(Color.Black, PorterDuff.Mode.SrcIn)); }

            var textPaint = new Paint { Color = ColorManagement.IsColorDark(iconPaint.Color) ? Color.Yellow : Color.White, TextSize = fontSize, AntiAlias = true, TextAlign = Paint.Align.Center };
            var bgPaint = new Paint { Color = Color.Argb(170, 0, 0, 0), AntiAlias = true };
            var iconResId = iconType == VehicleType.Bus ? Resource.Drawable.bus_icon : Resource.Drawable.tram_icon;
            using (var baseBitmap = BitmapFactory.DecodeResource(context.Resources, iconResId))
            using (var scaledBitmap = Bitmap.CreateScaledBitmap(baseBitmap, baseIconSize, baseIconSize, true))
            {
                var iconX = (resultWidth - baseIconSize) / 2f;
                var iconY = (resultHeight - baseIconSize) / 2f;
                canvas.DrawBitmap(scaledBitmap, iconX, iconY, iconPaint);
            }
            var textX = resultWidth / 2f;
            var textY = resultHeight / 2f - ((textPaint.Descent() + textPaint.Ascent()) / 2);
            Rect textBounds = new();
            textPaint.GetTextBounds(label, 0, label.Length, textBounds);

            var bgRect = new RectF(textX - textBounds.Width() / 2f - textPadding, textY + textBounds.Top - textPadding, textX + textBounds.Width() / 2f + textPadding, textY + textBounds.Bottom + textPadding);
            canvas.DrawRoundRect(bgRect, cornerRadius, cornerRadius, bgPaint);
            canvas.DrawText(label, textX, textY, textPaint);

            var isStopped = bearing is null or < 0;
            var overlayResId = isStopped ? Resource.Drawable.stop_icon : Resource.Drawable.arrow_icon_notfilled;
            var overlayPaint = new Paint { AntiAlias = true };
            overlayPaint.SetColorFilter(new PorterDuffColorFilter(isStopped ? Color.Red : Color.White, PorterDuff.Mode.SrcIn));

            using (var overlayBase = BitmapFactory.DecodeResource(context.Resources, overlayResId))
            using (var scaledOverlay = Bitmap.CreateScaledBitmap(overlayBase, overlaySize, overlaySize, true))
            {
                const float overlayX = 0;
                const float overlayY = 0;

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

        private static Bitmap CreateNumberOnlyPinBitmap(Context context, string label, string colorHex, double? bearing, int baseIconSize, int overlaySize, int padding)
        {
            label ??= string.Empty;

            Color tintColor;
            try { tintColor = Color.ParseColor(colorHex); }
            catch { tintColor = Color.Black; }

            var fontSize = baseIconSize * NumberOnlyTextScale;
            var textPadding = (int)(baseIconSize * NumberOnlyTextPaddingScale);
            var cornerRadius = baseIconSize * NumberOnlyCornerRadiusScale;
            var borderWidth = Math.Max(1f, baseIconSize * NumberOnlyBorderScale);

            var contrastColor = ColorManagement.IsColorDark(tintColor) ? Color.White : Color.Black;
            var textPaint = new Paint
            {
                Color = contrastColor,
                TextSize = fontSize,
                AntiAlias = true,
                TextAlign = Paint.Align.Center,
                FakeBoldText = true
            };

            Rect textBounds = new();
            textPaint.GetTextBounds(label, 0, label.Length, textBounds);

            var badgeWidth = Math.Max(baseIconSize, textBounds.Width() + textPadding * 2);
            var badgeHeight = Math.Max((int)(baseIconSize * 0.72f), textBounds.Height() + textPadding * 2);

            var resultWidth = badgeWidth + overlaySize + padding;
            var resultHeight = badgeHeight + overlaySize + padding;

            var resultBitmap = Bitmap.CreateBitmap(resultWidth, resultHeight, Bitmap.Config.Argb8888);
            using var canvas = new Canvas(resultBitmap);

            var badgeLeft = (resultWidth - badgeWidth) / 2f;
            var badgeTop = (resultHeight - badgeHeight) / 2f;
            var badgeRect = new RectF(badgeLeft, badgeTop, badgeLeft + badgeWidth, badgeTop + badgeHeight);

            var bgPaint = new Paint { Color = tintColor, AntiAlias = true };
            canvas.DrawRoundRect(badgeRect, cornerRadius, cornerRadius, bgPaint);

            // Contrasting border so the badge stays distinguishable on any map background.
            var borderPaint = new Paint { Color = contrastColor, AntiAlias = true, StrokeWidth = borderWidth };
            borderPaint.SetStyle(Paint.Style.Stroke);
            var borderRect = new RectF(badgeRect.Left + borderWidth / 2f, badgeRect.Top + borderWidth / 2f,
                badgeRect.Right - borderWidth / 2f, badgeRect.Bottom - borderWidth / 2f);
            canvas.DrawRoundRect(borderRect, cornerRadius, cornerRadius, borderPaint);

            var textX = resultWidth / 2f;
            var textY = badgeTop + badgeHeight / 2f - (textPaint.Descent() + textPaint.Ascent()) / 2f;
            canvas.DrawText(label, textX, textY, textPaint);

            var isStopped = bearing is null or < 0;
            var overlayResId = isStopped ? Resource.Drawable.stop_icon : Resource.Drawable.arrow_icon_notfilled;
            var overlayPaint = new Paint { AntiAlias = true };
            overlayPaint.SetColorFilter(new PorterDuffColorFilter(isStopped ? Color.Red : Color.White, PorterDuff.Mode.SrcIn));

            using (var overlayBase = BitmapFactory.DecodeResource(context.Resources, overlayResId))
            using (var scaledOverlay = Bitmap.CreateScaledBitmap(overlayBase, overlaySize, overlaySize, true))
            {
                const float overlayX = 0;
                const float overlayY = 0;

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
        private VehicleType VehicleType { get; }
        private string RouteShortName { get; }
        private string ColorValue { get; }
        private int RoundedBearing { get; }
        private bool IsStopped { get; }
        private bool IsStopIcon { get; }
        private int Size { get; }
        private bool NumberOnly { get; }

        public BitmapCacheKey(VehicleType vehicleType, string routeShortName, string colorValue, double? bearing, bool isStopIcon, int size, bool numberOnly = false)
        {
            VehicleType = vehicleType;
            RouteShortName = routeShortName ?? string.Empty;
            ColorValue = colorValue ?? string.Empty;
            IsStopIcon = isStopIcon;
            IsStopped = !bearing.HasValue || bearing.Value < 0;
            RoundedBearing = IsStopped ? -1 : (int)Math.Round(bearing!.Value);
            Size = size;
            NumberOnly = numberOnly;
        }

        public override int GetHashCode() => HashCode.Combine(VehicleType, RouteShortName, ColorValue, RoundedBearing, IsStopped, IsStopIcon, Size, NumberOnly);

        public bool Equals(BitmapCacheKey other)
        {
            return VehicleType == other.VehicleType &&
                   RouteShortName == other.RouteShortName &&
                   ColorValue == other.ColorValue &&
                   RoundedBearing == other.RoundedBearing &&
                   IsStopIcon == other.IsStopIcon &&
                   IsStopped == other.IsStopped &&
                   Size == other.Size &&
                   NumberOnly == other.NumberOnly;
        }

        public override bool Equals(object? obj) => obj is BitmapCacheKey key && Equals(key);
        
    }

}
