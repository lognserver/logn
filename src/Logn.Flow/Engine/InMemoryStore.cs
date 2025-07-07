// Copyright (c) 2025 Codefrog
// Business Source License 1.1 – see LICENSE.txt for details.
// Change Date: 2029-07-01   Change License: Apache-2.0

using System.Collections.Concurrent;

namespace Logn.Flow.Engine;

public sealed class InMemoryStore : IPersistenceStore
{
    private readonly ConcurrentDictionary<string, WorkflowState> _db = new();

    public ValueTask<WorkflowState?> LoadAsync(string id, CancellationToken ct = default) =>
        ValueTask.FromResult(_db.TryGetValue(id, out var s) ? s : null);

    public ValueTask SaveAsync(WorkflowState state, CancellationToken ct = default)
    {
        _db[state.Id] = state;
        return ValueTask.CompletedTask;
    }
}