using CoreGraphics;
using Foundation;
using IvanConnections_Travel.Models.Enums;
using IvanConnections_Travel.Utils;
using UIKit;

namespace IvanConnections_Travel.Utils
{
    public static partial class MapBitmapFactory
    {
        #region Sizes and Proportions

        public static int StopPinSize = 64;

        public static int VehiclePinBaseSize = 100;
        public static float VehiclePinOverlayScale = 0.48f;
        public static float VehiclePinPaddingScale = 0.20f;
        public static float VehiclePinTextScale = 0.20f;
        public static float VehiclePinTextPaddingScale = 0.08f;
        public static float VehiclePinCornerRadiusScale = 0.12f;
        public static float StopPinBorderScale = 0.047f; // ~3/64

        #endregion

        public static UIImage CreateStopPinImage(string label)
        {
            int size = StopPinSize;
            
            // Create a simple circular pin for stops
            UIGraphics.BeginImageContextWithOptions(new CGSize(size, size), false, 0);
            var context = UIGraphics.GetCurrentContext();

            if (context != null)
            {
                // Draw white circle
                context.SetFillColor(UIColor.White.CGColor);
                context.FillEllipseInRect(new CGRect(0, 0, size, size));

                // Draw border
                float borderWidth = size * StopPinBorderScale;
                context.SetStrokeColor(UIColor.Blue.CGColor);
                context.SetLineWidth(borderWidth);
                context.StrokeEllipseInRect(new CGRect(borderWidth / 2f, borderWidth / 2f, size - borderWidth, size - borderWidth));
            }

            var image = UIGraphics.GetImageFromCurrentImageContext();
            UIGraphics.EndImageContext();

            return image ?? new UIImage();
        }

        public static UIImage CreateCustomPinImage(VehicleType iconType, string? label, string? colorHex, double? bearing)
        {
            int baseIconSize = VehiclePinBaseSize;
            int overlaySize = (int)(VehiclePinBaseSize * VehiclePinOverlayScale);
            int padding = (int)(VehiclePinBaseSize * VehiclePinPaddingScale);
            int fontSize = (int)(VehiclePinBaseSize * VehiclePinTextScale);
            int textPadding = (int)(VehiclePinBaseSize * VehiclePinTextPaddingScale);
            int cornerRadius = (int)(VehiclePinBaseSize * VehiclePinCornerRadiusScale);

            int resultWidth = baseIconSize + overlaySize + padding;
            int resultHeight = baseIconSize + overlaySize + padding;

            UIGraphics.BeginImageContextWithOptions(new CGSize(resultWidth, resultHeight), false, 0);
            var context = UIGraphics.GetCurrentContext();

            if (context != null)
            {
                // Parse color
                UIColor iconColor;
                try
                {
                    iconColor = ColorManagement.ParseColor(colorHex ?? "#000000");
                }
                catch
                {
                    iconColor = UIColor.Black;
                }

                // Draw base icon (bus or tram)
                string iconName = iconType == VehicleType.Bus ? "bus_icon" : "tram_icon";
                var baseIcon = UIImage.FromBundle(iconName);
                
                if (baseIcon != null)
                {
                    float iconX = (resultWidth - baseIconSize) / 2f;
                    float iconY = (resultHeight - baseIconSize) / 2f;

                    // Tint and draw icon
                    context.SaveState();
                    context.TranslateCTM(iconX, iconY + baseIconSize);
                    context.ScaleCTM(1.0f, -1.0f);
                    
                    var rect = new CGRect(0, 0, baseIconSize, baseIconSize);
                    context.ClipToMask(rect, baseIcon.CGImage);
                    context.SetFillColor(iconColor.CGColor);
                    context.FillRect(rect);
                    context.RestoreState();
                }

                // Draw label with background
                if (!string.IsNullOrEmpty(label))
                {
                    var textAttributes = new UIStringAttributes
                    {
                        Font = UIFont.BoldSystemFontOfSize(fontSize),
                        ForegroundColor = ColorManagement.IsColorDark(iconColor) ? UIColor.Yellow : UIColor.White
                    };

                    var textSize = new NSString(label).GetSizeUsingAttributes(textAttributes);
                    float textX = (resultWidth - textSize.Width) / 2f;
                    float textY = (resultHeight - textSize.Height) / 2f;

                    // Draw background rectangle
                    var bgRect = new CGRect(textX - textPadding, textY - textPadding, 
                        textSize.Width + 2 * textPadding, textSize.Height + 2 * textPadding);
                    
                    var bgPath = UIBezierPath.FromRoundedRect(bgRect, cornerRadius);
                    context.SetFillColor(UIColor.Black.ColorWithAlpha(0.67f).CGColor);
                    bgPath.Fill();

                    // Draw text
                    new NSString(label).DrawString(new CGRect(textX, textY, textSize.Width, textSize.Height), textAttributes);
                }

                // Draw overlay (arrow or stop icon)
                bool isStopped = !bearing.HasValue || bearing.Value < 0;
                string overlayName = isStopped ? "stop_icon" : "arrow_icon_notfilled";
                var overlayIcon = UIImage.FromBundle(overlayName);
                UIColor overlayColor = isStopped ? UIColor.Red : UIColor.White;

                if (overlayIcon != null)
                {
                    float overlayX = 0;
                    float overlayY = 0;

                    context.SaveState();
                    
                    if (!isStopped)
                    {
                        // Rotate for bearing
                        context.TranslateCTM(overlayX + overlaySize / 2f, overlayY + overlaySize / 2f);
                        context.RotateCTM((nfloat)(bearing!.Value * Math.PI / 180.0));
                        context.TranslateCTM(-(overlayX + overlaySize / 2f), -(overlayY + overlaySize / 2f));
                    }

                    context.TranslateCTM(overlayX, overlayY + overlaySize);
                    context.ScaleCTM(1.0f, -1.0f);
                    
                    var overlayRect = new CGRect(0, 0, overlaySize, overlaySize);
                    context.ClipToMask(overlayRect, overlayIcon.CGImage);
                    context.SetFillColor(overlayColor.CGColor);
                    context.FillRect(overlayRect);
                    
                    context.RestoreState();
                }
            }

            var resultImage = UIGraphics.GetImageFromCurrentImageContext();
            UIGraphics.EndImageContext();

            return resultImage ?? new UIImage();
        }
    }
}
