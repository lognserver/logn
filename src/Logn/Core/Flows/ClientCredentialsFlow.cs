// Copyright (c) 2025 Codefrog
// Business Source License 1.1 – see LICENSE.txt for details.
// Change Date: 2029-07-01   Change License: Apache-2.0

using Logn.Flow;
using Logn.Flow.Basic;

namespace Logn.Core.Flows;

public class RequestTokenFlow : IWorkflowDefinition
{
    public string Name { get; } = nameof(RequestTokenFlow);

    public IReadOnlyList<IStep> Steps { get; } =
    [
        new LogStep("Requesting token..."),
        new SubWorkflowStep(nameof(ClientCredentialsFlow)),
        new LogStep("Token request completed.")
    ];
}

public class ClientCredentialsFlow : IWorkflowDefinition
{
    public string Name { get; } = nameof(ClientCredentialsFlow);

    public IReadOnlyList<IStep> Steps { get; } =
    [
        new LogStep("Hello World!"),
        new CodeStep(ctx =>
        {
            var log = ctx.Services?.GetService<ILogger<ClientCredentialsFlow>>();
            if (log is null)
            {
                Console.WriteLine("Hello from inline code at {0}", DateTimeOffset.UtcNow);
                return;
            }
            log?.LogInformation("Hello from inline code at {Time}", DateTimeOffset.UtcNow);
        }),
        new LogStep("It is nice to meet you!")
    ];
}

// public class ClientCredentialsFlow : WorkflowBase
// {
//     protected override void Build(IWorkflowBuilder builder)
//     {
//         builder.Root = new Sequence
//         {
//             Activities =
//             {
//                 new WriteLine("Hello World!"),
//                 new WriteLine("It is nice to meet you!")
//             }
//         };
//     }
// }

// public class RequestTokenFlow : WorkflowBase
// {
//     protected override void Build(IWorkflowBuilder builder)
//     {
//         builder.Root = new Sequence
//         {
//             Activities =
//             {
//                 new WriteLine("Requesting token..."),
//                 // new FlowDecision(),
//                 new DispatchWorkflow
//                 {
//                     WorkflowDefinitionId = new Input<string>(nameof(ClientCredentialsFlow)),
//                     Input = new Input<IDictionary<string, object>?>(new Dictionary<string, object>
//                     {
//                         { "clientId", "your-client-id" },
//                         { "clientSecret", "your-client-secret" },
//                         { "scope", "your-scope" }
//                     }),
//                     WaitForCompletion = new(true),
//                 //    RunAsynchronously = true
//                 },
//                 new WriteLine("Token request completed.")
//             }
//         };
//     }
// }
//
// public class SendEmailWorkflow : WorkflowBase
// {
//     protected override void Build(IWorkflowBuilder builder)
//     {
//         builder.Root = new Sequence
//         {
//             Activities =
//             {
//                 new Start(),
//                 new Complete(),
//             }
//         };
//     }
// }