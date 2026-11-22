using System.Windows.Input;

namespace IvanConnections_Travel.Controls;

public partial class FancySearchBar : ContentView
{
    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(FancySearchBar), default(string), BindingMode.TwoWay,
            propertyChanged: (b, _, n) => ((FancySearchBar)b).ClearButton.IsVisible = !string.IsNullOrEmpty(n as string));

    public static readonly BindableProperty PlaceholderProperty =
        BindableProperty.Create(nameof(Placeholder), typeof(string), typeof(FancySearchBar));

    public static readonly BindableProperty BorderThicknessProperty =
        BindableProperty.Create(nameof(BorderThickness), typeof(double), typeof(FancySearchBar), 1.0);

    public static readonly BindableProperty ClearCommandProperty =
        BindableProperty.Create(nameof(ClearCommand), typeof(ICommand), typeof(FancySearchBar));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public string Placeholder
    {
        get => (string)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    public double BorderThickness
    {
        get => (double)GetValue(BorderThicknessProperty);
        set => SetValue(BorderThicknessProperty, value);
    }

    /// <summary>Optional command executed when the ✕ button is tapped (after the text is cleared).</summary>
    public ICommand? ClearCommand
    {
        get => (ICommand?)GetValue(ClearCommandProperty);
        set => SetValue(ClearCommandProperty, value);
    }

    public FancySearchBar()
    {
        InitializeComponent();
    }

    /// <summary>Hides the soft keyboard and removes focus from the entry.</summary>
    public void Dismiss()
    {
        if (SearchBar.IsFocused) SearchBar.Unfocus();
        _ = SearchBar.HideSoftInputAsync(CancellationToken.None);
    }

    private void OnClearClicked(object? sender, EventArgs e)
    {
        Text = string.Empty;
        if (ClearCommand?.CanExecute(null) == true) ClearCommand.Execute(null);
        SearchBar.Focus();
    }

    private void OnCompleted(object? sender, EventArgs e) => Dismiss();
}
