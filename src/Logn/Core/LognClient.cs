// Copyright (c) 2025 Codefrog
// Business Source License 1.1 – see LICENSE.txt for details.
// Change Date: 2029-07-01   Change License: Apache-2.0

namespace Logn;

public class LognClient(HttpClient httpClient)
{
    public async ValueTask<DiscoveryDocument> GetDiscoveryDocumentAsync(string authority, CancellationToken ct = default)
    {
        var uri = $"{authority.TrimEnd('/')}/.well-known/openid-configuration";

        var response = await httpClient.GetAsync(uri, ct);

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadFromJsonAsync<DiscoveryDocument>(cancellationToken: ct);
            return content ?? throw new InvalidOperationException("No content returned from discovery endpoint.");
        }

        var error = await response.Content.ReadFromJsonAsync<OidcErrorResponse>(cancellationToken: ct);
        if (error is not null)
        {
            throw new OidcException(error);
        }

        throw new InvalidOperationException("No content returned from discovery endpoint and no error response.");
    }
    
    public async ValueTask<AuthorizationResponse> AuthorizeAsync(AuthorizationRequest request)
    {
        var uri = await GetAuthorizeUriAsync(request);

        var response = await httpClient.GetAsync(uri.ToString());
        if (!response.IsSuccessStatusCode)
        {
            var errorResponse = await response.Content.ReadFromJsonAsync<OidcErrorResponse>();
            if (errorResponse != null)
            {
                throw new OidcException(errorResponse);
            }
            throw new InvalidOperationException("No content returned from authorization endpoint and no error response.");
        }

        var content = await response.Content.ReadFromJsonAsync<AuthorizationResponse>();

        return content ?? throw new InvalidOperationException("No content returned from authorization endpoint.");
    }

    public ValueTask<Uri> GetAuthorizeUriAsync(AuthorizationRequest request)
    {
        var values = request.ToForm();
        var uri = new UriBuilder($"{request.Authority}/connect/authorize")
        {
            Query = QueryString.Create(values).ToString()
        };

        return ValueTask.FromResult(uri.Uri);
    }

    public async ValueTask<TokenResponse> RequestTokenAsync(string authority, ITokenForm request)
    {
        var uri = new UriBuilder($"{authority}/connect/token")
        {
        };

        var form = request.ToForm();
        var response = await httpClient.PostAsync(uri.ToString(), new FormUrlEncodedContent(form));

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadFromJsonAsync<TokenResponse>();

            return content ?? throw new InvalidOperationException("No content returned from token endpoint.");
        }

        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            var errorResponse = await response.Content.ReadFromJsonAsync<OidcErrorResponse>();
            if (errorResponse != null)
            {
                throw new OidcException(errorResponse);
            }
        }
        
        throw new HttpRequestException($"API request failed with status code {response.StatusCode}");
    }
}