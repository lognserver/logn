// Copyright (c) 2025 Codefrog
// Business Source License 1.1 – see LICENSE.txt for details.
// Change Date: 2029-07-01   Change License: Apache-2.0

using System.Text.Json.Serialization;

namespace Logn;

public sealed record TokenResponse(
    [property: JsonPropertyName("access_token")]
    string AccessToken,
    [property: JsonPropertyName("token_type")]
    string TokenType,
    [property: JsonPropertyName("expires_in")]
    int ExpiresIn,
    [property: JsonPropertyName("scope")] string Scope,
    [property: JsonPropertyName("refresh_token")]
    string? RefreshToken = null,
    [property: JsonPropertyName("id_token")]
    string? IdToken = null)
{
    /// <summary>UTC instant when the token expires.</summary>
    public DateTimeOffset ExpiresAt => DateTimeOffset.UtcNow.AddSeconds(ExpiresIn);

    /// <summary>True if <see cref="ExpiresAt"/> is in the past (grace ±30 s).</summary>
    public bool IsExpired(TimeSpan? grace = null) =>
        DateTimeOffset.UtcNow >= ExpiresAt - (grace ?? TimeSpan.FromSeconds(30));
}

public class OidcException(OidcErrorResponse error) : Exception
{
    public readonly OidcErrorResponse Error = error;
}

public record OidcErrorResponse
{
    [JsonPropertyName("error")] public required string Error { get; set; }

    [JsonPropertyName("error_description")]
    public string? ErrorDescription { get; set; }

    [JsonPropertyName("error_uri")] public string? ErrorUri { get; set; }
}