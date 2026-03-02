using Android.App;
using Android.Appwidget;
using Android.Content;
using IvanConnections_Travel.Services;
using Application = Android.App.Application;

namespace IvanConnections_Travel.Platforms.Android;

public class WidgetService : IWidgetService
{
    public bool IsPinningSupported()
    {
        var manager = AppWidgetManager.GetInstance(Application.Context);
        return manager?.IsRequestPinAppWidgetSupported ?? false;
    }

    public Task PinWidgetAsync(int stopId, string stopName)
    {
        if (!IsPinningSupported()) return Task.CompletedTask;

        Utils.Preferences.PendingWidgetStopId = stopId;
        Utils.Preferences.PendingWidgetStopName = stopName;

        var context = Application.Context;
        var manager = AppWidgetManager.GetInstance(context);
        
        var myProvider = new ComponentName(context, "com.ivanconnections.ivanconnectionstravel.StopWidgetProvider");

        var callbackIntent = new Intent(context, typeof(StopWidgetProvider));
        callbackIntent.SetAction("com.ivanconnections.ivanconnectionstravel.ACTION_WIDGET_PINNED");
        callbackIntent.PutExtra("stopId", stopId);
        callbackIntent.PutExtra("stopName", stopName);
        var successCallback = PendingIntent.GetBroadcast(context, 0, callbackIntent, 
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Mutable);
        
        manager!.RequestPinAppWidget(myProvider, null, successCallback);
        
        return Task.CompletedTask;
    }
}
