using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.OS;
using Android.Text;
using Android.Widget;
using System.Text.Json;
using System.IO;
using IvanConnections_Travel.Models;
using IvanConnections_Travel.Services;

namespace IvanConnections_Travel.Platforms.Android
{
    [Activity(Name = "com.ivanconnections.ivanconnectionstravel.StopWidgetConfigureActivity", Exported = true)]
    [IntentFilter(new string[] { AppWidgetManager.ActionAppwidgetConfigure })]
    public class StopWidgetConfigureActivity : Activity
    {
        private int _appWidgetId = AppWidgetManager.InvalidAppwidgetId;
        private List<Stop> _allStops = new();
        private List<Stop> _filteredStops = new();
        private ArrayAdapter<string>? _adapter;

        protected override async void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetResult(Result.Canceled);

            var intent = Intent;
            var extras = intent?.Extras;
            if (extras != null)
            {
                _appWidgetId = extras.GetInt(AppWidgetManager.ExtraAppwidgetId, AppWidgetManager.InvalidAppwidgetId);
            }

            if (_appWidgetId == AppWidgetManager.InvalidAppwidgetId)
            {
                Finish();
                return;
            }

            var prefs = GetSharedPreferences("StopWidgetPrefs", FileCreationMode.Private);
            if (prefs!.Contains($"stopId_{_appWidgetId}"))
            {
                var resultValue = new Intent();
                resultValue.PutExtra(AppWidgetManager.ExtraAppwidgetId, _appWidgetId);
                SetResult(Result.Ok, resultValue);
                Finish();
                return;
            }

            var pendingStopId = IvanConnections_Travel.Utils.Preferences.PendingWidgetStopId;
            var pendingStopName = IvanConnections_Travel.Utils.Preferences.PendingWidgetStopName;

            if (pendingStopId != -1 && !string.IsNullOrEmpty(pendingStopName))
            {
                SaveStopSelection(new Stop { StopId = pendingStopId, StopName = pendingStopName });
                IvanConnections_Travel.Utils.Preferences.ClearPendingWidgetStop();
                return;
            }

            SetContentView(IvanConnections_Travel.Resource.Layout.stop_configure);

            var searchEdit = FindViewById<global::Android.Widget.EditText>(IvanConnections_Travel.Resource.Id.stop_search);
            var listView = FindViewById<global::Android.Widget.ListView>(IvanConnections_Travel.Resource.Id.stops_list);

            try
            {
                using var stream = Assets!.Open("stops.json");
                using var reader = new StreamReader(stream);
                var json = await reader.ReadToEndAsync();
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                };
                _allStops = JsonSerializer.Deserialize<List<Stop>>(json, options) ?? new List<Stop>();
            }
            catch (Exception)
            {
                _allStops = new List<Stop>();
            }

            _filteredStops = new List<Stop>(_allStops);

            _adapter = new ArrayAdapter<string>(this, global::Android.Resource.Layout.SimpleListItem1, _filteredStops.Select(s => $"{s.StopId} - {s.StopName}").ToList());
            listView!.Adapter = _adapter;

            listView.ItemClick += (object? sender, AdapterView.ItemClickEventArgs e) =>
            {
                var selectedStop = _filteredStops[e.Position];
                SaveStopSelection(selectedStop);
            };

            searchEdit!.TextChanged += (object? sender, global::Android.Text.TextChangedEventArgs e) =>
            {
                var filter = e.Text?.ToString()?.ToLower() ?? "";
                _filteredStops = _allStops.Where(s => 
                    s.StopName.ToLower().Contains(filter) || 
                    s.StopId.ToString().Contains(filter)).ToList();
                _adapter.Clear();
                _adapter.AddAll(_filteredStops.Select(s => $"{s.StopId} - {s.StopName}").ToList());
                _adapter.NotifyDataSetChanged();
            };
        }

        private void SaveStopSelection(Stop stop)
        {
            var context = ApplicationContext;
            var prefs = context.GetSharedPreferences("StopWidgetPrefs", FileCreationMode.Private);
            var edit = prefs!.Edit();
            edit!.PutInt($"stopId_{_appWidgetId}", stop.StopId);
            edit!.PutString($"stopName_{_appWidgetId}", stop.StopName);
            edit!.Commit();

            var appWidgetManager = AppWidgetManager.GetInstance(context);
            StopWidgetProvider.UpdateAppWidget(context, appWidgetManager!, _appWidgetId);

            var resultValue = new Intent();
            resultValue.PutExtra(AppWidgetManager.ExtraAppwidgetId, _appWidgetId);
            SetResult(Result.Ok, resultValue);
            Finish();
        }
    }
}
