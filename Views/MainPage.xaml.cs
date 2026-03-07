using Android.Locations;
using CommunityToolkit.Maui.Core;
using IvanConnections_Travel.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Maps;
using Location = Microsoft.Maui.Devices.Sensors.Location;

namespace IvanConnections_Travel
{
    public partial class MainPage : ContentPage
    {
        public MainPage(MainPageViewModel mainPageViewModel)
        {
            InitializeComponent();
            Location location = new(47.1585, 27.6014);
            MapSpan mapSpan = new(location, 0.01, 0.01);
            MyMap.MoveToRegion(mapSpan);
            BindingContext = mainPageViewModel;
        }

        private void OnMenuClicked(object sender, EventArgs e)
        {
            Shell.Current.FlyoutIsPresented = true;
        }

        private void OnFilterExpanderExpandedChanged(object sender, ExpandedChangedEventArgs e)
{
    this.AbortAnimation("ExpandAnimation");
    this.AbortAnimation("CollapseAnimation");

    if (e.IsExpanded)
    {
        FilterIcon.RotateTo(180, 250, Easing.CubicInOut);
        FilterExpanderContent.HeightRequest = -1;
        FilterExpanderContent.IsVisible = true;
        var size = FilterExpanderContent.Measure(FilterExpanderContent.Width > 0 ? FilterExpanderContent.Width : double.PositiveInfinity, double.PositiveInfinity, MeasureFlags.IncludeMargins);
        var targetHeight = size.Request.Height;

        FilterExpanderContent.HeightRequest = 0;
        FilterExpanderContent.Opacity = 0;
        FilterExpanderContent.TranslationY = -10;

        var expandAnimation = new Animation();
        expandAnimation.Add(0, 1, new Animation(v => FilterExpanderContent.HeightRequest = v, 0, targetHeight, Easing.CubicOut));
        expandAnimation.Add(0, 0.5, new Animation(v => FilterExpanderContent.Opacity = v, 0, 1, Easing.Linear));
        expandAnimation.Add(0, 1, new Animation(v => FilterExpanderContent.TranslationY = v, -10, 0, Easing.CubicOut));

        expandAnimation.Commit(this, "ExpandAnimation", 16, 250, Easing.Linear, (v, c) =>
        {
            FilterExpanderContent.HeightRequest = -1;
        });
    }
    else
    {
        FilterIcon.RotateTo(0, 250, Easing.CubicInOut);
        if (FilterExpanderContent.HeightRequest == -1)
        {
            FilterExpanderContent.HeightRequest = FilterExpanderContent.Height;
        }
        var currentHeight = FilterExpanderContent.HeightRequest;
        var collapseAnimation = new Animation();
        collapseAnimation.Add(0, 1, new Animation(v => FilterExpanderContent.HeightRequest = v, currentHeight, 0, Easing.CubicIn));
        collapseAnimation.Add(0.5, 1, new Animation(v => FilterExpanderContent.Opacity = v, 1, 0, Easing.Linear));
        collapseAnimation.Add(0, 1, new Animation(v => FilterExpanderContent.TranslationY = v, 0, -10, Easing.CubicIn));

        collapseAnimation.Commit(this, "CollapseAnimation", 16, 250, Easing.Linear, (v, c) => 
        {
            if (!e.IsExpanded) FilterExpanderContent.IsVisible = false;
        });
    }
}
    }
}