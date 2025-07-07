// Copyright (c) 2025 Codefrog
// Business Source License 1.1 – see LICENSE.txt for details.
// Change Date: 2029-07-01   Change License: Apache-2.0

using System.Runtime.CompilerServices;

namespace Logn.Flow.Engine;

internal sealed class StepDispatcher(IPersistenceStore store, IScheduler scheduler, IEventBus bus)
{
    private static readonly ConditionalWeakTable<IWorkflowDefinition, Dictionary<string, int>>
        IndexCache = new();

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

            case Jump j:
                var target = GetIndex(def, j.NextStepName);
                await PersistAndContinueAsync(def, ctx, target, ct);
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

    private static int GetIndex(IWorkflowDefinition def, string name)
    {
        var map = IndexCache.GetValue(def, Build);

        return map.TryGetValue(name, out var idx)
            ? idx
            : throw new KeyNotFoundException(
                $"Step name '{name}' not found in workflow.");

        static Dictionary<string, int> Build(IWorkflowDefinition d)
        {
            var dict = new Dictionary<string, int>(StringComparer.Ordinal);
            for (var i = 0; i < d.Steps.Count; i++)
            {
                if (d.Steps[i] is NamedStep ns)
                {
                    dict[ns.Name] = i;
                }
            }

            return dict;
        }
    }
}

public sealed record Jump(string NextStepName) : IOutcome;

/// <summary>
/// Wraps any <see cref="IStep"/> with a name that can be the target of a
/// <see cref="Jump"/> outcome.
/// </summary>
public sealed class NamedStep : IStep
{
    public string Name { get; }
    private IStep Inner { get; }

    public NamedStep(string name, IStep inner)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    public ValueTask<IOutcome> ExecuteAsync(
        WorkflowContext ctx,
        CancellationToken ct = default)
        => Inner.ExecuteAsync(ctx, ct);
}

public static class Step
{
    public static NamedStep Label(string name, IStep inner) => new(name, inner);
}