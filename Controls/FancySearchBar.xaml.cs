namespace IvanConnections_Travel.Controls;

public partial class FancySearchBar : ContentView
{
    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(FancySearchBar), default(string), BindingMode.TwoWay);

    public static readonly BindableProperty PlaceholderProperty =
        BindableProperty.Create(nameof(Placeholder), typeof(string), typeof(FancySearchBar));
    
    public static readonly BindableProperty BorderThicknessProperty =
        BindableProperty.Create(nameof(BorderThickness), typeof(double), typeof(FancySearchBar), 1.0);

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

    public FancySearchBar()
    {
        InitializeComponent();
    }
}