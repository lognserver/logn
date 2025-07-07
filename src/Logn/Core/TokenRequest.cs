// Copyright (c) 2025 Codefrog
// Business Source License 1.1 – see LICENSE.txt for details.
// Change Date: 2029-07-01   Change License: Apache-2.0

namespace Logn;

public record TokenRequest
{
    public string GrantType { get; set; } = "authorization_code";

    public required string ClientId { get; set; }
    // public string? CodeVerifier { get; set; }
}

public interface ITokenForm
{
    IEnumerable<KeyValuePair<string, string?>> ToForm();
}

/* ── Authorization-code ── */
public sealed record AuthCodeTokenRequest(
    string ClientId,
    string Code,
    string RedirectUri,
    string? CodeVerifier = null)
    : ITokenForm
{
    public IEnumerable<KeyValuePair<string, string?>> ToForm()
    {
        yield return new("grant_type", "authorization_code");
        yield return new("client_id", ClientId);
        yield return new("code", Code);
        yield return new("redirect_uri", RedirectUri);
        if (CodeVerifier is not null) yield return new("code_verifier", CodeVerifier);
    }
}

public sealed record ClientCredentialsTokenRequest(
    string ClientId,
    string ClientSecret,
    string Scope = "")
    : ITokenForm
{
    public IEnumerable<KeyValuePair<string, string?>> ToForm()
    {
        yield return new("grant_type", "client_credentials");
        yield return new("client_id", ClientId);
        yield return new("client_secret", ClientSecret);
        yield return new("scope", Scope);
    }
}
//
// public record AuthorizationCodeTokenRequest : TokenRequest
// {
//     public AuthorizationCodeTokenRequest()
//     {
//         GrantType = "authorization_code";
//     }
//
//     public required string Code { get; set; }
//     public required string RedirectUri { get; set; }
// }
//
// public static class AuthorizationCodeTokenRequestExtensions
// {
//     public static IEnumerable<KeyValuePair<string, string>> GetBirthYear(this AuthorizationCodeTokenRequest request)
//     {
//         IEnumerable<KeyValuePair<string, string>> form =
//         [
//             new("grant_type", request.GrantType),
//             new("code", request.Code),
//             new("redirect_uri", request.RedirectUri),
//             new("client_id", request.ClientId)
//             // new KeyValuePair<string, string>("code_verifier", request.CodeVerifier),
//         ];
//
//         return form;
//     }
// }
//
// public record ClientCredentialsTokenRequest : TokenRequest
// {
//     public ClientCredentialsTokenRequest()
//     {
//         GrantType = "client_credentials";
//     }
//
//     public string? ClientSecret { get; set; }
// }

public enum OidcFlow
{
    AuthorizationCode,
    Implicit,
    Hybrid,
    ClientCredentials,
    ResourceOwnerPasswordCredentials
}