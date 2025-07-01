using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace Logn;

public static class SigningKeyFactory
{
    /// <summary>
    /// HS256 (HMAC-SHA256) – symmetric key
    /// </summary>
    public static (SecurityKey Key, string Algorithm, JsonWebKey Jwk) UseHS256(ReadOnlySpan<byte> secret)
    {
        var keyId = Guid.NewGuid().ToString("N");
        var key = new SymmetricSecurityKey(secret.ToArray())
        {
            KeyId = keyId
        };

        return (key, SecurityAlgorithms.HmacSha256,
            JsonWebKeyConverter.ConvertFromSymmetricSecurityKey(key));
    }

    /// <summary>
    /// RS256 (RSA-SHA256) – asymmetric key-pair
    /// </summary>
    public static (SecurityKey Key, string Algorithm, JsonWebKey Jwk) UseRS256()
    {
        var keyId = Guid.NewGuid().ToString("N");
        var rsaKey = new RsaSecurityKey(RSA.Create(2048))
        {
            KeyId = keyId
        };

        return (rsaKey, SecurityAlgorithms.RsaSha256,
            JsonWebKeyConverter.ConvertFromRSASecurityKey(rsaKey));
    }
}