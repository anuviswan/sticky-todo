# StickyDo

StickyDo combines the functionality of a TODO app with the familiarity of Windows Sticky Notes. Notes are created and edited as tasks, and rendered as sticky notes on the desktop.

The app is designed to be offline-first, syncing in the background whenever connectivity is available. (Phase 2)

## Project Architecture

The solution is organized as a multi-app system sharing a common domain layer:

```
StickyDo.Domain          Shared domain models, persistence, and sync engine
StickyDo.Widget           Windows desktop app (WPF)
├── Main App
├── Desktop Widgets
├── System Tray
└── Global Hotkeys
StickyDo.Api               Backend API (Phase 2)
├── Authentication
├── Sync
└── Push Notifications
StickyDo.Android           Android App (Phase 3)
```

> **Status:** `StickyDo.Domain` and `StickyDo.Widget` are under active development. `StickyDo.Api` and the Android app have not been started yet.

## Tech Stack

### Domain / Shared

- .NET 10, C# 14
- File-based persistence (see [`src/StickyDo.Domain/Repositories`](src/StickyDo.Domain/Repositories))
- MSTest for unit tests

### Windows Widget

- WPF, .NET 10 (Windows)
- CommunityToolkit.Mvvm
- Microsoft.Extensions.DependencyInjection

### Backend (planned)

- ASP.NET Core, .NET 10
- Azure Tables (Azurite emulator for local development)
- MSTest

### Mobile (planned)

- .NET MAUI
- SQLite

## Repository Structure

```
src/
├── StickyDo.Domain/            Domain models, services, repositories, sync engine
├── StickyDo.Domain.Tests/      Unit tests for the domain layer (MSTest)
└── StickyDo.Widget/
    ├── StickyDo.Widget/            WPF application (views, view models, services)
    ├── StickyDo.Widget.Controls/   Shared WPF user controls
    └── StickyDo.Widget.Package/    MSIX packaging project (Store submission)
```

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) or later
- Windows 11 (required to build/run `StickyDo.Widget`)

### Build

```bash
dotnet build src/StickyDo.Domain/StickyDo.Domain.csproj
dotnet build src/StickyDo.Widget/StickyDo.Widget.sln
```

### Run the Windows Widget app

```bash
dotnet run --project src/StickyDo.Widget/StickyDo.Widget/StickyDo.Widget.csproj
```

### Run tests

```bash
dotnet test src/StickyDo.Domain.Tests/StickyDo.Domain.Tests.csproj
```

## MSIX Packaging

`StickyDo.Widget.Package` wraps `StickyDo.Widget` in an MSIX package for Microsoft Store
submission. It's a classic Windows Application Packaging Project (`.wapproj`), not an SDK-style
project, so it can't be built with `dotnet build`/`dotnet restore` — those commands (and the
`PR Validation` CI workflow) only see `StickyDo.Widget.sln`, which intentionally does **not**
reference the packaging project. Building it requires full MSBuild from a Visual Studio
installation with the **Universal Windows Platform development** workload (specifically the
"MSIX Packaging Tools"/"Windows Application Packaging Project" component).

The `Build MSIX Package` CI workflow (`.github/workflows/msix-build.yml`) builds an **unsigned**
Release MSIX on every push/PR to `main` (and on demand via `workflow_dispatch`), uploading it as a
build artifact for testing. It skips signing entirely (`AppxPackageSigningEnabled=false`) since
Microsoft Partner Center signs the package at Store submission time — no certificate is needed in
CI.

### Prerequisites

- Visual Studio 2022+ with the **Universal Windows Platform development** workload installed
- A code-signing certificate. For local sideload testing, generate a throwaway self-signed one
  (never commit it — `*.pfx` is gitignored):

  ```powershell
  $cert = New-SelfSignedCertificate -Type Custom -Subject "CN=DefineStack" -KeyUsage DigitalSignature `
    -FriendlyName "StickyDo Dev Certificate" -CertStoreLocation "Cert:\CurrentUser\My" `
    -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3", "2.5.29.19={text}Subject Type:End Entity")
  Export-PfxCertificate -Cert $cert -FilePath src/StickyDo.Widget/StickyDo.Widget.Package/StickyDo.Widget.Package_TemporaryKey.pfx `
    -Password (New-Object System.Security.SecureString)
  ```

  Update `PackageCertificateThumbprint` in the `.wapproj` to match, or open the project in Visual
  Studio and use **Package.appxmanifest → Packaging → Choose Certificate → Create...** instead.

### Build the package

```powershell
$msbuild = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" -latest -requires Microsoft.Component.MSBuild -find "MSBuild\**\Bin\MSBuild.exe"
& $msbuild src/StickyDo.Widget/StickyDo.Widget.Package/StickyDo.Widget.Package.wapproj -t:Restore,Build -p:Configuration=Release -p:Platform=x64
```

The `.msix` is written to `src/StickyDo.Widget/StickyDo.Widget.Package/AppPackages/`.

### Install locally

AppX/MSIX deployment validates the package's signing certificate against the **Local Machine**
`TrustedPeople` store, not `Cert:\CurrentUser\Root` — trusting it there (e.g. via
`Import-PfxCertificate -CertStoreLocation Cert:\CurrentUser\Root`) leaves **Install** disabled in
App Installer and `Add-AppxPackage` failing with `0x800B0109`/`0x800B010A`. From an elevated
(Administrator) PowerShell:

```powershell
certutil.exe -f -p "" -addstore TrustedPeople src/StickyDo.Widget/StickyDo.Widget.Package/StickyDo.Widget.Package_TemporaryKey.pfx
Add-AppxPackage -Path <path-to-generated>.msix
```

This still triggers an interactive Windows confirmation dialog by design — there's no way around
that step, and there shouldn't be, since it's you granting trust to a certificate. Alternatively,
right-click the project in Visual Studio and choose **Deploy**, which handles certificate trust for
you, or run the VS-generated `Add-AppDevPackage.ps1` (found alongside a `.msix` produced via
**Create App Packages...**), which self-elevates and trusts the cert the same way.

Only `x64` is currently configured. `x86`/`arm64` and the final Store-quality icon set
(`Square44x44Logo`, `Square150x150Logo`, `Wide310x150Logo`, splash screen at all required scales)
are tracked separately.

> **Note:** `Configuration=Debug` produces an installable sideload/test package and is what's
> verified above. `Configuration=Release` additionally requires the standalone Windows 10/11 SDK
> (not just the VS packaging workload) to be installed, for its `Platforms\UAP\...\Platform.xml`;
> without it, Release fails with `APPX3217: SDK folder containing 'UAP.props' ... cannot be
> located`. Install the Windows SDK via the Visual Studio Installer's Individual Components tab
> before producing a Release/Store package.

## Privacy

StickyDo is fully local and offline in this version — no telemetry, no accounts, no cloud
sync. See [PRIVACY_POLICY.md](PRIVACY_POLICY.md) for details on what data is stored and where.

## Terms of Service

See [TERMS_OF_SERVICE.md](TERMS_OF_SERVICE.md) for the terms governing use of the app.

## License

StickyDo is licensed under the MIT License. See [LICENSE.md](LICENSE.md) for details.

Copyright © 2026 DefineStack.

Please note that the **StickyDo** name, logo, icons, artwork, screenshots, and other branding assets are the intellectual property of **DefineStack** and are **not** covered by the MIT License. These assets may not be used, copied, or redistributed without prior written permission.
