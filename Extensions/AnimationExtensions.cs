namespace IvanConnections_Travel.Extensions;

public static class AnimationExtensions
{
    public static Task AnimateGradientStop(
        VisualElement parent,
        GradientStop stop,
        double to,
        uint length)
    {
        var tcs = new TaskCompletionSource<bool>();

        var animation = new Animation(
            v => stop.Offset = (float)v,
            stop.Offset,
            to
        );

        animation.Commit(
            parent,
            Guid.NewGuid().ToString(),
            16,
            length,
            Easing.Linear,
            (v, c) => tcs.SetResult(true)
        );

        return tcs.Task;
    }
}