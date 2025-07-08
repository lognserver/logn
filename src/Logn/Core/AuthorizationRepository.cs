// Copyright (c) 2025 Codefrog
// Business Source License 1.1 – see LICENSE.txt for details.
// Change Date: 2029-07-01   Change License: Apache-2.0

using System.Collections.Concurrent;

namespace Logn;

public class AuthorizationRepository
{
    // In-memory store for generated auth codes
    public ConcurrentDictionary<string, AuthCodeInfo> authCodes = [];
}

public record ClientInfo(string ClientId, string ClientSecret, string[] AllowedScopes);

public sealed class ClientStore
{
    // client_id => ClientInfo
    internal readonly ConcurrentDictionary<string, ClientInfo> clients = new();

    public ClientStore()
    {
        clients["demo-api"] = new ClientInfo(
            ClientId: "demo-api",
            ClientSecret: "demo-secret",
            AllowedScopes: ["api.read", "api.write"]
        );

        clients["demo-web"] = new ClientInfo(
            ClientId: "demo-web",
            ClientSecret: "web-secret",
            AllowedScopes: ["web.read", "web.write"]
        );

        clients["FakeClientId123"] = new ClientInfo(
            ClientId: "FakeClientId123",
            ClientSecret: "FakeSecret123",
            AllowedScopes: []
        );
    }

    public bool TryGet(string clientId, out ClientInfo? info) => clients.TryGetValue(clientId, out info);
}