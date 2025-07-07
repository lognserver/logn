// Copyright (c) 2025 Codefrog
// Business Source License 1.1 – see LICENSE.txt for details.
// Change Date: 2029-07-01   Change License: Apache-2.0

namespace Logn.Flow;

// Marker interface
public interface IOutcome
{
}

public sealed record Success() : IOutcome;

public sealed record Failure(Exception Error) : IOutcome;

public sealed record Wait(string Message) : IOutcome;

public sealed record Delay(TimeSpan Duration) : IOutcome;