using CommunityToolkit.Maui.Views;

namespace IvanConnections_Travel.Views.Popups;

/// <summary>
/// The visual type of the popup — controls icon, accent color, and icon background.
/// </summary>
public enum MessagePopupType
{
    Error,
    Warning,
    Info,
    Success,
    Question
}

/// <summary>
/// Which buttons are shown in the popup.
/// Ok          → single "OK" button  (returns true)
/// OkCancel    → "OK" + "Anulează"   (returns true / false)
/// YesNo       → "Da"  + "Nu"        (returns true / false)
/// </summary>
public enum MessagePopupButtons
{
    Ok,
    OkCancel,
    YesNo
}

public partial class MessagePopup : Popup
{
    // Glyph codes from MaterialSymbolsRounded font
    // warning: f083 | check/success: e5ca | question: e8fd | info: e88e | error: e000
    private static readonly Dictionary<MessagePopupType, (string Glyph, Color Accent)> TypeConfig = new()
    {
        [MessagePopupType.Error]    = ("\ue000", Color.FromArgb("#E53935")),
        [MessagePopupType.Warning]  = ("\uf083", Color.FromArgb("#FB8C00")),
        [MessagePopupType.Info]     = ("\ue88e", Color.FromArgb("#1E88E5")),
        [MessagePopupType.Success]  = ("\ue5ca", Color.FromArgb("#43A047")),
        [MessagePopupType.Question] = ("\ue8fd", Color.FromArgb("#5763FF")),
    };

    /// <summary>
    /// Parameterless constructor for DI.
    /// </summary>
    public MessagePopup()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Full constructor — use this for maximum control.
    /// </summary>
    public MessagePopup(
        string title,
        string message,
        MessagePopupType type = MessagePopupType.Info,
        MessagePopupButtons buttons = MessagePopupButtons.Ok)
    {
        InitializeComponent();
        Configure(title, message, type, buttons);
    }

    public void Configure(string title, string message, MessagePopupType type, MessagePopupButtons buttons)
    {
        var (glyph, accent) = TypeConfig[type];
        var isDark = Application.Current?.RequestedTheme == AppTheme.Dark;

        TitleLabel.Text = title;
        MessageLabel.Text = message;

        IconLabel.Text = glyph;
        IconLabel.TextColor = accent;
        IconBorder.BackgroundColor = accent.WithAlpha(isDark ? 0.20f : 0.13f);

        PrimaryButton.BackgroundColor = accent;

        switch (buttons)
        {
            case MessagePopupButtons.Ok:
                PrimaryButton.Text = "OK";
                SecondaryButton.IsVisible = false;
                break;

            case MessagePopupButtons.OkCancel:
                PrimaryButton.Text = "OK";
                SecondaryButton.Text = "Anulează";
                SecondaryButton.IsVisible = true;
                break;

            case MessagePopupButtons.YesNo:
                PrimaryButton.Text = "Da";
                SecondaryButton.Text = "Nu";
                SecondaryButton.IsVisible = true;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(buttons), buttons, null);
        }
    }

    private void OnPrimaryButtonClicked(object sender, EventArgs e) => Close(true);
    private void OnSecondaryButtonClicked(object sender, EventArgs e) => Close(false);

    // -------------------------------------------------------------------------
    // Static factory helpers for the most common scenarios
    // -------------------------------------------------------------------------

    /// <summary>Shows an error popup with a single OK button.</summary>
    public static MessagePopup Error(string message, string title = "Eroare")
        => new(title, message, MessagePopupType.Error, MessagePopupButtons.Ok);

    /// <summary>Shows a warning popup with a single OK button.</summary>
    public static MessagePopup Warning(string message, string title = "Atenție")
        => new(title, message, MessagePopupType.Warning, MessagePopupButtons.Ok);

    /// <summary>Shows an informational popup with a single OK button.</summary>
    public static MessagePopup Info(string message, string title = "Informație")
        => new(title, message, MessagePopupType.Info, MessagePopupButtons.Ok);

    /// <summary>Shows a success popup with a single OK button.</summary>
    public static MessagePopup Success(string message, string title = "Succes")
        => new(title, message, MessagePopupType.Success, MessagePopupButtons.Ok);

    /// <summary>Shows a question popup. Defaults to Yes/No buttons.</summary>
    public static MessagePopup Question(
        string message,
        string title = "Confirmare",
        MessagePopupButtons buttons = MessagePopupButtons.YesNo)
        => new(title, message, MessagePopupType.Question, buttons);
    
    public static async Task<bool> ShowAsync(string title, string message, MessagePopupType type, MessagePopupButtons buttons)
    {
        var popup = new MessagePopup(title, message, type, buttons);
        var page = Shell.Current.CurrentPage;
        var result = await page.ShowPopupAsync(popup);
        return result is true;
    }
}
