// Copyright (c) 2025 Codefrog
// Business Source License 1.1 – see LICENSE.txt for details.
// Change Date: 2029-07-01   Change License: Apache-2.0

namespace Logn;

public record AuthorizationRequest : ITokenForm
{
    public required string Authority { get; set; }
    public required string ClientId { get; set; }
    public required string RedirectUri { get; set; }
    public string ResponseType { get; set; } = "code";
    public string Scope { get; set; } = "openid";
    public string? State { get; set; }
    public IEnumerable<KeyValuePair<string, string?>> ToForm()
    {
        yield return new("client_id", ClientId);
        yield return new("redirect_uri", RedirectUri);
        yield return new("response_type", ResponseType);
        yield return new("scope", Scope);
        yield return new("state", State);
    }
}