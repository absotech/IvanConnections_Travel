using System.Collections.Concurrent;
using CommunityToolkit.Maui.Core.Extensions;
using global::Android.Gms.Maps.Model;
using global::Android.Graphics;
using Android.Views;
using Microsoft.Maui.Platform;
using View = Android.Views.View;


namespace IvanConnections_Travel.Platforms.Android
{
    internal static class BitmapUtils
    {
        private static readonly ConcurrentDictionary<IView, BitmapDescriptor> Cache = new();

        public static BitmapDescriptor GetBitmapDescriptorForIcon(IView icon, IMauiContext mauiContext) => Cache.GetOrAdd(icon, i => ConvertPinToBitmapDescriptor(i, mauiContext));

        public static BitmapDescriptor ConvertPinToBitmapDescriptor(IView icon, IMauiContext mauiContext)
        {
            var bitmap = ConvertViewToBitmap(icon, mauiContext);
            return BitmapDescriptorFactory.FromBitmap(bitmap);
        }

        public static Bitmap? ConvertViewToBitmap(IView icon, IMauiContext mauiContext)
        {
            var nativeBorder = icon.ToPlatform(mauiContext);
            return ConvertViewToBitmap(nativeBorder, icon);
        }

        public static Bitmap? ConvertViewToBitmap(View? view, IView icon)
        {
            if (view is null)
            {
                return null;
            }

            var width = GetLengthOrDefault(icon?.Width);
            var height = GetLengthOrDefault(icon?.Height);
            view.Layout(0, 0, width, height);
            view.Measure(width, height);
            var bitmap = Bitmap.CreateBitmap(width, height, Bitmap.Config.Argb8888!);
            if (bitmap is null)
            {
                return null;
            }

            Canvas canvas = new(bitmap);
            view.Draw(canvas);
            return bitmap;
        }

        private static int GetLengthOrDefault(double? length, int defaultLength = 80)
        {
            if (length?.IsZeroOrNaN() != false)
            {
                return defaultLength;
            }

            return (int)length.Value;
        }
    }
}
