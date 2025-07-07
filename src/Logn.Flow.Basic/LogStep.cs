// Copyright (c) 2025 Codefrog
// Business Source License 1.1 – see LICENSE.txt for details.
// Change Date: 2029-07-01   Change License: Apache-2.0

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Logn.Flow.Basic;

/// <summary>
/// Logs a message to the configured logger or console.
/// </summary>
public sealed class LogStep(string message) : IStep
{
    public ValueTask<IOutcome> ExecuteAsync(WorkflowContext context, CancellationToken cancellation = default)
    {
        var logger = context.Services?
            .GetService<ILogger<LogStep>>();

        if (logger is not null)
        {
            logger.Log(LogLevel.Information,
                new EventId(0, nameof(LogStep)),
                message,
                null,
                (state, ex) => state.ToString());
        }
        else
        {
            Console.WriteLine(message);
        }

        return ValueTask.FromResult<IOutcome>(new Success());
    }
}

/// <summary>
/// Executes arbitrary C# code as a workflow step.
/// Any uncaught exception is wrapped in <see cref="Failure"/>.
/// </summary>
public sealed class CodeStep : IStep
{
    private readonly Func<WorkflowContext, CancellationToken, ValueTask<IOutcome>> _body;

    /// <summary>
    /// Supply a delegate that returns an <see cref="IOutcome"/>.
    /// </summary>
    public CodeStep(Func<WorkflowContext, CancellationToken, ValueTask<IOutcome>> body)
        => _body = body ?? throw new ArgumentNullException(nameof(body));

    /// <summary>
    /// Supply an action; success is assumed if it completes.
    /// </summary>
    public CodeStep(Action<WorkflowContext> action)
        : this((ctx, _) =>
        {
            action(ctx);
            return ValueTask.FromResult<IOutcome>(new Success());
        })
    {
    }

    /// <summary>
    /// Supply an async action; success is assumed if it awaits.
    /// </summary>
    public CodeStep(Func<WorkflowContext, CancellationToken, Task> asyncAction)
        : this(async (ctx, ct) =>
        {
            await asyncAction(ctx, ct).ConfigureAwait(false);
            return new Success();
        })
    {
    }

    public async ValueTask<IOutcome> ExecuteAsync(
        WorkflowContext ctx,
        CancellationToken ct = default)
    {
        try
        {
            return await _body(ctx, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return new Failure(ex);
        }
    }
}