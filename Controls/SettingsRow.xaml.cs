using System.Windows.Input;

namespace IvanConnections_Travel.Controls;

/// <summary>
/// Reusable settings card: icon/emoji, title, optional subtitle and a trailing slot
/// (e.g. a <see cref="Switch"/> or <see cref="Picker"/>). Optionally tappable via <see cref="Command"/>.
/// </summary>
[ContentProperty(nameof(TrailingContent))]
public partial class SettingsRow : ContentView
{
    public static readonly BindableProperty IconProperty =
        BindableProperty.Create(nameof(Icon), typeof(string), typeof(SettingsRow), null,
            propertyChanged: (b, _, _) => ((SettingsRow)b).OnPropertyChanged(nameof(HasIcon)));

    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(nameof(Title), typeof(string), typeof(SettingsRow), string.Empty);

    public static readonly BindableProperty SubtitleProperty =
        BindableProperty.Create(nameof(Subtitle), typeof(string), typeof(SettingsRow), null,
            propertyChanged: (b, _, _) => ((SettingsRow)b).OnPropertyChanged(nameof(HasSubtitle)));

    public static readonly BindableProperty TrailingContentProperty =
        BindableProperty.Create(nameof(TrailingContent), typeof(View), typeof(SettingsRow), null);

    public static readonly BindableProperty CommandProperty =
        BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(SettingsRow), null);

    public static readonly BindableProperty CommandParameterProperty =
        BindableProperty.Create(nameof(CommandParameter), typeof(object), typeof(SettingsRow), null);

    public string? Icon
    {
        get => (string?)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string? Subtitle
    {
        get => (string?)GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    public View? TrailingContent
    {
        get => (View?)GetValue(TrailingContentProperty);
        set => SetValue(TrailingContentProperty, value);
    }

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    public bool HasIcon => !string.IsNullOrEmpty(Icon);

    public bool HasSubtitle => !string.IsNullOrEmpty(Subtitle);

    public SettingsRow()
    {
        InitializeComponent();
    }

    private void OnRowTapped(object? sender, TappedEventArgs e)
    {
        if (Command?.CanExecute(CommandParameter) == true)
            Command.Execute(CommandParameter);
    }
}
