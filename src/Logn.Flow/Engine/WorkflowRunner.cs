// Copyright (c) 2025 Codefrog
// Business Source License 1.1 – see LICENSE.txt for details.
// Change Date: 2029-07-01   Change License: Apache-2.0

namespace Logn.Flow.Engine;

public sealed class WorkflowRunner(
    WorkflowRegistry registry,
    IPersistenceStore store,
    IScheduler scheduler,
    IEventBus bus,
    IServiceProvider services)
{
    private readonly StepDispatcher _dispatcher = new(store, scheduler, bus);

    /// <summary>Creates or resumes a workflow instance.</summary>
    public async ValueTask RunAsync<T>(
        string? instanceId = null,
        bool waitForResult = false,
        Action<WorkflowContext>? init = null,
        CancellationToken ct = default)
        where T : IWorkflowDefinition
    {
        var name = typeof(T).Name;
        var def = registry.Get(name);

        if (def is null)
        {
            throw new KeyNotFoundException($"Workflow '{name}' not found.");
        }

        await RunAsync(def, instanceId, waitForResult, init, ct);
    }

    public async ValueTask<TResult?> RunAsync<TFlow, TInput, TResult>(
        TInput input,
        string? instanceId = null,
        CancellationToken ct = default)
        where TFlow : IWorkflowDefinition
    {
        // Re‑use the existing internal pipeline; always wait for completion.
        object? boxed = await RunAsync(
            workflowName: typeof(TFlow).Name,
            instanceId: instanceId,
            waitForResult: true,
            init: ctx => ctx.Input = input,
            ct: ct);

        return (TResult?)boxed;
    }

    public ValueTask<object?> RunAsync<TFlow, TInput>(
        TInput input,
        string? instanceId = null,
        bool wait = false,
        Action<WorkflowContext>? initCtx = null,
        CancellationToken ct = default)
        where TFlow : IWorkflowDefinition
    {
        return RunAsync(typeof(TFlow).Name, instanceId, wait,
            ctx =>
            {
                ctx.Input = input;
                initCtx?.Invoke(ctx);
            }, ct);
    }

    /// <summary>
    /// Creates or resumes a workflow instance.
    /// Prefer using the generic overload with a type parameter.
    /// </summary>
    public async ValueTask<object?> RunAsync(
        string workflowName,
        string? instanceId,
        bool waitForResult = false,
        Action<WorkflowContext>? init = null,
        CancellationToken ct = default)
    {
        var def = registry.Get(workflowName);
        if (def is null)
        {
            throw new KeyNotFoundException($"Workflow '{workflowName}' not found.");
        }

        return await RunAsync(def, instanceId, waitForResult, init, ct);
    }

    private async ValueTask<object?> RunAsync(
        IWorkflowDefinition def,
        string? instanceId = null,
        bool waitForResult = false,
        Action<WorkflowContext>? init = null,
        CancellationToken ct = default)
    {
        var id = instanceId ?? Ulid.NewUlid().ToString();
        var ctx = new WorkflowContext(id, services);

        // initialize context with user-supplied action
        init?.Invoke(ctx);

        var state = await store.LoadAsync(id, ct);

        var startIndex = state?.StepIndex ?? 0;

        Task<object?>? waiter = null;

        if (waitForResult)
        {
            var tcs = new TaskCompletionSource<object?>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            // hand off to dispatcher
            ctx.Items["__completion__"] = tcs;
            waiter = tcs.Task;
        }

        await _dispatcher.DispatchAsync(def, ctx, startIndex, ct);

        if (waiter is not null)
        {
            return await waiter;
        }

        return null;
    }
}