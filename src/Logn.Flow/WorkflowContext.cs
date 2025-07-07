// Copyright (c) 2025 Codefrog
// Business Source License 1.1 – see LICENSE.txt for details.
// Change Date: 2029-07-01   Change License: Apache-2.0

namespace Logn.Flow;

public sealed class WorkflowContext
{
    public string WorkflowId { get; }
    public IServiceProvider? Services { get; init; }
    public IDictionary<string, object?> Items { get; } = new Dictionary<string, object?>(StringComparer.Ordinal);
    public WorkflowContext(string id) => WorkflowId = id;
}

public sealed record WorkflowState(
    string Id,
    int StepIndex,
    DateTimeOffset UpdatedAt);