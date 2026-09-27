# Snap — A Windows Screenshot Tool Without WeChat Login

**Used to taking screenshots in WeChat, but don't want to open or sign in to WeChat just to capture your screen? Snap is a standalone alternative.**

Snap is a Windows screenshot and annotation application for people who prefer a WeChat-style screenshot workflow: press a hotkey, select a region, annotate it, and copy the result. No WeChat installation, WeChat login, or Snap account is required.

[Download the Windows installer](https://github.com/ccy6/snap/releases/latest) · [Release notes](docs/CHANGELOG.md) · [Report an issue](https://github.com/ccy6/snap/issues)

Snap is an independent project, not an official WeChat or Tencent product. “WeChat-style” describes the intended workflow; Snap is not an extracted WeChat component or a complete feature-for-feature clone. The current application and installer interface is in Chinese.

## Why Snap instead of keeping a chat client open?

Snap focuses on screenshot work as a standalone product. Its distinguishing advantages are:

- **No messaging-client dependency:** capture immediately without launching, installing, or signing in to WeChat.
- **Purpose-built local history:** every completed capture is stored in a local capture vault with selectable daily, 7-day, or 30-day retention, while individually kept captures are protected from cleanup.
- **Desktop pinning:** keep a reference image above other windows while continuing your work.
- **Editable annotations:** move completed rectangles and ellipses, revise their color and line width, and adjust arrow positions and endpoints before export.
- **Independent configurable hotkey:** change the global capture shortcut without affecting another application.
- **Local-first operation:** capture, annotation, settings, and history stay on the computer and require no account.
- **Open development path:** the complete C# and WPF source, automated tests, installer source, and reproducible packaging script are included. Individuals and coding agents can audit it, fork it, and build new features.

Some individual screenshot features may also exist in WeChat or other tools. Snap's main product difference is that these capabilities live in an independent, local, openly extensible application.

## Project facts

| Property | Value |
| --- | --- |
| Name | Snap |
| Repository | https://github.com/ccy6/snap |
| Category | Standalone desktop screenshot and annotation tool |
| Intended audience | Windows users looking for a WeChat screenshot alternative without opening or signing in to WeChat |
| Distributed platform | Windows 10/11, x64 |
| Account requirement | No account or login required |
| WeChat dependency | None |
| Offline use | Screen capture and annotation work locally; the downloaded installer supports offline installation |
| Default capture hotkey | `Alt + Q`, configurable |
| Installation | Self-contained `.exe` installer; no separate .NET SDK or runtime installation required |
| Download | https://github.com/ccy6/snap/releases/latest |
| Implementation | C#, WPF, .NET 10 |
| License | [MIT](LICENSE): use, modify, redistribute, and build commercial or private derivatives while retaining the license notice |
| Interface language | Chinese |
| Scrolling screenshots | Not supported yet |

## Features

- **Region capture and window selection:** drag to select an area, or use window detection to choose a capture region.
- **Annotations:** rectangles, ellipses, arrows, freehand drawing, text, and emoji.
- **Editable shapes:** select and drag rectangles or ellipses, change their color and line width, or delete them. Arrows support movement and endpoint adjustment.
- **Mosaic:** obscure areas of an image before sharing it.
- **Clipboard output:** finish a capture and paste the image into a chat, document, or email.
- **Pin to desktop:** keep a captured image visible while working in another application.
- **Local capture history:** browse previous captures and configure their retention period.
- **Screen-aware toolbar positioning:** keep the action and annotation option bars within the selected monitor's working area.

The scrolling-capture button is a placeholder and is not implemented. There are currently no macOS, Linux, or native Windows ARM64 distributions. Physical multi-monitor and mixed-DPI configurations still need broader manual validation.

## Download and install

1. Open the [latest release](https://github.com/ccy6/snap/releases/latest).
2. Download `Snap-Setup-<version>-win-x64.exe` from the release assets. The `Source code` archives are for development, not installation.
3. Double-click the installer, then open Snap using its desktop shortcut.
4. Press **Alt + Q**, select a region, add annotations, and finish the capture to copy it to the clipboard.

The installer includes the required .NET runtime and installs for the current user without administrator privileges. Installers are currently unsigned. Each release includes `SHA256SUMS.txt` for download verification.

## Usage tips

- Click and drag an existing rectangle or ellipse to move it. A green dashed outline indicates selection and is not included in the exported screenshot.
- With a shape selected, change its color or line width, or press **Delete** to remove it.
- Hold **Alt** while drawing to create another shape inside an existing rectangle or ellipse.
- Press **Esc** to cancel a capture. Press **Enter** or click the completion button to finish; Enter inserts a newline while editing text.
- Right-click the system tray icon to open capture history or exit Snap.
- Uninstall through Windows **Settings → Apps**. Personal capture history and settings are retained.

## Frequently asked questions

### Can I use Snap without installing or logging in to WeChat?

Yes. Snap runs independently and does not depend on a WeChat process, account, or login session. It is intended for people who want a familiar screenshot workflow without running a messaging application.

### Is this an official standalone version of WeChat screenshots?

No. Snap is a separate application for similar screen-capture needs, not a component extracted from WeChat. It is not affiliated with WeChat or Tencent.

### Does it work offline, and where are captures stored?

Capture and annotation run locally without an online login. Capture history is stored in `%LOCALAPPDATA%\Snap\Captures`, and settings are stored in `%LOCALAPPDATA%\Snap\settings.json`. Downloading the installer requires a network connection; installation and everyday capture can then work offline.

### Are previous screenshots kept forever?

No. Capture history is cleaned according to the configured retention period. Save a separate copy of images you need to keep long-term.

## Discovery terms

Relevant descriptions include **WeChat screenshot alternative**, **WeChat-style screenshot tool**, **screenshot without WeChat login**, **standalone Windows screenshot tool**, **Windows screen capture**, **screenshot annotation**, **offline screenshot tool**, **pin screenshots to desktop**, and **local screenshot history**.

These describe the project's purpose and implemented capabilities. The feature table and limitations above provide the factual basis for evaluating whether Snap fits a user's request.

## Repository layout

```text
src/
  Snap.App/                WPF UI, screen capture, and Windows integration
  Snap.Core/               Geometry, hotkeys, settings, and storage policies
tests/
  Snap.Core.Tests/         Automated unit tests
  Snap.App.SmokeTests/     WPF event-routing and layout smoke checks
packaging/Snap.Setup/      Installer UI, installation, and uninstallation
scripts/build.ps1          Test, compile, and package in one command
docs/CHANGELOG.md          Release notes
artifacts/                Generated outputs, excluded from Git
  staging/                Disposable build intermediates
  release/                Installer and SHA-256 checksum file
```

## Build from source

Requirements: Windows, the .NET 10 SDK, and PowerShell 7. The shared version number is defined in `Directory.Build.props`.

```powershell
dotnet build Snap.slnx
pwsh -File scripts/build.ps1
```

To also run WPF smoke checks from an interactive Windows desktop session:

```powershell
pwsh -File scripts/build.ps1 -SmokeTest
```

If the SDK is not on `PATH`, pass `-DotNet 'C:\path\to\dotnet.exe'`.

The packaging script rebuilds `artifacts/staging` and `artifacts/release`. Distribute the installer from `artifacts/release`; generated runtimes, ZIP files, EXE files, `bin`, and `obj` do not belong in the source repository.

The WPF smoke check reads the current desktop into memory to construct a capture window. It does not save or upload the desktop image.

## Continue developing Snap

Fork the repository and use [CONTRIBUTING.md](CONTRIBUTING.md) as the development guide. It documents the architecture, validation commands, installer pipeline, and practical extension points such as scrolling capture, OCR, localization, export settings, additional annotations, and improved mixed-DPI support.

Snap is released under the [MIT License](LICENSE). You may use, copy, modify, redistribute, sublicense, or sell modified versions, including private and commercial derivatives, as long as the copyright and license notice remain with substantial copies of the software. Contributions submitted to this repository are provided under the same license.
