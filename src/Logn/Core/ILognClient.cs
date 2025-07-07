// Copyright (c) 2025 Codefrog
// Business Source License 1.1 – see LICENSE.txt for details.
// Change Date: 2029-07-01   Change License: Apache-2.0

namespace Logn;

/// <summary>
/// Provides interaction for OIDC providers.
/// </summary>
public interface ILognClient
{
    // ValueTask<AuthorizationResponse> AuthorizeAsync(AuthorizationRequest request);
    ValueTask<Uri> GetAuthorizeUriAsync(string authority, AuthorizationRequest request);
    ValueTask<TokenResponse> RequestTokenAsync(string authority, TokenRequest request);
    // Task<UserInfoResponse> UserInfoAsync(string accessToken);
    // Task<LogoutResponse> LogoutAsync(string idToken);
}