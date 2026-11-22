using CommunityToolkit.Maui.Core;
using IvanConnections_Travel.Utils;
using IvanConnections_Travel.ViewModels;
using Microsoft.Maui.Maps;
using Location = Microsoft.Maui.Devices.Sensors.Location;

namespace IvanConnections_Travel
{
    public partial class MainPage : ContentPage
    {
        private readonly MainPageViewModel _viewModel;

        public MainPage(MainPageViewModel mainPageViewModel)
        {
            InitializeComponent();
            Location location = new(47.1585, 27.6014);
            MapSpan mapSpan = new(location, 0.01, 0.01);
            MyMap.MoveToRegion(mapSpan);
            BindingContext = _viewModel = mainPageViewModel;
            _viewModel.DismissOverlaysRequested += OnDismissOverlaysRequested;
        }

        private void OnDismissOverlaysRequested(object? sender, EventArgs e)
        {
            SearchBox.Dismiss();
            LayersPanel.IsVisible = false;
            if (FilterExpander.IsExpanded) FilterExpander.IsExpanded = false;
        }

        private void OnMenuClicked(object? sender, EventArgs eventArgs)
        {
            Haptics.Click();
            Shell.Current.FlyoutIsPresented = true;
        }

        private async void OnLayersClicked(object? sender, EventArgs e)
        {
            Haptics.Click();
            if (LayersPanel.IsVisible)
            {
                await LayersPanel.FadeToAsync(0, 120);
                LayersPanel.IsVisible = false;
                return;
            }

            LayersPanel.Opacity = 0;
            LayersPanel.IsVisible = true;
            await LayersPanel.FadeToAsync(1, 150);
        }

        private void OnFilterExpanderExpandedChanged(object? sender, ExpandedChangedEventArgs e)
        {
            this.AbortAnimation("ExpandAnimation");
            this.AbortAnimation("CollapseAnimation");

            if (e.IsExpanded)
            {
                LayersPanel.IsVisible = false;
                _ = FilterIcon.RotateToAsync(180, 250, Easing.CubicInOut);
                FilterExpanderContent.HeightRequest = -1;
                FilterExpanderContent.IsVisible = true;
                var size = FilterExpanderContent.Measure(
                    FilterExpanderContent.Width > 0 ? FilterExpanderContent.Width : double.PositiveInfinity,
                    double.PositiveInfinity);
                var targetHeight = size.Height;

                FilterExpanderContent.HeightRequest = 0;
                FilterExpanderContent.Opacity = 0;
                FilterExpanderContent.TranslationY = -10;

                var expandAnimation = new Animation
                {
                    { 0, 1, new Animation(v => FilterExpanderContent.HeightRequest = v, 0, targetHeight, Easing.CubicOut) },
                    { 0, 0.5, new Animation(v => FilterExpanderContent.Opacity = v, 0, 1, Easing.Linear) },
                    { 0, 1, new Animation(v => FilterExpanderContent.TranslationY = v, -10, 0, Easing.CubicOut) }
                };

                expandAnimation.Commit(this, "ExpandAnimation", 16, 250, Easing.Linear,
                    (_, _) => FilterExpanderContent.HeightRequest = -1);
            }
            else
            {
                _ = FilterIcon.RotateToAsync(0, 250, Easing.CubicInOut);
                if (FilterExpanderContent.HeightRequest < 0)
                {
                    FilterExpanderContent.HeightRequest = FilterExpanderContent.Height;
                }

                var currentHeight = FilterExpanderContent.HeightRequest;
                var collapseAnimation = new Animation
                {
                    { 0, 1, new Animation(v => FilterExpanderContent.HeightRequest = v, currentHeight, 0, Easing.CubicIn) },
                    { 0.5, 1, new Animation(v => FilterExpanderContent.Opacity = v, 1, 0, Easing.Linear) },
                    { 0, 1, new Animation(v => FilterExpanderContent.TranslationY = v, 0, -10, Easing.CubicIn) }
                };

                collapseAnimation.Commit(this, "CollapseAnimation", 16, 250, Easing.Linear,
                    (_, _) => FilterExpanderContent.IsVisible = false);
            }
        }
    }
}
