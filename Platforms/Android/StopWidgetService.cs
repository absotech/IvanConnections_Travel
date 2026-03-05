using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Widget;
using IvanConnections_Travel.Models;
using IvanConnections_Travel.Services;

namespace IvanConnections_Travel.Platforms.Android
{
    [Service(Name = "com.ivanconnections.ivanconnectionstravel.StopWidgetService", Permission = "android.permission.BIND_REMOTEVIEWS", Exported = true)]
    public class StopWidgetService : RemoteViewsService
    {
        public override IRemoteViewsFactory? OnGetViewFactory(Intent? intent)
        {
            if (intent == null) return null;
            return new StopWidgetFactory(this.ApplicationContext, intent);
        }
    }

    public class StopWidgetFactory : Java.Lang.Object, RemoteViewsService.IRemoteViewsFactory
    {
        private readonly Context _context;
        private readonly int _appWidgetId;
        private readonly ApiService _apiService;
        private List<StopArrival> _arrivals = new();

        public StopWidgetFactory(Context context, Intent intent)
        {
            _context = context;
            _appWidgetId = intent.GetIntExtra(AppWidgetManager.ExtraAppwidgetId, AppWidgetManager.InvalidAppwidgetId);
            _apiService = new ApiService();
        }

        public void OnCreate() { }

        public void OnDataSetChanged()
        {
            var prefs = _context.GetSharedPreferences("StopWidgetPrefs", FileCreationMode.Private);
            var stopId = prefs?.GetInt($"stopId_{_appWidgetId}", -1) ?? -1;

            if (stopId == -1) return;
            var task = _apiService.GetArrivalsForStopAsync(stopId);
            task.Wait();
            _arrivals = task.Result
                .Where(a => a.ArrivalMinutes <= 25)
                .OrderBy(a => a.ArrivalMinutes == -1)
                .ThenBy(a => a.ArrivalMinutes)
                .ToList();
        }

        public void OnDestroy() { }

        public int Count => _arrivals.Count;

        public RemoteViews GetViewAt(int position)
        {
            if (position < 0 || position >= _arrivals.Count)
                return null!;

            var arrival = _arrivals[position];
            var views = new RemoteViews(_context.PackageName, IvanConnections_Travel.Resource.Layout.stop_arrival_item);

            var vehicleTypeName = IvanConnections_Travel.Utils.Translations.GetVehicleTypeNameInRomanian(arrival.VehicleType);
            var capitalizedVehicleType = !string.IsNullOrEmpty(vehicleTypeName) 
                ? char.ToUpper(vehicleTypeName[0]) + vehicleTypeName[1..] + " " 
                : "";
            
            views.SetTextViewText(IvanConnections_Travel.Resource.Id.item_vehicle_label, $"{capitalizedVehicleType}{arrival.VehicleLabel}");
            
            var arrivalText = arrival.ArrivalMinutes switch
            {
                -1 => "PE CAPĂT",
                0 => "Sosire",
                _ => $"{arrival.ArrivalMinutes} min"
            };
            views.SetTextViewText(IvanConnections_Travel.Resource.Id.item_arrival_time, arrivalText);

            return views;
        }

        public RemoteViews LoadingView => null!;

        public int ViewTypeCount => 1;

        public long GetItemId(int position) => position;

        public bool HasStableIds => true;
    }
}
