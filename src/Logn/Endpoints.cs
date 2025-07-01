// Copyright (c) 2025 Codefrog
// Business Source License 1.1 – see LICENSE.txt for details.
// Change Date: 2029-07-01   Change License: Apache-2.0

using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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
    
    private static bool UseRsa => true;
    private static readonly SecurityKey SecurityKey;
    private static readonly string      Algorithm;
    private static readonly JsonWebKey  Jwk;
    
    static Endpoints()
    {
        (SecurityKey, Algorithm, Jwk) = UseRsa
            ? SigningKeyFactory.UseRS256()  // RS256 (RSA)
            : SigningKeyFactory.UseHS256("A_super_secret_key_123!AND_IT_IS_LONG_ENOUGH"u8); // HS256 (HMAC)
    }
    
    private static readonly string LoginPath = "/simulator-login";

    public static WebApplication UseLogn(this WebApplication app, LognOptions options)
    {
        app.MapGet("/.well-known/openid-configuration", async context =>
        {
            var response = GetOpenIdConfiguration();

            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(response);
        });

        app.MapGet("/simulator-login", async (httpContext) =>
        {
            var page = $"""
                         <html>
                         
                         <head>
                             <title>Fake Login | Logn Server</title>
                             <script src="https://unpkg.com/@tailwindcss/browser@4"></script>
                         </head>
                         
                         <body>
                             <section class="max-w-sm mx-auto mt-8 p-6 bg-white/70 backdrop-blur-md shadow rounded-2xl">
                                 <h2 class="text-2xl font-semibold text-slate-800 mb-2">Sandbox Login</h2>
                         
                                 <p class="text-xs text-slate-500 mb-6">
                                     This is a <span class="italic">fake</span> login page meant only for use with the
                                     <a href="/simulator" class="text-sky-600 hover:text-sky-800 underline underline-offset-2">
                                         OIDC Flow Simulator
                                     </a> or for local testing.
                                     Credentials here aren't verified against a real user store.
                                 </p>
                         
                                 <form method="post" action="/simulator-login" class="space-y-4">
                                     <div>
                                         <label class="block text-sm font-medium text-slate-700 mb-1" for="username">
                                             Username
                                         </label>
                                         <input id="username" name="username" required class="w-full border border-slate-300 rounded-lg px-3 py-2 text-sm
                                                   focus:outline-none focus:ring-2 focus:ring-sky-400/70
                                                   placeholder-slate-400" />
                                     </div>
                         
                                     <div>
                                         <label class="block text-sm font-medium text-slate-700 mb-1" for="password">
                                             Password
                                         </label>
                                         <input id="password" name="password" type="password" required class="w-full border border-slate-300 rounded-lg px-3 py-2 text-sm
                                                   focus:outline-none focus:ring-2 focus:ring-sky-400/70
                                                   placeholder-slate-400" />
                                     </div>
                         
                                     <input type="hidden" name="returnUrl" value="{httpContext.Request.Query["returnUrl"]}" />
                         
                                     <button type="submit" class="w-full bg-blue-500 hover:bg-blue-600 text-white font-semibold py-2 rounded-lg shadow
                                                transition-colors">
                                         Login
                                     </button>
                                 </form>
                             </section>
                         </body>
                         
                         </html>
                         """;
            await httpContext.Response.WriteAsync(page, Encoding.UTF8);
        });

        app.MapPost("/simulator-login", async (HttpContext httpContext) =>
        {
            var form = httpContext.Request.Form;
            string? username = form["username"];
            string? password = form["password"];
            string? returnUrl = form["returnUrl"];

            // demo username: "alice"
            // demo password: "password"
            if (username == "alice" && password == "password")
            {
                var claims = new List<Claim>
                {
                    new(ClaimTypes.Name, username)
                };
                var identity = new ClaimsIdentity(
                    claims,
                    OpenIdConnectDefaults.AuthenticationScheme
                );
                var principal = new ClaimsPrincipal(identity);

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
        
        app.MapGet("/.well-known/jwks", async context =>
        {
            var jwks = new
            {
                keys = new[] { Jwk }
            };

            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(jwks);
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
                    $"/{LoginPath.Trim('/')}?returnUrl={Uri.EscapeDataString(httpContext.Request.Path + httpContext.Request.QueryString)}";
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

            // Build and return the token response
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
        }).RequireAuthorization("UserInformation");

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
                keys = new[] { Jwk },
            };
        }
    }
}