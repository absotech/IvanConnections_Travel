using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace IvanConnections_Travel.ViewModels;

public sealed record OnboardingSlide(string Emoji, string Title, string Description);

public partial class OnboardingViewModel : ObservableObject
{
    public const string PrefOnboardingDoneKey = "OnboardingDone";

    /// <summary>Raised when the tour is finished or skipped; the page closes itself.</summary>
    public event EventHandler? Completed;

    public IReadOnlyList<OnboardingSlide> Slides { get; } =
    [
        new("🚌", "Atinge un vehicul",
            "Atinge orice autobuz sau tramvai de pe hartă pentru a vedea linia, direcția și următoarele stații."),
        new("📍", "Urmărește vehiculul",
            "Din fereastra vehiculului poți porni urmărirea: harta îl va centra automat pe măsură ce se deplasează."),
        new("🚏", "Stații & widgeturi",
            "Atinge o stație pentru sosirile în timp real și adaugă widgetul pe ecranul principal pentru acces rapid.")
    ];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLastSlide))]
    [NotifyPropertyChangedFor(nameof(NextButtonText))]
    private int _position;

    public bool IsLastSlide => Position >= Slides.Count - 1;

    public string NextButtonText => IsLastSlide ? "Începe" : "Înainte";

    public static bool IsOnboardingDone => Preferences.Default.Get(PrefOnboardingDoneKey, false);

    [RelayCommand]
    private void Next()
    {
        if (IsLastSlide)
        {
            Complete();
            return;
        }

        Position++;
    }

    [RelayCommand]
    private void Skip() => Complete();

    public void MarkDone() => Preferences.Default.Set(PrefOnboardingDoneKey, true);

    private void Complete()
    {
        MarkDone();
        Completed?.Invoke(this, EventArgs.Empty);
    }
}
