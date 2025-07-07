// Copyright (c) 2025 Codefrog
// Business Source License 1.1 – see LICENSE.txt for details.
// Change Date: 2029-07-01   Change License: Apache-2.0

namespace Logn.Flow.Engine;

internal sealed class StepDispatcher(IPersistenceStore store, IScheduler scheduler, IEventBus bus)
{
    public async ValueTask DispatchAsync(
        IWorkflowDefinition def,
        WorkflowContext ctx,
        int stepIndex,
        CancellationToken ct)
    {
        if (stepIndex >= def.Steps.Count)
        {
            // workflow completed, persist final state and notify completion
            TryComplete(ctx);
            return;
        }

        var step = def.Steps[stepIndex];
        var outcome = await step.ExecuteAsync(ctx, ct);

        switch (outcome)
        {
            case Success:
                await PersistAndContinueAsync(def, ctx, stepIndex + 1, ct);
                break;

            case Failure f:
                TryFail(ctx, f.Error);
                await bus.PublishAsync(f, ct);
                break;

            case Wait w:
                // persist position & rely on external event to resume
                await store.SaveAsync(new(ctx.WorkflowId, stepIndex, DateTimeOffset.UtcNow), ct);
                await bus.PublishAsync(w, ct);
                break;

            case Delay d:
                var next = stepIndex + 1;
                await store.SaveAsync(new(ctx.WorkflowId, next, DateTimeOffset.UtcNow), ct);
                await scheduler.ScheduleAsync(
                    DateTimeOffset.UtcNow + d.Duration,
                    _ => PersistAndContinueAsync(def, ctx, next, CancellationToken.None),
                    ct);
                break;

            default:
                throw new NotSupportedException($"Unknown outcome type '{outcome.GetType().Name}'");
        }
    }

    private async ValueTask PersistAndContinueAsync(
        IWorkflowDefinition def,
        WorkflowContext ctx,
        int nextStep,
        CancellationToken ct)
    {
        await store.SaveAsync(new(ctx.WorkflowId, nextStep, DateTimeOffset.UtcNow), ct);
        await DispatchAsync(def, ctx, nextStep, ct);
    }

    private static void TryComplete(WorkflowContext ctx)
    {
        if (ctx.Items.TryGetValue("__completion__", out var o)
            && o is TaskCompletionSource<object?> tcs)
            tcs.TrySetResult(null);
    }

    private static void TryFail(WorkflowContext ctx, Exception ex)
    {
        if (ctx.Items.TryGetValue("__completion__", out var o)
            && o is TaskCompletionSource<object?> tcs)
            tcs.TrySetException(ex);
    }
}