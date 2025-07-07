// Copyright (c) 2025 Codefrog
// Business Source License 1.1 – see LICENSE.txt for details.
// Change Date: 2029-07-01   Change License: Apache-2.0

namespace Logn.Flow.Basic;

/// <summary>
/// Delays the workflow execution for a specified duration.
/// </summary>
public sealed class DelayStep(TimeSpan t) : IStep
{
    public ValueTask<IOutcome> ExecuteAsync(
        WorkflowContext _, CancellationToken __) =>
        ValueTask.FromResult<IOutcome>(new Delay(t));
}