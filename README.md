# Session Helper

## Overview

Session Helper is a standalone Unity UPM package for authenticated session lifecycle management.

It stores the current session, restores saved sessions on app start, supports backend-specific login and refresh flows through small interfaces, and notifies listeners when session data or calculated session state changes.

Package ID: `com.jorishoef.session-helper`

## Installation

Install the package through Unity Package Manager with a Git URL:

```json
{
  "dependencies": {
    "com.jorishoef.session-helper": "https://github.com/JorisHoef/Session-Helper.git#main"
  }
}
```

For development builds, use:

```json
"com.jorishoef.session-helper": "https://github.com/JorisHoef/Session-Helper.git#develop"
```

The package requires Unity `2021.3` or newer and depends on `com.unity.modules.jsonserialize`.

## Core Concepts

`SessionService` coordinates restore, login, refresh, logout, state calculation, persistence, and change events.

`SessionData` is immutable token data: access token, optional refresh token, and optional UTC access-token expiry.

`ISessionStore` abstracts persistence. The package includes `InMemorySessionStore` and `PlayerPrefsSessionStore`.

`ISessionLoginService<TLoginRequest>` and `ISessionRefreshService` are application-specific backend adapters. Session Helper does not assume one login request shape or backend response shape.

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
- `SessionAuthProvider`: optional APIHelper adapter in the `SessionHelper.APIHelper` assembly.

Standalone workflow:

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using JorisHoef.SessionHelper;

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

## Samples

The package contains two sample entries.

`Basic Session Usage`:

- Path: `Samples~/BasicUsage`
- Scene: `BasicUsage.unity`
- Scripts: `SessionHelperSampleController`, `FakeSessionLoginService`, `FakeSessionRefreshService`, and `FakeLoginRequest`

Open the scene and enter Play Mode. The sample uses IMGUI buttons for fake login, restore, refresh, logout, and clearing the persisted sample store.

`APIHelper Integration`:

- Path: `Samples~/APIHelperIntegration`
- Script: `ApiHelperSessionAdapterSample`

This sample shows how to configure APIHelper with a Session Helper-backed auth provider. It compiles only when APIHelper is installed and `SESSION_HELPER_APIHELPER` is enabled.

## Integrations

Session Helper has one optional integration: APIHelper authentication.

The integration assembly is `SessionHelper.APIHelper`. It references `SessionHelper` and `APIHelper`, and has this define constraint:

```text
SESSION_HELPER_APIHELPER
```

Enable the integration by:

1. Installing APIHelper in the same Unity project.
2. Adding `SESSION_HELPER_APIHELPER` to scripting define symbols.
3. Referencing `SessionHelper.APIHelper` from your own asmdef if your code uses asmdefs.

Usage:

```csharp
using JorisHoef.APIHelper.Configuration;
using JorisHoef.APIHelper.Core;
using JorisHoef.SessionHelper;
using JorisHoef.SessionHelper.APIHelper;

ISessionService sessionService = new SessionService(
    new PlayerPrefsSessionStore("my-game.session"),
    new MyRefreshService());

var authProvider = new SessionAuthProvider(sessionService);
IApiClient apiClient = ApiClientFactory.Create(apiClientConfig, authProvider);
```

`SessionAuthProvider` returns `null` when there is no authenticated session. By default, it tries to refresh when the current access token is expired or expiring soon.

## Versioning

Current package version: `1.0.0`.

Branch strategy:

- `main`: stable package branch.
- `develop`: development package branch.

Use branch refs for active development and stable release tags when tags are available.

## Limitations

- Session Helper does not include backend HTTP calls. Implement login and refresh services for your backend.
- `PlayerPrefsSessionStore` is convenient but not secure token storage. Use a custom `ISessionStore` for stronger protection.
- The package does not run an automatic background refresh loop.
- The package does not include runtime UI beyond samples.
- APIHelper integration is optional and compile-gated; the base `SessionHelper` assembly remains standalone.
