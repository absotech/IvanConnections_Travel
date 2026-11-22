using CommunityToolkit.Maui.Views;
using IvanConnections_Travel.ViewModels.Popups;

namespace IvanConnections_Travel.Views.Popups;

public partial class StopPopup : Popup
{
    // Popup-ul nu trebuie să depășească ecranul, iar acțiunile de jos rămân mereu
    // vizibile: doar lista de sosiri se derulează.
    private const double MaxScreenHeightRatio = 0.88;
    private const double MinPopupHeight = 320;
    private const double MinListHeight = 120;
    private const double MinSideMargin = 32;
    private const double FallbackHeaderHeight = 84;
    private const double FallbackActionsHeight = 60;

    private double PopupHeight;

    private readonly StopPopupViewModel ViewModel;
    private bool IsClosed;

    public StopPopup(StopPopupViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = ViewModel = viewModel;

        ApplyResponsiveSizing();
        HeaderBorder.SizeChanged += OnSectionSizeChanged;
        ActionsPanel.SizeChanged += OnSectionSizeChanged;
        DeviceDisplay.MainDisplayInfoChanged += OnMainDisplayInfoChanged;
        ViewModel.CloseRequested += OnCloseRequested;

        // Reîmprospătarea automată rulează doar cât timp popup-ul este afișat.
        Loaded += OnPopupLoaded;
        Unloaded += OnPopupUnloaded;
        Closed += (s, e) =>
        {
            IsClosed = true;
            ViewModel.StopAutoRefresh();
            ViewModel.CloseRequested -= OnCloseRequested;
            Loaded -= OnPopupLoaded;
            Unloaded -= OnPopupUnloaded;
            DeviceDisplay.MainDisplayInfoChanged -= OnMainDisplayInfoChanged;
            HeaderBorder.SizeChanged -= OnSectionSizeChanged;
            ActionsPanel.SizeChanged -= OnSectionSizeChanged;
        };
    }

    private void OnPopupLoaded(object? sender, EventArgs e)
    {
        if (!IsClosed)
        {
            ViewModel.StartAutoRefresh();
        }
    }

    private void OnPopupUnloaded(object? sender, EventArgs e) => ViewModel.StopAutoRefresh();

    private async void OnCloseRequested(object? sender, EventArgs e)
    {
        if (IsClosed)
        {
            return;
        }

        try
        {
            await CloseAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Nu s-a putut închide popup-ul stației: {ex.Message}");
        }
    }

    private void OnMainDisplayInfoChanged(object? sender, DisplayInfoChangedEventArgs e)
        => MainThread.BeginInvokeOnMainThread(ApplyResponsiveSizing);

    private void OnSectionSizeChanged(object? sender, EventArgs e) => UpdateListMaxHeight();

    /// <summary>
    /// Limitează popup-ul la o fracțiune din ecran, astfel încât să rămână utilizabil
    /// și pe ecrane mici sau cu text/afișare mărită.
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

        UpdateListMaxHeight();
    }

    /// <summary>
    /// Rândurile grilei sunt Auto, deci cardul se strânge exact pe conținut (fără spațiu
    /// gol când sunt puține sosiri). Singura limită este înălțimea maximă a zonei
    /// derulabile: spațiul rămas din popup, minus antetul și butoanele de acțiune.
    /// </summary>
    private void UpdateListMaxHeight()
    {
        if (PopupHeight <= 0)
        {
            return;
        }

        var headerHeight = HeaderBorder.Height > 0 ? HeaderBorder.Height : FallbackHeaderHeight;
        var actionsHeight = ActionsPanel.Height > 0 ? ActionsPanel.Height : FallbackActionsHeight;
        var available = PopupHeight - headerHeight - actionsHeight;

        DetailsScroll.MaximumHeightRequest = Math.Max(MinListHeight, available);
    }

    private async void CloseButton_Clicked(object sender, EventArgs e)
    {
        ViewModel.StopAutoRefresh();
        await CloseAsync();
    }
}
