
using IvanConnections_Travel.Converters;
using IvanConnections_Travel.CustomControls.Interfaces;
using IvanConnections_Travel.Models;
using IvanConnections_Travel.Platforms.Android.Handlers;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Maps;

namespace IvanConnections_Travel.CustomControls
{
    public class CustomPin : Pin, ICustomPin
    {
        public static readonly BindableProperty IconProperty = BindableProperty.Create(nameof(Icon), typeof(IView), typeof(CustomPin), null, BindingMode.OneWay);

        private static readonly CustomPin TempPin = new() { Label = "temp", Location = new Location() };

        private bool isSelected;

        public CustomPin()
        {
            this.MarkerClicked += this.OnMarkerClicked;
        }

        public IView? Icon
        {
            get
            {
#if ANDROID || IOS
                if (!CustomMapHandler.EnableMapSymbols)
                {
                    return null;
                }
#endif

                return (IView?)this.GetValue(IconProperty);
            }
            set => this.SetValue(IconProperty, value);
        }

        public bool IsSelected
        {
            get => this.isSelected;
            private set
            {
                if (this.isSelected == value)
                {
                    return;
                }

                this.isSelected = value;

                if (this.BindingContext is Record currentRecord)
                {
                    if (this.isSelected)
                    {
                        // Set selected pin color
                        this.Icon = RecordToIconConverter.GeneratePathIcon(currentRecord, Colors.Yellow);
                    }
                    else
                    {
                        // Unset previously selected pin color
                        this.Icon = RecordToIconConverter.GeneratePathIcon(currentRecord, Colors.Black);
                    }
                }
            }
        }

        public Microsoft.Maui.Maps.IMap? Map { get; set; }

        public string? PinColor => ((SolidColorBrush?)((Shape?)this.Icon)?.Fill)?.Color.ToArgbHex();

        protected override void OnParentSet()
        {
            base.OnParentSet();

            if (this.Parent is null)
            {
                this.MarkerClicked -= this.OnMarkerClicked;
            }
            else
            {
                this.MarkerClicked += this.OnMarkerClicked;
            }
        }

        private static void RefreshMapPins(MvvmMap customMap)
        {
            if (!customMap.Pins.Remove(TempPin))
            {
                customMap.Pins.Add(TempPin);
            }
        }

        private void OnMarkerClicked(object? sender, PinClickedEventArgs e)
        {
            if (this.Map is MvvmMap customMap && sender is CustomPin selectedPin)
            {
                var selectedRecord = (Record)selectedPin.BindingContext;

                if (selectedRecord is null)
                {
                    return;
                }

                if (e.HideInfoWindow)
                {
                    selectedPin.IsSelected = false;
                    customMap.SelectedItem = null;
                    this.Icon = RecordToIconConverter.GeneratePathIcon(selectedRecord, Colors.Yellow);

                    RefreshMapPins(customMap);
                }
                else if (customMap.SelectedItem != selectedPin.BindingContext)
                {
                    // Else-if block only does something if user is not re-selecting the same pin.

                    foreach (var pin in customMap.Pins.OfType<CustomPin>())
                    {
                        if (pin is null)
                        {
                            continue;
                        }

                        pin.IsSelected = pin == selectedPin;
                    }

                    customMap.SelectedItem = selectedRecord;

                    RefreshMapPins(customMap);
                }
            }
        }
    }
}
