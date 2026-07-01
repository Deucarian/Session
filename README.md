# Deucarian Session

## What this is

`com.deucarian.session` is a standalone Unity UPM package for authenticated session lifecycle management.

It stores the current session, restores saved sessions on app start, supports backend-specific login and refresh flows through small interfaces, and notifies listeners when session data or calculated session state changes.

Current package version: `1.0.4`.

## When to use it

- You need a reusable authenticated-session state model.
- You need restore, login, refresh, logout, persistence, and change notification contracts.
- You want backend-specific login/refresh adapters without coupling Session to an HTTP implementation.
- You need a standalone session package that can later compose with API through an integration package.

## When not to use it

- Do not use Session as an HTTP/API client; `com.deucarian.api` owns transport.
- Do not put UI navigation, object loading, telemetry, or package installation behavior here.
- Do not add API integration code to this core package; use `com.deucarian.session.api-integration`.

## Install

Stable:

```json
"com.deucarian.session": "https://github.com/Deucarian/Session.git#main"
```

Development:

```json
"com.deucarian.session": "https://github.com/Deucarian/Session.git#develop"
```

Dependencies:

- `com.deucarian.logging`: package logging facade and diagnostics output.
- `com.unity.modules.jsonserialize`: Unity JSON serialization module used by this package.

## Unity compatibility

Requires Unity 2021.3 or newer.

## Logging

This package uses `com.deucarian.logging`.

Deucarian Session diagnostics use stable package categories: `Session`, `Session.Authentication`, `Session.Storage`, and `Session.Samples`. Configure Deucarian Logging filters by category and level to isolate authentication, persistence, or sample output. Entries flow through the shared ring buffer for recent-diagnostic inspection and remain compatible with future telemetry sinks.

## 60-second quick start

`SessionService` coordinates restore, login, refresh, logout, state calculation, persistence, and change events.

`SessionData` is immutable token data: access token, optional refresh token, and optional UTC access-token expiry.

`ISessionStore` abstracts persistence. The package includes `InMemorySessionStore` and `PlayerPrefsSessionStore`.

`ISessionLoginService<TLoginRequest>` and `ISessionRefreshService` are application-specific backend adapters. Session does not assume one login request shape or backend response shape.

`SessionResult` wraps successful session operations or a `SessionError` with a stable code, message, and optional exception.

## Public API map

- `ISessionService`: login, logout, restore, refresh, current session, state checks, and `SessionChanged`.
- `SessionService`: default service implementation.
- `SessionData`: access token, refresh token, expiry, and expiry helper methods.
- `SessionState`: `Unauthenticated`, `Authenticated`, or `Expired`.
- `SessionChangedEventArgs` and `SessionChangeReason`: change event details.
- `ISessionStore`: persistence contract.
- `InMemorySessionStore`: memory-only store for tests, tools, or temporary sessions.
- `PlayerPrefsSessionStore`: simple PlayerPrefs-backed store.
- `ISessionLoginService<TLoginRequest>`: backend login adapter.
- `ISessionRefreshService`: backend refresh adapter.
- `SessionRefreshFailurePolicy`: preserve or clear the current session after failed refresh.
- `SessionResult` and `SessionError`: operation result and failure details.

Standalone workflow:

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using Deucarian.Session;

public sealed class LoginRequest
{
    public string Username;
    public string Password;
}

public sealed class MyLoginService : ISessionLoginService<LoginRequest>
{
    public async Task<SessionResult> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        await Task.Yield();

        return SessionResult.Success(
            new SessionData(
                accessToken: "access-token",
                refreshToken: "refresh-token",
                expiresAtUtc: DateTimeOffset.UtcNow.AddMinutes(30)));
    }
}
```

Create and use a session service:

```csharp
var store = new PlayerPrefsSessionStore("my-game.session");
var refreshService = new MyRefreshService();
var sessionService = new SessionService(
    store,
    refreshService,
    expiryLeeway: TimeSpan.FromMinutes(2),
    refreshFailurePolicy: SessionRefreshFailurePolicy.PreserveSession);

sessionService.SessionChanged += (sender, args) =>
{
    SessionLog.General.Info(args.Reason + ": " + args.PreviousState + " -> " + args.CurrentState);
};

await sessionService.RestoreAsync();

if (!sessionService.IsAuthenticated)
{
    await sessionService.LoginAsync(new LoginRequest(), new MyLoginService());
}

if (sessionService.IsAccessTokenExpiringSoon)
{
    await sessionService.RefreshAsync();
}
```

`IsAuthenticated` is true only when an access token exists and is not expired. A session without an expiry time is treated as authenticated until it is explicitly cleared or replaced.

## Refresh Failure Policy

`SessionRefreshFailurePolicy.PreserveSession` keeps the current session if refresh fails. This is useful when the current token may still be accepted.

`SessionRefreshFailurePolicy.ClearSession` clears the store and in-memory state if refresh fails. This is useful when a failed refresh means the user must authenticate again.

## Storage

Use `InMemorySessionStore` for tests, tools, or sessions that should disappear when the app closes.

Use `PlayerPrefsSessionStore` for simple local persistence:

```csharp
var store = new PlayerPrefsSessionStore("com.example.game.session");
```

For production apps, consider implementing `ISessionStore` with platform-secure storage if tokens need stronger protection than PlayerPrefs.

## Samples

The package contains one sample entry.

`Basic Session Usage`:

- Path: `Samples~/BasicUsage`
- Scene: `BasicUsage.unity`
- Scripts: `SessionSampleController`, `FakeSessionLoginService`, `FakeSessionRefreshService`, and `FakeLoginRequest`

Open the scene and enter Play Mode. The sample uses IMGUI buttons for fake login, restore, refresh, logout, and clearing the persisted sample store.

## Integration Packages

Session core is standalone and does not include API assemblies or integration code.

API support lives in a separate integration package:

```text
com.deucarian.session.api-integration
```

Install that package when a Unity project needs to pass Session tokens to API. No scripting define symbol is needed in this core package.

## Limitations

- Session does not include backend HTTP calls. Implement login and refresh services for your backend.
- `PlayerPrefsSessionStore` is convenient but not secure token storage. Use a custom `ISessionStore` for stronger protection.
- The package does not run an automatic background refresh loop.
- The package does not include runtime UI beyond samples.
- API integration code is not part of this package. Use `com.deucarian.session.api-integration` for that adapter.

## Troubleshooting

- If `IsAuthenticated` is false, check whether the access token is missing or expired.
- If refresh fails, confirm the selected `SessionRefreshFailurePolicy` matches the app's expected behavior.
- If token storage needs platform security, implement `ISessionStore` instead of relying on PlayerPrefs.

## Validation

Run the shared package validator from the repository root:

```powershell
python C:/Repositories/Package-Registry/Tools/deucarian_package_validator.py --registry-root C:/Repositories/Package-Registry --repository-root . --config deucarian-package.json
```

Run the package's EditMode tests in Unity after code or assembly definition changes.

Documentation-only updates should still pass:

```powershell
git diff --check
```

## Architecture / Contributor Notes

- [AGENTS.md](AGENTS.md) contains repository-specific ownership and Codex guidance.
- Deucarian architecture rules live in [Package Registry](https://github.com/Deucarian/Package-Registry/blob/develop/ARCHITECTURE.md).
- Capability ownership is tracked in [CAPABILITY_OWNERSHIP.md](https://github.com/Deucarian/Package-Registry/blob/develop/CAPABILITY_OWNERSHIP.md).

## License

See [LICENSE.md](LICENSE.md).
