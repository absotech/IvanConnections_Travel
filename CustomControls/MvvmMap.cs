using Microsoft.Maui.Maps;
using Map = Microsoft.Maui.Controls.Maps.Map;
namespace IvanConnections_Travel.CustomControls
{

    public class MvvmMap : Map
    {
        public static readonly BindableProperty MapSpanProperty = BindableProperty.Create(nameof(MapSpan), typeof(MapSpan), typeof(MvvmMap), null, BindingMode.TwoWay, propertyChanged: (b, _, n) =>
        {
            if (b is MvvmMap map && n is MapSpan mapSpan)
            {
                MoveMap(map, mapSpan);
            }
        });

        public static readonly BindableProperty SelectedItemProperty = BindableProperty.Create(nameof(SelectedItem), typeof(object), typeof(MvvmMap), null, BindingMode.TwoWay);

        public MapSpan MapSpan
        {
            get => (MapSpan)this.GetValue(MapSpanProperty);
            set => this.SetValue(MapSpanProperty, value);
        }

        public object SelectedItem
        {
            get => this.GetValue(SelectedItemProperty);
            set => this.SetValue(SelectedItemProperty, value);
        }

        private static void MoveMap(MvvmMap map, MapSpan mapSpan)
        {
            var timer = Application.Current!.Dispatcher.CreateTimer();
            timer.Interval = TimeSpan.FromMilliseconds(500);
            timer.Tick += (s, e) =>
            {
                if (s is IDispatcherTimer timer)
                {
                    timer.Stop();

                    MainThread.BeginInvokeOnMainThread(() => map.MoveToRegion(mapSpan));
                }
            };

            timer.Start();
        }
    }
}
