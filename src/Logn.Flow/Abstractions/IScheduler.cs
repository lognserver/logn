// Copyright (c) 2025 Codefrog
// Business Source License 1.1 – see LICENSE.txt for details.
// Change Date: 2029-07-01   Change License: Apache-2.0

namespace Logn.Flow;

public interface IScheduler
{
    /// <summary>Schedules the callback and returns a handle that can cancel it.</summary>
    ValueTask<IDisposable> ScheduleAsync(
        DateTimeOffset when,
        Func<CancellationToken, ValueTask> callback,
        CancellationToken ct = default);
}