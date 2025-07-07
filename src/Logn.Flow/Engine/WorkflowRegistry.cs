// Copyright (c) 2025 Codefrog
// Business Source License 1.1 – see LICENSE.txt for details.
// Change Date: 2029-07-01   Change License: Apache-2.0

using System.Collections.Concurrent;

namespace Logn.Flow.Engine;

public sealed class WorkflowRegistry
{
    private readonly ConcurrentDictionary<string, IWorkflowDefinition> _defs;

    public WorkflowRegistry(Dictionary<string, IWorkflowDefinition>? defs = null)
    {
        _defs = defs is not null
            ? new ConcurrentDictionary<string, IWorkflowDefinition>(defs, StringComparer.Ordinal)
            : new ConcurrentDictionary<string, IWorkflowDefinition>(StringComparer.Ordinal);
    }

    public void Register(string name, IWorkflowDefinition def) =>
        _defs.TryAdd(name, def);

    public IWorkflowDefinition Get(string name) =>
        _defs.TryGetValue(name, out var def)
            ? def
            : throw new KeyNotFoundException($"Workflow '{name}' not found.");
}

internal interface IWorkflowWrapper
{
    IWorkflowDefinition InnerDefinition { get; }
}