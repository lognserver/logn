// Copyright (c) 2025 Codefrog
// Business Source License 1.1 – see LICENSE.txt for details.
// Change Date: 2029-07-01   Change License: Apache-2.0

using Logn.Flow;
using Logn.Flow.Basic;

namespace Logn.Core.Flows;

public class RequestTokenFlow : IWorkflowDefinition
{
    public string Name => "request_token";

    public IReadOnlyList<IStep> Steps { get; } =
    [
        new LogStep("Requesting token…"),

        new ConditionalSubWorkflowStep(
            ctx => ctx.GetInput<TokenRequest>().GrantType == "client_credentials",
            workflowWhenTrue: nameof(ClientCredentialsFlow),
            workflowWhenFalse: nameof(UnsupportedGrantTypeFlow), waitForResult: true),

        new LogStep("Token request completed.")
    ];
}

public sealed class ClientCredentialsFlow : IWorkflowDefinition
{
    public string Name { get; } = nameof(ClientCredentialsFlow);

    public IReadOnlyList<IStep> Steps { get; } =
    [
        new LogStep("Processing client credentials…"),
        new ValidateClientCredentialsStep(),
        new EmitTokenStep(),
        new LogStep("Client credentials processed successfully."),
    ];
}

file sealed class EmitTokenStep : IStep
{
    public ValueTask<IOutcome> ExecuteAsync(
        WorkflowContext ctx, CancellationToken _)
    {
        // todo: implement claims, id_token, and more, this is pretty basic/fake
        var req = ctx.GetInput<TokenRequest>();
        var accessTokenBuilder = ctx.Services.GetRequiredService<AccessTokenBuilder>();
        var token = accessTokenBuilder.BuildToken([]);

        ctx.Services.GetRequiredService<ILogger<EmitTokenStep>>()
            .LogInformation("Generated access token: {Token}", token);
        
        ctx.SetOutput(token);

        return ValueTask.FromResult<IOutcome>(new Success());
    }
}

public class ValidateClientCredentialsStep : IStep
{
    public ValueTask<IOutcome> ExecuteAsync(WorkflowContext context, CancellationToken cancellation = default)
    {
        var request = context.GetInput<TokenRequest>();
        var clientStore = context.Services.GetRequiredService<ClientStore>();

        if (!clientStore.TryGet(request.ClientId, out var clientInfo) || clientInfo == null ||
            clientInfo.ClientSecret != request.ClientSecret)
        {
            return ValueTask.FromResult<IOutcome>(
                new Failure(new UnauthorizedAccessException("Invalid client credentials.")));
        }

        return ValueTask.FromResult<IOutcome>(new Success());
    }
}