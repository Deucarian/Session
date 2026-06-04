# Contributing

Thanks for helping improve Session Helper.

## Development Guidelines

- Keep the runtime API small and backend-agnostic.
- Do not add UI dependencies to the core runtime assembly.
- Do not make a static singleton the only usage path.
- Keep persistence, login, and refresh behavior behind interfaces.
- Add XML documentation for public runtime APIs.
- Add editor tests for behavior changes.

## Testing

Run the package editor tests from Unity's Test Runner.

APIHelper integration tests require APIHelper to be installed and the `SESSION_HELPER_APIHELPER` scripting define symbol to be enabled.
