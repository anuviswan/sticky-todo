# Sticky TODO

Sticky TODO combines the functionality of a TODO app with the familiarity of Windows Sticky Notes. Notes are created and edited as tasks, and rendered as sticky notes on the desktop.

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
    └── StickyDo.Widget.Controls/   Shared WPF user controls
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
