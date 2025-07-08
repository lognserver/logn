// Copyright (c) 2025 Codefrog
// Business Source License 1.1 – see LICENSE.txt for details.
// Change Date: 2029-07-01   Change License: Apache-2.0

using Logn.Flow.Engine;
using Microsoft.Extensions.DependencyInjection;

namespace Logn.Flow;

/// <summary>
/// Used to branch into two different steps based on the evaluation of a predicate.
///
/// For example, if an order is placed by a first‑time customer, we might want to expedite the order or
/// provide a welcome gift. If the customer is a returning one, we might want to apply a loyalty discount.
/// </summary>
public sealed class BranchStep(
    Func<WorkflowContext, bool> predicate,
    IStep whenTrue,
    IStep? whenFalse = null)
    : IStep
{
    private readonly Func<WorkflowContext, bool> _predicate =
        predicate ?? throw new ArgumentNullException(nameof(predicate));

    private readonly IStep _whenTrue = whenTrue ?? throw new ArgumentNullException(nameof(whenTrue));

    public async ValueTask<IOutcome> ExecuteAsync(
        WorkflowContext ctx,
        CancellationToken ct = default)
    {
        try
        {
            var step = _predicate(ctx) ? _whenTrue : whenFalse;

            if (step is null)
            {
                return new Success();
            }

            return await step.ExecuteAsync(ctx, ct);
        }
        catch (Exception ex)
        {
            return new Failure(ex);
        }
    }
}

// experimental
internal sealed class ConditionalStep(
    Func<WorkflowContext, bool> test,
    string ifTrue,
    string ifFalse) : IStep
{
    public ValueTask<IOutcome> ExecuteAsync(
        WorkflowContext ctx,
        CancellationToken _)
        => ValueTask.FromResult<IOutcome>(
            test(ctx) ? new Jump(ifTrue) : new Jump(ifFalse));
}

/// <summary>
/// Executes a sub-workflow based on a condition. For example, when fulfilling a digital delivery vs physical delivery.
/// </summary>
public sealed class ConditionalSubWorkflowStep : IStep
{
    // TODO: find a good common core for SubWorkflowStep and ConditionalSubWorkflowStep
    private readonly Func<WorkflowContext, bool> _predicate;
    private readonly (string Name, bool Wait) _trueBranch;
    private readonly (string Name, bool Wait) _falseBranch;
    private readonly Func<WorkflowContext, object?>? _inputSelector = null;

    public ConditionalSubWorkflowStep(
        Func<WorkflowContext, bool> predicate,
        string workflowWhenTrue,
        string? workflowWhenFalse = null,
        Func<WorkflowContext, object?>? inputSelector = null,
        bool waitForResult = true)
    {
        _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
        _trueBranch = (workflowWhenTrue ?? throw new ArgumentNullException(nameof(workflowWhenTrue)), waitForResult);
        _falseBranch = (workflowWhenFalse ?? "", waitForResult);
        _inputSelector = inputSelector;
    }

    public async ValueTask<IOutcome> ExecuteAsync(
        WorkflowContext ctx,
        CancellationToken ct = default)
    {
        try
        {
            var runner = ctx.Services?.GetRequiredService<WorkflowRunner>()
                         ?? throw new InvalidOperationException("WorkflowRunner not available via DI.");

            var (selected, wait) = _predicate(ctx) ? _trueBranch : _falseBranch;

            if (!string.IsNullOrWhiteSpace(selected))
                await runner.RunAsync(selected, instanceId: null, waitForResult: wait, init: c =>
                {
                    // pass the same service provider to the sub-workflow
                    c.Services = ctx.Services;

                    // pass the input to the sub-workflow or use the selector if provided
                    c.Input = _inputSelector?.Invoke(ctx) ?? ctx.Input;
                }, ct: ct);

            return new Success();
        }
        catch (Exception ex)
        {
            return new Failure(ex);
        }
    }
}