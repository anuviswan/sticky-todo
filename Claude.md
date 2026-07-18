# Sticky TODO

The purpose of Sticky TODO is to combine the functionality of TODO apps and Sticky notes. It provides user to create TODOs, which can be viewed as sticky notes on a Windows 11. User can create TODOS via Windows 11 Sticky notes or using the Android App.

## Project Architecture

The solution would consist of

- Backend API
- Windows Widget Desktop App
- Android App

The solution would be an offline-first app, which would allow users to use app even if there is no internet. The synchornization would happen in the background whenever the connectivity is possible.

Core
├── Domain
├── Sync Engine
├── API Client

Windows (WPF)
├── Main App
├── Desktop Widgets
├── System Tray
├── Global Hotkeys

Android (MAUI)
├── Mobile UI
├── Notifications
├── Home Screen Widgets

ASP.NET Core Backend
├── Authentication
├── Sync
├── Push Notifications

---

## Tech Stack

### Backend

- ASP.Net Core
- Azure Tables (Azurite emulator for local development)
- .Net 10 or higher
- C# 14
- MSTest

### Mobile

- .Net MAUI
- SQL Lite

### Windows Widget

- WPF
- SQL Lite
- C# 14

---

## Definition of Done

A feature is complete when:

- Always create a feature branch for new ticket
- implementation is complete
- Solution can be build without errors or warnings
- tests pass
- public APIs are documented
- architecture remains consistent
- no unnecessary coupling is introduced

## Communication

When completing a task:

- Summarize the changes made.
- Mention assumptions.
- Highlight potential risks.
- Call out any breaking changes.
- Suggest improvements separately from requested work.

If requirements are unclear:

- Ask concise questions.
- Do not invent requirements.
- Do not guess business rules.

---

## Expectations

Every implementation should strive to be:

- Correct
- Readable
- Maintainable
- Testable
- Secure
- Performant
- Consistent
- Production-ready

The highest priority is maintaining consistency with the existing codebase. Prefer established project conventions over introducing new patterns.
