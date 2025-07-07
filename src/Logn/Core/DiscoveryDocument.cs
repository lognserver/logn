// Copyright (c) 2025 Codefrog
// Business Source License 1.1 – see LICENSE.txt for details.
// Change Date: 2029-07-01   Change License: Apache-2.0

using System.Text.Json.Serialization;

namespace Logn;

public sealed record DiscoveryDocument
{
    [JsonPropertyName("issuer")] 
    public string? Issuer { get; init; }

    [JsonPropertyName("authorization_endpoint")]
    public string? AuthorizationEndpoint { get; init; }

    [JsonPropertyName("token_endpoint")] 
    public string? TokenEndpoint { get; init; }
    
    [JsonPropertyName("jwks_uri")] 
    public string? JwksUri { get; init; }
    
    [JsonPropertyName("userinfo_endpoint")]
    public string? UserInfoEndpoint { get; init; }
    
    [JsonPropertyName("scopes_supported")] 
    public string[]? ScopesSupported { get; init; }
    
    [JsonPropertyName("response_types_supported")]
    public string[]? ResponseTypes { get; init; }
}

public sealed record DiscoveryRequest
{
    public required string Authority { get; set; }
}