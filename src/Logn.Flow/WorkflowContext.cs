// Copyright (c) 2025 Codefrog
// Business Source License 1.1 – see LICENSE.txt for details.
// Change Date: 2029-07-01   Change License: Apache-2.0

using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;

[assembly: InternalsVisibleTo("Logn.Flow.Test")]

namespace Logn.Flow;

public sealed class WorkflowContext(string id, IServiceProvider provider)
{
    /// <summary>
    /// The unique identifier of the workflow instance.
    /// </summary>
    /// <remarks>This is a string over a GUID to allow flexibility. By default, we will use ULID strings.</remarks>
    public string WorkflowId { get; } = id;

    /// <summary>
    /// Ensures that workflow steps can resolve dependencies.
    /// </summary>
    public IServiceProvider Services { get; set; } = provider;

    /// <summary>
    /// Information about the current state of the workflow.
    /// </summary>
    internal IDictionary<string, object?> Items { get; } = new Dictionary<string, object?>(StringComparer.Ordinal);

    /// <summary>
    /// The input provided by the caller to the workflow.
    /// </summary>
    public object? Input { get; set; }

    /// <summary>
    /// Output from the workflow, typically set by the last step.
    /// </summary>
    public object? Output { get; set; }
}

public static class WorkflowContextExt
{
    public static T GetInput<T>(this WorkflowContext ctx)
        => ctx.Input is T t
            ? t
            : throw new InvalidOperationException($"Expected input of type {typeof(T).Name}");

    public static void SetOutput<T>(this WorkflowContext ctx, T value)
        => ctx.Output = value;

    public static T? GetOutput<T>(this WorkflowContext ctx)
        => ctx.Output is T t ? t : default;
    
    // experimental
    internal static T Get<T>(this WorkflowContext ctx, string key)
        => ctx.Items.TryGetValue(key, out var o) && o is T t
            ? t
            : throw new KeyNotFoundException(key);

    // experimental
    internal static void Set<T>(this WorkflowContext ctx, string key, T value) =>
        ctx.Items[key] = value;
}

public sealed record WorkflowState(
    string Id,
    int StepIndex,
    DateTimeOffset UpdatedAt);