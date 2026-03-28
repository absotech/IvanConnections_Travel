namespace IvanConnections_Travel.Utils;

public class Debouncer
{
    private CancellationTokenSource? _cts  = new();

    public async Task RunAsync(Func<Task> action, int delayMs = 20)
    {
        await _cts?.CancelAsync()!;
        _cts = new CancellationTokenSource();

        try
        {
            await Task.Delay(delayMs, _cts.Token);
            await action();
        }
        catch (TaskCanceledException)
        {
            
        }
    }
}