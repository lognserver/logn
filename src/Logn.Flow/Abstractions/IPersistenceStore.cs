// Copyright (c) 2025 Codefrog
// Business Source License 1.1 – see LICENSE.txt for details.
// Change Date: 2029-07-01   Change License: Apache-2.0

namespace Logn.Flow;

public interface IPersistenceStore
{
    ValueTask SaveAsync(WorkflowState state, CancellationToken ct = default);
    ValueTask<WorkflowState?> LoadAsync(string id, CancellationToken ct = default);
}