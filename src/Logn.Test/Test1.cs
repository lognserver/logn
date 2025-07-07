// Copyright (c) 2025 Codefrog
// Business Source License 1.1 – see LICENSE.txt for details.
// Change Date: 2029-07-01   Change License: Apache-2.0

using Logn.Core.Flows;
using Logn.Flow;
using Logn.Flow.Engine;
using Microsoft.Extensions.DependencyInjection;

namespace Logn.Test;

[TestClass]
public sealed class Test1
{
    [TestMethod]
    public void TestMethod1()
    {
    }
    
        
    [TestMethod]
    public async Task NestedWorkflowRunsToCompletion()
    {
        var services = new ServiceCollection()
            .AddLognFlow(flow =>
            {
                flow.AddWorkflow<RequestTokenFlow>(nameof(RequestTokenFlow))
                    .AddWorkflow<ClientCredentialsFlow>(nameof(ClientCredentialsFlow));
            })
            .BuildServiceProvider();

        var runner = services.GetRequiredService<WorkflowRunner>();

        await runner.RunAsync(nameof(RequestTokenFlow), null, waitForResult: true);
    }
}