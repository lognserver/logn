// Copyright (c) 2025 Codefrog
// Business Source License 1.1 – see LICENSE.txt for details.
// Change Date: 2029-07-01   Change License: Apache-2.0

using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Logn;

public static class LognConstants
{
    public const string AuthenticationScheme = "Logn";
}

public static class Endpoints
{
    private static readonly string[] SubjectTypes = ["public"];
    private static readonly string[] ResponseTypes = ["code", "id_token", "token"];
    private static readonly string[] Algorithms = ["RS256"];
    private static readonly string[] Scopes = ["openid", "profile", "email"];
    private static readonly string[] GrantTypes = ["authorization_code", "client_credentials", "refresh_token"];
    private static readonly string KeyId = Guid.NewGuid().ToString("N");
    private static readonly byte[] KeyBytes = "A_super_secret_key_123!AND_IT_IS_LONG_ENOUGH"u8.ToArray();
    private static readonly SymmetricSecurityKey SecurityKey = new(KeyBytes)
    {
        KeyId = KeyId
    };
    private static readonly string? Algorithm = SecurityAlgorithms.HmacSha256;
    private static readonly JsonWebKey JsonWebKey = JsonWebKeyConverter.ConvertFromSymmetricSecurityKey(SecurityKey);

    public static WebApplication UseLogn(this WebApplication app, LognOptions options)
    {
        app.MapGet("/.well-known/openid-configuration", async context =>
        {
            var response = GetOpenIdConfiguration();

            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(response);
        });

        app.MapGet("/login", async (httpContext) =>
        {
            var page = $"""
                        <html>
                        <body>
                            <form method='post' action='/login'>
                                <label>Username: <input name='username' /></label>
                                <br />
                                <label>Password: <input name='password' type='password'/></label>
                                <br />
                                <input type='hidden' name='returnUrl' value='{httpContext.Request.Query["returnUrl"]}' />
                                <button type='submit'>Login</button>
                            </form>
                        </body>
                        </html>
                        """;
            await httpContext.Response.WriteAsync(page, Encoding.UTF8);
        });

        app.MapPost("/login", async (HttpContext httpContext) =>
        {
            var form = httpContext.Request.Form;
            string? username = form["username"];
            string? password = form["password"];
            string? returnUrl = form["returnUrl"];

            // demo username: "alice"
            // demo password: "password"
            if (username == "alice" && password == "password")
            {
                var claims = new List<System.Security.Claims.Claim>
                {
                    new(System.Security.Claims.ClaimTypes.Name, username)
                };
                var identity = new System.Security.Claims.ClaimsIdentity(
                    claims,
                    OpenIdConnectDefaults.AuthenticationScheme
                );
                var principal = new System.Security.Claims.ClaimsPrincipal(identity);

                await httpContext.SignInAsync(
                    LognConstants.AuthenticationScheme,
                    principal
                );

                // redirect back to the original request or to a default page
                return Results.Redirect(string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl);
            }

            // Invalid credentials
            return Results.BadRequest("Invalid username or password");
        });

        app.MapGet("/connect/authorize", async (HttpContext httpContext, AuthorizationRepository authRepository) =>
        {
            var clientId = httpContext.Request.Query["client_id"];
            var redirectUri = httpContext.Request.Query["redirect_uri"];
            var responseType = httpContext.Request.Query["response_type"];
            var scope = httpContext.Request.Query["scope"];
            var state = httpContext.Request.Query["state"];

            // TODO: PKCE parameters
            string? codeChallenge = httpContext.Request.Query["code_challenge"];
            string? codeChallengeMethod = httpContext.Request.Query["code_challenge_method"];
            var nonce = httpContext.Request.Query["nonce"];

            // simple validation only
            if (string.IsNullOrWhiteSpace(clientId) ||
                string.IsNullOrWhiteSpace(redirectUri) ||
                string.IsNullOrWhiteSpace(responseType))
            {
                // todo: return valid openid error response
                return Results.BadRequest("Missing required parameters.");
            }

            // odd we need these to prevent compiler warning, with the above validation?
            ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
            ArgumentException.ThrowIfNullOrWhiteSpace(redirectUri);
            ArgumentException.ThrowIfNullOrWhiteSpace(responseType);

            // TODO: skip checking: client validation, redirect URI, response type, scopes, etc.

            if (!httpContext.User.Identity?.IsAuthenticated ?? false)
            {
                // go to login page with return URL
                var loginUrl =
                    $"/login?returnUrl={Uri.EscapeDataString(httpContext.Request.Path + httpContext.Request.QueryString)}";
                return Results.Redirect(loginUrl);
            }

            // generate an authorization code (this is not secure, demo only)
            var code = Guid.NewGuid().ToString("n");

            // store code details in memory
            authRepository.authCodes[code] = new AuthCodeInfo(
                UserId: httpContext.User.Identity?.Name ?? "unknown",
                ClientId: clientId,
                RedirectUri: redirectUri,
                CodeChallenge: codeChallenge ?? "",
                CodeChallengeMethod: codeChallengeMethod ?? "",
                CreatedAt: DateTime.UtcNow,
                Nonce: nonce
            );

            // build the redirect URI: redirect_uri?code=xxx&state=xxx
            var uriBuilder = new UriBuilder(redirectUri)
            {
                Query = $"code={code}" + (string.IsNullOrEmpty(state) ? "" : $"&state={state}")
            };

            // redirect back to the client
            return Results.Redirect(uriBuilder.Uri.ToString());
        });

        app.MapPost("/connect/token", async (HttpContext httpContext, AuthorizationRepository authRepository) =>
        {
            var form = httpContext.Request.Form;
            string? grantType = form["grant_type"];
            string? code = form["code"];
            string? redirectUri = form["redirect_uri"];
            string? clientId = form["client_id"];
            string? codeVerifier = form["code_verifier"];

            if (grantType != "authorization_code" ||
                string.IsNullOrWhiteSpace(code) ||
                string.IsNullOrWhiteSpace(clientId) ||
                string.IsNullOrWhiteSpace(redirectUri))
            {
                return Results.BadRequest(new
                {
                    error = "invalid_request",
                    error_description = "Missing or invalid parameters."
                });
            }

            // lookup authorization code in memory
            if (!authRepository.authCodes.TryGetValue(code, out var authCodeInfo))
            {
                return Results.BadRequest(new
                {
                    error = "invalid_grant",
                    error_description = "Authorization code is invalid or expired."
                });
            }

            // validate client_id and redirect_uri
            if (!string.Equals(authCodeInfo.ClientId, clientId, StringComparison.Ordinal) ||
                !string.Equals(authCodeInfo.RedirectUri, redirectUri, StringComparison.Ordinal))
            {
                return Results.BadRequest(new
                {
                    error = "invalid_request",
                    error_description = "Client_id or redirect_uri mismatch."
                });
            }

            // code expiration
            if ((DateTime.UtcNow - authCodeInfo.CreatedAt).TotalMinutes > 5)
            {
                return Results.BadRequest(new
                {
                    error = "invalid_grant",
                    error_description = "Authorization code has expired."
                });
            }

            // PKCE validation 
            if (!string.IsNullOrEmpty(authCodeInfo.CodeChallenge))
            {
                if (authCodeInfo.CodeChallengeMethod == "S256")
                {
                    // Compute the SHA-256 of code_verifier
                    using var sha256 = SHA256.Create();
                    var codeVerifierBytes = Encoding.UTF8.GetBytes(codeVerifier ?? "");
                    var hashBytes = sha256.ComputeHash(codeVerifierBytes);
                    var codeVerifierHash = Base64UrlEncode(hashBytes);

                    if (codeVerifierHash != authCodeInfo.CodeChallenge)
                    {
                        return Results.BadRequest(new
                        {
                            error = "invalid_grant",
                            error_description = "PKCE validation failed."
                        });
                    }
                }
                else if (authCodeInfo.CodeChallengeMethod == "plain")
                {
                    if (codeVerifier != authCodeInfo.CodeChallenge)
                    {
                        return Results.BadRequest(new
                        {
                            error = "invalid_grant",
                            error_description = "PKCE validation failed."
                        });
                    }
                }
            }

            // remove the authorization code from the store to prevent reuse
            authRepository.authCodes.TryRemove(code, out _);

            // create tokens
            var tokenHandler = new JsonWebTokenHandler();

            var signingCredentials = new SigningCredentials(
                SecurityKey,
                Algorithm
            );

            // access token
            var accessTokenDescriptor = new SecurityTokenDescriptor
            {
                Issuer = options.Authority,
                Audience = "resource-api",
                Subject = new ClaimsIdentity([
                    new Claim("sub", authCodeInfo.UserId),
                    new Claim("client_id", authCodeInfo.ClientId)
                ]),
                NotBefore = DateTime.UtcNow,
                Expires = DateTime.UtcNow.AddMinutes(30),
                SigningCredentials = signingCredentials
            };
            var accessTokenString = tokenHandler.CreateToken(accessTokenDescriptor);

            // id token
            var idTokenDescriptor = new SecurityTokenDescriptor
            {
                Issuer = options.Authority,
                Audience = clientId,
                Subject = new ClaimsIdentity([
                    new Claim("sub", authCodeInfo.UserId),
                    new Claim("iss", options.Authority)
                ]),
                NotBefore = DateTime.UtcNow,
                Expires = DateTime.UtcNow.AddMinutes(30),
                SigningCredentials = signingCredentials
            };

            if (!string.IsNullOrEmpty(authCodeInfo.Nonce))
            {
                idTokenDescriptor.Subject.AddClaim(new Claim("nonce", authCodeInfo.Nonce));
            }

            var idTokenString = tokenHandler.CreateToken(idTokenDescriptor);

            // c) Build and return the token response
            var response = new
            {
                access_token = accessTokenString,
                token_type = "Bearer",
                expires_in = 1800, // 30 minutes in seconds
                id_token = idTokenString,
                scope = "openid profile"
            };

            return Results.Json(response);
        });
        
        app.MapGet("/connect/userinfo", (HttpContext context) =>
        {
            if (context.User?.Identity is not { IsAuthenticated: true })
            {
                return Results.Unauthorized();
            }

            var userInfo = new
            {
                Name = context.User.Identity?.Name,
                Email = context.User.FindFirst(ClaimTypes.Email)?.Value,
                Claims = context.User.Claims.Select(c => new { c.Type, c.Value })
            };

            return Results.Json(userInfo);
        });

        return app;

        static string Base64UrlEncode(byte[] input)
        {
            return Convert.ToBase64String(input)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");
        }
        
        object GetOpenIdConfiguration()
        {
            return new
            {
                issuer = options.Authority,
                authorization_endpoint = $"{options.Authority}/connect/authorize",
                token_endpoint = $"{options.Authority}/connect/token",
                userinfo_endpoint = $"{options.Authority}/connect/userinfo",
                jwks_uri = $"{options.Authority}/.well-known/jwks",
                response_types_supported = ResponseTypes,
                grant_types_supported = GrantTypes,
                subject_types_supported = SubjectTypes,
                id_token_signing_alg_values_supported = Algorithms,
                scopes_supported = Scopes,
                keys = new[] { JsonWebKey },
            };
        }
    }

    private static JsonWebKey CreateJsonWebKey(string keyId)
    {
        return JsonWebKeyConverter.ConvertFromSymmetricSecurityKey(SecurityKey);
    }
}