using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Widget;
using Android.Net;
using IvanConnections_Travel.Services;

namespace IvanConnections_Travel.Platforms.Android
{
    [BroadcastReceiver(Name = "com.ivanconnections.ivanconnectionstravel.StopWidgetProvider", Label = "Widget Statie", Exported = true)]
    [IntentFilter(new string[] { "android.appwidget.action.APPWIDGET_UPDATE" })]
    [MetaData("android.appwidget.provider", Resource = "@xml/stop_widget_info")]
    public class StopWidgetProvider : AppWidgetProvider
    {
        public override void OnUpdate(Context? context, AppWidgetManager? appWidgetManager, int[]? appWidgetIds)
        {
            if (context == null || appWidgetManager == null || appWidgetIds == null) return;

            foreach (var appWidgetId in appWidgetIds)
            {
                UpdateAppWidget(context, appWidgetManager, appWidgetId);
            }
        }

        public static void UpdateAppWidget(Context context, AppWidgetManager appWidgetManager, int appWidgetId)
        {
            var prefs = context.GetSharedPreferences("StopWidgetPrefs", FileCreationMode.Private);
            var stopName = prefs?.GetString($"stopName_{appWidgetId}", "Selecteaza o statie") ?? "Selecteaza o statie";

            var views = new RemoteViews(context.PackageName, IvanConnections_Travel.Resource.Layout.stop_widget);
            views.SetTextViewText(IvanConnections_Travel.Resource.Id.widget_stop_name, stopName);

            var serviceIntent = new Intent(context, typeof(StopWidgetService));
            serviceIntent.PutExtra(AppWidgetManager.ExtraAppwidgetId, appWidgetId);
            // Ensure unique intent per widget instance to avoid caching issues with extras
            serviceIntent.SetData(global::Android.Net.Uri.Parse(serviceIntent.ToUri(global::Android.Content.IntentUriType.Scheme)));
            views.SetRemoteAdapter(IvanConnections_Travel.Resource.Id.widget_arrivals_list, serviceIntent);
            
            var updateIntent = new Intent(context, typeof(StopWidgetProvider));
            updateIntent.SetAction(AppWidgetManager.ActionAppwidgetUpdate);
            updateIntent.PutExtra(AppWidgetManager.ExtraAppwidgetIds, new int[] { appWidgetId });
            var pendingUpdate = PendingIntent.GetBroadcast(context, appWidgetId, updateIntent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
            views.SetOnClickPendingIntent(IvanConnections_Travel.Resource.Id.widget_refresh_button, pendingUpdate);

            views.SetTextViewText(IvanConnections_Travel.Resource.Id.widget_last_updated, $"Actualizat: {DateTime.Now:HH:mm}");

            appWidgetManager.NotifyAppWidgetViewDataChanged(appWidgetId, IvanConnections_Travel.Resource.Id.widget_arrivals_list);
            appWidgetManager.UpdateAppWidget(appWidgetId, views);
        }

        public override void OnReceive(Context? context, Intent? intent)
        {
            base.OnReceive(context, intent);
            
            if (context == null || intent == null) return;

            switch (intent.Action)
            {
                case AppWidgetManager.ActionAppwidgetUpdate:
                {
                    var appWidgetIds = intent.GetIntArrayExtra(AppWidgetManager.ExtraAppwidgetIds);
                    if (appWidgetIds != null)
                    {
                        var appWidgetManager = AppWidgetManager.GetInstance(context);
                        foreach (var appWidgetId in appWidgetIds)
                        {
                            UpdateAppWidget(context, appWidgetManager!, appWidgetId);
                        }
                    }

                    break;
                }
                case "com.ivanconnections.ivanconnectionstravel.ACTION_WIDGET_PINNED":
                {
                    var appWidgetId = intent.GetIntExtra(AppWidgetManager.ExtraAppwidgetId, AppWidgetManager.InvalidAppwidgetId);
                    var stopName = intent.GetStringExtra("stopName");
                    if (appWidgetId != AppWidgetManager.InvalidAppwidgetId)
                    {
                        var stopId = intent.GetIntExtra("stopId", -1);

                        if (stopId == -1 || string.IsNullOrEmpty(stopName))
                        {
                            stopId = IvanConnections_Travel.Utils.Preferences.PendingWidgetStopId;
                            stopName = IvanConnections_Travel.Utils.Preferences.PendingWidgetStopName;
                        }
                    
                        if (stopId != -1 && !string.IsNullOrEmpty(stopName))
                        {
                            var prefs = context.GetSharedPreferences("StopWidgetPrefs", FileCreationMode.Private);
                            var edit = prefs!.Edit();
                            edit!.PutInt($"stopId_{appWidgetId}", stopId);
                            edit!.PutString($"stopName_{appWidgetId}", stopName);
                            edit!.Commit();
                        
                            IvanConnections_Travel.Utils.Preferences.ClearPendingWidgetStop();
                        
                            var appWidgetManager = AppWidgetManager.GetInstance(context);
                            UpdateAppWidget(context, appWidgetManager!, appWidgetId);
                        }
                    }

                    break;
                }
            }
        }
    }
}
