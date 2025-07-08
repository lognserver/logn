// Copyright (c) 2025 Codefrog
// Business Source License 1.1 – see LICENSE.txt for details.
// Change Date: 2029-07-01   Change License: Apache-2.0

using Logn.Flow.Engine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Logn.Flow;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Flow services
    /// </summary>
    public static IServiceCollection AddLognFlow(
        this IServiceCollection services,
        Action<FlowOptionsBuilder> configure)
    {
        if (configure is null)
            throw new ArgumentNullException(nameof(configure));

        // core defaults for in-memory services, replace as-needed with a plugin architecture
        // community can add their own, we'll keep this flow lib as minimal as possible
        services.TryAddSingleton<IPersistenceStore, InMemoryStore>();
        services.TryAddSingleton<IEventBus, InMemoryEventBus>();
        services.TryAddSingleton<IScheduler, SystemScheduler>();

        // required services
        services.AddScoped<StepDispatcher>();
        services.AddScoped<WorkflowContext>(sp =>
        {
            var id = Ulid.NewUlid().ToString();
            return new WorkflowContext(id, sp);
        });

        // caller add their workflows
        var builder = new FlowOptionsBuilder();
        configure(builder);

        // build the registry upon finalization
        services.AddSingleton<WorkflowRegistry>(sp =>
        {
            var dict = new Dictionary<string, IWorkflowDefinition>(
                capacity: builder.Items.Count,
                comparer: StringComparer.Ordinal);

            foreach (var (name, factory) in builder.Items)
            {
                var def = factory(sp);
                if (!dict.TryAdd(name, def))
                    throw new InvalidOperationException(
                        $"Duplicate workflow name '{name}' detected.");
            }

            return new WorkflowRegistry(dict);
        });

        // register the runner as a singleton (TODO: maybe not?)
        services.AddSingleton<WorkflowRunner>(sp => new WorkflowRunner(
            sp.GetRequiredService<WorkflowRegistry>(),
            sp.GetRequiredService<IPersistenceStore>(),
            sp.GetRequiredService<IScheduler>(),
            sp.GetRequiredService<IEventBus>(),
            sp));

        return services;
    }
}