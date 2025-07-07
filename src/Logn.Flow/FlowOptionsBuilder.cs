// Copyright (c) 2025 Codefrog
// Business Source License 1.1 – see LICENSE.txt for details.
// Change Date: 2029-07-01   Change License: Apache-2.0

using Microsoft.Extensions.DependencyInjection;

namespace Logn.Flow;

public sealed class FlowOptionsBuilder
{
    internal List<(string Name,
        Func<IServiceProvider, IWorkflowDefinition> Factory)> Items { get; }
        = new();

    /// <summary>
    /// Registers a workflow *type* under a required name.
    /// </summary>
    public FlowOptionsBuilder AddWorkflow<T>()
        where T : class, IWorkflowDefinition
    {
        var type = typeof(T);
        AddWorkflow<T>(type.Name);
        return this;
    }

    /// <summary>
    /// Registers a workflow *type* under a required name.
    /// </summary>
    public FlowOptionsBuilder AddWorkflow<T>(string name)
        where T : class, IWorkflowDefinition
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Workflow name must be non‑empty.", nameof(name));

        Items.Add((name, sp => ActivatorUtilities.CreateInstance<T>(sp)));
        return this;
    }
}