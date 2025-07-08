// Copyright (c) 2025 Codefrog
// Business Source License 1.1 – see LICENSE.txt for details.
// Change Date: 2029-07-01   Change License: Apache-2.0

using Logn.Flow;
using Logn.Flow.Basic;

namespace Logn.Core.Flows;

public sealed class UnsupportedGrantTypeFlow : IWorkflowDefinition
{
    public string Name => "unsupported_grant_type";

    public IReadOnlyList<IStep> Steps { get; } =
    [
        new LogStep("Unsupported grant type. Please check your request parameters.")
    ];
}