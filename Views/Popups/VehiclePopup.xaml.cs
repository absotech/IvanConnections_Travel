using CommunityToolkit.Maui.Views;
using IvanConnections_Travel.ViewModels.Popups;
using System.Collections.Specialized;
using System.ComponentModel;
namespace IvanConnections_Travel.Views.Popups;
public partial class VehiclePopup : Popup<bool>
{
    // Cât din ecran poate ocupa popup-ul și cât din el este rezervat chatului,
    // astfel încât panoul de chat să fie mereu vizibil integral.
    private const double MaxScreenHeightRatio = 0.88;
    private const double ChatHeightRatio = 0.45;
    private const double MinPopupHeight = 320;
    private const double MinChatHeight = 170;
    private const double MaxChatHeight = 380;
    private const double MinDetailsHeight = 96;
    private const double MinSideMargin = 32;
    private const double FallbackHeaderHeight = 84;
    private const double ChatBottomMargin = 16;

    private double PopupHeight;
    private double ChatHeight;

    private readonly VehiclePopupViewModel VehiclePopupViewModel;
    public VehiclePopup(VehiclePopupViewModel vehiclePopupViewModel)
    {
        InitializeComponent();
        BindingContext = VehiclePopupViewModel = vehiclePopupViewModel;
        VehiclePopupViewModel.FollowVehicle += async (s, e) =>
        {
            await CloseAsync(e ?? false);
        };
        VehiclePopupViewModel.Messages.CollectionChanged += OnMessagesChanged;
        VehiclePopupViewModel.PropertyChanged += OnViewModelPropertyChanged;
        ApplyResponsiveSizing();
        HeaderBorder.SizeChanged += OnHeaderSizeChanged;
        DeviceDisplay.MainDisplayInfoChanged += OnMainDisplayInfoChanged;
        Closed += async (s, e) =>
        {
            DeviceDisplay.MainDisplayInfoChanged -= OnMainDisplayInfoChanged;
            HeaderBorder.SizeChanged -= OnHeaderSizeChanged;
            VehiclePopupViewModel.Messages.CollectionChanged -= OnMessagesChanged;
            VehiclePopupViewModel.PropertyChanged -= OnViewModelPropertyChanged;
            await VehiclePopupViewModel.CleanupAsync();
        };
    }

    private void OnMainDisplayInfoChanged(object? sender, DisplayInfoChangedEventArgs e)
        => MainThread.BeginInvokeOnMainThread(ApplyResponsiveSizing);

    private void OnHeaderSizeChanged(object? sender, EventArgs e) => UpdateDetailsMaxHeight();

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(VehiclePopupViewModel.IsChatExpanded))
        {
            return;
        }

        // Zona de detalii primește tot spațiul disponibil când chatul este închis
        // și îl cedează chatului când acesta se deschide.
        UpdateDetailsMaxHeight();

        if (!VehiclePopupViewModel.IsChatExpanded)
        {
            return;
        }

        // După deschiderea chatului, zona de detalii se micșorează, așa că o derulăm
        // până la butoanele de acțiune, ca utilizatorul să vadă contextul de lângă chat.
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await Task.Delay(120);
            await DetailsScroll.ScrollToAsync(0, DetailsScroll.ContentSize.Height, true);
        });
    }

    /// <summary>
    /// Limitează popup-ul la o fracțiune din ecran și împarte spațiul disponibil între
    /// detalii (care se derulează) și chat (care rămâne mereu complet vizibil). Astfel
    /// popup-ul rămâne utilizabil și pe ecrane mici sau cu text/afișare mărită.
    /// </summary>
    private void ApplyResponsiveSizing()
    {
        var display = DeviceDisplay.MainDisplayInfo;
        var density = display.Density > 0 ? display.Density : 1;
        var screenHeight = display.Height / density;
        var screenWidth = display.Width / density;

        if (screenHeight <= 0 || screenWidth <= 0)
        {
            return;
        }

        PopupHeight = Math.Max(MinPopupHeight, screenHeight * MaxScreenHeightRatio);
        RootCard.MaximumHeightRequest = PopupHeight;
        RootCard.MaximumWidthRequest = Math.Max(300, Math.Min(400, screenWidth - MinSideMargin));

        // Chatul primește o parte din înălțimea disponibilă, dar niciodată atât de mult
        // încât detaliile să dispară complet.
        ChatHeight = Math.Clamp(PopupHeight * ChatHeightRatio, MinChatHeight, MaxChatHeight);
        ChatHeight = Math.Min(ChatHeight, Math.Max(MinChatHeight, PopupHeight - MinDetailsHeight));
        ChatPanel.HeightRequest = ChatHeight;
        ChatPanel.MaximumHeightRequest = ChatHeight;

        UpdateDetailsMaxHeight();
    }

    /// <summary>
    /// Rândurile grilei sunt Auto, deci cardul se strânge exact pe conținut și nu mai
    /// rămâne spațiu gol sub detalii când chatul este închis. Singura limită este
    /// înălțimea maximă a zonei derulabile: tot spațiul rămas din popup, minus antetul
    /// și (doar când chatul este deschis) spațiul rezervat panoului de chat.
    /// </summary>
    private void UpdateDetailsMaxHeight()
    {
        if (PopupHeight <= 0)
        {
            return;
        }

        var headerHeight = HeaderBorder.Height > 0 ? HeaderBorder.Height : FallbackHeaderHeight;
        var chatAllowance = VehiclePopupViewModel.IsChatExpanded ? ChatHeight + ChatBottomMargin : 0;
        var available = PopupHeight - headerHeight - chatAllowance;

        DetailsScroll.MaximumHeightRequest = Math.Max(MinDetailsHeight, available);
    }
    private async void CloseButton_Clicked(object sender, EventArgs e)
    {
        await CloseAsync(false);
    }

    private void OnMessagesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add && VehiclePopupViewModel.Messages.Count > 0)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Task.Delay(100); 
                var lastIndex = VehiclePopupViewModel.Messages.Count - 1;
                MessagesCollection.ScrollTo(
                    index: lastIndex, 
                    groupIndex: -1,
                    position: ScrollToPosition.End, 
                    animate: true);
            });
        }
    }
}
