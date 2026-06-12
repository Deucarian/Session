# Session

Session is a standalone Unity UPM package for session lifecycle management. It stores the current session, restores saved sessions on app start, supports backend-specific login and refresh flows through small interfaces, and notifies listeners when the session changes.

Package name: `com.deucarian.session`

## What It Provides

- `ISessionService` for login, logout, restore, refresh, and state checks.
- `ISessionStore` for persistence.
- `ISessionLoginService<TLoginRequest>` for backend-specific login.
- `ISessionRefreshService` for backend-specific refresh.
- `SessionData` with access token, optional refresh token, and optional expiry time.
- `InMemorySessionStore` for tests and temporary sessions.
- `PlayerPrefsSessionStore` for simple local persistence.
- Optional `SessionAuthProvider` adapter for API's `IApiAuthProvider`.

The package does not include UI, does not require a Unity scene, and does not assume one backend response shape.

## Standalone Usage

Create your backend-specific login service by implementing `ISessionLoginService<TLoginRequest>`:

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
    public async Task<SessionResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        // Call your backend here and map its response into SessionData.
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
    UnityEngine.Debug.Log($"Session changed: {args.PreviousState} -> {args.CurrentState}");
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

## API Integration

Session can integrate with API without modifying API. The optional adapter implements API's `IApiAuthProvider` and returns the current Session access token. API adds the `Authorization: Bearer` header itself.

Because Session must remain standalone, the API adapter is compiled only when you opt in:

1. Install API in the same Unity project.
2. Add `DEUCARIAN_SESSION_API` to the project's scripting define symbols.
3. Reference `Deucarian.Session.API` from your own asmdef if your code uses asmdefs.

Example:

```csharp
using Deucarian.API.Services;
using Deucarian.Session;
using Deucarian.Session.API;

ISessionService sessionService = new SessionService(
    new PlayerPrefsSessionStore("my-game.session"),
    new MyRefreshService());

var authProvider = new SessionAuthProvider(sessionService);
ApiServices.Configure(config, authProvider);
```

The adapter returns `null` when the session is unauthenticated or expired and cannot be refreshed, so API will omit the bearer token.

## Samples

The `Basic Session Usage` sample contains fake login, refresh, restore, and logout scripts that can be dropped into any scene or called from code.

The `API Integration` sample is gated by `DEUCARIAN_SESSION_API` and demonstrates constructing the adapter for API.

## Tests

Editor tests cover login, logout, restore, expiry detection, refresh, refresh failure policy, session change events, and API adapter behavior.

API adapter tests are also gated by `DEUCARIAN_SESSION_API` because they intentionally compile against API's `IApiAuthProvider`.
