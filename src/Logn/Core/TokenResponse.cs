// Copyright (c) 2025 Codefrog
// Business Source License 1.1 – see LICENSE.txt for details.
// Change Date: 2029-07-01   Change License: Apache-2.0

using System.Text.Json.Serialization;

namespace Logn;

public record TokenResponse
{
    [JsonPropertyName("access_token")]
    public required string AccessToken { get; set; }
    
    [JsonPropertyName("token_type")]
    public required string TokenType { get; set; }
    
    [JsonPropertyName("expires_in")]
    public required int ExpiresIn { get; set; }
    
    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }
    
    [JsonPropertyName("id_token")]
    public string? IdToken { get; set; }
    
    [JsonPropertyName("scope")]
    public required string Scope { get; set; }
}

public class OidcException(OidcErrorResponse error) : Exception
{
    public readonly OidcErrorResponse Error = error;
}

public record OidcErrorResponse
{
    [JsonPropertyName("error")]
    public required string Error { get; set; }
    
    [JsonPropertyName("error_description")]
    public string? ErrorDescription { get; set; }
    
    [JsonPropertyName("error_uri")]
    public string? ErrorUri { get; set; }
}