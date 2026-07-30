using Microsoft.Extensions.Options;
using OCAP.Core.Events.Distributed;

namespace OCAP.Infrastructure.Events.Distributed;

public sealed class ExponentialBackoffRetryPolicy : IMessageRetryPolicy
{
    private readonly EventBusOptions _options;

    public ExponentialBackoffRetryPolicy(IOptions<EventBusOptions> options)
    {
        _options = options?.Value ?? new EventBusOptions();
    }

    public int MaxRetries => Math.Max(1, _options.MaxRetries);

    public TimeSpan GetDelay(int attempt)
    {
        var capped = Math.Min(Math.Max(attempt, 1), 8);
        var seconds = Math.Min(60, Math.Pow(2, capped));
        return TimeSpan.FromSeconds(seconds);
    }

    public async Task ExecuteWithRetryAsync(Func<Task> action, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        Exception? last = null;

        for (var attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                await action();
                return;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                last = ex;
                if (attempt >= MaxRetries)
                {
                    break;
                }

                await Task.Delay(GetDelay(attempt), cancellationToken);
            }
        }

        throw new InvalidOperationException($"Action failed after {MaxRetries} retries.", last);
    }
}
