# Deucarian Session

## Overview

Deucarian Session is a standalone Unity UPM package for authenticated session lifecycle management.

It stores the current session, restores saved sessions on app start, supports backend-specific login and refresh flows through small interfaces, and notifies listeners when session data or calculated session state changes.

Package ID: `com.deucarian.session`

## Installation

Install the package through Unity Package Manager with a Git URL:

```json
{
  "dependencies": {
    "com.deucarian.session": "https://github.com/Deucarian/Session.git#main"
  }
}
```

For development builds, use:

```json
"com.deucarian.session": "https://github.com/Deucarian/Session.git#develop"
```

The package requires Unity `2021.3` or newer and depends on `com.unity.modules.jsonserialize`.

## Core Concepts

`SessionService` coordinates restore, login, refresh, logout, state calculation, persistence, and change events.

`SessionData` is immutable token data: access token, optional refresh token, and optional UTC access-token expiry.

`ISessionStore` abstracts persistence. The package includes `InMemorySessionStore` and `PlayerPrefsSessionStore`.

`ISessionLoginService<TLoginRequest>` and `ISessionRefreshService` are application-specific backend adapters. Session does not assume one login request shape or backend response shape.

`SessionResult` wraps successful session operations or a `SessionError` with a stable code, message, and optional exception.

## Public API

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
    UnityEngine.Debug.Log(args.Reason + ": " + args.PreviousState + " -> " + args.CurrentState);
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

## Bridge Packages

Session core is standalone and does not include API assemblies or bridge code.

API support lives in a separate bridge package:

```text
com.deucarian.session.api-bridge
```

Install that package when a Unity project needs to pass Session tokens to API. No scripting define symbol is needed in this core package.

## Versioning

Current package version: `1.0.0`.

Branch strategy:

- `main`: stable package branch.
- `develop`: development package branch.

Use branch refs for active development and stable release tags when tags are available.

## Limitations

- Session does not include backend HTTP calls. Implement login and refresh services for your backend.
- `PlayerPrefsSessionStore` is convenient but not secure token storage. Use a custom `ISessionStore` for stronger protection.
- The package does not run an automatic background refresh loop.
- The package does not include runtime UI beyond samples.
- API bridge code is not part of this package. Use `com.deucarian.session.api-bridge` for that adapter.
