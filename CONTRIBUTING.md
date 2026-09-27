# Contributing to Snap

Snap is intentionally structured so that people and coding agents can inspect, modify, test, and redistribute it.

## Getting started

Requirements:

- Windows 10 or 11
- .NET 10 SDK
- PowerShell 7 for the packaging script

Clone the repository and run:

```powershell
dotnet build Snap.slnx
dotnet test tests/Snap.Core.Tests/Snap.Core.Tests.csproj
```

From an interactive Windows desktop session, also run the WPF smoke check:

```powershell
dotnet run --project tests/Snap.App.SmokeTests/Snap.App.SmokeTests.csproj -c Release
```

Build the self-contained installer with:

```powershell
pwsh -File scripts/build.ps1 -SmokeTest
```

The final installer and checksum are written to `artifacts/release/`.

## Architecture

- `src/Snap.App`: WPF interface, screen capture, system tray integration, global hotkeys, annotations, and local capture storage.
- `src/Snap.Core`: UI-independent geometry, settings, hotkey models, retention rules, and undo history.
- `tests/Snap.Core.Tests`: unit tests for UI-independent behavior.
- `tests/Snap.App.SmokeTests`: focused WPF event-routing and layout checks.
- `packaging/Snap.Setup`: self-contained per-user installer and uninstaller.
- `scripts/build.ps1`: reproducible test, publish, and packaging pipeline.

Keep new business logic in `Snap.Core` where practical so it can be tested without opening a desktop window. Keep Windows and WPF integration in `Snap.App`.

## Good extension points

- scrolling screenshots
- image export and configurable save destinations
- more annotation types and shape resizing
- OCR and text extraction
- color picker and measurement tools
- localization and additional interface languages
- improved mixed-DPI and multi-monitor behavior
- configurable history policies and search
- portable builds and additional Windows architectures

## Pull requests

Describe the user-visible behavior, include focused tests for reusable logic, and state which Windows display configurations were manually checked. Do not commit `bin`, `obj`, `artifacts`, packaged runtimes, or generated installers.

By contributing, you agree that your contribution is provided under the repository's MIT License.
