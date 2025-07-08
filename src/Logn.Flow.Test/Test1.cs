// Copyright (c) 2025 Codefrog
// Business Source License 1.1 – see LICENSE.txt for details.
// Change Date: 2029-07-01   Change License: Apache-2.0

using Logn.Flow.Basic;
using Logn.Flow.Engine;
using Microsoft.Extensions.DependencyInjection;

namespace Logn.Flow.Test;

[TestClass]
public sealed class Test1
{
    [TestMethod]
    public async Task HelloWorkflow()
    {
        var registry = new WorkflowRegistry();
        registry.Register("hello", new HelloWorkflow());

        var runner = new WorkflowRunner(
            registry,
            new InMemoryStore(),
            new SystemScheduler(),
            new InMemoryEventBus(), new ServiceCollection().BuildServiceProvider());

        await runner.RunAsync("hello", null, waitForResult: true);
    }

    [TestMethod]
    public async Task NestedWorkflowRunsToCompletion()
    {
        var services = new ServiceCollection()
            .AddLognFlow(flow =>
            {
                flow.AddWorkflow<FooWorkflow>()
                    .AddWorkflow<BarWorkflow>();
            });
        var serviceProvider = services.BuildServiceProvider();
        var runner = serviceProvider.GetRequiredService<WorkflowRunner>();
        await runner.RunAsync<FooWorkflow>(waitForResult: true);
    }

    [TestMethod]
    public async Task InlineWorkflowCompletes()
    {
        var sp = new ServiceCollection()
            .AddLognFlow(f => f.AddWorkflow<InlineWorkflow>("inline"))
            .BuildServiceProvider();

        await sp.GetRequiredService<WorkflowRunner>()
            .RunAsync("inline", null, waitForResult: true);
    }
}

file sealed class InlineWorkflow : IWorkflowDefinition
{
    public string Name { get; } = "inline";

    public IReadOnlyList<IStep> Steps { get; } =
    [
        new CodeStep(ctx => { Console.WriteLine("Inline hello!"); }),
        new CodeStep(async (ctx, ct) =>
        {
            await Task.Delay(200, ct);
            Console.WriteLine("Async bit completed");
            return new Success();
        })
    ];
}

file sealed class FooWorkflow : IWorkflowDefinition
{
    public string Name { get; } = "foo";

    public IReadOnlyList<IStep> Steps { get; } =
    [
        new PrintStep("Foo"),
        new SubWorkflowStep<BarWorkflow>(),
        new PrintStep("Foo after Bar"),
    ];
}

file sealed class BarWorkflow : IWorkflowDefinition
{
    public string Name { get; } = "bar";

    public IReadOnlyList<IStep> Steps { get; } =
    [
        new PrintStep("Bar"),
    ];
}

file sealed class HelloWorkflow : IWorkflowDefinition
{
    public string Name { get; } = "hello";

    public IReadOnlyList<IStep> Steps { get; } =
    [
        new PrintStep("A"),
        new PrintStep("B"),
        new DelayStep(TimeSpan.FromSeconds(1)),
        new PrintStep("C")
    ];
}

file sealed class PrintStep(string label) : IStep
{
    public ValueTask<IOutcome> ExecuteAsync(
        WorkflowContext ctx, CancellationToken _)
    {
        Console.WriteLine($"[{ctx.WorkflowId}] Print {label}");
        return ValueTask.FromResult<IOutcome>(new Success());
    }
}