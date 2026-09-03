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
        private static float VehiclePinArrowGapScale = 0.02f;
        private static float VehiclePinTextScale = 0.40f;
        private static float VehiclePinTextPaddingScale = 0.08f;
        private static float VehiclePinCornerRadiusScale = 0.12f;

        // Color used to fill the direction arrow overlay. Change this to recolor the arrow.
        private static Color VehiclePinArrowColor = Color.White;

        // Margin/outline drawn behind the direction arrow so it stays visible on any map
        // background. Change VehiclePinArrowMarginColor to recolor it and
        // VehiclePinArrowMarginScale (fraction of the arrow size) to change its thickness.
        private static Color VehiclePinArrowMarginColor = Color.Black;
        private static float VehiclePinArrowMarginScale = 0.12f;

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

        public static VehiclePinBitmap CreateCustomPinBitmap(Context context, VehicleType iconType, string label, string colorHex, double? bearing, int vehiclePinSize, bool numberOnly = false, bool isStale = false)
        {
            var density = context.Resources?.DisplayMetrics?.Density ?? 1.0f;
            var baseIconSize = (int)(vehiclePinSize * density);
            var overlaySize = (int)(baseIconSize * VehiclePinOverlayScale);
            var padding = (int)(baseIconSize * VehiclePinPaddingScale);
            var gap = Math.Max(1, (int)(baseIconSize * VehiclePinArrowGapScale));
            var fontSize = (int)(baseIconSize * VehiclePinTextScale);
            var textPadding = (int)(baseIconSize * VehiclePinTextPaddingScale);
            var cornerRadius = (int)(baseIconSize * VehiclePinCornerRadiusScale);

            if (numberOnly)
            {
                return CreateNumberOnlyPinBitmap(context, label, colorHex, bearing, baseIconSize, overlaySize, padding, gap, isStale);
            }

            var resultWidth = baseIconSize + overlaySize + padding;
            // The vehicle icon sits on top and the direction arrow (or stop marker) is placed
            // directly beneath it, so the arrow can be anchored exactly on the vehicle's location.
            var resultHeight = baseIconSize + gap + overlaySize;

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
            var iconX = (resultWidth - baseIconSize) / 2f;
            const float iconY = 0f;
            using (var baseBitmap = BitmapFactory.DecodeResource(context.Resources, iconResId))
            using (var scaledBitmap = Bitmap.CreateScaledBitmap(baseBitmap, baseIconSize, baseIconSize, true))
            {
                canvas.DrawBitmap(scaledBitmap, iconX, iconY, iconPaint);
            }
            var textX = resultWidth / 2f;
            var textY = baseIconSize / 2f - ((textPaint.Descent() + textPaint.Ascent()) / 2);
            Rect textBounds = new();
            textPaint.GetTextBounds(label, 0, label.Length, textBounds);

            var bgRect = new RectF(textX - textBounds.Width() / 2f - textPadding, textY + textBounds.Top - textPadding, textX + textBounds.Width() / 2f + textPadding, textY + textBounds.Bottom + textPadding);
            canvas.DrawRoundRect(bgRect, cornerRadius, cornerRadius, bgPaint);
            canvas.DrawText(label, textX, textY, textPaint);

            var isStopped = bearing is null or < 0;
            var overlayResId = isStopped ? Resource.Drawable.stop_icon : Resource.Drawable.arrow_icon;

            var overlayX = (resultWidth - overlaySize) / 2f;
            var overlayY = (float)(baseIconSize + gap);
            DrawDirectionOverlay(context, canvas, overlayResId, isStopped, bearing, overlayX, overlayY, overlaySize);

            if (isStale)
            {
                DrawStaleBadge(canvas, resultWidth, overlaySize);
            }

            // Anchor the marker on the centre of the overlay so the arrow/stop icon sits
            // exactly on the vehicle's real-world location (underneath the vehicle icon).
            var anchorU = (overlayX + overlaySize / 2f) / resultWidth;
            var anchorV = (overlayY + overlaySize / 2f) / resultHeight;
            return new VehiclePinBitmap(resultBitmap, anchorU, anchorV);
        }

        private static VehiclePinBitmap CreateNumberOnlyPinBitmap(Context context, string label, string colorHex, double? bearing, int baseIconSize, int overlaySize, int padding, int gap, bool isStale = false)
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
            // The number badge sits on top and the direction arrow (or stop marker) is placed
            // directly beneath it, so the arrow can be anchored exactly on the vehicle's location.
            var resultHeight = badgeHeight + gap + overlaySize;

            var resultBitmap = Bitmap.CreateBitmap(resultWidth, resultHeight, Bitmap.Config.Argb8888);
            using var canvas = new Canvas(resultBitmap);

            var badgeLeft = (resultWidth - badgeWidth) / 2f;
            const float badgeTop = 0f;
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
            var overlayResId = isStopped ? Resource.Drawable.stop_icon : Resource.Drawable.arrow_icon;

            var overlayX = (resultWidth - overlaySize) / 2f;
            var overlayY = (float)(badgeHeight + gap);
            DrawDirectionOverlay(context, canvas, overlayResId, isStopped, bearing, overlayX, overlayY, overlaySize);

            if (isStale)
            {
                DrawStaleBadge(canvas, resultWidth, overlaySize);
            }

            // Anchor the marker on the centre of the overlay so the arrow/stop icon sits
            // exactly on the vehicle's real-world location (underneath the number badge).
            var anchorU = (overlayX + overlaySize / 2f) / resultWidth;
            var anchorV = (overlayY + overlaySize / 2f) / resultHeight;
            return new VehiclePinBitmap(resultBitmap, anchorU, anchorV);
        }

        /// <summary>
        /// Desenează iconița direcției (săgeata) cu un contur/margine în spate, astfel încât
        /// săgeata să rămână vizibilă pe orice fundal al hărții. Pentru vehiculele oprite
        /// desenează iconița de stop, fără margine.
        /// </summary>
        private static void DrawDirectionOverlay(Context context, Canvas canvas, int overlayResId, bool isStopped, double? bearing, float overlayX, float overlayY, int overlaySize)
        {
            using var overlayBase = BitmapFactory.DecodeResource(context.Resources, overlayResId);

            canvas.Save();
            if (!isStopped)
            {
                canvas.Rotate((float)bearing!.Value, overlayX + overlaySize / 2f, overlayY + overlaySize / 2f);

                // Draw a slightly larger version of the arrow behind it, acting as a margin/outline.
                var margin = Math.Max(1, (int)(overlaySize * VehiclePinArrowMarginScale));
                var marginSize = overlaySize + margin * 2;
                using var marginPaint = new Paint { AntiAlias = true };
                marginPaint.SetColorFilter(new PorterDuffColorFilter(VehiclePinArrowMarginColor, PorterDuff.Mode.SrcIn));
                using (var scaledMargin = Bitmap.CreateScaledBitmap(overlayBase, marginSize, marginSize, true))
                {
                    canvas.DrawBitmap(scaledMargin, overlayX - margin, overlayY - margin, marginPaint);
                }

                using var arrowPaint = new Paint { AntiAlias = true };
                arrowPaint.SetColorFilter(new PorterDuffColorFilter(VehiclePinArrowColor, PorterDuff.Mode.SrcIn));
                using var scaledArrow = Bitmap.CreateScaledBitmap(overlayBase, overlaySize, overlaySize, true);
                canvas.DrawBitmap(scaledArrow, overlayX, overlayY, arrowPaint);
            }
            else
            {
                using var stopPaint = new Paint { AntiAlias = true };
                stopPaint.SetColorFilter(new PorterDuffColorFilter(Color.Red, PorterDuff.Mode.SrcIn));
                using var scaledStop = Bitmap.CreateScaledBitmap(overlayBase, overlaySize, overlaySize, true);
                canvas.DrawBitmap(scaledStop, overlayX, overlayY, stopPaint);
            }
            canvas.Restore();
        }

        /// <summary>
        /// Desenează un mic semn de avertizare (cerc portocaliu cu „!”) în colțul din dreapta-sus
        /// al iconiței, pentru a marca faptul că poziția vehiculului este învechită.
        /// </summary>
        private static void DrawStaleBadge(Canvas canvas, int resultWidth, int overlaySize)
        {
            var radius = overlaySize / 2f;
            var centerX = resultWidth - radius;
            var centerY = radius;

            using var ringPaint = new Paint { Color = Color.White, AntiAlias = true };
            canvas.DrawCircle(centerX, centerY, radius, ringPaint);

            using var circlePaint = new Paint { Color = Color.Argb(255, 245, 158, 11), AntiAlias = true };
            canvas.DrawCircle(centerX, centerY, radius * 0.86f, circlePaint);

            using var markPaint = new Paint
            {
                Color = Color.White,
                AntiAlias = true,
                TextAlign = Paint.Align.Center,
                FakeBoldText = true,
                TextSize = radius * 1.3f
            };
            var textY = centerY - (markPaint.Descent() + markPaint.Ascent()) / 2f;
            canvas.DrawText("!", centerX, textY, markPaint);
        }
    }

    /// <summary>
    /// A generated vehicle pin bitmap together with the anchor fractions that place the
    /// direction arrow (or stop marker) exactly on the vehicle's real-world location.
    /// </summary>
    public readonly record struct VehiclePinBitmap(Bitmap Bitmap, float AnchorU, float AnchorV);

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
        private bool IsStale { get; }

        public BitmapCacheKey(VehicleType vehicleType, string routeShortName, string colorValue, double? bearing, bool isStopIcon, int size, bool numberOnly = false, bool isStale = false)
        {
            VehicleType = vehicleType;
            RouteShortName = routeShortName ?? string.Empty;
            ColorValue = colorValue ?? string.Empty;
            IsStopIcon = isStopIcon;
            IsStopped = !bearing.HasValue || bearing.Value < 0;
            RoundedBearing = IsStopped ? -1 : (int)Math.Round(bearing!.Value);
            Size = size;
            NumberOnly = numberOnly;
            IsStale = isStale;
        }

        public override int GetHashCode() => HashCode.Combine(HashCode.Combine(VehicleType, RouteShortName, ColorValue, RoundedBearing, IsStopped, IsStopIcon, Size, NumberOnly), IsStale);

        public bool Equals(BitmapCacheKey other)
        {
            return VehicleType == other.VehicleType &&
                   RouteShortName == other.RouteShortName &&
                   ColorValue == other.ColorValue &&
                   RoundedBearing == other.RoundedBearing &&
                   IsStopIcon == other.IsStopIcon &&
                   IsStopped == other.IsStopped &&
                   Size == other.Size &&
                   NumberOnly == other.NumberOnly &&
                   IsStale == other.IsStale;
        }

        public override bool Equals(object? obj) => obj is BitmapCacheKey key && Equals(key);
        
    }

}
