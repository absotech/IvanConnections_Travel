using IvanConnections_Travel.Models;
using IvanConnections_Travel.Views.Popups;

namespace IvanConnections_Travel.Controls;

public partial class BadgeHexagon : ContentView
{
    private CancellationTokenSource? _pulseCts;

    public static readonly BindableProperty IsLockedProperty =
        BindableProperty.Create(nameof(IsLocked), typeof(bool), typeof(BadgeHexagon), false,
            propertyChanged: (bindable, _, newValue) =>
            {
                if (bindable is not BadgeHexagon badge) return;
                var locked = (bool)newValue;
                badge.LockOverlay.IsVisible = locked;
                if (locked)
                {
                    badge._pulseCts?.Cancel();
                }
                else if (badge.BindingContext is UserBadgeDto b)
                {
                    badge.StartPulse(b.Badge.Rarity.DisplayOrder.Value);
                }
            });

    public bool IsLocked
    {
        get => (bool)GetValue(IsLockedProperty);
        set => SetValue(IsLockedProperty, value);
    }

    public BadgeHexagon()
    {
        InitializeComponent();
    }
    private async void OnBadgeTapped(object sender, TappedEventArgs e)
    {
        if (BindingContext is not UserBadgeDto userBadge) return;

        var title = IsLocked ? "Medalie indisponibilă" : "Medalie câștigată";
        var message = IsLocked
            ? userBadge.Badge.Description
            : $"Deblocat la {userBadge.EarnedAt:dd.MM.yyyy}";
        var type = IsLocked ? MessagePopupType.Info : MessagePopupType.Success;

        await MessagePopup.ShowAsync(title, message, type, MessagePopupButtons.Ok);
    }
    private void StartPulse(int rarity)
    {
        if (rarity < 3) return;

        _pulseCts?.Cancel();
        _pulseCts = new CancellationTokenSource();
        var token = _pulseCts.Token;

        const double minScale = 1.0;
        var maxScale = rarity switch
        {
            3 => 1.02,
            4 => 1.04,
            5 => 1.06,
            _ => 1.0
        };

        uint duration = rarity switch
        {
            5 => 800,
            4 => 1000,
            _ => 1400
        };

        _ = RunPulseLoop(minScale, maxScale, duration, token);
    }

    private async Task RunPulseLoop(double minScale, double maxScale, uint duration, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            await Task.WhenAll(
                HexPath.ScaleTo(maxScale, duration, Easing.SinInOut),
                AnimateGlowRadius(GlowShadow, 6, 12, duration)
            );
            if (token.IsCancellationRequested) break;

            await Task.WhenAll(
                HexPath.ScaleTo(minScale, duration, Easing.SinInOut),
                AnimateGlowRadius(GlowShadow, 12, 6, duration)
            );
        }
    }
    private Task AnimateGlowRadius(Shadow shadow, double from, double to, uint length)
    {
        var tcs = new TaskCompletionSource<bool>();

        var animation = new Animation(
            v => shadow.Radius = (float)v,
            from,
            to
        );

        animation.Commit(
            this,
            Guid.NewGuid().ToString(),
            16,
            length,
            Easing.SinInOut,
            (v, c) => tcs.SetResult(true)
        );

        return tcs.Task;
    }
    
    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();

        if (BindingContext is UserBadgeDto badge && !IsLocked)
            StartPulse(badge.Badge.Rarity.DisplayOrder.Value);
    }
}