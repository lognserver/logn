using Logn.Flow.Basic;
using Logn.Flow.Engine;
using Microsoft.Extensions.DependencyInjection;

namespace Logn.Flow.Test;

[TestClass]
public class ConditionalTests
{
    [TestMethod]
    public async Task VipBranchIsTaken()
    {
        var sp = new ServiceCollection()
            .AddLognFlow(flow => { flow.AddWorkflow<PaymentWorkflow>("payment"); })
            .BuildServiceProvider();

        var runner = sp.GetRequiredService<WorkflowRunner>();

        await runner.RunAsync(
            "payment",
            null,
            waitForResult: true,
            init: ctx => ctx.Items["vip"] = true);
    }
}

file sealed class ConditionalStep(
    Func<WorkflowContext, bool> test,
    string ifTrue,
    string ifFalse) : IStep
{
    public ValueTask<IOutcome> ExecuteAsync(
        WorkflowContext ctx,
        CancellationToken _)
        => ValueTask.FromResult<IOutcome>(
            test(ctx) ? new Jump(ifTrue) : new Jump(ifFalse));
}

public sealed class PaymentWorkflow : IWorkflowDefinition
{
    public string Name => "payment";

    public IReadOnlyList<IStep> Steps { get; } =
    [
        new ConditionalStep(
            ctx => (bool)ctx.Items["vip"],
            ifTrue: "vip_route",
            ifFalse: "normal_route"),

        Step.Label("vip_route",
            new LogStep("Processed via VIP gateway")),

        Step.Label("normal_route",
            new LogStep("Processed via standard gateway")),

        new LogStep("Send receipt email")
    ];
}