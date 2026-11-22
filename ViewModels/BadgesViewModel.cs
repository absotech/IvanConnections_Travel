using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IvanConnections_Travel.Models;
using IvanConnections_Travel.Services;
using IvanConnections_Travel.Views.Popups;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;

namespace IvanConnections_Travel.ViewModels;

public partial class BadgesViewModel : ObservableObject
{
    private readonly ApiService _apiService;
    private readonly AppShellViewModel _appShellViewModel;

    [ObservableProperty] private ObservableCollection<BadgeDisplayItem> _badges = [];
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _progressText = string.Empty;

    public BadgesViewModel(ApiService apiService, AppShellViewModel appShellViewModel)
    {
        _apiService = apiService;
        _appShellViewModel = appShellViewModel;
        
        _appShellViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(AppShellViewModel.CurrentUser))
            {
                MainThread.BeginInvokeOnMainThread(async () => await LoadBadgesAsync());
            }
        };
    }

    [RelayCommand]
    public async Task LoadBadgesAsync()
    {
        if (IsLoading) return;
        IsLoading = true;
        try
        {
            var allBadges = await _apiService.GetAllBadgesAsync();

            var userBadgeMap = _appShellViewModel.CurrentUser?.UserBadges
                .ToDictionary(ub => ub.Badge.Id) ?? [];

            var items = allBadges
                .Select(badge =>
                {
                    var isUnlocked = userBadgeMap.TryGetValue(badge.Id, out var owned);
                    var userBadge = owned ?? new UserBadgeDto { Badge = badge };
                    return new BadgeDisplayItem { UserBadge = userBadge, IsUnlocked = isUnlocked };
                })
                // Unlocked first, then by rarity, then by name.
                .OrderByDescending(i => i.IsUnlocked)
                .ThenBy(i => i.UserBadge.Badge.Rarity?.DisplayOrder ?? 0)
                .ThenBy(i => i.UserBadge.Badge.Name)
                .ToList();

            Badges = new ObservableCollection<BadgeDisplayItem>(items);
            var unlocked = items.Count(i => i.IsUnlocked);
            ProgressText = items.Count == 0 ? string.Empty : $"{unlocked} din {items.Count} medalii deblocate";
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[BadgesViewModel] Error loading badges: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ShowBadgeDetailsAsync(BadgeDisplayItem? item)
    {
        if (item?.UserBadge?.Badge is not { } badge) return;

        try
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
        }
        catch
        {
            // Haptics unavailable — ignore.
        }

        var message = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(badge.Description))
            message.AppendLine(badge.Description).AppendLine();

        if (item.IsUnlocked)
        {
            message.Append($"✅ Deblocată la {item.UserBadge.EarnedAt:dd.MM.yyyy}");
        }
        else
        {
            message.AppendLine("🔒 Încă blocată")
                .Append("Cum o deblochezi: ")
                .Append(string.IsNullOrWhiteSpace(badge.Description)
                    ? "participă activ în comunitate."
                    : "îndeplinește cerința de mai sus.");
        }

        if (!string.IsNullOrWhiteSpace(badge.Rarity?.Name))
            message.AppendLine().AppendLine().Append($"Raritate: {badge.Rarity.Name}");

        var title = item.IsUnlocked ? "Medalie câștigată" : "Medalie indisponibilă";
        var type = item.IsUnlocked ? MessagePopupType.Success : MessagePopupType.Info;

        try
        {
            await MessagePopup.ShowAsync($"{title}\n{badge.Name}", message.ToString(), type, MessagePopupButtons.Ok);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[BadgesViewModel] Error showing badge details: {ex.Message}");
        }
    }
}
