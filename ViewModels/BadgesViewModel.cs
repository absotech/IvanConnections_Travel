using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IvanConnections_Travel.Models;
using IvanConnections_Travel.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace IvanConnections_Travel.ViewModels;

public partial class BadgesViewModel : ObservableObject
{
    private readonly ApiService _apiService;
    private readonly AppShellViewModel _appShellViewModel;

    [ObservableProperty] private ObservableCollection<BadgeDisplayItem> _badges = [];
    [ObservableProperty] private bool _isLoading;

    public BadgesViewModel(ApiService apiService, AppShellViewModel appShellViewModel)
    {
        _apiService = apiService;
        _appShellViewModel = appShellViewModel;
    }

    [RelayCommand]
    public async Task LoadBadgesAsync()
    {
        IsLoading = true;
        try
        {
            var allBadges = await _apiService.GetAllBadgesAsync();

            var userBadgeMap = _appShellViewModel.CurrentUser?.UserBadges
                .ToDictionary(ub => ub.Badge.Id) ?? [];

            var items = allBadges
                .OrderBy(b => b.Rarity?.DisplayOrder ?? 0)
                .Select(badge =>
                {
                    var isUnlocked = userBadgeMap.ContainsKey(badge.Id);
                    var userBadge = isUnlocked
                        ? userBadgeMap[badge.Id]
                        : new UserBadgeDto { Badge = badge };
                    return new BadgeDisplayItem { UserBadge = userBadge, IsUnlocked = isUnlocked };
                });

            Badges = new ObservableCollection<BadgeDisplayItem>(items);
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
}
