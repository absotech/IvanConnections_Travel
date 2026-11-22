namespace IvanConnections_Travel.Utils;

/// <summary>
/// Small, safe wrapper around MAUI haptic feedback (never throws on unsupported devices).
/// </summary>
public static class Haptics
{
    public static void Click() => Perform(HapticFeedbackType.Click);

    public static void Confirm() => Perform(HapticFeedbackType.LongPress);

    private static void Perform(HapticFeedbackType type)
    {
        try
        {
            if (HapticFeedback.Default.IsSupported)
                HapticFeedback.Default.Perform(type);
        }
        catch (Exception)
        {
            // Haptics are a nicety – ignore failures.
        }
    }
}
