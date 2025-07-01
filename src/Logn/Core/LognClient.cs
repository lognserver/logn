// Copyright (c) 2025 Codefrog
// Business Source License 1.1 – see LICENSE.txt for details.
// Change Date: 2029-07-01   Change License: Apache-2.0

using Microsoft.Extensions.Options;

namespace Logn;

public class LognClient(IOptionsMonitor<LognOptions> options, HttpClient httpClient) : ILognClient
{
    public async ValueTask<AuthorizationResponse> AuthorizeAsync(AuthorizationRequest request)
    {
        var uri = await GetAuthorizeUriAsync(request);

        var response = await httpClient.GetAsync(uri.ToString());

        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadFromJsonAsync<AuthorizationResponse>();

        return content ?? throw new InvalidOperationException("No content returned from authorization endpoint.");
    }

    public ValueTask<Uri> GetAuthorizeUriAsync(AuthorizationRequest request)
    {
        var values = new Dictionary<string, string?>
        {
            ["client_id"] = request.ClientId,
            ["redirect_uri"] = request.RedirectUri,
            ["response_type"] = request.ResponseType,
            ["scope"] = request.Scope,
            ["state"] = request.State,
        };

        var uri = new UriBuilder($"{options.CurrentValue.Authority}/connect/authorize")
        {
            Query = QueryString.Create(values).ToString()
        };

        return ValueTask.FromResult(uri.Uri);
    }

    public async ValueTask<TokenResponse> RequestTokenAsync(TokenRequest request)
    {
        var uri = new UriBuilder($"{options.CurrentValue.Authority}/connect/token")
        {
        };

        IEnumerable<KeyValuePair<string, string>> form =
        [
            new("grant_type", request.GrantType),
            new("code", request.Code),
            new("redirect_uri", request.RedirectUri),
            new("client_id", request.ClientId)
            // new KeyValuePair<string, string>("code_verifier", request.CodeVerifier),
        ];

        var response = await httpClient.PostAsync(uri.ToString(), new FormUrlEncodedContent(form));

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadFromJsonAsync<TokenResponse>();

            return content ?? throw new InvalidOperationException("No content returned from token endpoint.");
        }
        var errorResponse = await response.Content.ReadFromJsonAsync<OidcErrorResponse>();
        if (errorResponse != null)
        {
            throw new OidcException(errorResponse);
        }
        
        throw new InvalidOperationException("No content returned from token endpoint and no error response.");
    }
}