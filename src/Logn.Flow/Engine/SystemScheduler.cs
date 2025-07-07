// Copyright (c) 2025 Codefrog
// Business Source License 1.1 – see LICENSE.txt for details.
// Change Date: 2029-07-01   Change License: Apache-2.0

namespace Logn.Flow.Engine;

public sealed class SystemScheduler : IScheduler
{
    public ValueTask<IDisposable> ScheduleAsync(
        DateTimeOffset when,
        Func<CancellationToken, ValueTask> callback,
        CancellationToken ct = default)
    {
        var cts = new CancellationTokenSource();
        _ = Task.Run(async () =>
        {
            var delay = when - DateTimeOffset.UtcNow;
            if (delay > TimeSpan.Zero)
                await Task.Delay(delay, cts.Token).ConfigureAwait(false);

            if (!cts.IsCancellationRequested)
                await callback(cts.Token).ConfigureAwait(false);
        }, cts.Token);

        return ValueTask.FromResult<IDisposable>(cts);
    }
}