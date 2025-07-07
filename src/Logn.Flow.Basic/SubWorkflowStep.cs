// Copyright (c) 2025 Codefrog
// Business Source License 1.1 – see LICENSE.txt for details.
// Change Date: 2029-07-01   Change License: Apache-2.0

using Logn.Flow.Engine;
using Microsoft.Extensions.DependencyInjection;

namespace Logn.Flow.Basic;

/// <summary>
/// Executes another workflow as an atomic step.
/// If the inner workflow finishes successfully the step returns <see cref="Success"/>.
/// If the inner workflow throws, the exception is wrapped in <see cref="Failure"/>.
/// </summary>
public class SubWorkflowStep(string workflowName, bool waitForResult = true) : IStep
{
    private readonly string _workflowName = workflowName ?? throw new ArgumentNullException(nameof(workflowName));

    public async ValueTask<IOutcome> ExecuteAsync(
        WorkflowContext ctx,
        CancellationToken ct = default)
    {
        var sp = ctx.Services
                 ?? throw new InvalidOperationException("IServiceProvider missing.");

        var runner = sp.GetRequiredService<WorkflowRunner>();

        try
        {
            await runner.RunAsync(_workflowName, null, waitForResult: waitForResult, ct: ct);
            return new Success();
        }
        catch (Exception ex)
        {
            return new Failure(ex);
        }
    }
}