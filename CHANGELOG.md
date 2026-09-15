# Changelog

## [1.1.0] - 2026-09-11

- Add typed reusable definition authoring and/or scoped Inspector components that share the existing C# service behavior.
- Include a playable Definition Workflow sample with configured hosts, short callers and usage documentation.
- Align declared package dependencies with the definition-authoring development wave.


## 1.0.7 - Unreleased

- Prevent stale login/refresh/restore work from restoring a logged-out or superseded session. Coalesce refreshes with independent waiter cancellation and serialized storage ownership.

## 1.0.6 - 2026-07-24

- Added safe runtime access-token replacement for externally managed sessions while preserving refresh tokens and publishing a dedicated change reason.
- Centralized bearer-token syntax validation in `SessionData`.

## 1.0.5 - 2026-07-17

- Documented the Basic Usage sample and aligned the exact Logging dependency for the portfolio release.

## 1.0.4 - 2026-06-22

- Updated the exact `com.deucarian.logging` dependency to `1.0.1`.

## 1.0.3 - 2026-06-22

- Accepted the public release automation state for `com.deucarian.session` 1.0.3 on develop.

## 1.0.2 - 2026-06-17

- Renamed package ecosystem references from API bridge to API integration.

## Unreleased

- Improved README structure and clarified public APIs, samples, API integration support, versioning, and limitations.
- Moved API support to the separate `com.deucarian.session.api-integration` integration package.

## 1.0.1 - 2026-06-15

- Standardized package logging on com.deucarian.logging.
- Added `SessionLog` package categories for session, authentication, storage, and sample diagnostics.

## 1.0.0

- Initial package scaffold.
- Added session state, result, error, store, login, refresh, and service APIs.
- Added in-memory and PlayerPrefs-backed stores.
- Added editor tests and sample scripts.
