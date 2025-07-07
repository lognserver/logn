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

public sealed class SubWorkflowStep<T>(bool waitForResult = true) : SubWorkflowStep(typeof(T).Name, waitForResult)
    where T : IWorkflowDefinition;