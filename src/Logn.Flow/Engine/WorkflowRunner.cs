// Copyright (c) 2025 Codefrog
// Business Source License 1.1 – see LICENSE.txt for details.
// Change Date: 2029-07-01   Change License: Apache-2.0

namespace Logn.Flow.Engine;

public sealed class WorkflowRunner(
    WorkflowRegistry registry,
    IPersistenceStore store,
    IScheduler scheduler,
    IEventBus bus,
    IServiceProvider? services = null)
{
    private readonly StepDispatcher _dispatcher = new(store, scheduler, bus);

    /// <summary>Creates or resumes a workflow instance.</summary>
    public async ValueTask RunAsync<T>(
        string? instanceId = null,
        bool waitForResult = false,
        CancellationToken ct = default)
        where T : IWorkflowDefinition
    {
        var name = typeof(T).Name;
        var def = registry.Get(name);

        if (def is null)
        {
            throw new KeyNotFoundException($"Workflow '{name}' not found.");
        }

        await RunAsync(def, instanceId, waitForResult, ct);
    }

    /// <summary>
    /// Creates or resumes a workflow instance.
    /// Prefer using the generic overload with a type parameter.
    /// </summary>
    public async ValueTask RunAsync(
        string workflowName,
        string? instanceId,
        bool waitForResult = false,
        CancellationToken ct = default)
    {
        var def = registry.Get(workflowName);
        if (def is null)
        {
            throw new KeyNotFoundException($"Workflow '{workflowName}' not found.");
        }

        await RunAsync(def, instanceId, waitForResult, ct);
    }

    private async ValueTask RunAsync(
        IWorkflowDefinition def,
        string? instanceId = null,
        bool waitForResult = false,
        CancellationToken ct = default)
    {
        var id = instanceId ?? Ulid.NewUlid().ToString();
        var ctx = new WorkflowContext(id)
        {
            Services = services
        };
        var state = await store.LoadAsync(id, ct);

        var startIndex = state?.StepIndex ?? 0;

        Task? waiter = null;

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
            await waiter;
        }
    }
}