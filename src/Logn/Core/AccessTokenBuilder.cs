// Copyright (c) 2025 Codefrog
// Business Source License 1.1 – see LICENSE.txt for details.
// Change Date: 2029-07-01   Change License: Apache-2.0

using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Logn;

public class AccessTokenBuilder
{
    private bool UseRsa => true;
    private readonly SecurityKey SecurityKey;
    private readonly string Algorithm;
    private readonly JsonWebKey Jwk;

    private readonly LognOptions options;
    public AccessTokenBuilder(IOptions<LognOptions> options)
    {
        this.options = options.Value;
        (SecurityKey, Algorithm, Jwk) = UseRsa
            ? SigningKeyFactory.UseRS256() // RS256 (RSA)
            : SigningKeyFactory.UseHS256("A_super_secret_key_123!AND_IT_IS_LONG_ENOUGH"u8); // HS256 (HMAC)
    }
    
    public string BuildToken(List<Claim> claims)
    {
        // Implementation for building an access token
        // This could involve setting claims, signing the token, etc.
        // For example:
        // var token = new JwtSecurityToken(...);
        // return new JwtSecurityTokenHandler().WriteToken(token);
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
            Subject = new ClaimsIdentity(claims),
            NotBefore = DateTime.UtcNow,
            Expires = DateTime.UtcNow.AddMinutes(30),
            SigningCredentials = signingCredentials
        };
        var accessTokenString = tokenHandler.CreateToken(accessTokenDescriptor);
        return accessTokenString;
    }
}