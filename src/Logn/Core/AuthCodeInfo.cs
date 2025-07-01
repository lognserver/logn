// Copyright (c) 2025 Codefrog
// Business Source License 1.1 – see LICENSE.txt for details.
// Change Date: 2029-07-01   Change License: Apache-2.0

namespace Logn;

public record AuthCodeInfo(
    string UserId,
    string ClientId,
    string RedirectUri,
    string CodeChallenge,
    string CodeChallengeMethod,
    DateTime CreatedAt,
    string? Nonce);