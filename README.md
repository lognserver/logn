# Logn Server

Developer-focused Blazor tools for troubleshooting OpenID Connect:

* **Error Explainer** – lookup any OIDC error code with plain-language fixes.  
  Route: `/explain`
* **Flow Simulator** – step through an authorization-code flow, inspect each stage, and test callback handling.  
  Route: `/simulator`

---

## Requirements

* .NET 9 SDK

---

## Running Locally

```bash
git clone https://github.com/lognserver/logn.git
cd logn
dotnet run --project src/Logn.Diagnostics
````

Visit `http://localhost:5114` (or the port shown in the console).

### Sandbox Credentials

When using the default authority, log in with:

```
username: alice
password: password
```

---

## Configuration

Edit **`appsettings.json`** (or environment variables) to override defaults:

```jsonc
{
  "Logn": {
    "Authority": "https://login.example.com"
  }
}
```

Provide a different `authority` in the Simulator UI to test against other OIDC providers.

---

## License

Business Source License 1.1 – view **LICENSE.txt** for details. The Change Date is 2029-07-01, after which the project becomes Apache-2.0.
