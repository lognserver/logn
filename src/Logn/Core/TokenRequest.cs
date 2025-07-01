// Copyright (c) 2025 Codefrog
// Business Source License 1.1 – see LICENSE.txt for details.
// Change Date: 2029-07-01   Change License: Apache-2.0

namespace Logn;

public record TokenRequest
{
    public required string GrantType { get; set; } = "authorization_code";
    public required string Code { get; set; }
    public required string RedirectUri { get; set; }
    public required string ClientId { get; set; }
    // public string? CodeVerifier { get; set; }
}